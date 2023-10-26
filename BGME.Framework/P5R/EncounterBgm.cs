using BGME.Framework.Models;
using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Serilog;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P5R;

internal unsafe class EncounterBgm : BaseEncounterBgm
{
    [Function(new[] { Register.rbx, Register.rdx }, Register.rax, true)]
    private delegate int GetEncounterBgmId(nint encounterPtr, int encounterId);
    private IReverseWrapper<GetEncounterBgmId>? getEncounterBgmWrapper;
    private IAsmHook? getEncounterBgmHook;

    [Function(Register.rdx, Register.rax, true)]
    private delegate int GetVictoryBgmFunction(int defaultBgmId);
    private IReverseWrapper<GetVictoryBgmFunction>? victoryBgmWrapper;
    private IAsmHook? victoryBgmHook;

    private readonly Sound sound;

    public EncounterBgm(IReloadedHooks hooks, IStartupScanner scanner, Sound sound, MusicService music)
        : base(music)
    {
        this.sound = sound;
        scanner.AddMainModuleScan("E8 33 C4 FF FF", result =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find get encounter bgm pattern.");
            }

            var address = Utilities.BaseAddress + result.Offset;
            var patch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.GetEncounterBgmIdImpl, out this.getEncounterBgmWrapper)}",
                $"{Utilities.PopCallerRegisters}",
                "cmp eax, -1",
                "jng original",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(unchecked((nint) 0x140974A20), true)}",
                $"{hooks.Utilities.GetAbsoluteJumpMnemonics(unchecked((nint) 0x1409786EB), true)}",
                $"label original"
            };

            try
            {
                this.getEncounterBgmHook = hooks.CreateAsmHook(patch, address, AsmHookBehaviour.ExecuteFirst).Activate()
                    ?? throw new Exception("Failed to create get encounter bgm hook.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Join('\n', patch));
            }
        });

        scanner.AddMainModuleScan("48 8B CE E8 8B 23 FF FF", result =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find victory bgm pattern.");
            }

            var address = Utilities.BaseAddress + result.Offset;
            var patch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.GetVictoryBgm, out this.victoryBgmWrapper)}",
                $"{Utilities.PopCallerRegisters}",
                "cmp eax, -1",
                "jng original",
                "mov edx, eax",
                "label original",
            };

            try
            {
                this.victoryBgmHook = hooks.CreateAsmHook(patch, address, AsmHookBehaviour.ExecuteFirst).Activate()
                    ?? throw new Exception("Failed to create victory bgm hook.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Join('\n', patch));
            }
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

    private int GetEncounterBgmIdImpl(nint encounterPtr, int encounterId)
    {
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

        var battleMusicId = this.GetBattleMusic(encounterId, context);

        // Write bgm id to encounter bgm var.
        var encounterBgmPtr = (int*)(encounterPtr + 0x9ac);
        *encounterBgmPtr = battleMusicId;

        Log.Debug("Encounter BGM ID written: {id}", battleMusicId);
        return battleMusicId;
    }
}
