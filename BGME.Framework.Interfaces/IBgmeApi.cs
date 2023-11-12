namespace BGME.Framework.Interfaces;

public interface IBgmeApi
{
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
}
