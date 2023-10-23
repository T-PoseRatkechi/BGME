using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Diagnostics;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework;

internal class EncounterPatcher
{
    [Function(new[] { Register.rdx }, Register.rdx, false)]
    private delegate int GetEncounterBgm(int encounterIndex);

    private nint baseAddress = 0;
    private IReverseWrapper<GetEncounterBgm>? reverseWrapper;
    private IAsmHook? encounterBgmHook;

    public EncounterPatcher(IReloadedHooks? hooks, IStartupScanner scanner)
    {
        var proc = Process.GetCurrentProcess();
        this.baseAddress = proc.MainModule!.BaseAddress;

        scanner.AddMainModuleScan("0F B7 4C D0 16", (result) =>
        {
            var address = result.Offset;

            var encounterPatch = new string[]
            {
                "use64",
                $"{hooks?.Utilities.GetAbsoluteCallMnemonics(this.GetEncounterBgmImpl, out this.reverseWrapper)}",
                "mov r9, 0x1400bc6fd",
                "jmp r9"
            };

            this.encounterBgmHook = hooks?.CreateAsmHook(encounterPatch, this.baseAddress + address).Activate()
                ?? throw new Exception("Failed to create encounter bgm hook.");
        });
    }

    private int GetEncounterBgmImpl(int encounterIndex)
    {
        return 775;
    }
}
