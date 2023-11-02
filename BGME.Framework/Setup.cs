using CriFs.V2.Hook.Interfaces;
using PersonaMusicScript.Library;
using System.IO.Compression;
using static CriFs.V2.Hook.Interfaces.ICriFsRedirectorApi;

namespace BGME.Framework;

internal static class Setup
{
    public static void Start(ICriFsRedirectorApi criFsApi, string modDir, string game)
    {
        if (game == Game.P5R_PC)
        {
            SetupP5R(criFsApi, modDir);
        }
    }

    private static void SetupP5R(ICriFsRedirectorApi criFsApi, string modDir)
    {
        var dlcBgmDir = Path.Join(modDir, "P5R");
        var dlcAcbFile = Path.Join(dlcBgmDir, "BGM_42.acb");
        var dlcAwbFile = Path.Join(dlcBgmDir, "BGM_42.awb");

        InstallDlcBgm(modDir, dlcBgmDir, dlcAcbFile, dlcAwbFile);
        criFsApi.AddBindCallback(context => OnBindP5R(context, dlcAcbFile, dlcAwbFile));
    }

    private static void InstallDlcBgm(string modDir, string dlcBgmDir, string dlcAcbFile, string dlcAwbFile)
    {
        if (File.Exists(dlcAcbFile) && File.Exists(dlcAwbFile))
        {
            Log.Debug("P5R BGME DLC already installed.");
            return;
        }

        Log.Information("Installing P5R BGME DLC.");
        var dlcBgmZip = Path.Join(modDir, "resources", "p5r-bgme-dlc.zip");
        if (!File.Exists(dlcBgmZip))
        {
            Log.Error($"P5R BGME DLC file not found.\nFile: {dlcBgmZip}");
            return;
        }

        try
        {
            Directory.CreateDirectory(dlcBgmDir);
            ZipFile.ExtractToDirectory(dlcBgmZip, dlcBgmDir, true);
            File.Move($"{dlcAcbFile}.bin", dlcAcbFile);
            File.Move($"{dlcAwbFile}.bin", dlcAwbFile);
            Log.Information("P5R BGME DLC installed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed install P5R BGME DLC.");
            Log.Information("Manually unzip P5R BGME DLC if this always fails.");
            Log.Information($"P5R BGME DLC Zip: {dlcBgmZip}");
            Log.Information($"Unzip To: {dlcBgmDir}");
        }
    }

    private static void OnBindP5R(BindContext context, string dlcAcbFile, string dlcAwbFile)
    {
        var dlcBgmAcb = @"R2\SOUND\BGM_42.acb";
        var dlcBgmAwb = @"R2\SOUND\BGM_42.awb";

        context.RelativePathToFileMap[dlcBgmAcb] = new()
        {
            new()
            {
                FullPath = dlcAcbFile,
                LastWriteTime = DateTime.UtcNow,
                ModId = "BGME Framework",
            }
        };

        context.RelativePathToFileMap[dlcBgmAwb] = new()
        {
            new()
            {
                FullPath = dlcAwbFile,
                LastWriteTime = DateTime.UtcNow,
                ModId = "BGME Framework",
            }
        };
    }
}
