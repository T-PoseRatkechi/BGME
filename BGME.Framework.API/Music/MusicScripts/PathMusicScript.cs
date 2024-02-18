namespace BGME.Framework.API.Music.MusicScripts;

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
            Log.Debug($"Added music script from file.\nFile: {this.MusicPath}");
        }
        else
        {
            var files = Directory.GetFiles(this.MusicPath, "*.pme", SearchOption.AllDirectories)
                .Order().ToArray();

            foreach (var file in files)
            {
                musicScripts.Add(File.ReadAllText(file));
                Log.Debug($"Added music script from file.\nFile: {file}");
            }
        }
    }
}
