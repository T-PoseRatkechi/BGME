using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P3P;

internal class BgmeService : IBgmeService
{
    private readonly SoundPatcher soundPatcher;

    public BgmeService(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
    {
        this.soundPatcher = new(hooks, scanner);
    }
}
