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
        scanner.Scan("Floor BGM", "83 3D ?? ?? ?? ?? 01 8B D8 0F 84 ?? ?? ?? ?? 85 C0 0F 8E", address =>
        {
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
                $"{hooks.Utilities.GetAbsoluteJumpMnemonics(Utilities.BaseAddress + 0x3b5790, true)}",
                "label original",
                "mov rax, r9",
            };

            this.floorBgmHook = hooks.CreateAsmHook(floorBgmPatch, (long)address, AsmHookBehaviour.ExecuteFirst).Activate();
        });
    }
}
