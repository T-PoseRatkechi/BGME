using BGME.Framework.API.Music;
using BGME.Framework.API.Template;
using BGME.Framework.Interfaces;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using System.Diagnostics;
using System.Drawing;

namespace BGME.Framework.API;

public class Mod : ModBase, IExports
{
    private readonly IModLoader modLoader;
    private readonly IReloadedHooks? hooks;
    private readonly ILogger log;
    private readonly IMod owner;

    private Config config;
    private readonly IModConfig modConfig;
    private readonly MusicScriptsManager musicScripts = new();

    public Mod(ModContext context)
    {
        this.modLoader = context.ModLoader;
        this.hooks = context.Hooks;
        this.log = context.Logger;
        this.owner = context.Owner;
        this.config = context.Configuration;
        this.modConfig = context.ModConfig;

#if DEBUG
        Debugger.Launch();
#endif

        Log.Initialize("BGME Framework API", this.log, Color.LightBlue);
        Log.LogLevel = this.config.LogLevel;

        this.modLoader.AddOrReplaceController<IBgmeApi>(this.owner, this.musicScripts);
        this.modLoader.ModLoading += this.OnModLoading;
    }

    private void OnModLoading(IModV1 mod, IModConfigV1 config)
    {
        var modDir = this.modLoader.GetDirectoryForModId(config.ModId);
        this.musicScripts.AddBgmeMod(config.ModId, modDir);
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        config = configuration;
        log.WriteLine($"[{modConfig.ModId}] Config Updated: Applying");
        Log.LogLevel = this.config.LogLevel;
    }

    public Type[] GetTypes() => new Type[] { typeof(IBgmeApi) };

    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}