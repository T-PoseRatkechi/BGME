namespace BGME.Framework.Interfaces;

public interface IBgmeApi
{
    /// <summary>
    /// Add a path to load music script(s) from. Can be a file or folder.
    /// </summary>
    /// <param name="path">Path to add.</param>
    void AddPath(string path);

    /// <summary>
    /// Remove an added path.
    /// </summary>
    /// <param name="path">Path to remove.</param>
    void RemovePath(string path);

    /// <summary>
    /// Add a folder to load music scripts from.
    /// </summary>
    /// <param name="folder">Folder path.</param>
    void AddFolder(string folder);

    /// <summary>
    /// Remove a folder for loading music scripts.
    /// </summary>
    /// <param name="folder">Folder path.</param>
    void RemoveFolder(string folder);

    /// <summary>
    /// Add a music script entry.
    /// </summary>
    /// <param name="callback">Callback that returns a new music script entry.</param>
    void AddMusicScript(Func<string> callback);

    /// <summary>
    /// Remove an existing music script entry.
    /// </summary>
    /// <param name="callback">Previously added callback to remove.</param>
    void RemoveMusicScript(Func<string> callback);

    /// <summary>
    /// Adds or replaces a music script callback.
    /// </summary>
    /// <param name="callback">Callback to add.</param>
    /// <param name="originalCallback">Callback to replace.</param>
    void AddMusicScript(Func<string> callback, Func<string> originalCallback);

    /// <summary>
    /// Gets loaded BGME mods.
    /// </summary>
    /// <returns>BGME mods currently loaded.</returns>
    BgmeMod[] GetLoadedMods();

    /// <summary>
    /// Gets loaded music scripts.
    /// </summary>
    /// <returns>Music scripts currently loaded.</returns>
    string[] GetMusicScripts();

    public Action<string[]>? MusicScriptsChanged { get; set; }

    public Action<BgmeMod>? BgmeModLoading { get; set; }
}

public record BgmeMod(string ModId, string ModDir);