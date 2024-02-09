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
using Reloaded.Mod.Interfaces.Internal;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;

namespace BGME.Framework;

public class Mod : ModBase, IExports
{
    private static readonly Regex numReg = new(@"\d+");

    private readonly IModLoader modLoader;
    private readonly IReloadedHooks hooks;
    private readonly ILogger logger;
    private readonly IMod owner;
    private readonly IModConfig modConfig;
    private readonly Config config;

    private readonly ICriFsRedirectorApi criFsApi;
    private readonly IP5RLib p5rLib;
    private readonly IBgmeService? bgme;
    private readonly Game game;
    private readonly MusicService? music;
    private readonly MusicScriptsManager musicScripts = new();
    private readonly CriAtomEx? criAtomEx;

    public Mod(ModContext context)
    {
        this.modLoader = context.ModLoader;
        this.hooks = context.Hooks!;
        this.logger = context.Logger;
        this.owner = context.Owner;
        this.config = context.Configuration;
        this.modConfig = context.ModConfig;

        Log.Initialize("BGME Framework", this.logger, Color.AliceBlue);
        Log.LogLevel = this.config.LogLevel;

#if DEBUG
        Debugger.Launch();
#endif

        var appId = this.modLoader.GetAppConfig().AppId;
        this.game = GetGame(this.modLoader.GetAppConfig().AppId);

        this.modLoader.GetController<IStartupScanner>().TryGetTarget(out var scanner);
        this.modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out this.criFsApi!);
        this.modLoader.GetController<IP5RLib>().TryGetTarget(out this.p5rLib!);

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
                this.bgme = new P5R.BgmeService(this.p5rLib, this.criAtomEx, this.music);
                this.bgme.Initialize(scanner!, hooks);
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
        if (Directory.Exists(bgmeDir))
        {
            this.musicScripts.AddPath(bgmeDir);
        }

        // Bind FEmulator/AWB with CriFs.
        if (this.game == Game.P5R_PC)
        {
            Log.Debug("Binding BGM_42.AWB files.");

            var awbDir = Path.Join(modDir, "FEmulator", "AWB", "BGM_42.AWB");
            if (Directory.Exists(awbDir))
            {
                foreach (var file in Directory.EnumerateFiles(awbDir, "*.adx"))
                {
                    var fileNameIndex = int.Parse(Path.GetFileNameWithoutExtension(file).Split('_')[0]);
                    var bindPath = $"BGME/P5R/BGM_42/{fileNameIndex}.adx";
                    this.criFsApi.AddBind(file, bindPath, "BGME.Framework");
                }
            }

            var awbDir2 = Path.Join(modDir, "bgme", "p5r");
            if (Directory.Exists(awbDir2))
            {
                foreach (var file in Directory.EnumerateFiles(awbDir2, "*.adx"))
                {
                    var fileNameIndex = int.Parse(Path.GetFileNameWithoutExtension(file).Split('_')[0]);
                    var bindPath = Path.GetRelativePath(modDir, file);
                    this.criFsApi.AddBind(file, bindPath, "BGME.Framework");
                }
            }
        }
        else if (this.game == Game.P4G_PC)
        {
            Log.Debug("Binding BGME P4G music.");

            var p4gMusicDir = Path.Join(modDir, "bgme", "p4g");
            if (Directory.Exists(p4gMusicDir))
            {
                foreach (var file in Directory.EnumerateFiles(p4gMusicDir, "*.hca"))
                {
                    var bindPath = Path.GetRelativePath(modDir, file);
                    this.criFsApi.AddBind(file, bindPath, "BGME.Framework");
                }
            }

            var p4gAwbDir = Path.Join(modDir, "FEmulator", "AWB", "snd00_bgm.awb");
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
