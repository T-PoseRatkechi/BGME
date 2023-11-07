namespace BGME.Framework.Models;

/// <summary>
/// Shell cue properties.
/// </summary>
/// <param name="CueId">Cue ID.</param>
/// <param name="WaveTableIndex">Index in waveform table.</param>
public record ShellCue(int CueId, int WaveTableIndex);
