using PersonaModdingMetadata.Shared.Games;

namespace BGME.Framework.CRI;

internal static class CriAtomExGames
{
    private static readonly CriAtomExPatterns[] patterns = new CriAtomExPatterns[]
    {
        new(Game.P5R_PC)
        {
            CriAtomExPlayer_Create = "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 55 41 54 41 55 41 56 41 57 48 8B EC 48 83 EC 40 45 33 ED",
            CriAtomExPlayer_SetStartTime = "48 85 C9 74 ?? 48 85 D2 78 ?? B8 FF FF FF FF 48 3B D0 48 0F 4C C2",
            CriAtomExPlayback_GetTimeSyncedWithAudio = "48 83 EC 28 E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0 7E",
            CriAtomExPlayer_GetNumPlayedSamples = "33 C0 4C 8D 54 24",
            CriAtomExAcb_LoadAcbFile = "48 8B C4 48 89 58 ?? 48 89 68 ?? 48 89 70 ?? 48 89 78 ?? 41 54 41 56 41 57 48 83 EC 40 49 8B E9",
            CriAtomExPlayer_SetCueId = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 49 63 F8 48 8B F2 48 8B D9",
            CriAtomExPlayer_Start = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 48 85 C9 75 ?? 44 8D 41 ?? 48 8D 15 ?? ?? ?? ?? E8 ?? ?? ?? ?? 83 C8 FF EB ?? E8 ?? ?? ?? ?? 33 D2",
            CriAtomExPlayer_SetFile = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 49 8B F0 48 8B EA 48 8B F9",
            CriAtomExPlayer_SetFormat = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 48 85 C9 75 ?? 48 8D 15",
            CriAtomExPlayer_SetSamplingRate = "48 89 5C 24 ?? 57 48 83 EC 20 8B FA 48 8B D9 48 85 C9 74 ?? 85 D2",
            CriAtomExPlayer_SetNumChannels = "48 89 5C 24 ?? 57 48 83 EC 20 8B FA 48 8B D9 48 85 C9 74 ?? 8D 42",
            CriAtomExPlayer_SetVolumeById = "40 53 48 83 EC 20 8B D9 33 C9 E8 ?? ?? ?? ?? 85 C0 75 ?? 48 8D 15 ?? ?? ?? ?? 33 C9 E8 ?? ?? ?? ?? F3 0F 10 05",
            CriAtomExPlayer_SetVolume = "48 85 C9 75 ?? 44 8D 41 ?? 48 8D 15 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 89 ?? ?? ?? ?? 33 D2",
            CriAtomExPlayer_SetCategoryById = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 50 48 8B F9 8B F2",
            CriAtomExCategory_GetVolumeById = "40 53 48 83 EC 20 8B D9 33 C9 E8 ?? ?? ?? ?? 85 C0 75 ?? 48 8D 15 ?? ?? ?? ?? 33 C9 E8 ?? ?? ?? ?? F3 0F 10 05",
            CriAtomExPlayer_GetLastPlaybackId = "48 83 EC 28 48 85 C9 75 ?? 44 8D 41 ?? 48 8D 15 ?? ?? ?? ?? E8 ?? ?? ?? ?? 83 C8 FF EB ?? 8B 81",
        },
        new(Game.P4G_PC)
        {
            CriAtomExPlayer_Create = "40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC C8 00 00 00 45 8B F8",
            CriAtomExPlayer_SetStartTime = "48 85 C9 74 ?? 48 85 D2 78 ?? B8 FF FF FF FF 48 3B D0 48 0F 4C C2",
            CriAtomExPlayback_GetTimeSyncedWithAudio = "48 83 EC 28 E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0 7E",
            CriAtomExPlayer_GetNumPlayedSamples = "33 C0 4C 8D 54 24",
            CriAtomExAcb_LoadAcbFile = "48 8B C4 48 89 58 ?? 48 89 68 ?? 48 89 70 ?? 48 89 78 ?? 41 54 41 56 41 57 48 83 EC 40 49 8B E9",
            CriAtomExPlayer_SetCueId = "48 8B C4 48 89 58 ?? 48 89 68 ?? 48 89 70 ?? 48 89 78 ?? 41 54 41 56 41 57 48 81 EC 80 00 00 00 4D 63 F0",
            CriAtomExPlayer_Start = "48 89 E0 48 89 58 ?? 48 89 68 ?? 48 89 70 ?? 48 89 78 ?? 41 56 48 83 EC 60",
            CriAtomExPlayer_SetFile = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 49 8B F0 48 8B EA 48 8B F9",
            CriAtomExPlayer_SetFormat = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 48 85 C9 75 ?? 48 8D 15",
            CriAtomExPlayer_SetSamplingRate = "48 89 5C 24 ?? 57 48 83 EC 20 8B FA 48 8B D9 48 85 C9 74 ?? 85 D2",
            CriAtomExPlayer_SetNumChannels = "48 89 5C 24 ?? 57 48 83 EC 20 8B FA 48 8B D9 48 85 C9 74 ?? 8D 42",
            CriAtomExPlayer_SetVolumeById = "40 53 48 83 EC 20 8B D9 33 C9 E8 ?? ?? ?? ?? 85 C0 75 ?? 48 8D 15 ?? ?? ?? ?? 33 C9 E8 ?? ?? ?? ?? F3 0F 10 05",
            CriAtomExPlayer_SetVolume = "40 53 48 83 EC 30 0F 29 74 24 ?? 0F 28 F1 48 8B D9 48 85 C9 75 ?? 44 8D 41 ?? 48 8D 15 ?? ?? ?? ?? E8 ?? ?? ?? ?? EB ?? 48 8B 81 ?? ?? ?? ?? 48 85 C0 74 ?? 48 8B 89 ?? ?? ?? ?? 0F 28 DE 45 33 C0",
            CriAtomExPlayer_SetCategoryById = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 50 48 8B F9 8B F2",
            CriAtomExCategory_GetVolumeById = "40 53 48 83 EC 20 8B D9 33 C9 E8 ?? ?? ?? ?? 85 C0 75 ?? 48 8D 15 ?? ?? ?? ?? 33 C9 E8 ?? ?? ?? ?? F3 0F 10 05",
            CriAtomExPlayer_GetLastPlaybackId = "48 83 EC 28 48 85 C9 75 ?? 44 8D 41 ?? 48 8D 15 ?? ?? ?? ?? E8 ?? ?? ?? ?? 83 C8 FF EB ?? 8B 81",
            CriAtomExPlayer_SetCategoryByName = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 50 48 8B F9 48 8B F2",
            CriAtomExPlayer_GetCategoryInfo = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 89 CB 4C 89 C6",
        }
    };

