using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Ryo.Definitions.Structs;
using Ryo.Interfaces;
using System.Runtime.InteropServices;
using System.Text;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P3P;

/// <summary>
/// Patch BGM function to play any BGM audio file.
/// </summary>
internal unsafe class Sound : BaseSound
{
    private const int MAX_STRING_SIZE = 16;

    [Function(Register.rax, Register.rax, true)]
    private delegate byte* GetBgmString(int bgmId);
    private IReverseWrapper<GetBgmString>? bgmReverseWrapper;
    private IAsmHook? bgmHook;
    private IAsmHook? fixBgmCrashHook;

    private delegate void PlayComseOrBse(SE_TYPE seType, int param2, ushort majorId, short minorId);
    private IHook<PlayComseOrBse>? playComseOrBseHook;

    private readonly IRyoApi ryo;
    private readonly ICriAtomEx criAtomEx;
    private readonly ICriAtomRegistry criAtomRegistry;
    private readonly byte* bgmStringBuffer;

    public Sound(
        IReloadedHooks hooks,
        IStartupScanner scanner,
        IRyoApi ryo,
        ICriAtomEx criAtomEx,
        ICriAtomRegistry criAtomRegistry,
        MusicService music)
        : base(music)
    {
        this.ryo = ryo;
        this.criAtomEx = criAtomEx;
        this.criAtomRegistry = criAtomRegistry;
        this.bgmStringBuffer = (byte*)NativeMemory.AllocZeroed(MAX_STRING_SIZE, sizeof(byte));

        scanner.Scan("BGM Patch", "4E 8B 84 ?? ?? ?? ?? ?? E8 ?? ?? ?? ?? 8B 0D", address =>
        {
            var bgmPatch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.GetBgmStringImpl, out this.bgmReverseWrapper)}",
                $"{Utilities.PopCallerRegisters}",
                $"mov r8, rax",
            };

            this.bgmHook = hooks.CreateAsmHook(bgmPatch, address, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        scanner.Scan(
            "Fix BGM Crashes",
            "8B 0D ?? ?? ?? ?? BB 01 00 00 00 89 DA E8 ?? ?? ?? ?? 66 44 8B 05",
            result =>
            {
                this.fixBgmCrashHook = hooks.CreateAsmHook(new[] { "use64", "xor ecx, ecx" }, result + 0x7C, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
            });

        scanner.Scan(nameof(PlayComseOrBse), "40 53 55 56 57 41 54 41 57 48 81 EC B8 00 00 00", result =>
        {
            this.playComseOrBseHook = hooks.CreateHook<PlayComseOrBse>(this.PlayComseOrBseImpl, result).Activate();
        });
    }

    private nint _customSePlayer = IntPtr.Zero;

    private nint CustomeSePlayer
    {
        get
        {
            if (this._customSePlayer == IntPtr.Zero)
            {
                var config = (CriAtomExPlayerConfigTag*)Marshal.AllocHGlobal(Marshal.SizeOf<CriAtomExPlayerConfigTag>());
                config->maxPathStrings = 8;
                config->maxPath = 256;

                this._customSePlayer = this.criAtomEx.Player_Create(config, (void*)0, 0);
            }

            return this._customSePlayer;
        }
    }

    /// <summary>
    /// Play COMSE or BSE SFX.
    /// </summary>
    /// <param name="seType">SE type: 0 = COMSE, 1 = BSE</param>
    /// <param name="param2">Unknown/unused?</param>
    /// <param name="majorId">First two digits of file.</param>
    /// <param name="minorId">Last two digits of file.</param>
    private void PlayComseOrBseImpl(SE_TYPE seType, int param2, ushort majorId, short minorId)
    {
        if (Enum.IsDefined(seType))
        {
            var seFilePath = $"sound/se/{(seType == SE_TYPE.COMSE ? "comse.pak/" : "bse.pak/b")}{majorId:00}{minorId:00}.vag";
            Log.Debug($"{nameof(PlayComseOrBse)}: {seFilePath}");
            if (this.ryo.HasFileContainer(seFilePath))
            {
                //var playerHn = this.criAtomRegistry.GetPlayerById(0)!.Handle;
                this.criAtomEx.Player_SetFile(this.CustomeSePlayer, 0, (byte*)StringsCache.GetStringPtr(seFilePath));
                this.criAtomEx.Player_Start(this.CustomeSePlayer);
            }
            else
            {
                this.playComseOrBseHook!.OriginalFunction(seType, param2, majorId, minorId);
            }
        }
        else
        {
            Log.Debug($"Unknown SE value: {seType}");
            this.playComseOrBseHook!.OriginalFunction(seType, param2, majorId, minorId);
        }
    }

    protected override int VictoryBgmId { get; } = 60;

    protected override void PlayBgm(int bgmId)
    {
        Log.Debug("Play BGM not supported.");
    }

    private byte* GetBgmStringImpl(int bgmId)
    {
        // Handle Mass Destruction being played
        // through ID 2 for some reason.
        int? currentBgmId = bgmId == 2 ? 26 : bgmId;
        currentBgmId = this.GetGlobalBgmId((int)currentBgmId);

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

    private enum SE_TYPE
        : short
    {
        COMSE,
        BSE,
    }
}
