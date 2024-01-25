using BGME.Framework.Models;
using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P4G;

internal unsafe class EncounterBgm : BaseEncounterBgm, IGameHook
{
    [Function(new[] { Register.r8, Register.rcx }, Register.rax, true)]
    private delegate int GetEncounterBgm(nint encounterPtr, int encounterId);
    private IReverseWrapper<GetEncounterBgm>? encounterReverseWrapper;
    private IAsmHook? encounterBgmHook;

    [Function(new[] { Register.rdx }, Register.rax, true)]
    private delegate int GetVictoryBgm(int defaultMusicId);
    private IReverseWrapper<GetVictoryBgm>? victoryReverseWrapper;
    private IAsmHook? victoryBgmHook;

    public EncounterBgm(MusicService music)
        : base(music)
    {
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.AddMainModuleScan("0F B7 4C D0 16", (result) =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find encounter bgm pattern.");
            }

            var offset = result.Offset;
            var encounterPatch = new string[]
            {
                "use64",
                "mov r12, rax",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.GetEncounterBgmImpl, out this.encounterReverseWrapper)}",
                $"{Utilities.PopCallerRegisters}",
                "cmp eax, -1",
                "jng original",
                "mov rdx, rax",
                "mov r9, 0x1400bc6fd",
                "jmp r9",
                "label original",
                "mov rax, r12",
            };

            this.encounterBgmHook = hooks.CreateAsmHook(encounterPatch, Utilities.BaseAddress + offset).Activate()
                ?? throw new Exception("Failed to create encounter bgm hook.");
        });

        scanner.AddMainModuleScan("BA 07 00 00 00 E8 ?? ?? ?? ?? BA 07 00 00 00", (result) =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find victory bgm pattern.");
            }

            var offset = result.Offset + 10;
            var victoryPatch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks?.Utilities.GetAbsoluteCallMnemonics(this.GetVictoryBgmImpl, out this.victoryReverseWrapper)}",
                $"{Utilities.PopCallerRegisters}",
                "mov rdx, rax"
            };

            this.victoryBgmHook = hooks?.CreateAsmHook(victoryPatch, Utilities.BaseAddress + offset, AsmHookBehaviour.ExecuteAfter).Activate()
                ?? throw new Exception("Failed to create victory bgm hook.");
        });
    }

    private int GetVictoryBgmImpl(int defaultMusicId)
    {
        var victoryMusicId = this.GetVictoryMusic();
        if (victoryMusicId != -1)
        {
            return victoryMusicId;
        }

        return defaultMusicId;
    }

    private int GetEncounterBgmImpl(nint encounterPtr, int encounterId)
    {
        var context = (EncounterContext)(*(ushort*)(encounterPtr + 0x1e));
        return this.GetBattleMusic(encounterId, context);
    }
}
