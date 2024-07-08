using BGME.Framework.Music;
using BGME.Framework.P5R.Rhythm;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P5R;

internal class BgmeService : IBgmeService
{
    private readonly IP5RLib p5rLib;
    private readonly BgmPlayback bgm;
    private readonly EncounterBgm encounterBgm;

    private readonly RhythmGame? rhythmGame;

    public BgmeService(IP5RLib p5rLib, MusicService music)
    {
        this.p5rLib = p5rLib;
        this.bgm = new(music);
        this.encounterBgm = new(music);
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        this.bgm.Initialize(scanner, hooks);
        this.encounterBgm.Initialize(scanner, hooks);
    }

    public void SetVictoryDisabled(bool isDisabled)
    {
        Log.Debug($"Disable Victory BGM: {isDisabled}");
        this.bgm.SetVictoryDisabled(isDisabled);
        this.encounterBgm.SetVictoryDisabled(isDisabled);
    }
}
