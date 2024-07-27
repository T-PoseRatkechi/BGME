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
using Ryo.Interfaces;
using SharedScans.Interfaces;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;

namespace BGME.Framework;

public class Mod : ModBase
{
    public static string NAME = "BGME.Framework";
    private static readonly Regex numReg = new(@"\d+");

    private readonly IModLoader modLoader;
    private readonly IReloadedHooks hooks;
    private readonly ILogger logger;
    private readonly IMod owner;
    private readonly IModConfig modConfig;
    private Config config;

    private readonly IBgmeApi bgmeApi;
    private readonly ICriFsRedirectorApi criFsApi;

    private readonly IBgmeService bgme;
    private readonly Game game;
    private readonly MusicService? music;
    private readonly IRyoApi ryo;
    private bool foundDisableVictoryMod;

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
        this.modLoader.GetController<IRyoApi>().TryGetTarget(out this.ryo!);
        this.modLoader.GetController<ICriAtomEx>().TryGetTarget(out var criAtomEx);
        this.modLoader.GetController<ICriAtomRegistry>().TryGetTarget(out var criAtomRegistry);
        this.modLoader.GetController<ISharedScans>().TryGetTarget(out var scans);

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
                this.bgme = new P4G.BgmeService(scans!, criAtomRegistry!, this.music);
                this.bgme.Initialize(scanner!, hooks);
                break;
            case Game.P3P_PC:
                this.bgme = new P3P.BgmeService(this.hooks, scanner!, this.ryo, criAtomEx!, criAtomRegistry!, this.music);
                break;
            case Game.P5R_PC:
                this.modLoader.GetController<IP5RLib>().TryGetTarget(out var p5rLib);
                this.bgme = new P5R.BgmeService(p5rLib!, this.music);
                this.bgme.Initialize(scanner!, hooks);
                break;
            default:
                throw new Exception($"Missing BGME service for game {game}.");
        }

        this.ApplyConfig();
    }

    private void OnBgmeModLoading(BgmeMod mod)
    {
        if (this.game == Game.P5R_PC)
        {
            var femuAwbDir_P5R = Path.Join(mod.ModDir, "FEmulator", "AWB", "BGM_42.AWB");
            if (Directory.Exists(femuAwbDir_P5R))
            {
                // For legacy BGM files in BGM_42, assign them to Cue IDs 10,000 more
                // than their AWB index.
                foreach (var file in Directory.EnumerateFiles(femuAwbDir_P5R, "*.adx"))
                {
                    var ryoCueId = GetAwbIndex(file) + 10000;
                    this.ryo.AddAudioPath(file, new()
                    {
                        CueName = ryoCueId.ToString(),
                        AcbName = "bgm",
                        CategoryIds = new int[] { 1, 8 },
                    });
                }
            }

            var bgmeAudioDir_P5R = Path.Join(mod.ModDir, "BGME", "P5R");
            if (Directory.Exists(bgmeAudioDir_P5R))
            {
                this.ryo.AddAudioPath(bgmeAudioDir_P5R, new() { AcbName = "bgm", CategoryIds = new int[] { 1, 8 } });
            }
        }
        else if (this.game == Game.P4G_PC)
        {
            // For legacy BGM files using unused AWB indexes in 'snd00_bgm.awb'.
            var femuAwbDir_P4G = Path.Join(mod.ModDir, "FEmulator", "AWB", "snd00_bgm.awb");
            if (Directory.Exists(femuAwbDir_P4G))
            {
                foreach (var file in Directory.EnumerateFiles(femuAwbDir_P4G, "*.hca"))
                {
                    var awbIndex = GetAwbIndex(file);
                    if (awbIndex >= 678 && awbIndex <= 835)
                    {
                        this.ryo.AddAudioPath(file, new() { AcbName = "snd00_bgm", CategoryIds = new int[] { 6, 13 } });
                    }
                }
            }

            var bgmeAudioDir_P4G = Path.Join(mod.ModDir, "BGME", "P4G");
            if (Directory.Exists(bgmeAudioDir_P4G))
            {
                this.ryo.AddAudioPath(bgmeAudioDir_P4G, new() { AcbName = "snd00_bgm", CategoryIds = new int[] { 6, 13 } });
            }
        }
        else if (this.game == Game.P3P_PC)
        {
            var bgmeAudioDir_P3P = Path.Join(mod.ModDir, "BGME", "P3P");
            if (Directory.Exists(bgmeAudioDir_P3P))
            {
                foreach (var file in Directory.EnumerateFiles(bgmeAudioDir_P3P, "*.hca"))
                {
                    this.ryo.AddAudioPath(file, new()
                    {
                        AudioFilePath = $"data/sound/bgm/{GetAwbIndex(file)}.adx"
                    });
                }
            }
        }

        if (mod.ModId == "BGME.DisableVictoryTheme")
        {
            this.foundDisableVictoryMod = true;
            this.bgme.SetVictoryDisabled(true);
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

    private void ApplyConfig()
    {
        Log.LogLevel = this.config.LogLevel;
        if (this.config.DisableVictoryBgm || this.foundDisableVictoryMod)
        {
            this.bgme.SetVictoryDisabled(true);
        }
        else
        {
            this.bgme.SetVictoryDisabled(false);
        }
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        this.config = configuration;
        logger.WriteLine($"[{modConfig.ModId}] Config Updated: Applying");
        this.ApplyConfig();
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}
