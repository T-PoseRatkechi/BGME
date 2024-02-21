using BGME.Framework.Models;
using BGME.Framework.Music;
using Project.Utils;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P3R.P3R;

internal unsafe class EncounterBgm : BaseEncounterBgm, IGameHook
{
    [Function(new Register[] { Register.rdi, Register.r8 }, Register.rax, true)]
    private delegate ushort GetEncounterBgm(UBtlCoreComponent* encounter, EncounterStage stage);
    private IReverseWrapper<GetEncounterBgm>? encounterBgmWrapper;
    private IAsmHook? encounterBgmHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void UBtlCoreComponent_FadeoutBGM(UBtlCoreComponent* btlCore, uint param2);
    private IHook<UBtlCoreComponent_FadeoutBGM>? fadeoutBgmHook;

    public EncounterBgm(MusicService music)
        : base(music)
    {
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan("Encounter BGM Hook", "3B 8F ?? ?? ?? ?? 74 ?? 89 8F", result =>
        {
            var patch = new string[]
            {
                "use64",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(this.GetEncounterBgmImpl, out this.encounterBgmWrapper),
                Utilities.PopCallerRegisters,
                "test ax, ax",
                "jz original",
                "movzx ecx, ax",
                "original:"
            };

            this.encounterBgmHook = hooks.CreateAsmHook(patch, result).Activate();
        });

        scanner.Scan(
            nameof(UBtlCoreComponent_FadeoutBGM),
            "40 53 48 83 EC 20 48 8B D9 8B CA E8 ?? ?? ?? ?? C7 83 ?? ?? ?? ?? 00 00 00 00"
            , result =>
            {
                this.fadeoutBgmHook = hooks.CreateHook<UBtlCoreComponent_FadeoutBGM>(this.FadeoutBGM_Impl, result).Activate();
            });
    }

    private void FadeoutBGM_Impl(UBtlCoreComponent* btlCore, uint param2)
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

    private ushort GetEncounterBgmImpl(UBtlCoreComponent* encounter, EncounterStage stage)
        => stage switch
        {
            EncounterStage.Victory => this.GetVictoryBgm(),
            _ => this.GetBattleBgm(encounter),
        };

    private ushort GetVictoryBgm()
    {
        var victoryMusicId = this.GetVictoryMusic();
        if (victoryMusicId == -1)
        {
            return 0;
        }

        Log.Debug($"Victory BGM ID: {victoryMusicId}");
        return (ushort)victoryMusicId;
    }

    private ushort GetBattleBgm(UBtlCoreComponent* encounter)
    {
        var id = encounter->Id;
        var context = encounter->Context;

        // P3R swaps encounter context values.
        if (context == EncounterContext.Advantage)
        {
            context = EncounterContext.Disadvantage;
        }
        else if (context == EncounterContext.Disadvantage)
        {
            context = EncounterContext.Advantage;
        }

        var battleMusicId = this.GetBattleMusic((int)id, context);
        if (battleMusicId == -1)
        {
            return 0;
        }

        Log.Debug($"Battle BGM ID: {battleMusicId}");
        return (ushort)battleMusicId;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct UBtlCoreComponent
    {
        [FieldOffset(0x298)]
        public uint Id;

        [FieldOffset(0x29c)]
        public EncounterContext Context;

        [FieldOffset(0x46c)]
        public uint CurrentBgm;

        [FieldOffset(0x03D0)]
        public ABtlPhase* CurrentPhase;
    }

    private enum EncounterStage
        : byte
    {
        Fighting,
        Victory,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x280)]
    public unsafe struct ABtlPhase
    {
    }
}
