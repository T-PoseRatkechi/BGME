namespace BGME.Framework.Music.MusicScripts;

internal class PathMusicScript : IMusicScript
{
    public PathMusicScript(string musicPath)
    {
        this.MusicPath = musicPath;
        this.IsFile = File.Exists(musicPath);
    }

    public string MusicPath { get; }

    public bool IsFile { get; }

    public object MusicSource => this.MusicPath;

    public void AddMusic(List<string> musicScripts)
    {
        if (this.IsFile)
        {
            musicScripts.Add(File.ReadAllText(this.MusicPath));
            Log.Debug($"Add music script from file.\nFile: {this.MusicPath}");
        }
        else
        {
            foreach (var file in Directory.EnumerateFiles(this.MusicPath, "*.pme", SearchOption.AllDirectories))
            {
                musicScripts.Add(File.ReadAllText(file));
                Log.Debug($"Add music script from file.\nFile: {this.MusicPath}");
            }
        }
    }
}
