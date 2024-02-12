namespace BGME.Framework.API.Music.MusicScripts;

internal class CallbackMusicScript : IMusicScript
{
    private readonly string musicScript;

    public CallbackMusicScript(Func<string> callback)
    {
        this.MusicSource = callback;
        this.musicScript = callback.Invoke();
    }

    public object MusicSource { get; }

    public void AddMusic(List<string> musicScripts)
    {
        musicScripts.Add(this.musicScript);
        Log.Debug("Added music script from callback.");
    }
}
