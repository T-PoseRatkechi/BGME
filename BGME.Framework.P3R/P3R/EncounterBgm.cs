using BGME.Framework.Models;
using BGME.Framework.Music;
using Project.Utils;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;

namespace BGME.Framework.P3R.P3R;

internal unsafe class EncounterBgm : BaseEncounterBgm, IGameHook
{
    [Function(CallingConventions.Microsoft)]
    private delegate void UBtlCoreComponent_RequestBGM(UBtlCoreComponent* btlCore, EBtlPhaseType phase);
    private IHook<UBtlCoreComponent_RequestBGM>? requestBgmHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void UBtlCoreComponent_FadeoutBGM(UBtlCoreComponent* btlCore, uint param2);
    private IHook<UBtlCoreComponent_FadeoutBGM>? fadeoutBgmHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void USoundApp_PlayBattleBGM(int cueId);
    private USoundApp_PlayBattleBGM? playBattleBgm;

    public EncounterBgm(MusicService music)
        : base(music)
    {
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan(
            nameof(UBtlCoreComponent_RequestBGM),
            "48 89 5C 24 ?? 57 48 83 EC 20 0F B6 DA 48 8B F9 E8 ?? ?? ?? ?? 84 DB 74 ?? 83 FB 01",
            result => this.requestBgmHook = hooks.CreateHook<UBtlCoreComponent_RequestBGM>(this.RequestBGM, result).Activate());

        scanner.Scan(
            nameof(UBtlCoreComponent_FadeoutBGM),
            "40 53 48 83 EC 20 48 8B D9 8B CA E8 ?? ?? ?? ?? C7 83 ?? ?? ?? ?? 00 00 00 00",
            result => this.fadeoutBgmHook = hooks.CreateHook<UBtlCoreComponent_FadeoutBGM>(this.FadeoutBGM, result).Activate());

        scanner.Scan(
            nameof(USoundApp_PlayBattleBGM),
            "89 4C 24 ?? 48 83 EC 38 8B 4C 24 ?? E8 ?? ?? ?? ?? 89 44 24",
            result => this.playBattleBgm = hooks.CreateWrapper<USoundApp_PlayBattleBGM>(result, out _));
    }

    private EncounterMusicP3R? currentEncounter;

    private void RequestBGM(UBtlCoreComponent* btlCore, EBtlPhaseType phase)
    {
        // Reset encounter music on new encounter.
        if (this.currentEncounter != null && this.currentEncounter.Id != btlCore->EncountParameter.EncountID)
        {
            this.currentEncounter = null;
        }

        var currentBgmId = -1;

        // Initial battle phase (probably).
        if (phase == EBtlPhaseType.None || phase != EBtlPhaseType.Fighting)
        {
            var encounterId = btlCore->EncountParameter.EncountID;
            var preemptive = btlCore->EncountParameter.Preemptive;
            var context = EncounterContext.Normal;

            // P3R swaps encounter context values.
            if (preemptive == EBtlEncountPreemptive.Ally)
            {
                context = EncounterContext.Advantage;
            }
            else if (preemptive == EBtlEncountPreemptive.Enemy)
            {
                context = EncounterContext.Disadvantage;
            }

            var battleBgmId = this.GetBattleMusic(encounterId, context);
            var victoryBgmId = this.GetVictoryMusic();

            // Save battle and victory music for future calls;
            this.currentEncounter = new(encounterId, battleBgmId, victoryBgmId);
            currentBgmId = this.currentEncounter.BattleBgmId;
        }

        else if (this.currentEncounter != null)
        {
            currentBgmId = this.currentEncounter.VictoryBgmId;
        }

        if (currentBgmId == -1)
        {
            this.requestBgmHook!.OriginalFunction(btlCore, phase);
            return;
        }

        Log.Debug($"{nameof(UBtlCoreComponent_RequestBGM)} || Phase: {phase} || BGM ID: {currentBgmId}");
        if (btlCore->CurrentCueId != currentBgmId)
        {
            btlCore->CurrentCueId = (uint)currentBgmId;
            this.playBattleBgm!(currentBgmId);
        }
    }

    private void FadeoutBGM(UBtlCoreComponent* btlCore, uint param2)
    {
        // param2 = 10 when fading out back to overworld music.
        // Maybe a fade out duration?

        // Block normal BGM fadeout until battle is over
        // to fix BGM muting after starting.
        // TODO: Maybe hook RequestBGM? It's weird that it's an issue
        // but maybe Ryo redirecting adds some unexpected delay?
        if (btlCore->CurrentPhase != null)
        {
            Log.Debug($"{nameof(UBtlCoreComponent_FadeoutBGM)} || {param2} || Blocked.");
        }
        else
        {
            this.fadeoutBgmHook!.OriginalFunction(btlCore, param2);
        }
    }

    private record EncounterMusicP3R(int Id, int BattleBgmId, int VictoryBgmId);

    [StructLayout(LayoutKind.Explicit)]
    private struct UBtlCoreComponent
    {
        [FieldOffset(0x298)]
        public FBtlEncountParam EncountParameter;

        [FieldOffset(0x3D8)]
        public ABtlPhase* CurrentPhase;

        [FieldOffset(0x474)]
        public uint CurrentCueId;
    }

    private enum EBtlPhaseType : byte
    {
        None = 0,
        Fighting = 1,
        Victory = 2,
        Annihilation = 3,
        Escape = 4,
        EscapeSkill = 5,
        Others = 6,
        EBtlPhaseType_MAX = 7,
    };
    
    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    private unsafe struct FBtlEncountParam
    {
        [FieldOffset(0x0000)] public int EncountID;
        [FieldOffset(0x0004)] public EBtlEncountPreemptive Preemptive;
        [FieldOffset(0x0005)] public EBtlEncountPreemptive PreemptiveOriginal;
        [FieldOffset(0x0010)] public int StageMajor;
        [FieldOffset(0x0014)] public int StageMinor;
        [FieldOffset(0x0018)] public int EnemyBadStatus;
        [FieldOffset(0x001C)] public bool CalledFromScript;
        [FieldOffset(0x001D)] public bool IsEventResult;
    }

    private enum EBtlEncountPreemptive : byte
    {
        Normal = 0,
        Enemy = 1,
        Ally = 2,
        MAX = 3,
    };

    [StructLayout(LayoutKind.Explicit, Size = 0x280)]
    private unsafe struct ABtlPhase
    {
        [FieldOffset(0x0278)]
        public EBtlPhaseType Type;
    }
}
