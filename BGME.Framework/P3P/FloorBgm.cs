using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P3P;

internal class FloorBgm : BaseFloorBgm
{
    [Function(Register.rax, Register.rax, true)]
    private delegate int GetFloorBgmFunction(int floorId);
    private IReverseWrapper<GetFloorBgmFunction>? floorBgmWrapper;
    private IAsmHook? floorBgmHook;

    public FloorBgm(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
        : base(music)
    {
        scanner.AddMainModuleScan("83 3D 46 8A 7B 00 01", (result) =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find floor bgm pattern.");
            }

            var address = Utilities.BaseAddress + result.Offset;
            var floorBgmPatch = new string[]
            {
                "use64",
                "mov r9, rax",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.GetFloorBgm, out this.floorBgmWrapper)}",
                $"{Utilities.PopCallerRegisters}",
                "cmp eax, -1",
                "jng original",
                "mov ecx, eax",
                "add rsp, 0x20",
                "pop rbx",
                "mov rax, 0x1403B5790",
                "jmp rax",
                "label original",
                "mov rax, r9",
            };

            this.floorBgmHook = hooks.CreateAsmHook(floorBgmPatch, address, AsmHookBehaviour.ExecuteFirst).Activate()
                ?? throw new Exception("Failed to create encounter bgm hook.");
        });
    }
}
