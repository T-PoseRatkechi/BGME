using CriFs.V2.Hook.Interfaces;
using LibellusLibrary.Event.Types.Frame;
using LibellusLibrary.Event.Types;
using LibellusLibrary.Event;
using CriFsV2Lib.Definitions;
using PersonaMusicScript.Types.Music;

namespace BGME.Framework.Music;

internal class PmdFileMerger : IFileBuilder
{
    private readonly ICriFsRedirectorApi criFsApi;
    private readonly ICriFsLib criFsLib;
    private readonly PmdReader pmdReader;
    private readonly string bindDir;

    public PmdFileMerger(
        ICriFsRedirectorApi criFsApi,
        string baseDirectory)
    {
        this.criFsApi = criFsApi;
        this.criFsLib = criFsApi.GetCriFsLib();

        this.pmdReader = new();

        // Setup binding folder.
        this.bindDir = Path.Join(criFsApi.GenerateBindingDirectory(baseDirectory), "bgme");
        Directory.CreateDirectory(this.bindDir);
        var probingPath = Path.GetRelativePath(baseDirectory, this.bindDir);
        criFsApi.AddProbingPath(probingPath);
    }

    public void Build(MusicService music)
    {
        Log.Information("Building files.");

        // Build files.
        // Persona 4 Golden.
        var dataFile = this.criFsApi.GetCpkFilesInGameDir().First(x => Path.GetFileNameWithoutExtension(x) == "data");

        using var dataStream = new FileStream(dataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var reader = this.criFsLib.CreateCpkReader(dataStream, false);
        Task.WaitAll(this.CurrentEvents(music.Events).Select(x => this.BuildEventFile(reader, dataFile, x.Key, x.Value)).ToArray());

        Log.Information("Files built.");
    }

    private Dictionary<string, FrameTable> CurrentEvents(Dictionary<EventIds, FrameTable> events)
    {
        var eventFiles = new Dictionary<string, FrameTable>();
        foreach (var eventEntry in events)
        {
            var eventIds = eventEntry.Key;
            var musicFrameTable = eventEntry.Value;

            // Generate expected relative file path for event.
            var folderId = eventIds.MajorId - (eventIds.MajorId % 10);
            var eventFilePath = $@"event\e{folderId}\E{eventIds.MajorId:000}_{eventIds.MinorId:000}{GetEventExt(eventIds.PmdType)}";

            eventFiles[eventFilePath] = musicFrameTable;
            Log.Debug($"Added event file to build: {eventFilePath}");
        }

        return eventFiles;
    }

    private static string GetEventExt(PmdType pmdType) => pmdType switch
    {
        PmdType.PM1 => ".pm1",
        PmdType.PM2 => ".pm2",
        _ => ".pm3"
    };

    private MemoryStream? GetFile(ICpkReader reader, string dataFile, string relativePath)
    {
        Log.Debug($"Getting game file: {relativePath}");
        var cachedFile = this.criFsApi.GetCpkFilesCached(dataFile);
        if (cachedFile.FilesByPath.TryGetValue(relativePath, out var index))
        {
            var fileEntry = cachedFile.Files[index];
            using var extractedFile = reader.ExtractFile(fileEntry.File);
            return new MemoryStream(extractedFile.Span.ToArray());
        }

        Log.Error($"Game file not found.\nFile: {relativePath}");
        return null;
    }

    private async Task BuildEventFile(ICpkReader reader, string dataFile, string eventFilePath, FrameTable musicFrameTable)
    {
        using var eventFileStream = this.GetFile(reader, dataFile, eventFilePath);
        if (eventFileStream == null)
        {
            return;
        }

        // Get (or add) frame table.
        var pmd = await this.pmdReader.ReadPmd(eventFileStream);
        if (pmd.PmdDataTypes.FirstOrDefault(x => x.Type == PmdTypeID.FrameTable) is not PmdData_FrameTable frameTable)
        {
            frameTable = new();
            pmd.PmdDataTypes.Add(frameTable);
            Log.Information($"Added new Frame Table. This is untested.\nFile: {eventFilePath}");
        }

        // Add frame entries for BGM.
        foreach (var frame in musicFrameTable.Frames)
        {
            // Remove duplicate frames from original frame table.
            var existingFrame = frameTable.Frames.FirstOrDefault(x =>
                x.StartFrame == (ushort)frame.Key && x.TargetType == PmdTargetTypeID.BGM)!;

            frameTable.Frames.Remove(existingFrame);

            // Duplicate frame was to remove existing BGM.
            if (frame.Value == null)
            {
                continue;
            }

            var frameId = frame.Key;
            if (frame.Value is FrameBgm frameBgm)
            {
                var bgmId = MusicUtils.CalculateMusicId(frameBgm.Music!);
                var frameBgmObj = new PmdTarget_Bgm()
                {
                    StartFrame = (ushort)frameId,
                    BgmId = (ushort)(bgmId ?? 0),
                    BgmType = (LibellusLibrary.Event.Types.Frame.PmdBgmType)frameBgm.BgmType,
                };

                frameTable.Frames.Add(frameBgmObj);
            }
            else
            {
                var bgmId = MusicUtils.CalculateMusicId(frame.Value);
                var frameBgmObj = new PmdTarget_Bgm()
                {
                    StartFrame = (ushort)frameId,
                    BgmId = (ushort)(bgmId ?? 0),
                    BgmType = default,
                };

                frameTable.Frames.Add(frameBgmObj);
            }
        }

        // Build new file.
        var outputFile = Path.Join(this.bindDir, "R2", eventFilePath);
        pmd.SavePmd(outputFile);

        Log.Debug($"Event file built.\nFile: {eventFilePath}\nOutput: {outputFile}");
    }
}
