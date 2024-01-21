using BGME.Framework.Interfaces;
using BGME.Framework.Music;
using BGME.Framework.Template;
using BGME.Framework.Template.Configuration;
using CriFs.V2.Hook.Interfaces;
using PersonaModdingMetadata.Shared.Games;
using PersonaMusicScript.Types;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;

namespace BGME.Framework;

public class Mod : ModBase, IExports
{
    private readonly IModLoader modLoader;
    private readonly IReloadedHooks hooks;
    private readonly ILogger logger;
    private readonly IMod owner;
    private readonly IModConfig modConfig;
    private readonly Config config;

    private readonly ICriFsRedirectorApi criFsApi;
    private readonly IBgmeService? bgme;
    private readonly Game game;
    private readonly MusicService? music;
    private readonly MusicScriptsManager musicScripts = new();

    public Mod(ModContext context)
    {
        this.modLoader = context.ModLoader;
        this.hooks = context.Hooks!;
        this.logger = context.Logger;
        this.owner = context.Owner;
        this.config = context.Configuration;
        this.modConfig = context.ModConfig;

        Log.Logger = this.logger;
        Log.LogLevel = this.config.LogLevel;

#if DEBUG
        Debugger.Launch();
#endif

        var appId = this.modLoader.GetAppConfig().AppId;
        this.game = GetGame(this.modLoader.GetAppConfig().AppId);

        this.modLoader.GetController<IStartupScanner>().TryGetTarget(out var scanner);
        this.modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out this.criFsApi!);

        var modDir = this.modLoader.GetDirectoryForModId(this.modConfig.ModId);
        Setup.Start(this.criFsApi, modDir, game);

        var musicResources = new MusicResources(game, modDir);
        var fileBuilder = GetGameBuilder(this.criFsApi, modDir, game);

        this.music = new(musicResources, this.musicScripts, fileBuilder, this.config.HotReload);
        this.modLoader.AddOrReplaceController<IBgmeApi>(this.owner, musicScripts);

        this.modLoader.ModLoading += this.OnModLoading;
        this.modLoader.OnModLoaderInitialized += () =>
        {
            fileBuilder?.Build(this.music);
        };

        switch (game)
        {
            case Game.P4G_PC:
                this.bgme = new P4G.BgmeService(this.hooks, scanner!, this.music);
                break;
            case Game.P3P_PC:
                this.bgme = new P3P.BgmeService(this.hooks, scanner!, this.music);
                break;
            case Game.P5R_PC:
                this.bgme = new P5R.BgmeService(this.hooks, scanner!, this.music);
                break;
            default:
                Log.Error($"Missing BGME service for game {game}.");
                break;
        }
    }

    private void OnModLoading(IModV1 mod, IModConfigV1 config)
    {
        //if (!config.ModDependencies.Contains(this.modConfig.ModId))
        //{
        //    return;
        //}

        var modDir = this.modLoader.GetDirectoryForModId(config.ModId);
        var bgmeDir = Path.Join(modDir, "bgme");
        if (Directory.Exists(bgmeDir))
        {
            this.musicScripts.AddPath(bgmeDir);
        }

        // Bind FEmulator/AWB with CriFs.
        if (this.game == Game.P5R_PC)
        {
            Log.Debug("Binding BGM_42.AWB files.");
            var awbDir = Path.Join(modDir, "FEmulator", "AWB", "BGM_42.AWB");
            if (!Directory.Exists(awbDir))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(awbDir, "*.adx"))
            {
                var fileNameIndex = int.Parse(Path.GetFileNameWithoutExtension(file).Split('_')[0]);
                var bindPath = $"FEmulator/AWB/BGM_42.AWB/{fileNameIndex}.adx";
                this.criFsApi.AddBind(file, bindPath, "BGME.Framework");
            }
        }
    }

    private static Game GetGame(string appId)
    {
        if (appId.Contains("p5r", StringComparison.OrdinalIgnoreCase))
        {
            return Game.P5R_PC;
        }
        else if (appId.Contains("p4g", StringComparison.OrdinalIgnoreCase))
        {
            return Game.P4G_PC;
        }
        else
        {
            return Game.P3P_PC;
        }
    }

    private static IFileBuilder? GetGameBuilder(ICriFsRedirectorApi criFsApi, string modDir, Game game)
    {
        return game switch
        {
            Game.P4G_PC => new PmdFileMerger(criFsApi, modDir),
            _ => null
        };
    }

    public Type[] GetTypes() => new[] { typeof(IBgmeApi) };

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}

internal static class ICriFsRedirectorApiExtensions
{
    public static void AddBind(
        this ICriFsRedirectorApi api,
        string file,
        string bindPath,
        string modId)
    {
        api.AddBindCallback(context =>
        {
            context.RelativePathToFileMap[$@"R2\{bindPath}"] = new()
            {
                new()
                {
                    FullPath = file,
                    LastWriteTime = DateTime.UtcNow,
                    ModId = modId,
                },
            };

            Log.Debug($"Bind: {bindPath}\nFile: {file}");
        });
    }
}