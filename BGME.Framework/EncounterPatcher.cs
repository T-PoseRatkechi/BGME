using BGME.Framework.Models;
using BGME.Framework.Music;
using PersonaMusicScript.Library.Models;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Serilog;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework;

internal unsafe partial class EncounterPatcher
{
    [Function(new[] { Register.r8, Register.rcx }, Register.rax, true)]
    private delegate int GetEncounterBgm(nint encounterPtr, int encounterId);
    private IReverseWrapper<GetEncounterBgm>? encounterReverseWrapper;
    private IAsmHook? encounterBgmHook;

    [Function(new[] { Register.rdx }, Register.rax, true)]
    private delegate int GetVictoryBgm(int defaultMusicId);
    private IReverseWrapper<GetVictoryBgm>? victoryReverseWrapper;
    private IAsmHook? victoryBgmHook;

    private readonly MusicService music;
    private EncounterMusic? currentEncounterMusic;

    public EncounterPatcher(
        IReloadedHooks hooks,
        IStartupScanner scanner,
        MusicService music)
    {
        this.music = music;

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
        if (this.currentEncounterMusic?.Encounter.VictoryMusic != null)
        {
            Log.Debug("Victory Music uses BGME");
            var musicId = Utilities.CalculateMusicId(this.currentEncounterMusic.Encounter.VictoryMusic, this.currentEncounterMusic.Context);
            this.currentEncounterMusic = null;
            return musicId;
        }

        return defaultMusicId;
    }

    private int GetEncounterBgmImpl(nint encounterPtr, int encounterId)
    {
        Log.Debug("Encounter: {id}", encounterId);

        var context = (EncounterContext) (*(ushort*)(encounterPtr + 0x1e));
        Log.Debug("Context: {context}", context);

        if (this.music.Encounters.TryGetValue(encounterId, out var encounter))
        {
            Log.Debug("Encounter uses BGME");
            this.currentEncounterMusic = new(encounter, context);
            if (encounter.BattleMusic != null)
            {
                Log.Debug("Battle Music uses BGME");
                var musicValue = Utilities.CalculateMusicId(encounter.BattleMusic, context);
                return musicValue;
            }
        }

        return -1;
    }

    private record EncounterMusic(Encounter Encounter, EncounterContext Context);
}
