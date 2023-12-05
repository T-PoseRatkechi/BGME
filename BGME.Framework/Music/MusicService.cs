using BGME.Framework.Interfaces;
using PersonaMusicScript.Library;
using PersonaMusicScript.Types;
using PersonaMusicScript.Types.Music;
using PersonaMusicScript.Types.MusicCollections;
using Reloaded.Mod.Loader.IO.Utility;

namespace BGME.Framework.Music;

internal class MusicService : IBgmeApi
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
        MusicResources resources,
        IFileBuilder? fileBuilder = null,
        bool hotReload = false)
    {
        this.parser = new(resources);
        this.fileBuilder = fileBuilder;
        this.hotReload = hotReload;

        this.currentMusic = new(resources);
        this.musicReloadTimer.Elapsed += (sender, args) =>
        {
            this.currentMusic = new(resources);
            foreach (var folder in this.musicFolders)
            {
                var folderPath = folder.Path;
                if (!Directory.Exists(folderPath))
                {
                    continue;
                }

                this.ProcessMusicFolder(folderPath);
            }

            foreach(var callback in this.apiEntries)
            {
                try
                {
                    var entryMusicScript = callback();
                    this.parser.Parse(entryMusicScript, this.currentMusic);
                    Log.Information("Added music entry from API callback.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to add music entry from API callback.");
                }
            }

            if (this.hotReload && this.fileBuilder != null)
            {
                this.fileBuilder.Build(this);
                Log.Information("Rebuilt files.");
            }

            Log.Information("Reloaded music scripts.");
        };
    }

    public EncounterMusic Encounters => this.currentMusic.Encounters;

    public FloorMusic Floors => this.currentMusic.Floors;

    public GlobalMusic Global => this.currentMusic.Global;

    public EventMusic Events => this.currentMusic.Events;

    public FrameTable? GetEventFrame(int majorId, int minorId, PmdType pmdType) => this.Events.GetEventFrame(majorId, minorId, pmdType);

    public void RemoveFolder(string folder)
    {
        if (this.musicFolders.FirstOrDefault(x => x.Path == folder) is FileSystemWatcher musicFolder)
        {
            musicFolder.Dispose();
            this.musicFolders.Remove(musicFolder);
            this.ReloadMusic();
            Log.Debug($"Removed music folder.\nFolder: {folder}");
        }
        else
        {
            Log.Warning($"Could not find music folder to remove.\nFolder: {folder}");
        }
    }

    public void AddFolder(string folder)
    {
        var watcher = FileSystemWatcherFactory.Create(
            folder,
            (sender, args) =>
            {
                this.ReloadMusic();
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
        this.musicReloadTimer.Stop();
        this.musicReloadTimer.Start();
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

            var presetFile = Path.ChangeExtension(file, ".project");
            this.parser.CreatePreset(file, presetFile);
            Log.Debug($"Created project preset.\nFile: {presetFile}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to parse music script.\nFile: {file}");
        }
    }

    private readonly List<Func<string>> apiEntries = new();

    public void AddMusicScript(Func<string> callback)
    {
        this.apiEntries.Add(callback);
        Log.Debug("Added API music entry callback.");
        this.ReloadMusic();
    }

    public void RemoveMusicScript(Func<string> callback)
    {
        if (this.apiEntries.Remove(callback))
        {
            Log.Debug("Removed API music entry callback.");
            this.ReloadMusic();
        }
        else
        {
            Log.Warning("Music entry was not found and could not be removed.");
        }
    }
}
