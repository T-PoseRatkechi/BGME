using BGME.Framework.Models;
using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P3P;

internal class EncounterBgm : BaseEncounterBgm
{
    [Function(new[] { Register.rcx, Register.r12 }, Register.rax, true)]
    private delegate int GetEncounterBgm(int encounterId, EncounterContext context);
    private IReverseWrapper<GetEncounterBgm>? encounterBgmWrapper;
    private IAsmHook? encounterBgmHook;

    [Function(Register.rcx, Register.rax, true)]
    private delegate int GetVictoryBgmFunction(int defaultBgmId);
    private IReverseWrapper<GetVictoryBgmFunction>? victoryBgmWrapper;
    private IAsmHook? victoryBgmHook;

    private IAsmHook? encounterContextHook;

    public EncounterBgm(
        IReloadedHooks hooks,
        IStartupScanner scanner,
        MusicService music)
        : base(music)
    {
        scanner.Scan("Encounter BGM", "0F B7 4C 02 16", address =>
        {
            if (address == null)
            {
                return;
            }


            var encounterPatch = new string[]
            {
                "use64",
                "mov r9, rax",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.GetBattleMusic, out this.encounterBgmWrapper)}",
                $"{Utilities.PopCallerRegisters}",
                "cmp eax, -1",
                "jng original",
                "mov ecx, eax",
                $"{hooks.Utilities.GetAbsoluteJumpMnemonics((nint)(address + 0x3e), true)}",
                "label original",
                "mov rax, r9",
            };

            this.encounterBgmHook = hooks.CreateAsmHook(encounterPatch, (long)address).Activate();
        });

        scanner.Scan("Encounter Context", "F6 40 0C 40 0F 84 ?? 00 00 00", address =>
        {
            if (address == null)
            {
                return;
            }

            var encounterContextPatch = new string[]
            {
                "use64",
                "movzx r12, byte [rax + 0x1a]",
            };

            this.encounterContextHook = hooks.CreateAsmHook(
                encounterContextPatch,
                (long)address,
                AsmHookBehaviour.ExecuteFirst)
            .Activate();
        });

        scanner.Scan("Victory BGM", "E8 53 A5 22 FA", address =>
        {
            if (address == null)
            {
                return;
            }

            var victoryBgmPatch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.GetVictoryBgm, out this.victoryBgmWrapper)}",
                $"{Utilities.PopCallerRegisters}",
                $"mov rcx, rax",
            };

            this.victoryBgmHook = hooks.CreateAsmHook(
                victoryBgmPatch,
                (long)address,
                AsmHookBehaviour.ExecuteFirst)
            .Activate();
        });
    }

    private int GetVictoryBgm(int defaultMusicId)
    {
        var victoryMusicId = this.GetVictoryMusic();
        if (victoryMusicId != -1)
        {
            return victoryMusicId;
        }

        return defaultMusicId;
    }
}
