using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.Metaphor;

internal class BgmeService : IBgmeService
{
    private readonly EncounterBgm encounterBgm;

    public BgmeService(MusicService music)
    {
        this.encounterBgm = new(music);
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        this.encounterBgm.Initialize(scanner, hooks);
    }

    public void SetVictoryDisabled(bool isDisabled)
    {
        this.encounterBgm.SetVictoryDisabled(isDisabled);
    }
}
