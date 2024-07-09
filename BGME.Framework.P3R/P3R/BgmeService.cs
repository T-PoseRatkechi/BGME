using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Ryo.Interfaces;

namespace BGME.Framework.P3R.P3R;

internal class BgmeService : IBgmeService
{
    private readonly EncounterBgm encounterBgm;
    private readonly Sound bgm;

    public BgmeService(ICriAtomEx criAtomEx, IRyoUtils ryoUtils, MusicService music)
    {
        this.bgm = new(criAtomEx, ryoUtils, music);
        this.encounterBgm = new(music);
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        this.bgm.Initialize(scanner, hooks);
        this.encounterBgm.Initialize(scanner, hooks);
    }

    public void SetVictoryDisabled(bool isDisabled)
    {
        this.bgm.SetVictoryDisabled(isDisabled);
        this.encounterBgm.SetVictoryDisabled(isDisabled);
    }
}
