using PersonaMusicScript.Library;
using PersonaMusicScript.Library.Models;
using Reloaded.Mod.Loader.IO.Utility;

namespace BGME.Framework.Music;

internal class MusicService
{
    private readonly MusicParser parser;
    private readonly IFileBuilder? fileBuilder;
    private readonly bool hotReload;
    private readonly List<FileSystemWatcher> musicFolders = new();
    private readonly System.Timers.Timer musicReloadTimer = new(1000)
    {
        AutoReset = false,
    };

    private MusicSource currentMusic;

    public MusicService(
        MusicParser parser,
        IFileBuilder? fileBuilder = null,
        bool hotReload = false)
    {
        this.parser = parser;
        this.fileBuilder = fileBuilder;
        this.hotReload = hotReload;

        this.currentMusic = new();
        this.musicReloadTimer.Elapsed += (sender, args) => this.ReloadMusic();
    }

    public Dictionary<int, Encounter> Encounters => this.currentMusic.Encounters;

    public Dictionary<int, IMusic> Floors => this.currentMusic.Floors;

    public Dictionary<int, IMusic> Global => this.currentMusic.Global;

    public Dictionary<EventIds, FrameTable> Events => this.currentMusic.Events;

    public FrameTable? GetEventFrame(int majorId, int minorId, PmdType pmdType) => this.currentMusic.GetEventFrame(majorId, minorId, pmdType);

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

        if (this.hotReload && this.fileBuilder != null)
        {
            this.fileBuilder.Build(this);
            Log.Information("Rebuilt files.");
        }

        Log.Information("Reloaded music scripts.");
    }

    private void ProcessMusicFolder(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*.pme", SearchOption.AllDirectories))
        {
            this.ParseMusicScript(file);
        }
    }

    private void ParseMusicScript(string file)
    {
        try
        {
            this.parser.ParseFile(file, this.currentMusic);
            Log.Information($"Parsed music script.\nFile: {file}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to parse music script.\nFile: {file}");
        }
    }

}
