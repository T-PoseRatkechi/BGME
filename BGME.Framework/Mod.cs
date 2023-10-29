using BGME.Framework.Music;
using BGME.Framework.Template;
using PersonaMusicScript.Library;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using System.Diagnostics;

namespace BGME.Framework;

public class Mod : ModBase
{
    private static readonly Dictionary<string, string> Games = new(StringComparer.OrdinalIgnoreCase)
    {
        ["p4g.exe"] = Game.P4G_PC,
        ["p5r.exe"] = Game.P5R_PC,
        ["p3p.exe"] = Game.P3P_PC,
    };

    private readonly IModLoader _modLoader;
    private readonly IReloadedHooks _hooks;
    private readonly Reloaded.Mod.Interfaces.ILogger _logger;
    private readonly IMod _owner;
    private readonly IModConfig _modConfig;

    private readonly IBgmeService? bgme;
    private readonly MusicService? music;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks ?? throw new Exception("ReloadedHooks is null.");
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;

        Log.Logger = this._logger;
        Log.LoggerLevel = LogLevel.Debug;

#if DEBUG
        Debugger.Launch();
#endif

        var appId = this._modLoader.GetAppConfig().AppId;
        if (!Games.TryGetValue(appId, out var game))
        {
            Log.Error($"Unsupported app id {appId}.");
            return;
        }

        var scannerController = this._modLoader.GetController<IStartupScanner>();
        if (scannerController == null
            || !scannerController.TryGetTarget(out var scanner))
        {
            Log.Error("Failed to get startup scanner.");
            return;
        }

        var modDir = this._modLoader.GetDirectoryForModId(this._modConfig.ModId);
        var resourcesDir = Path.Join(modDir, "resources");
        var musicParser = new MusicParser(game, resourcesDir);

        this.music = new(musicParser);
        this._modLoader.ModLoading += OnModLoading;

        switch (game)
        {
            case Game.P4G_PC:
                this.bgme = new P4G.BgmeService(this._hooks, scanner, this.music);
                break;
            case Game.P3P_PC:
                this.bgme = new P3P.BgmeService(this._hooks, scanner, this.music);
                break;
            case Game.P5R_PC:
                this.bgme = new P5R.BgmeService(this._hooks, scanner, this.music);
                break;
            default:
                Log.Error($"Missing BGME service for game {game}.");
                break;
        }
    }

    private void OnModLoading(IModV1 mod, IModConfigV1 config)
    {
        if (!config.ModDependencies.Contains(this._modConfig.ModId))
        {
            return;
        }

        var modDir = this._modLoader.GetDirectoryForModId(config.ModId);
        var bgmeDir = Path.Join(modDir, "bgme");
        if (!Directory.Exists(bgmeDir))
        {
            return;
        }

        this.music?.AddMusicFolder(bgmeDir);
    }

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}