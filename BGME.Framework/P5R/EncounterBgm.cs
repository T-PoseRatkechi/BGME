using BGME.Framework.CRI;
using BGME.Framework.Models;
using BGME.Framework.Music;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P5R;

internal unsafe class EncounterBgm : BaseEncounterBgm, IGameHook
{
    [Function(Register.rbx, Register.rax, true)]
    private delegate void GetEncounterBgmId(nint encounterPtr);
    private IReverseWrapper<GetEncounterBgmId>? getEncounterBgmWrapper;
    private IAsmHook? getEncounterBgmHook;

    [Function(Register.rdx, Register.rax, true)]
    private delegate int GetVictoryBgmFunction(int defaultBgmId);
    private IReverseWrapper<GetVictoryBgmFunction>? victoryBgmWrapper;

    private BeatHitEffect beatHitEffect;
    private IAsmHook? victoryBgmHook;
    private IAsmHook? victoryBgmHook2;

    private readonly IP5RLib p5rLib;
    private readonly CriAtomEx criAtomEx;
    private readonly BgmPlayback bgm;

    public EncounterBgm(IP5RLib p5rLib, MusicService music, CriAtomEx criAtomEx, BgmPlayback bgm)
        : base(music)
    {
        this.p5rLib = p5rLib;
        this.criAtomEx = criAtomEx;
        this.bgm = bgm;
        this.beatHitEffect = new(p5rLib, 0.35f);
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        this.beatHitEffect.Initialize(scanner, hooks);
        var victoryBgmCall = hooks.Utilities.GetAbsoluteCallMnemonics(this.GetVictoryBgm, out this.victoryBgmWrapper);
        scanner.Scan("Encounter BGM", "8B 83 ?? ?? ?? ?? 3D 81 02 00 00", result =>
        {
            var patch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.GetEncounterBgmIdImpl, out this.getEncounterBgmWrapper)}",
                $"{Utilities.PopCallerRegisters}",
            };


            this.getEncounterBgmHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        scanner.Scan("Victory BGM", "BA 54 01 00 00 48 8B CE E8 ?? ?? ?? ?? 45 33 C9", result =>
        {
            var patch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{victoryBgmCall}",
                $"{Utilities.PopCallerRegisters}",
                "cmp eax, -1",
                "jng original",
                "mov edx, eax",
                "label original",
            };

            this.victoryBgmHook = hooks.CreateAsmHook(patch, result + 5, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        scanner.Scan("Victory BGM on Victory", "BA 54 01 00 00 48 8B CE E8 ?? ?? ?? ?? 33 D2", result =>
        {
            var patch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{victoryBgmCall}",
                $"{Utilities.PopCallerRegisters}",
                "cmp eax, -1",
                "jng original",
                "mov edx, eax",
                "label original",
            };

            this.victoryBgmHook2 = hooks.CreateAsmHook(patch, result + 5, AsmHookBehaviour.ExecuteFirst).Activate();
        });
    }

    private int GetVictoryBgm(int defaultBgmId)
    {
        var victoryMusicId = this.GetVictoryMusic();
        if (victoryMusicId != -1)
        {
            return victoryMusicId;
        }

        return defaultBgmId;
    }

    private void GetEncounterBgmIdImpl(nint encounterPtr)
    {
        this.StartBattleBgm();
        var encounterId = (int*)(encounterPtr + 0x278);
        var context = (EncounterContext) (*(int*)(encounterPtr + 0x28c));

        // P5R swaps encounter context values.
        if (context == EncounterContext.Advantage)
        {
            context = EncounterContext.Disadvantage;
        }
        else if (context == EncounterContext.Disadvantage)
        {
            context = EncounterContext.Advantage;
        }

        var battleMusicId = this.GetBattleMusic(*encounterId, context);
        if (battleMusicId == -1)
        {
            return;
        }

        // Write bgm id to encounter bgm var.
        var encounterBgmPtr = (int*)(encounterPtr + 0x9ac);
        *encounterBgmPtr = battleMusicId;

        Log.Debug($"Encounter BGM ID written: {battleMusicId}");
    }

    private Task? rhythmGameLoop;
    private CancellationTokenSource rhythmGameLoopToken;

