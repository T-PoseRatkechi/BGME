using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P3P;

internal class BgmeService : IBgmeService
{
    private readonly SoundPatcher soundPatcher;
    private readonly EncounterBgm encounterPatcher;
    private readonly FloorBgm floorBgm;

    public BgmeService(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
    {
        this.soundPatcher = new(hooks, scanner, music);
        this.encounterPatcher = new(hooks, scanner, music);
        this.floorBgm = new(hooks, scanner, music);
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
    }
}
