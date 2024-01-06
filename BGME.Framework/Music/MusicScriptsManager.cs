using BGME.Framework.Interfaces;
using BGME.Framework.Music.MusicScripts;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Timer = System.Timers.Timer;

namespace BGME.Framework.Music;

internal class MusicScriptsManager : IBgmeApi
{
    private readonly ObservableCollection<IMusicScript> musicScripts = new();
    private readonly List<FileSystemWatcher> watchers = new();
    private readonly Timer musicReloadTimer = new(1000)
    {
        AutoReset = false,
    };

    public MusicScriptsManager()
    {
        this.musicScripts.CollectionChanged += this.OnMusicCollectionChanged;
        this.musicReloadTimer.Elapsed += (sender, args) => this.OnMusicScriptsChanged();
    }

    public Action<string[]>? MusicScriptsChanged;

    public void AddPath(string path)
    {
        if (!this.musicScripts.Any(x => x.MusicSource.Equals(path)))
        {
            var pathMusicScript = new PathMusicScript(path);
            var watcher = Utilities.CreateWatch(path, (sender, arg) => this.OnMusicChanged(), pathMusicScript.IsFile ? null : "*.pme");
            this.watchers.Add(watcher);
        }
    }

    public void RemovePath(string path)
    {
        if (this.musicScripts.FirstOrDefault(x => x.MusicSource.Equals(path)) is PathMusicScript item)
        {
            this.musicScripts.Remove(item);

            var watcherPath = item.IsFile ? Path.GetDirectoryName(item.MusicPath)! : item.MusicPath;
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
        => this.musicScripts.Add(new CallbackMusicScript(callback));

    public void RemoveMusicScript(Func<string> callback)
    {
        var item = this.musicScripts.FirstOrDefault(x => x.MusicSource.Equals(callback));
        if (item != null)
        {
            this.musicScripts.Remove(item);
        }
    }

    public void AddFolder(string folder)
        => this.AddPath(folder);

    public void RemoveFolder(string folder)
        => this.RemovePath(folder);

    private void OnMusicScriptsChanged()
    {
        var musicScripts = new List<string>();

        // Reload music scripts.
        foreach (var musicScript in this.musicScripts)
        {
            try
            {
                musicScript.AddMusic(musicScripts);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add music script from source.");
            }
        }

        this.MusicScriptsChanged?.Invoke(musicScripts.ToArray());
    }

    private void OnMusicCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Log.Information("Music script added.");
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            Log.Information("Music script removed.");
        }

        this.OnMusicChanged();
    }

    private void OnMusicChanged()
    {
        this.musicReloadTimer.Stop();
        this.musicReloadTimer.Start();
    }
}