    private void StartBattleBgm()
    {
        Log.Information("Starting Battle Rhythm Game");
        this.initialPlaybackId = this.criAtomEx.Player_GetLastPlaybackId(this.bgm.BgmPlayer.PlayerHn);
        Log.Information($"Initial BGM: {initialPlaybackId}");
        this.battleBgmPlaybackId = null;

        this.rhythmGameLoopToken = new();
        this.rhythmGameLoop = Task.Run(() =>
        {
            var lastUpdate = DateTime.Now;
            while (true)
            {
                var current = DateTime.Now;
                var elapsedMs = (current - lastUpdate).TotalMilliseconds;
                if (elapsedMs > 1)
                {
                    this.UpdateRhythmGame();
                    lastUpdate = DateTime.Now;
                }
            }
        }, this.rhythmGameLoopToken.Token);

        this.rhythmGameLoopToken.Token.Register(() =>
        {
            this.initialPlaybackId = 0;
            this.battleBgmPlaybackId = null;
            this.beatsPlayed.Clear();
            this.beatHitEffect.Deactivate();
            this.p5rLib.FlowCaller.FLD_DASH_EFFECT(0);
            Log.Information("Stopped Battle Rhythm Game");
        });
    }

    private uint initialPlaybackId;
    private uint? battleBgmPlaybackId;

    private Conductor conductor = new();

    private HashSet<int> beatsPlayed = new();
    private int? currentEffect;

    private void UpdateRhythmGame()
    {
        var currentPlaybackId = this.criAtomEx.Player_GetLastPlaybackId(this.bgm.BgmPlayer.PlayerHn);

        // Wait for BGM to change.
        if (this.initialPlaybackId == currentPlaybackId)
        {
            return;
        }

        // Set battle BGM playback ID.
        else if (this.battleBgmPlaybackId == null)
        {
            Log.Information($"Battle BGM: {currentPlaybackId}");
            this.battleBgmPlaybackId = currentPlaybackId;
            this.conductor = new();
            this.conductor.Start(125);
        }

        // End battle loop when BGM ends.
        else if (currentPlaybackId != this.battleBgmPlaybackId)
        {
            Log.Information($"Current BGM: {currentPlaybackId}");
            this.rhythmGameLoopToken.Cancel();
        }

        var currentBgmTime = this.criAtomEx.Playback_GetTimeSyncedWithAudio((uint)this.battleBgmPlaybackId!);
        this.conductor.Update(currentBgmTime);

        var currentWholeBeat = (int)Math.Round(this.conductor.SongPositionInBeats);
        if (!this.beatsPlayed.Contains(currentWholeBeat) && currentWholeBeat % 2 == 0)
        {
            Log.Debug($"Beat: {currentWholeBeat}");
            this.beatsPlayed.Add(currentWholeBeat);
            this.beatHitEffect.Activate(this.conductor.SongPositionInBeats);

            // Always flowscript last, issues with syncronous code?
            this.p5rLib.FlowCaller.FLD_DASH_EFFECT(1);
        }
        else
        {
            this.p5rLib.FlowCaller.FLD_DASH_EFFECT(0);
        }

        this.beatHitEffect.Update(this.conductor.SongPositionInBeats);
    }
}

internal class Conductor
{
    public float SongBpm { get; set; }

    public float SecPerBeat { get; set; }

    public float SongPosition { get; set; }

    public float SongPositionInBeats { get; set; }

    public DateTime DspSongTime { get; set; }

    public void Start(float songBpm)
    {
        this.SongBpm = songBpm;
        this.SecPerBeat = 60f / songBpm;
        this.DspSongTime = DateTime.Now;
    }

    public void Update(int songPosMs)
    {
        this.SongPosition = (float)(this.DspSongTime + TimeSpan.FromMilliseconds(songPosMs) - this.DspSongTime).TotalSeconds;
        this.SongPositionInBeats = this.SongPosition / this.SecPerBeat;
    }
}

internal unsafe class BeatHitEffect : IGameHook
{
    [Function(Register.rbx, Register.rax, true)]
    private delegate int SetPostDamageHp(int originalHp);
    private IReverseWrapper<SetPostDamageHp>? setHpWrapper;
    private IAsmHook? setHpHook;

