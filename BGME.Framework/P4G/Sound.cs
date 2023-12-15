using BGME.Framework.Models;
using BGME.Framework.Music;
using PersonaMusicScript.Types.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P4G;

internal unsafe class Sound : BaseSound
{
    // Constants.
    private const int WAVEFORM_ENTRY_SIZE = 20;
    private const int EXTENDED_BGM_ID = 64;

    private const ushort SONG_CUE_ID_1 = 58;
    private const ushort SONG_WAVEFORM_INDEX_1 = 57;
    private const ushort SONG_AWB_INDEX_1 = 80;

    private const ushort SONG_CUE_ID_2 = 61;
    private const ushort SONG_WAVEFORM_INDEX_2 = 60;
    private const ushort SONG_AWB_INDEX_2 = 82;

    // Shell songs.
    private static readonly ShellCue SHELL_SONG_1 = new(SONG_CUE_ID_1, SONG_WAVEFORM_INDEX_1);
    private static readonly ShellCue SHELL_SONG_2 = new(SONG_CUE_ID_2, SONG_WAVEFORM_INDEX_2);

    [Function(CallingConventions.Microsoft)]
    private delegate void PlaySoundFunction(int soundCategory, int soundId, nint param3, nint param4);
    private IFunction<PlaySoundFunction>? playSoundFunction;

    private IHook<PlaySoundFunction> playSoundHook;

    private ShellCue currentShellSong = SHELL_SONG_1;
    private ushort currentAwbIndex = 0;

    public Sound(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
        : base(music)
    {
        scanner.Scan("Play Sound Function", "48 63 E9 89 D6", result =>
        {
            this.playSoundFunction = hooks.CreateFunction<PlaySoundFunction>(result - 30);
            this.playSoundHook = this.playSoundFunction.Hook(this.PlaySoundImpl).Activate();
        });

        this.playSoundHook = default!;
    }

    /// <summary>
    /// Gets address to waveform table in ACB.
    /// </summary>
    private unsafe nint WaveformAddress
    {
        get
        {
            // Calculate address.
            nint* pointer = (nint*)(Utilities.BaseAddress + 0xBEAB30);
            if (*pointer == 0)
            {
                Log.Error("ACB address pointer is null.");
                return 0;
            }

            pointer = (nint*)(*pointer + 0x18);
            var tableAddress = *pointer + 0xAF77;
            Log.Debug($"Waveform Table Address: {tableAddress:X}");
            return tableAddress;
        }
    }

    protected override void PlayBgm(int bgmId)
    {
        this.playSoundFunction?.GetWrapper()(0, bgmId, 0, 0);
    }

    public void PlayMusic(IMusic music)
    {
        var bgmId = Utilities.CalculateMusicId(music);
        if (bgmId == null)
        {
            return;
        }
        else if (music is PersonaMusicScript.Types.Music.Sound sound)
        {
            Log.Debug($"PlaySound({sound.Setting_1}, {bgmId}, {sound.Setting_2}, {sound.Setting_3})");
            this.playSoundFunction?.GetWrapper()(sound.Setting_1, (int)bgmId, sound.Setting_2, sound.Setting_3);
        }
        else
        {
            Log.Debug($"PlaySound(0, {bgmId}, 0, 0)");
            this.playSoundFunction?.GetWrapper()(0, (int)bgmId, 0, 0);
        }
    }

    /// <summary>
    /// Play sound function.
    /// </summary>
    /// <param name="soundCategory">Sound category? 0 to 3 = BGM, 4 = SFX</param>
    /// <param name="soundId">Sound ID to play? Equal to Cue ID for BGM, unknown value for others.</param>
    /// <param name="param3">Unknown param.</param>
    /// <param name="param4">Unknown param.</param>
    /// <returns></returns>
    private void PlaySoundImpl(int soundCategory, int soundId, nint param3, nint param4)
    {
        if (soundCategory != 0)
        {
            Log.Verbose($"Playing Sound ID: {soundId}");
            this.playSoundHook.OriginalFunction(soundCategory, soundId, param3, param4);
            return;
        }

        var currentBgmId = this.GetGlobalBgmId(soundId);
        if (currentBgmId == null)
        {
            return;
        }

        if (currentBgmId == SHELL_SONG_1.CueId || currentBgmId == SHELL_SONG_2.CueId)
        {
            this.ResetShellSongs();
        }

        if (currentBgmId >= EXTENDED_BGM_ID)
        {
            // Swap shell cue ID to trigger a song change.
            if (this.currentAwbIndex != currentBgmId)
            {
                this.SwapShellCue();
                this.currentAwbIndex = (ushort)currentBgmId;
            }

            // Pointer to AWB property of shell cue ID.
            var entryAwbIndexPtr = (ushort*)(this.WaveformAddress + (WAVEFORM_ENTRY_SIZE * this.currentShellSong.WaveTableIndex) + 16);
            Log.Verbose($"Entry AWB Address: {(nint)entryAwbIndexPtr:X}");
            *entryAwbIndexPtr = this.currentAwbIndex.ToBigEndian();

            Log.Debug($"Playing AWB index {this.currentAwbIndex} using Cue ID {this.currentShellSong.CueId}.");
            currentBgmId = this.currentShellSong.CueId;
        }

        Log.Debug($"Playing BGM ID: {currentBgmId}");
        this.playSoundHook.OriginalFunction(soundCategory, (int)currentBgmId, param3, param4);
    }

    /// <summary>
    /// Resets shell songs data.
    /// </summary>
    private void ResetShellSongs()
    {
        var shellAwbPtr1 = (ushort*)(this.WaveformAddress + (WAVEFORM_ENTRY_SIZE * SHELL_SONG_1.WaveTableIndex) + 16);
        *shellAwbPtr1 = SONG_AWB_INDEX_1.ToBigEndian();

        var shellAwbPtr2 = (ushort*)(this.WaveformAddress + (WAVEFORM_ENTRY_SIZE * SHELL_SONG_2.WaveTableIndex) + 16);
        *shellAwbPtr2 = SONG_AWB_INDEX_2.ToBigEndian();

        Log.Debug("Reset shell songs data.");
    }

    private void SwapShellCue()
    {
        this.currentShellSong = (this.currentShellSong == SHELL_SONG_1) ? SHELL_SONG_2 : SHELL_SONG_1;
    }
}
