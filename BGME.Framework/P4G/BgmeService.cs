using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P4G;

internal class BgmeService : IBgmeService
{
    private readonly SoundPatcher soundPatcher;
    private readonly EncounterBgm encounterPatcher;
    private readonly FloorBgm floorPatcher;

    public BgmeService(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
    {
        this.soundPatcher = new(hooks, music);
        this.encounterPatcher = new(hooks, scanner, music);
        this.floorPatcher = new(hooks, scanner, music);
    }
}
