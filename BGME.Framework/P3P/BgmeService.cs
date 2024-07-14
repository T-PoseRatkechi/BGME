using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Ryo.Interfaces;

namespace BGME.Framework.P3P;

internal class BgmeService : IBgmeService
{
    private readonly Sound soundPatcher;
    private readonly EncounterBgm encounterPatcher;
    private readonly FloorBgm floorBgm;

    public BgmeService(
        IReloadedHooks hooks,
        IStartupScanner scanner,
        IRyoApi ryo,
        ICriAtomEx criAtomEx,
        ICriAtomRegistry criAtomRegistry,
        MusicService music)
    {
        this.soundPatcher = new(hooks, scanner, ryo, criAtomEx, criAtomRegistry, music);
        this.encounterPatcher = new(hooks, scanner, music);
        this.floorBgm = new(hooks, scanner, music);
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
    }

    public void SetVictoryDisabled(bool isDisabled)
    {
        this.soundPatcher.SetVictoryDisabled(isDisabled);
        this.encounterPatcher.SetVictoryDisabled(isDisabled);
    }
}
