using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P5R;

internal class BgmeService : IBgmeService
{
    private readonly Sound sound;
    private readonly EncounterBgm encounterBgm;

    public BgmeService(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
    {
        this.sound = new(hooks, scanner, music);
        this.encounterBgm = new(hooks, scanner, this.sound, music);
    }
}