    public static CriAtomExPatterns GetGamePatterns(Game game)
        => patterns.First(x => x.Game.Contains(game));
}

internal class CriAtomExPatterns
{
    public CriAtomExPatterns(Game game)
    {
        this.Game = new Game[] { game };
    }

    public CriAtomExPatterns(Game[] game)
    {
        this.Game = game;
    }

    public Game[] Game { get; }

    public string? CriAtomExPlayer_GetNumPlayedSamples { get; init; }

    public string? CriAtomExAcb_LoadAcbFile { get; init; }

    public string? CriAtomExPlayer_SetCueId { get; init; }

    public string? CriAtomExPlayer_Start { get; init; }

    public string? CriAtomExPlayer_SetFile { get; init; }

    public string? CriAtomExPlayer_SetFormat { get; init; }

    public string? CriAtomExPlayer_SetSamplingRate { get; init; }

    public string? CriAtomExPlayer_SetNumChannels { get; init; }

    public string? CriAtomExCategory_GetVolumeById { get; init; }

    public string? CriAtomExPlayer_SetVolume { get; init; }

    public string? CriAtomExPlayer_SetVolumeById { get; init; }

    public string? CriAtomExPlayer_SetCategoryById { get; init; }

    public string? CriAtomExPlayer_SetStartTime { get; init; }

    public string? CriAtomExPlayback_GetTimeSyncedWithAudio { get; init; }

    public string? CriAtomExPlayer_Create { get; init; }

    public string? CriAtomExPlayer_GetLastPlaybackId { get; init; }

    public string? CriAtomExPlayer_SetCategoryByName { get; init; }

    public string? CriAtomExPlayer_GetCategoryInfo { get; init; }
}
