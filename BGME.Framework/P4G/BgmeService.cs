using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P4G;

internal class BgmeService : IBgmeService
{
    private readonly Sound sound;
    private readonly EncounterBgm encounterPatcher;
    private readonly FloorBgm floorPatcher;
    private readonly EventBgm eventBgm;

    public BgmeService(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
    {
        this.sound = new(hooks, scanner, music);
        this.encounterPatcher = new(hooks, scanner, music);
        this.floorPatcher = new(hooks, scanner, music);
        this.eventBgm = new(hooks, scanner, this.sound, music);
    }
}
