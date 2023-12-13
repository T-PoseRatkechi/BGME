using BGME.Framework.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Timer = System.Timers.Timer;

namespace BGME.Framework.Music;

internal class MusicScriptsManager : IBgmeApi
{
    private readonly ObservableCollection<MusicPath> musicPaths = new();
    private readonly ObservableCollection<Func<string>> apiCallbacks = new();
    private readonly List<FileSystemWatcher> watchers = new();
    private readonly Timer musicReloadTimer = new(1000)
    {
        AutoReset = false,
    };

    public MusicScriptsManager()
    {
        this.musicPaths.CollectionChanged += this.OnMusicCollectionChanged;
        this.apiCallbacks.CollectionChanged += this.OnMusicCollectionChanged;
        this.musicReloadTimer.Elapsed += (sender, args) => this.OnMusicScriptsChanged();
    }

    public Action<string[]>? MusicScriptsChanged;

    public void AddPath(string path)
    {
        if (!this.musicPaths.Any(x => x.Path == path))
        {
            var musicPath = new MusicPath(path);
            this.musicPaths.Add(musicPath);
            var watcher = Utilities.CreateWatch(path, (sender, arg) => this.OnMusicChanged(), musicPath.IsFile ? null : "*.pme");
            this.watchers.Add(watcher);
        }
    }

    public void RemovePath(string path)
    {
        var item = this.musicPaths.FirstOrDefault(x => x.Path == path);
        if (item != null)
        {
            this.musicPaths.Remove(item);
            var watcherPath = item.IsFile ? Path.GetDirectoryName(item.Path)! : item.Path;

            var pathWatcher = this.watchers.FirstOrDefault(x => x.Path == watcherPath);
            if (pathWatcher != null)
            {
                this.watchers.Remove(pathWatcher);
                pathWatcher.Dispose();
            }
            else
            {
                Log.Warning($"Failed to remove music script watch for path.\nPath: {path}");
            }
        }
        else
        {
            Log.Verbose($"{path} music path was not found for removal.");
        }
    }

    public void AddMusicScript(Func<string> callback)
        => this.apiCallbacks.Add(callback);

    public void RemoveMusicScript(Func<string> callback)
        => this.apiCallbacks.Remove(callback);

    public void AddFolder(string folder)
        => this.AddPath(folder);

    public void RemoveFolder(string folder)
        => this.RemovePath(folder);

    private void OnMusicScriptsChanged()
    {
        var musicScripts = new List<string>();

        // Add from files/folders.
        foreach (var musicPath in this.musicPaths)
        {
            if (musicPath.IsFile)
            {
                AddMusicFromFile(musicScripts, musicPath.Path);
            }
            else
            {
                foreach (var file in Directory.EnumerateFiles(musicPath.Path, "*.pme", SearchOption.AllDirectories))
                {
                    AddMusicFromFile(musicScripts, file);
                }
            }
        }

        // Add from callbacks.
        foreach (var callback in this.apiCallbacks)
        {
            AddMusicFromCallback(musicScripts, callback);
        }

        this.MusicScriptsChanged?.Invoke(musicScripts.ToArray());
    }

    private static void AddMusicFromFile(List<string> musicScripts, string file)
    {
        try
        {
            musicScripts.Add(File.ReadAllText(file));
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to add music script from file.\nFile: {file}");
        }
    }

    private static void AddMusicFromCallback(List<string> musicScripts, Func<string> callback)
    {
        try
        {
            musicScripts.Add(callback());
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to add music script from callback.");
        }
    }

    private void OnMusicCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Log.Information(sender == this.musicPaths ? "Music path added." : "Music callback added.");
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            Log.Information(sender == this.musicPaths ? "Music path removed." : "Music callback removed.");
        }

        this.OnMusicChanged();
    }

    private void OnMusicChanged()
    {
        this.musicReloadTimer.Stop();
        this.musicReloadTimer.Start();
    }
}

internal record MusicPath(string Path)
{
    public bool IsFile { get; } = File.Exists(Path);
}