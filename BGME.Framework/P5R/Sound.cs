﻿using BGME.Framework.Models;
using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;
using Timer = System.Timers.Timer;

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

    [Function(new Register[] { Register.rbx, Register.rax }, Register.rax, true)]
    private delegate void AcbLoadedFunction(byte* fileName, nint acbPtr);
    private IReverseWrapper<AcbLoadedFunction>? acbLoadedReverseWrapper;
    private IAsmHook? acbLoadedHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void LoadDlcBgm();
    private LoadDlcBgm? loadDlcBgmWrapper;

    private readonly DlcBgmHook dlcBgmHook;
    private readonly Timer mainMenuTimer = new(100);

    private ShellCue currentShellSong = SHELL_SONG_1;
    private ushort currentAwbIndex = 0;

    private nint? acbAddress;

    public Sound(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
        : base(music)
    {
        this.dlcBgmHook = new(scanner, hooks);

        scanner.Scan("Play BGM Function", "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 80 7C 24 ?? 00", result =>
        {
            this.playBgmHook = hooks.CreateHook<PlayBgmFunction>(this.PlayBgm, result).Activate();
        });

        scanner.Scan("Get Costume ACB Pointer", "48 89 83 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? BA 01 00 00 00", result =>
        {
            var patch = new string[]
            {
                "use64",
                $"{Utilities.PushCallerRegisters}",
                $"{hooks.Utilities.GetAbsoluteCallMnemonics(this.AcbLoaded, out this.acbLoadedReverseWrapper)}",
                $"{Utilities.PopCallerRegisters}",
            };

            this.acbLoadedHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteAfter).Activate();
        });

        scanner.Scan(
            "Load DLC BGM Function",
            "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 81 EC D0 02 00 00 0F B7 2D",
            result =>
            {
                this.loadDlcBgmWrapper = hooks.CreateWrapper<LoadDlcBgm>(result, out _);
            });
    }

    private void AcbLoaded(byte* fileName, nint acbPointer)
    {
        var fileNameString = Marshal.PtrToStringAnsi((nint)fileName + 4);

        if (fileNameString == "SOUND/BGM_42.ACB")
        {
            // Save pointer.
            this.acbAddress = *(nint*)(acbPointer + 0x18);
            Log.Debug($"Costume ACB Pointer: {acbPointer:X}");
            Log.Information($"Costume ACB Address: {this.acbAddress:X}");
        }
        else
        {
            Log.Verbose($"ACB loaded: {fileNameString}");
            Log.Verbose($"ACB Pointer: {acbPointer:X}");
        }
    }

    protected override void PlayBgm(int bgmId)
    {
        //this.PlayBgm(0, 0, bgmId, 0);
    }

    private void PlayBgm(nint param1, nint param2, int bgmId, nint param4)
    {
        // Delay main menu BGM playback until DLC BGM
        // is loaded to support new BGM in main menu.
        if (this.acbAddress == null)
        {
            this.loadDlcBgmWrapper!();
            this.mainMenuTimer.Elapsed += (sender, args) =>
            {
                if (this.acbAddress != null)
                {
                    this.PlayBgm(param1, param2, bgmId, param4);
                    this.mainMenuTimer.Dispose();
                }
            };

            this.mainMenuTimer.Start();
            return;
        }

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
            if (this.WaveformTableAddress is nint address)
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
    /// Gets address of waveform table rows.
    /// </summary>
    private nint? WaveformTableAddress
    {
        get
        {
            if (this.acbAddress is nint address)
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
