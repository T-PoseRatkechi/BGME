namespace BGME.Framework.API.Music.MusicScripts;

internal interface IMusicScript
{
    object MusicSource { get; }

    void AddMusic(List<string> musicScripts);
}
