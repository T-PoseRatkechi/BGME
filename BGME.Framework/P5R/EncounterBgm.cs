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
    private readonly EffectsHook effectsHook;

    public EncounterBgm(
        IP5RLib p5rLib,
        MusicService music,
        CriAtomEx criAtomEx,
        BgmPlayback bgm,
        EffectsHook effectsHook)
        : base(music)
    {
        this.p5rLib = p5rLib;
        this.criAtomEx = criAtomEx;
        this.bgm = bgm;
        this.beatHitEffect = new(p5rLib, this.timingWindows, effectsHook);
        this.effectsHook = effectsHook;

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
            this.nextHitActivation = 0;
            Log.Information("Stopped Battle Rhythm Game");
        });
    }

    private uint initialPlaybackId;
    private uint? battleBgmPlaybackId;

    private Conductor conductor = new();

    private HashSet<int> beatsPlayed = new();
    private int? currentEffect;

    private float nextHitActivation = 0;
    private readonly HitTimingWindows timingWindows = new();

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
        this.beatHitEffect.Update();

        // Visual window logic.
        var currentWholeBeat = (int)Math.Round(this.conductor.SongPositionInBeats);
        if (!this.beatsPlayed.Contains(currentWholeBeat) && currentWholeBeat % 2 == 0)
        {
            this.beatsPlayed.Add(currentWholeBeat);

            Log.Information($"Visual Beat: {this.conductor.SongPositionInSeconds}");
            Log.Information($"Next Hit Beat: {this.conductor.SongPositionInSeconds}");
            this.p5rLib.FlowCaller.FLD_DASH_EFFECT(1);
        }
        else
        {
            this.p5rLib.FlowCaller.FLD_DASH_EFFECT(0);
        }

        // Hit window logic.
        if (this.conductor.SongPositionInSeconds >= this.nextHitActivation)
        {
            Log.Debug($"Current Hit: {this.nextHitActivation}");
            this.nextHitActivation = this.beatHitEffect.Activate(conductor);
        }
    }
}

internal class Conductor
{
    public float SongBpm { get; set; }

    public float SecPerBeat { get; set; }

    public float SongPositionInSeconds { get; set; }

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
        this.SongPositionInSeconds = (float)TimeSpan.FromMilliseconds(songPosMs).TotalSeconds;
        this.SongPositionInBeats = this.SongPositionInSeconds / this.SecPerBeat;
    }
}

internal unsafe class BeatHitEffect : IGameHook
{
    [Function(Register.rbx, Register.rax, true)]
    private delegate int SetPostDamageHp(int originalHp);
    private IReverseWrapper<SetPostDamageHp>? setHpWrapper;
    private IAsmHook? setHpHook;

    private readonly IP5RLib p5rLib;
    private readonly EffectsHook effectsHook;
    private readonly HitTimingWindows hitWindows;
    private readonly bool* effectEnabled;
    private int? currentEffect;
    private bool successEffect = false;

    private readonly BattleHooks battle = new();
    private Conductor? conductor;

    private float lastHitTime;

    public BeatHitEffect(IP5RLib p5rLib, HitTimingWindows hitWindows, EffectsHook effectsHook)
    {
        this.p5rLib = p5rLib;
        this.effectsHook = effectsHook;
        this.hitWindows = hitWindows;
        this.effectEnabled = (bool*)Marshal.AllocHGlobal(sizeof(bool));
        *this.effectEnabled = false;

        this.battle.ParticipantActed += (arg1, arg2) => this.OnParticipantActed((nint*)arg1, (void*)arg2);
    }

    private void OnParticipantActed(nint* participantPtr, void* param2)
    {
        // Act event can fire before the hit window is set.
        // Save last hit time then check if success in update.
        this.lastHitTime = this.conductor?.SongPositionInSeconds ?? 0;
        Log.Information($"Hit Time: {lastHitTime} || {(nint)participantPtr:X} / {*participantPtr:X} || {(nint)param2:X}");
    }

