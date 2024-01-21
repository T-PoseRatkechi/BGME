using PersonaModdingMetadata.Shared.Games;

namespace BGME.Framework.CRI;

internal class CriAdxPatterns
{
    public Game Game { get; init; }

    public string? CriAtomExPlayer_GetNumPlayedSamples { get; init; }

    public string? criAtomExAcb_LoadAcbFile { get; init; }

    public string? CriAtomExPlayer_SetCueId { get; init; }

    public string? CriAtomExPlayer_Start { get; init; }

    public string? CriAtomExPlayer_SetFile { get; init; }

    public string? CriAtomExPlayer_SetFormat { get; init; }

    public string? CriAtomExPlayer_SetSamplingRate { get; init; }

    public string? CriAtomExPlayer_SetNumChannels { get; init; }

    public string? CriAtomExCategory_GetVolumeById { get; init; }

    public string? CriAtomExPlayer_SetVolume { get; init; }

    public string? CriAtomExPlayer_SetCategoryById { get; init; }

    public string? CriAtomExPlayer_SetStartTime { get; init; }

    public string? CriAtomExPlayback_GetTimeSyncedWithAudio { get; init; }

    public string? CriAtomExPlayer_Create { get; init; }
}
