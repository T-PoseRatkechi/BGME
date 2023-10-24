using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Serilog;

namespace BGME.Framework;

internal class SoundPatcher
{
    // Constants.
    private const int WAVEFORM_ENTRY_SIZE = 20;

    private const ushort SONG_CUE_ID_1 = 58;
    private const ushort SONG_WAVEFORM_INDEX_1 = 57;

    private const ushort SONG_CUE_ID_2 = 61;
    private const ushort SONG_WAVEFORM_INDEX_2 = 60;

    private readonly IHook<PlaySound> playSoundHook;

    private int? waveformAddress;
    private int currentAwbIndex = 0;
    private ushort currentShellCueId = 0;

    [Function(CallingConventions.Microsoft)]
    unsafe private delegate void* PlaySound(int soundCategory, int soundId, int param3, int param4);

    public SoundPatcher(IReloadedHooks? hooks)
    {
        unsafe
        {
            this.playSoundHook = hooks?.CreateHook<PlaySound>(this.PlaySoundImpl, 0x16BB7F130).Activate()
                ?? throw new Exception("Failed to create play sound hook.");
        }
    }

    /// <summary>
    /// Gets address to waveform table in ACB.
    /// </summary>
    private unsafe int WaveformAddress
    {
        get
        {
            if (this.waveformAddress == null)
            {
                // Use saved address if previously calculated.
                if (this.waveformAddress != null)
                {
                    return (int)this.waveformAddress;
                }

                // Calculate address.
                int* address = (int*)0x140BEAB30;
                address = (int*)(*address + 0x18);
                this.waveformAddress = *address + 0xAF77;
            }

            return (int)this.waveformAddress;
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
    private unsafe void* PlaySoundImpl(int soundCategory, int soundId, int param3, int param4)
    {
        if (!UseExtendedBgm(soundCategory, soundId))
        {
            return this.playSoundHook.OriginalFunction(soundCategory, soundId, param3, param4);
        }

        // Swap shell cue ID to trigger a song change.
        if (this.currentAwbIndex != soundId)
        {
            this.currentAwbIndex = soundId;
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

        Log.Debug("Playing AWB index {currentAwbIndex} using Cue ID {currentShellCueId}.", this.currentAwbIndex, this.currentShellCueId);
        return this.playSoundHook.OriginalFunction(soundCategory, this.currentShellCueId, param3, param4);
    }

    /// <summary>
    /// Changes the AWB index property of the given Waveform entry.
    /// </summary>
    /// <param name="waveformIndex">Index of waveform entry to change.</param>
    /// <param name="newAwbIndex">New AWB index to use.</param>
    unsafe private void SetWaveformAwbIndex(int waveformIndex, ushort newAwbIndex)
    {
        // AWB index property uses big endian.
        var bigEndianAwbIndex = BitConverter.ToUInt16(BitConverter.GetBytes(newAwbIndex).Reverse().ToArray());

        // Change AWB index.
        var entryOffset = this.WaveformAddress + waveformIndex * WAVEFORM_ENTRY_SIZE;
        ushort* entryAwbIndex = (ushort*)(entryOffset + 16);
        *entryAwbIndex = bigEndianAwbIndex;
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
    }
}
