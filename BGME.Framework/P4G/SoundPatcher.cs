using BGME.Framework.Music;
using PersonaMusicScript.Library.Models;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P4G;

internal unsafe class SoundPatcher : BaseSound
{
    // Constants.
    private const int WAVEFORM_ENTRY_SIZE = 20;

    private const ushort SONG_CUE_ID_1 = 58;
    private const ushort SONG_WAVEFORM_INDEX_1 = 57;

    private const ushort SONG_CUE_ID_2 = 61;
    private const ushort SONG_WAVEFORM_INDEX_2 = 60;

    [Function(CallingConventions.Microsoft)]
    private delegate void* PlaySoundFunction(int soundCategory, int soundId, nint param3, nint param4);
    private IFunction<PlaySoundFunction>? playSoundFunction;

    private IHook<PlaySoundFunction>? playSoundHook;

    private int currentAwbIndex = 0;
    private ushort currentShellCueId = 0;

    public SoundPatcher(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
        : base(music)
    {
        scanner.Scan("Play Sound Function", "48 63 E9 89 D6", result =>
        {
            if (result == null)
            {
                return;
            }

            this.playSoundFunction = hooks.CreateFunction<PlaySoundFunction>((nint)result - 30);
            this.playSoundHook = this.playSoundFunction.Hook(this.PlaySoundImpl).Activate();
        });
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

    public void PlayMusic(IMusic music)
    {
        var bgmId = Utilities.CalculateMusicId(music);
        if (music is Sound sound)
        {
            Log.Debug($"PlaySound({sound.Setting_1}, {bgmId}, {sound.Setting_2}, {sound.Setting_3})");
            this.playSoundFunction?.GetWrapper()(sound.Setting_1, bgmId, sound.Setting_2, sound.Setting_3);
        }
        else
        {
            Log.Debug($"PlaySound(0, {bgmId}, 0, 0)");
            this.playSoundFunction?.GetWrapper()(0, bgmId, 0, 0);
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
    private unsafe void* PlaySoundImpl(int soundCategory, int soundId, nint param3, nint param4)
    {
        if (!UseExtendedBgm(soundCategory, soundId))
        {
            if (soundCategory == 0)
            {
                Log.Debug($"Playing BGM ID: {soundId}");
            }
            else
            {
                Log.Verbose($"Playing Sound ID: {soundId}");
            }

            return this.playSoundHook.OriginalFunction(soundCategory, soundId, param3, param4);
        }

        var bgmId = this.GetGlobalBgmId(soundId);

        // Swap shell cue ID to trigger a song change.
        if (this.currentAwbIndex != bgmId)
        {
            this.currentAwbIndex = bgmId;
            this.SwitchShellCueId();
        }

        if (this.currentShellCueId == SONG_CUE_ID_1)
        {
            this.SetWaveformAwbIndex(SONG_WAVEFORM_INDEX_1, (ushort)this.currentAwbIndex);
        }
        else
        {
            this.SetWaveformAwbIndex(SONG_WAVEFORM_INDEX_2, (ushort)this.currentAwbIndex);
        }

        Log.Debug($"Playing AWB index {this.currentAwbIndex} using Cue ID {this.currentShellCueId}.");
        return this.playSoundHook.OriginalFunction(soundCategory, this.currentShellCueId, param3, param4);
    }

    /// <summary>
    /// Changes the AWB index property of the given Waveform entry.
    /// </summary>
    /// <param name="waveformIndex">Index of waveform entry to change.</param>
    /// <param name="newAwbIndex">New AWB index to use.</param>
    unsafe private void SetWaveformAwbIndex(int waveformIndex, ushort newAwbIndex)
    {
        Log.Debug($"Setting Waveform AWB Index || Waveform Index: {waveformIndex} || New AWB Index: {newAwbIndex}");

        // AWB index property uses big endian.
        var bigEndianAwbIndex = BitConverter.ToUInt16(BitConverter.GetBytes(newAwbIndex).Reverse().ToArray());

        // Change AWB index.
        var entryOffset = this.WaveformAddress + waveformIndex * WAVEFORM_ENTRY_SIZE;
        Log.Debug($"Entry Address: {entryOffset:X}");

        ushort* entryAwbIndex = (ushort*)(entryOffset + 16);
        *entryAwbIndex = bigEndianAwbIndex;
        Log.Debug($"Set Waveform AWB Index || Waveform Index: {waveformIndex} || New AWB Index: {newAwbIndex}");
    }

    /// <summary>
    /// Test whether to use extended BGM.
    /// </summary>
    /// <param name="soundCategory">Sound category.</param>
    /// <param name="soundId">Sound ID to play.</param>
    private static bool UseExtendedBgm(int soundCategory, int soundId)
    {
        if (soundCategory == 0 && soundId > 64)
        {
            Log.Debug("Using Extended BGM");
            return true;
        }

        return false;
    }

    private void SwitchShellCueId()
    {
        if (this.currentShellCueId == SONG_CUE_ID_1)
        {
            this.currentShellCueId = SONG_CUE_ID_2;
        }
        else
        {
            this.currentShellCueId = SONG_CUE_ID_1;
        }

        Log.Debug($"Swapped Shell Cue ID to: {this.currentShellCueId}");
    }
}
