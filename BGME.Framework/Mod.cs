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
    private static readonly Dictionary<string, Game> Games = new(StringComparer.OrdinalIgnoreCase)
    {
        ["p4g.exe"] = Game.P4G_PC,
        ["p5r.exe"] = Game.P5R_PC,
        ["p3p.exe"] = Game.P3P_PC,
    };

    private readonly IModLoader modLoader;
    private readonly IReloadedHooks hooks;
    private readonly ILogger logger;
    private readonly IMod owner;
    private readonly IModConfig modConfig;
    private readonly Config config;

    private readonly IBgmeService? bgme;
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
        if (!Games.TryGetValue(appId, out var game))
        {
            Log.Error($"Unsupported app id {appId}.");
            return;
        }

        this.modLoader.GetController<IStartupScanner>().TryGetTarget(out var scanner);
        this.modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out var criFsApi);

        var modDir = this.modLoader.GetDirectoryForModId(this.modConfig.ModId);
        Setup.Start(criFsApi!, modDir, game);

        var musicResources = new MusicResources(game, modDir);
        var fileBuilder = GetGameBuilder(criFsApi!, modDir, game);

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
        if (!config.ModDependencies.Contains(this.modConfig.ModId))
        {
            return;
        }

        var modDir = this.modLoader.GetDirectoryForModId(config.ModId);
        var bgmeDir = Path.Join(modDir, "bgme");
        if (!Directory.Exists(bgmeDir))
        {
            return;
        }

        this.musicScripts.AddPath(bgmeDir);
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