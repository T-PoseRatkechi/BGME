using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;

namespace BGME.Framework.P5R;

#pragma warning disable IDE0052 // Remove unread private members
internal unsafe class DlcBgmHook
{
    private IAsmHook? dlcBgmHook;
    private IAsmHook? dlcBgmBitCheckHook;
    private IAsmHook? persistsDlcBgmHook1;
    private IAsmHook? persistsDlcBgmHook2;

    public DlcBgmHook(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan("DLC BGM Table Hook", "66 39 6F ?? 75 ?? 8B 0F", result =>
        {
            var bgmTable = new DlcBgmTable();
            var ptr = Marshal.AllocHGlobal(sizeof(DlcBgmTable));
            Marshal.StructureToPtr(bgmTable, ptr, false);

            var patch = new string[]
            {
                "use64",
                $"mov r14, {ptr}",
                "mov bp, [rdi - 2]",
            };

            this.dlcBgmHook = hooks.CreateAsmHook(patch, result).Activate();

            var patch2 = new string[]
            {
                "use64",
                "mov rax, 1"
            };

            this.dlcBgmBitCheckHook = hooks.CreateAsmHook(patch2, result + 0xC, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        var persistsDlcBgmPatch = new string[]
        {
            "use64",
            $"stc"
        };

        scanner.Scan("Persist DLC BGM 1", "77 ?? E8 ?? ?? ?? ?? EB ?? B9 06 00 00 00", result =>
        {
            this.persistsDlcBgmHook1 = hooks.CreateAsmHook(persistsDlcBgmPatch, result, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        scanner.Scan("Persist DLC BGM 2", "77 ?? E8 ?? ?? ?? ?? B8 07 00 00 00", result =>
        {
            this.persistsDlcBgmHook2 = hooks.CreateAsmHook(persistsDlcBgmPatch, result, AsmHookBehaviour.ExecuteFirst).Activate();
        });
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DlcBgmTable
    {
        public DlcBgmTable()
        {
            this.bgmId = 42;
            this.outfitId = 1;
            this.flags = 1;
        }

        public ushort bgmId;
        public ushort outfitId;
        public uint flags;
    }
}