    private readonly IP5RLib p5rLib;
    private readonly float beatWindow;
    private readonly bool* effectEnabled;
    private int? currentEffect;
    private bool successEffect = false;

    public BeatHitEffect(IP5RLib p5rLib, float beatWindow)
    {
        this.p5rLib = p5rLib;
        this.beatWindow = beatWindow;
        this.effectEnabled = (bool*)Marshal.AllocHGlobal(sizeof(bool));
        *this.effectEnabled = false;
    }

    private float endBeat;
    public void Activate(float currentBeat)
    {
        this.endBeat = currentBeat + this.beatWindow;
        *this.effectEnabled = true;
    }

    public void Deactivate()
    {
        *this.effectEnabled = false;
    }

    public void Update(float currentBeat)
    {
        if (currentBeat > endBeat)
        {
            this.Deactivate();
        }

        if (this.successEffect && this.currentEffect == null)
        {
            this.p5rLib.FlowCaller.FLD_EFFECT_BANK_FREE(1);
            this.p5rLib.FlowCaller.FLD_EFFECT_BANK_LOAD(1, 50);
            this.p5rLib.FlowCaller.FLD_EFFECT_BANK_SYNC(1);
            this.currentEffect = this.p5rLib.FlowCaller.FLD_EFFECT_BANK_START(1);
        }

        if (this.currentEffect != null && currentBeat > endBeat + 0.5f)
        {
            this.p5rLib.FlowCaller.FLD_EFFECT_END((int)this.currentEffect);
            this.currentEffect = null;
            this.successEffect = false;
        }
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan("Set Post-Damage HP Hook", "89 5F ?? 41 F7 85 ?? ?? ?? ?? 00 80 00 00", result =>
        {
            var patch = new string[]
            {
                "use64",
                $"mov rax, {(nint)this.effectEnabled}",
                "mov al, [rax]",
                "test al, al",
                "jz original",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(this.SetPostDamageHpImpl, out this.setHpWrapper),
                Utilities.PopCallerRegisters,
                "mov ebx, eax",
                "original:"
            };

            this.setHpHook = hooks.CreateAsmHook(patch, result).Activate();
        });
    }

    private string[] messages = new string[]
    {
        "Nice!",
        "Perfect!",
        "Awesome!",
        "Feel the beat!",
        "Flawless!",
    };

    private int SetPostDamageHpImpl(int originalHp)
    {
        Log.Information(this.messages[Random.Shared.Next(0, this.messages.Length)]);
        this.successEffect = true;

        // crashes
        //var current = this.p5rLib.FlowCaller.BTL_GET_CURRENT_CHARAID();

        //var x = this.p5rLib.FlowCaller.FLD_MODEL_GET_X_TRANSLATE(this.p5rLib.FlowCaller.FLD_PC_GET_RESHND(0));
        //var y = this.p5rLib.FlowCaller.FLD_MODEL_GET_Y_TRANSLATE(this.p5rLib.FlowCaller.FLD_PC_GET_RESHND(0));
        //var z = this.p5rLib.FlowCaller.FLD_MODEL_GET_Z_TRANSLATE(this.p5rLib.FlowCaller.FLD_PC_GET_RESHND(0));

        //this.p5rLib.FlowCaller.FLD_EFFECT_BANK_FREE(2);
        //this.p5rLib.FlowCaller.FLD_EFFECT_BANK_LOAD(2, 50);
        //this.p5rLib.FlowCaller.FLD_EFFECT_BANK_SYNC(2);
        //this.currentEffect = this.p5rLib.FlowCaller.FLD_EFFECT_BANK_START(2);
        //this.p5rLib.FlowCaller.FLD_EFFECT_SET_POS((int)this.currentEffect, x, y, z);
        //this.p5rLib.FlowCaller.FLD_EFFECT_SET_SCALE((int)this.currentEffect, 1, 1, 1);
        //this.p5rLib.FlowCaller.FLD_EFFECT_SET_ALPHA((int)this.currentEffect, 1);
        //this.p5rLib.FlowCaller.FLD_EFFECT_SET_SPEED((int)this.currentEffect, 1);

        return 0;
    }
}
