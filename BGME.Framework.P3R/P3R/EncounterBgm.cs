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
    private delegate ushort GetEncounterBgm(Encounter* encounter, EncounterStage stage);
    private IReverseWrapper<GetEncounterBgm>? encounterBgmWrapper;
    private IAsmHook? encounterBgmHook;

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
    }

    private ushort GetEncounterBgmImpl(Encounter* encounter, EncounterStage stage)
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

    private ushort GetBattleBgm(Encounter* encounter)
    {
        var id = encounter->id;
        var context = encounter->context;

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
    private struct Encounter
    {
        [FieldOffset(0x298)]
        public uint id;

        [FieldOffset(0x29c)]
        public EncounterContext context;
    }

    private enum EncounterStage
        : byte
    {
        Battle,
        Victory,
    }
}
