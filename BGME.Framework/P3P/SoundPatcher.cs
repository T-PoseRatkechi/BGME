using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using System.Text;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P3P;

/// <summary>
/// Patch BGM function to play any BGM audio file.
/// </summary>
#pragma warning disable IDE0052 // Remove unread private members
internal unsafe class SoundPatcher : BaseSound
{
    private const int MAX_STRING_SIZE = 16;

    [Function(Register.rax, Register.rax, true)]
    private delegate byte* GetBgmString(int bgmId);
    private IReverseWrapper<GetBgmString>? bgmReverseWrapper;
    private IAsmHook? bgmHook;
    private IAsmHook? fixBgmCrashHook;

    private readonly byte* bgmStringBuffer;

    public SoundPatcher(
        IReloadedHooks hooks,
        IStartupScanner scanner,
        MusicService music)
        : base(music)
    {
        this.bgmStringBuffer = (byte*)NativeMemory.AllocZeroed(MAX_STRING_SIZE, sizeof(byte));

        scanner.Scan("BGM Patch", "4E 8B 84 C3 28 4C 81 00", address =>
        {
            var bgmPatch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.GetBgmStringImpl, out this.bgmReverseWrapper)}",
                $"{Utilities.PopCallerRegisters}",
                $"mov r8, rax",
            };

            this.bgmHook = hooks.CreateAsmHook(bgmPatch, (long)address, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        scanner.Scan(
            "Fix BGM Crashes",
            "8B 0D ?? ?? ?? ?? BB 01 00 00 00 89 DA E8 ?? ?? ?? ?? 66 44 8B 05",
            result =>
            {
                this.fixBgmCrashHook = hooks.CreateAsmHook(new[] { "use64", "xor ecx, ecx" }, result + 0x7C, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
            });
    }

    protected override void PlayBgm(int bgmId)
    {
        Log.Debug("Play BGM not supported.");
    }

    private byte* GetBgmStringImpl(int bgmId)
    {
        var currentBgmId = this.GetGlobalBgmId(bgmId);
        if (currentBgmId == null)
        {
            Log.Warning("Music disabling not supported.");
            currentBgmId = 1;
        }

        var bgmString = string.Format("{0:00}.ADX\0", (int)currentBgmId);
        if (bgmString.Length > MAX_STRING_SIZE)
        {
            bgmString = "01.ADX\0";
            Log.Error($"BGM value too large. Value: {bgmId}");
        }

        if (bgmId < 1)
        {
            bgmString = "01.ADX\0";
            Log.Error("Negative BGM value, previous file probably does not exist.");
        }

        var bgmStringBytes = Encoding.ASCII.GetBytes(bgmString);

        var handle = GCHandle.Alloc(bgmStringBytes, GCHandleType.Pinned);
        NativeMemory.Copy((void*)handle.AddrOfPinnedObject(), this.bgmStringBuffer, (nuint)bgmStringBytes.Length);
        handle.Free();

        Log.Debug($"Playing BGM: {bgmString.Trim('\0')}");
        return this.bgmStringBuffer;
    }
}
