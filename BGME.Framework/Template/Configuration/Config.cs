namespace BGME.Framework.Template.Configuration;

using System.ComponentModel;

public class Config : Configurable<Config>
{
    [DisplayName("Hot Reload")]
    [Description("Rebuild game files on music script changes.")]
    [DefaultValue(false)]
    public bool HotReload { get; set; }

    [DisplayName("Log Level")]
    [DefaultValue(LogLevel.Information)]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    [DisplayName("Disable Victory Theme")]
    [DefaultValue(false)]
    public bool DisableVictoryBgm { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}