    private float hitTimeActual;

    public float Activate(Conductor conductor)
    {
        this.conductor = conductor;
        this.hitTimeActual = (float)(conductor.SongPositionInSeconds + this.hitWindows.GoodWindow.TotalSeconds);

        var currentWholeBeat = Math.Round(conductor.SongPositionInBeats);
        var nextActivation = (float)((currentWholeBeat + 2) * this.conductor.SecPerBeat) - this.hitWindows.GoodWindow.TotalSeconds;
        return (float)nextActivation;
    }

    public void Deactivate()
    {
        *this.effectEnabled = false;
    }

    public void Update()
    {
        if (this.lastHitTime != 0 && this.IsHitSuccessful(this.hitWindows.GoodWindow))
        {
            this.lastHitTime = 0;
            this.p5rLib.FlowCaller.SET_COUNT(500, 1);
            // this.p5rLib.FlowCaller.FLD_EFFECT_BANK_FREE(1);
            //if (this.IsHitSuccessful(this.hitWindows.PerfectWindow))
            //{
            //    Log.Information("Perfect!");
            //    this.p5rLib.FlowCaller.FLD_EFFECT_BANK_LOAD(1, 51);
            //}
            //else if (this.IsHitSuccessful(this.hitWindows.GreatWindow))
            //{
            //    Log.Information("Great!");
            //    this.p5rLib.FlowCaller.FLD_EFFECT_BANK_LOAD(1, 55);
            //}
            //else
            //{
            //    Log.Information("Good!");
            //    this.p5rLib.FlowCaller.FLD_EFFECT_BANK_LOAD(1, 54);
            //}

            //this.p5rLib.FlowCaller.FLD_EFFECT_BANK_SYNC(1);
            //this.currentEffect = this.p5rLib.FlowCaller.FLD_EFFECT_BANK_START(1);
            //this.successEffect = true;
            //var x = this.p5rLib.FlowCaller.FLD_CAMERA_GET_X_POS();
            //var y = this.p5rLib.FlowCaller.FLD_CAMERA_GET_Y_POS();
            //var z = this.p5rLib.FlowCaller.FLD_CAMERA_GET_Z_POS();
            //this.p5rLib.FlowCaller.FLD_EFFECT_SET_POS((int)this.currentEffect, x, y, z);

            //var bcd = this.p5rLib.FlowCaller.BTL_CUTSCENE_LOAD(779, 1);
            //this.p5rLib.FlowCaller.BTL_CUTSCENE_LOADSYNC(bcd);
            //this.p5rLib.FlowCaller.BTL_CUTSCENE_PLAY(bcd, 1, 0, 2, 0);
            //this.p5rLib.FlowCaller.BTL_CUTSCENE_END(bcd);
            //var bcd = this.p5rLib.FlowCaller.SCRIPT_READ(1, 2, 3);
            //this.p5rLib.FlowCaller.SCRIPT_READ_SYNC(bcd);
            //this.p5rLib.FlowCaller.SCRIPT_EXEC(bcd, 0);
            //this.p5rLib.FlowCaller.SCRIPT_FREE(bcd);
        }
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        this.battle.Initialize(scanner, hooks);

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

    private int SetPostDamageHpImpl(int originalHp)
    {
        return 0;
        return originalHp;
    }

    private bool IsHitSuccessful(TimeSpan hitWindow)
    {
        var min = this.hitTimeActual - hitWindow.TotalSeconds;
        var max = this.hitTimeActual + hitWindow.TotalSeconds;

        if (this.lastHitTime >= min && this.lastHitTime <= max)
        {
            return true;
        }

        return false;
    }
}

internal class HitTimingWindows
{
    public TimeSpan GoodWindow { get; } = TimeSpan.FromMilliseconds(142);

    public TimeSpan GreatWindow { get; } = TimeSpan.FromMilliseconds(92);

    public TimeSpan PerfectWindow { get; } = TimeSpan.FromMilliseconds(33);
}