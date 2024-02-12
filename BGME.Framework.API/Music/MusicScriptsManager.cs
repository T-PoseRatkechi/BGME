using BGME.Framework.API.Music.MusicScripts;
using BGME.Framework.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Timer = System.Timers.Timer;

namespace BGME.Framework.API.Music;

internal class MusicScriptsManager : IBgmeApi
{
    private readonly List<BgmeMod> bgmeMods = new();
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

        this.BgmeModLoading += (newMod) =>
        {
            this.bgmeMods.Add(newMod);
        };
    }

    public Action<string[]>? MusicScriptsChanged { get; set; }

    public Action<BgmeMod>? BgmeModLoading { get; set; }

    public BgmeMod[] GetLoadedMods() => this.bgmeMods.ToArray();

    public string[] GetMusicScripts()
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

        return musicScripts.ToArray();
    }

    public void AddBgmeMod(string modId, string modDir)
    {
        this.BgmeModLoading?.Invoke(new(modId, modDir));

        var bgmeDir = Path.Join(modDir, "bgme");
        if (Directory.Exists(bgmeDir))
        {
            this.AddPath(bgmeDir);
        }
    }

    public void AddPath(string path)
    {
        if (!this.musicScripts.Any(x => x.MusicSource.Equals(path)))
        {
            var pathMusicScript = new PathMusicScript(path);
            this.musicScripts.Add(pathMusicScript);
            var watcher = Utils.CreateWatch(path, (sender, arg) => this.OnMusicChanged(), pathMusicScript.IsFile ? null : "*.pme");
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

    public void AddMusicScript(Func<string> callback, Func<string> originalCallback)
    {
        var existingItem = this.musicScripts.FirstOrDefault(x => x.MusicSource.Equals(originalCallback));
        if (existingItem != null)
        {
            var existingIndex = this.musicScripts.IndexOf(existingItem);
            this.musicScripts[existingIndex] = new CallbackMusicScript(callback);
            Log.Information("Replaced existing music script callback.");
        }
        else
        {
            this.AddMusicScript(callback);
        }
    }

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
        => this.MusicScriptsChanged?.Invoke(this.GetMusicScripts());

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
