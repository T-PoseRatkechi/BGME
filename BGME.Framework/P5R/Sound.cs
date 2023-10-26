using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Serilog;

namespace BGME.Framework.P5R;

internal unsafe class Sound : BaseSound
{
    // Constants.
    private const int CUSTOM_BGM_ID = 42;
    private const int EXTENDED_BGM_ID = 10000;
    private const int WAVEFORM_ENTRY_SIZE = 2;

    private const int NORMAL_CUE_ID = 300;
    private const int NORMAL_WAVEFORM_INDEX = 997;

    private const int VICTORY_CUE_ID = 340;
    private const int VICTORY_WAVEFORM_INDEX = 998;

    private const int ADVANTAGE_CUE_ID = 907;
    private const int ADVANTAGE_WAVEFORM_INDEX = 999;

    private const int FALLBACK_CUE_ID = 900;

    // Shell songs.
    private static readonly ShellCue SHELL_SONG_1 = new(NORMAL_CUE_ID, NORMAL_WAVEFORM_INDEX);
    private static readonly ShellCue SHELL_SONG_2 = new(VICTORY_CUE_ID, VICTORY_WAVEFORM_INDEX);

    private readonly nint* acbPointer;

    [Function(CallingConventions.Microsoft)]
    private delegate void PlayBgmFunction(nint param1, nint param2, nint param3, nint param4);
    private IHook<PlayBgmFunction>? playBgmHook;

    private IAsmHook? customAcbHook;
    private IAsmHook? customAwbHook;
    private IAsmHook? persistentDlcBgmHook;
    private IAsmHook? alwaysLoadDlcBgmHook;

    private ShellCue currentShellSong = SHELL_SONG_1;
    private ushort currentAwbIndex = 0;

    public Sound(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
        : base(music)
    {
        this.acbPointer = (nint*)(Utilities.BaseAddress + 0x26E05A0);

        scanner.AddMainModuleScan("E8 35 66 7C EA 44 0F B7 07", result =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find acb pattern.");
            }

            var address = Utilities.BaseAddress + result.Offset;
            var patch = new string[]
            {
                "use64",
                $"mov r8, {CUSTOM_BGM_ID}"
            };

            this.customAcbHook = hooks.CreateAsmHook(patch, address, AsmHookBehaviour.ExecuteFirst).Activate()
                ?? throw new Exception("Failed to create custom acb hook.");
        });

        scanner.AddMainModuleScan("E8 20 66 7C EA 02 05 E9 FB F4 F5", result =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find awb pattern.");
            }

            var address = Utilities.BaseAddress + result.Offset;
            var patch = new string[]
            {
                "use64",
                $"mov r8, {CUSTOM_BGM_ID}"
            };

            this.customAwbHook = hooks.CreateAsmHook(patch, address, AsmHookBehaviour.ExecuteFirst).Activate()
                ?? throw new Exception("Failed to create custom awb hook.");
        });

        scanner.AddMainModuleScan("77 07 E8 01 D8 5D 00", result =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find persist dlc bgm pattern.");
            }

            var address = Utilities.BaseAddress + result.Offset;
            var patch = new string[]
            {
                "use64",
                $"stc"
            };

            this.persistentDlcBgmHook = hooks.CreateAsmHook(patch, address, AsmHookBehaviour.ExecuteFirst).Activate()
                ?? throw new Exception("Failed to create persist dlc bgm hook.");
        });

        scanner.AddMainModuleScan("48 8B 0D B6 2F 23 ED", result =>
        {
            if (!result.Found)
            {
                throw new Exception("Failed to find always load dlc dlc bgm pattern.");
            }

            var address = Utilities.BaseAddress + result.Offset;
            var patch = new string[]
            {
                "use64",
                $"mov eax, 1"
            };

            this.alwaysLoadDlcBgmHook = hooks.CreateAsmHook(patch, address, AsmHookBehaviour.ExecuteFirst).Activate()
                ?? throw new Exception("Failed to create always load dlc bgm hook.");
        });


        this.playBgmHook = hooks.CreateHook<PlayBgmFunction>(this.PlayBgm, 0x155966B00).Activate()
            ?? throw new Exception("Failed to create persist dlc bgm pattern.");
    }

    private void PlayBgm(nint param1, nint param2, nint bgmId, nint param4)
    {
        var currentBgmId = this.GetGlobalBgmId((int)bgmId);

        // Disable DLC BGM if trying to play original songs.
        if (currentBgmId == SHELL_SONG_1.CueId || currentBgmId == SHELL_SONG_2.CueId)
        {
            // TODO: Disable dlc bgm.
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
                if (this.currentAwbIndex != currentBgmId)
                {
                    this.SwapShellCue();
                    this.currentAwbIndex = awbIndex;
                }

                // AWB index property uses big endian.
                var bigEndianAwbIndex = BitConverter.ToUInt16(BitConverter.GetBytes(awbIndex).Reverse().ToArray());

                // Pointer to AWB property of shell cue ID.
                var entryAwbIndexPtr = (ushort*)(address + (WAVEFORM_ENTRY_SIZE * this.currentShellSong.WaveTableIndex));
                Log.Debug("Entry AWB Address: {address}", ((nint)entryAwbIndexPtr).ToString("X"));
                *entryAwbIndexPtr = bigEndianAwbIndex;

                Log.Debug("Playing AWB index {currentAwbIndex} using Cue ID {currentShellCueId}.", this.currentAwbIndex, this.currentShellSong.CueId);
                currentBgmId = this.currentShellSong.CueId;
            }
            else
            {

                Log.Error("Failed to play extended BGM ID: {id}", currentBgmId);
                currentBgmId = FALLBACK_CUE_ID;
            }
        }

        Log.Debug("Playing BGM ID: {id}", currentBgmId);
        this.playBgmHook?.OriginalFunction(param1, param2, currentBgmId, param4);
    }

    /// <summary>
    /// Gets ACB address of DLC BGM, null if not loaded.
    /// </summary>
    private nint? AcbAddress
    {
        get
        {
            // DLC BGM ACB not loaded.
            if (*this.acbPointer == 0)
            {
                Log.Warning("Attempted to get DLC BGM ACB address while none loaded.");
                return null;
            }

            var acbAddress = *(nint*)(*this.acbPointer + 0x18);
            // Log.Debug("ACB Address: {address}", acbAddress.ToString("X"));
            return acbAddress;
        }
    }

    /// <summary>
    /// Gets address of waveform table rows.
    /// </summary>
    private nint? WaveformTableAddress
    {
        get
        {
            if (this.AcbAddress is nint address)
            {
                var waveformTableAddress = address + 2178;
                // Log.Debug("Waveform Table Address: {address}", waveformTableAddress.ToString("X"));
                return waveformTableAddress;
            }

            return null;
        }
    }

    private void SwapShellCue()
    {
        this.currentShellSong = (this.currentShellSong == SHELL_SONG_1) ? SHELL_SONG_2 : SHELL_SONG_1;
    }

    private record ShellCue(int CueId, int WaveTableIndex);
}
