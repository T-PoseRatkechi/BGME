namespace BGME.Framework.Music.MusicScripts;

internal interface IMusicScript
{
    object MusicSource { get; }

    void AddMusic(List<string> musicScripts);
}
