using Reloaded.Mod.Loader.IO.Utility;

namespace BGME.Framework.API;

internal static class Utils
{
    public static FileSystemWatcher CreateWatch(string path, FileSystemEventHandler handler, string? filter = null)
    {
        var isFile = File.Exists(path);

        // Use file name as filter for paths that are files.
        var folder = isFile ? Path.GetDirectoryName(path)! : path;
        var fileName = isFile ? Path.GetFileName(path) : null;

        var watcher = FileSystemWatcherFactory.Create(
            folder,
            handler,
            null,
            FileSystemWatcherFactory.FileSystemWatcherEvents.Deleted
            | FileSystemWatcherFactory.FileSystemWatcherEvents.Created
            | FileSystemWatcherFactory.FileSystemWatcherEvents.Changed,
            !isFile,
            isFile ? fileName : filter,
            false);

        return watcher;
    }
}
