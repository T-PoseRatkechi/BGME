using BGME.Framework.Models;
using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;

namespace BGME.Framework.P5R;

internal unsafe class Sound : BaseSound
{
    // Constants.
    private const int CUSTOM_BGM_ID = 42;
    private const int EXTENDED_BGM_ID = 10000;
    private const int WAVEFORM_ENTRY_SIZE = 2;

    private const int DEN_CUE_ID = 939;
    private const int FALLBACK_CUE_ID = 900;

    // Shell songs.
    private static readonly ShellCue SHELL_SONG_1 = new(200, 0);
    private static readonly ShellCue SHELL_SONG_2 = new(201, 1);

    [Function(CallingConventions.Microsoft)]
    private delegate void PlayBgmFunction(nint param1, nint param2, int bgmId, nint param4);
    private IHook<PlayBgmFunction>? playBgmHook;

    private IAsmHook? customAcbHook;
    private IAsmHook? customAwbHook;
    private IAsmHook? persistentDlcBgmHook;
    private IAsmHook? setCostumeIdHook;

    private ShellCue currentShellSong = SHELL_SONG_1;
    private ushort currentAwbIndex = 0;

    private readonly int* currentCostumeId = (int*)NativeMemory.AllocZeroed(sizeof(int));

    public Sound(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
        : base(music)
    {
        *this.currentCostumeId = 1;
        scanner.Scan("Costume ACB", "E8 35 66 7C EA 44 0F B7 07", result =>
        {
            var patch = new string[]
            {
                "use64",
                $"mov r8, {CUSTOM_BGM_ID}"
            };

            this.customAcbHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        scanner.Scan("Costume AWB", "E8 20 66 7C EA 02 05 E9 FB F4 F5", result =>
        {
            var patch = new string[]
            {
                "use64",
                $"mov r8, {CUSTOM_BGM_ID}"
            };

            this.customAwbHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        scanner.Scan("Persist DLC BGM", "77 07 E8 01 D8 5D 00", result =>
        {
            var patch = new string[]
            {
                "use64",
                $"stc"
            };

            this.persistentDlcBgmHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        scanner.Scan("Set Costume ID", "0F B7 07 48 8B 0D B6 2F 23 ED", result =>
        {
            var patch = new string[]
            {
                "use64",
                $"mov rax, {(nint)this.currentCostumeId}",
                "mov eax, [rax]"
            };

            this.setCostumeIdHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteAfter).Activate();
        });

        scanner.Scan("Play BGM Function", "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 80 7C 24 ?? 00", result =>
        {
            this.playBgmHook = hooks.CreateHook<PlayBgmFunction>(this.PlayBgm, result).Activate();
        });
    }

    protected override void PlayBgm(int bgmId)
    {
        //this.PlayBgm(0, 0, bgmId, 0);
    }

    private void PlayBgm(nint param1, nint param2, int bgmId, nint param4)
    {
        Log.Debug($"{param1:X} || {param2:X} || {bgmId:X} || {param4:X}");
        var currentBgmId = this.GetGlobalBgmId(bgmId);
        if (currentBgmId == null)
        {
            return;
        }

        if (bgmId == SHELL_SONG_1.CueId || bgmId == SHELL_SONG_2.CueId)
        {
            currentBgmId = FALLBACK_CUE_ID;
            Log.Warning("Tried to play a shell song Cue ID. I guess they're not unused...");
        }

        // Use extended BGM.
        if (currentBgmId >= EXTENDED_BGM_ID)
        {
            Log.Debug("Using Extended BGM");
            if (WaveformTableAddress is nint address)
            {
                // AWB index to play.
                var awbIndex = (ushort)(currentBgmId - EXTENDED_BGM_ID);

                // Swap shell cue ID to trigger a song change.
                if (this.currentAwbIndex != awbIndex)
                {
                    this.SwapShellCue();
                    this.currentAwbIndex = awbIndex;
                }

                // Pointer to AWB property of shell cue ID.
                var entryAwbIndexPtr = (ushort*)(address + (WAVEFORM_ENTRY_SIZE * this.currentShellSong.WaveTableIndex));
                Log.Verbose($"Entry AWB Address: {(nint)entryAwbIndexPtr:X}");
                *entryAwbIndexPtr = this.currentAwbIndex.ToBigEndian();

                Log.Debug($"Playing AWB index {this.currentAwbIndex} using Cue ID {this.currentShellSong.CueId}.");
                currentBgmId = this.currentShellSong.CueId;
            }
            else
            {

                Log.Error($"Failed to play extended BGM ID: {currentBgmId}");
                currentBgmId = FALLBACK_CUE_ID;
            }
        }

        Log.Debug($"Playing BGM ID: {currentBgmId}");
        this.playBgmHook?.OriginalFunction(param1, param2, (int)currentBgmId, param4);
    }

    /// <summary>
    /// Gets ACB address of DLC BGM, null if not loaded.
    /// </summary>
    private static nint? AcbAddress
    {
        get
        {
            nint acbAddress;
            if (AcbPointers.AcbAddres_1 is nint address1)
            {
                Log.Verbose("Using ACB pointer 1.");
                acbAddress = address1;
            }
            else if (AcbPointers.AcbAddress_2 is nint address2)
            {
                Log.Verbose("Using ACB pointer 2.");
                acbAddress = address2;
            }
            else
            {
                Log.Verbose("Using ACB pointer 3.");
                acbAddress = AcbPointers.AcbAddress_3;
            }

            Log.Verbose($"ACB Address: {acbAddress:X}");
            return acbAddress;
        }
    }

    /// <summary>
    /// Gets address of waveform table rows.
    /// </summary>
    private static nint? WaveformTableAddress
    {
        get
        {
            if (AcbAddress is nint address)
            {
                var waveformTableAddress = address + 2146;
                Log.Verbose($"Waveform Table Address: {waveformTableAddress:X}");
                return waveformTableAddress;
            }

            return null;
        }
    }

    private void SwapShellCue()
    {
        this.currentShellSong = (this.currentShellSong == SHELL_SONG_1) ? SHELL_SONG_2 : SHELL_SONG_1;
    }
}
