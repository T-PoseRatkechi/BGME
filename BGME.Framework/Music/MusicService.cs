using PersonaMusicScript.Library;
using PersonaMusicScript.Library.Models;
using Reloaded.Mod.Loader.IO.Utility;
using Serilog;

namespace BGME.Framework.Music;

internal class MusicService
{
    private readonly MusicParser parser;
    private readonly List<FileSystemWatcher> musicFolders = new();
    private readonly System.Timers.Timer musicReloadTimer = new(1000)
    {
        AutoReset = false,
    };

    private MusicSource currentMusic;

    public MusicService(MusicParser parser)
    {
        this.parser = parser;

        this.currentMusic = new();
        this.musicReloadTimer.Elapsed += (sender, args) => this.ReloadMusic();
    }

    public Dictionary<int, Encounter> Encounters => this.currentMusic.Encounters;

    public Dictionary<int, IMusic> Floors => this.currentMusic.Floors;

    public void AddMusicFolder(string folder)
    {
        var watcher = FileSystemWatcherFactory.Create(
            folder,
            (sender, args) =>
            {
                this.musicReloadTimer.Stop();
                this.musicReloadTimer.Start();
            },
            null,
            FileSystemWatcherFactory.FileSystemWatcherEvents.Deleted
            | FileSystemWatcherFactory.FileSystemWatcherEvents.Created
            | FileSystemWatcherFactory.FileSystemWatcherEvents.Changed,
            true,
            "*.pme",
            false);

        this.musicFolders.Add(watcher);
        this.ProcessMusicFolder(folder);
    }

    private void ReloadMusic()
    {
        this.currentMusic = new();
        foreach (var folder in this.musicFolders)
        {
            var folderPath = folder.Path;
            if (!Directory.Exists(folderPath))
            {
                continue;
            }

            this.ProcessMusicFolder(folderPath);
        }

        Log.Information("Reloaded music scripts.");
    }

    private void ParseMusicScript(string file)
    {
        try
        {
            this.parser.ParseFile(file, this.currentMusic);
            Log.Information("Parsed music script.\nFile: {file}", file);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse music script.\nFile: {file}", file);
        }
    }

    private void ProcessMusicFolder(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*.pme", SearchOption.AllDirectories))
        {
            this.ParseMusicScript(file);
        }
    }
}
