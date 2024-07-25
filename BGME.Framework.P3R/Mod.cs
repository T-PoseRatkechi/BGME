using BGME.Framework.Interfaces;
using BGME.Framework.Music;
using BGME.Framework.P3R.Configuration;
using BGME.Framework.P3R.P3R;
using BGME.Framework.P3R.Template;
using PersonaModdingMetadata.Shared.Games;
using PersonaMusicScript.Types;
using Project.Utils;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Ryo.Interfaces;
using System.Diagnostics;
using System.Drawing;

namespace BGME.Framework.P3R;

public class Mod : ModBase
{
    private readonly IModLoader modLoader;
    private readonly IReloadedHooks hooks;
    private readonly ILogger log;
    private readonly IMod owner;

    private Config config;
    private readonly IModConfig modConfig;

    private readonly IRyoApi ryo;
    private readonly IBgmeApi bgmeApi;
    private readonly IBgmeService bgme;
    private bool foundDisableVictoryMod;

    public Mod(ModContext context)
    {
        this.modLoader = context.ModLoader;
        this.hooks = context.Hooks!;
        this.log = context.Logger;
        this.owner = context.Owner;
        this.config = context.Configuration;
        this.modConfig = context.ModConfig;

#if DEBUG
        Debugger.Launch();
#endif

        Log.Initialize("BGME Framework", this.log, Color.LightBlue);
        Log.LogLevel = this.config.LogLevel;

        this.modLoader.GetController<IBgmeApi>().TryGetTarget(out this.bgmeApi!);
        this.modLoader.GetController<IRyoApi>().TryGetTarget(out this.ryo!);
        this.modLoader.GetController<ICriAtomEx>().TryGetTarget(out var criAtomEx);
        this.modLoader.GetController<IStartupScanner>().TryGetTarget(out var scanner);

        var modDir = this.modLoader.GetDirectoryForModId(this.modConfig.ModId);
        var musicResources = new MusicResources(Game.P3R_PC, modDir);
        var music = new MusicService(musicResources, this.bgmeApi, null, false);

        // Register music from BGME mods.
        this.bgmeApi!.BgmeModLoading += this.OnBgmeModLoading;
        foreach (var mod in this.bgmeApi.GetLoadedMods())
        {
            this.OnBgmeModLoading(mod);
        }

        this.bgme = new BgmeService(criAtomEx!, music);
        this.bgme.Initialize(scanner!, this.hooks);

        this.ApplyConfig();
    }

    private void OnBgmeModLoading(BgmeMod mod)
    {
        var bgmeMusicDir = Path.Join(mod.ModDir, "bgme", "p3r");
        if (Directory.Exists(bgmeMusicDir))
        {
            this.ryo.AddAudioPath(bgmeMusicDir, new() { CategoryIds = new int[] { 0, 13 } });
        }

        if (mod.ModId == "BGME.DisableVictoryTheme")
        {
            this.foundDisableVictoryMod = true;
            this.bgme.SetVictoryDisabled(true);
        }
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
        config = configuration;
        log.WriteLine($"[{modConfig.ModId}] Config Updated: Applying");
        this.ApplyConfig();
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}