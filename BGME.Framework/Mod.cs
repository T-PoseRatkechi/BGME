using BGME.Framework.CRI;
using BGME.Framework.Interfaces;
using BGME.Framework.Music;
using BGME.Framework.Template;
using BGME.Framework.Template.Configuration;
using CriFs.V2.Hook.Interfaces;
using p5rpc.lib.interfaces;
using PersonaModdingMetadata.Shared.Games;
using PersonaMusicScript.Types;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;

namespace BGME.Framework;

public class Mod : ModBase
{
    private static readonly Regex numReg = new(@"\d+");

    private readonly IModLoader modLoader;
    private readonly IReloadedHooks hooks;
    private readonly ILogger logger;
    private readonly IMod owner;
    private readonly IModConfig modConfig;
    private readonly Config config;

    private readonly IBgmeApi bgmeApi;
    private readonly ICriFsRedirectorApi criFsApi;

    private readonly IBgmeService? bgme;
    private readonly Game game;
    private readonly MusicService? music;
    private readonly CriAtomEx? criAtomEx;

    public Mod(ModContext context)
    {
        this.modLoader = context.ModLoader;
        this.hooks = context.Hooks!;
        this.logger = context.Logger;
        this.owner = context.Owner;
        this.config = context.Configuration;
        this.modConfig = context.ModConfig;

        Log.Initialize("BGME Framework", this.logger, Color.LightBlue);
        Log.LogLevel = this.config.LogLevel;

#if DEBUG
        Debugger.Launch();
#endif

        var appId = this.modLoader.GetAppConfig().AppId;
        this.game = GetGame(this.modLoader.GetAppConfig().AppId);

        this.modLoader.GetController<IStartupScanner>().TryGetTarget(out var scanner);
        this.modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out this.criFsApi!);

        var modDir = this.modLoader.GetDirectoryForModId(this.modConfig.ModId);

        var musicResources = new MusicResources(game, modDir);
        var fileBuilder = GetGameBuilder(this.criFsApi, modDir, game);

        this.modLoader.GetController<IBgmeApi>().TryGetTarget(out this.bgmeApi!);
        this.music = new(musicResources, bgmeApi!, fileBuilder, this.config.HotReload);

        this.modLoader.OnModLoaderInitialized += () =>
        {
            fileBuilder?.Build(this.music);
        };

        this.bgmeApi.BgmeModLoading += this.OnBgmeModLoading;
        foreach (var mod in this.bgmeApi.GetLoadedMods())
        {
            this.OnBgmeModLoading(mod);
        }

        switch (game)
        {
            case Game.P4G_PC:
                this.criAtomEx = new CriAtomEx(game);
                this.criAtomEx.Initialize(scanner!, this.hooks);
                this.bgme = new P4G.BgmeService(this.config, this.criAtomEx, this.music);
                this.bgme.Initialize(scanner!, hooks);
                break;
            case Game.P3P_PC:
                this.bgme = new P3P.BgmeService(this.hooks, scanner!, this.music);
                break;
            case Game.P5R_PC:
                this.criAtomEx = new CriAtomEx(game);
                this.criAtomEx.Initialize(scanner!, this.hooks);
                this.modLoader.GetController<IP5RLib>().TryGetTarget(out var p5rLib);
                this.bgme = new P5R.BgmeService(p5rLib!, this.criAtomEx, this.music);
                this.bgme.Initialize(scanner!, hooks);
                break;
            default:
                Log.Error($"Missing BGME service for game {game}.");
                break;
        }
    }

    private void OnBgmeModLoading(BgmeMod mod)
    {
        // Bind FEmulator/AWB with CriFs.
        if (this.game == Game.P5R_PC)
        {
            var awbDir = Path.Join(mod.ModDir, "FEmulator", "AWB", "BGM_42.AWB");
            if (Directory.Exists(awbDir))
            {
                Log.Debug("Binding BGM_42.AWB files.");
                foreach (var file in Directory.EnumerateFiles(awbDir, "*.adx"))
                {
                    var fileNameIndex = int.Parse(Path.GetFileNameWithoutExtension(file).Split('_')[0]);
                    var bindPath = $"BGME/P5R/BGM_42/{fileNameIndex}.adx";
                    this.criFsApi.AddBind(file, bindPath, "BGME.Framework");
                }
            }

            var awbDir2 = Path.Join(mod.ModDir, "bgme", "p5r");
            if (Directory.Exists(awbDir2))
            {
                Log.Debug("Binding P5R files.");
                foreach (var file in Directory.EnumerateFiles(awbDir2, "*.adx"))
                {
                    var fileNameIndex = int.Parse(Path.GetFileNameWithoutExtension(file).Split('_')[0]);
                    var bindPath = $"BGME/P5R/{fileNameIndex}.adx";
                    this.criFsApi.AddBind(file, bindPath, "BGME.Framework");
                }
            }
        }
        else if (this.game == Game.P4G_PC)
        {
            Log.Debug("Binding BGME P4G music.");

            var p4gMusicDir = Path.Join(mod.ModDir, "bgme", "p4g");
            if (Directory.Exists(p4gMusicDir))
            {
                foreach (var file in Directory.EnumerateFiles(p4gMusicDir, "*.hca"))
                {
                    var bindPath = Path.GetRelativePath(mod.ModDir, file);
                    this.criFsApi.AddBind(file, bindPath, "BGME.Framework");
                }
            }

            var p4gAwbDir = Path.Join(mod.ModDir, "FEmulator", "AWB", "snd00_bgm.awb");
            if (Directory.Exists(p4gAwbDir))
            {
                foreach (var file in Directory.EnumerateFiles(p4gAwbDir, "*.hca"))
                {
                    var awbIndex = GetAwbIndex(file);
                    if (awbIndex >= 678)
                    {
                        this.criFsApi.AddBind(file, $"BGME/P4G/{awbIndex}.hca", "BGME.Framework");
                    }
                }
            }
        }
    }

    private static int GetAwbIndex(string file)
    {
        var index = int.Parse(numReg.Match(Path.GetFileNameWithoutExtension(file)).Groups[0].Value);
        return index;
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
        else if (appId.Contains("p3r", StringComparison.OrdinalIgnoreCase))
        {
            return Game.P3R_PC;
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

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        logger.WriteLine($"[{modConfig.ModId}] Config Updated: Applying");
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}
