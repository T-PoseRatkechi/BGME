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
        scanner.AddMainModuleScan("0F B7 4C 02 16", (result) =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find encounter bgm pattern.");
            }

            var address = Utilities.BaseAddress + result.Offset;
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
                "mov r9, 0x1465B0855",
                "jmp r9",
                "label original",
                "mov rax, r9",
            };

            this.encounterBgmHook = hooks.CreateAsmHook(encounterPatch, address).Activate()
                ?? throw new Exception("Failed to create encounter bgm hook.");
        });

        scanner.AddMainModuleScan("F6 40 0C 40 0F 84 ?? 00 00 00", result =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find encounter context pattern.");
            }

            var address = Utilities.BaseAddress + result.Offset;
            var encounterContextPatch = new string[]
            {
                "use64",
                "movzx r12, byte [rax + 0x1a]",
            };

            this.encounterContextHook = hooks.CreateAsmHook(
                encounterContextPatch,
                address,
                AsmHookBehaviour.ExecuteFirst)
            .Activate() ?? throw new Exception("Failed to create encounter context hook.");
        });

        scanner.AddMainModuleScan("E8 53 A5 22 FA", result =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find victory bgm pattern.");
            }

            var address = Utilities.BaseAddress + result.Offset;
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
                address,
                AsmHookBehaviour.ExecuteFirst)
            .Activate() ?? throw new Exception("Failed to create victory bgm hook.");
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
