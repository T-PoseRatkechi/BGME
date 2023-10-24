using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Serilog;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework;

internal class FloorPatcher
{
    [Function(Register.rdi, Register.rax, true)]
    private delegate int GetFloorBgm(int floorId);
    private IReverseWrapper<GetFloorBgm>? floorReverseWrapper;
    private IAsmHook? floorBgmHook;

    private readonly MusicService music;

    public FloorPatcher(
        IReloadedHooks hooks,
        IStartupScanner scanner,
        MusicService music)
    {
        this.music = music;
        scanner.AddMainModuleScan("83 FF 05 0F 8E C1 00 00 00", (result) =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find encounter bgm pattern.");
            }

            var offset = result.Offset;
            var encounterPatch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.GetFloorBgmImpl, out this.floorReverseWrapper)}",
                $"{Utilities.PopCallerRegisters}",
                "cmp eax, -1",
                "jng original",
                "mov ebx, eax",
                "mov r9, 0x14031525D",
                "jmp r9",
                "label original",
            };

            this.floorBgmHook = hooks.CreateAsmHook(encounterPatch, Utilities.BaseAddress + offset, AsmHookBehaviour.ExecuteFirst).Activate()
                ?? throw new Exception("Failed to create floor bgm hook.");
        });
    }

    private int GetFloorBgmImpl(int floorId)
    {
        Log.Debug("Floor: {id}", floorId);
        if (this.music.Floors.TryGetValue(floorId, out var floorMusic))
        {
            Log.Debug("Floor uses BGME");
            return Utilities.CalculateMusicId(floorMusic);
        }

        return -1;
    }
}
