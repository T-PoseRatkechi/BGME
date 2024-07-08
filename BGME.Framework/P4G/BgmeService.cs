using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P4G;

internal class BgmeService : IBgmeService, IGameHook
{
    private readonly MusicService music;

    private readonly BgmPlayback bgm;
    private readonly EncounterBgm encounterPatcher;
    private readonly FloorBgm floorPatcher;
    private readonly EventBgm eventBgm;
    private LegacySound? sound;

    public BgmeService(MusicService music)
    {
        this.music = music;

        this.bgm = new(this.music);
        this.encounterPatcher = new(music);
        this.floorPatcher = new(music);
        this.eventBgm = new(this.bgm, music);
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        this.bgm.Initialize(scanner, hooks);
        this.encounterPatcher.Initialize(scanner, hooks);
        this.floorPatcher.Initialize(scanner, hooks);
        this.eventBgm.Initialize(scanner, hooks);
        this.sound = new(hooks, scanner, this.music);
    }

    public void SetVictoryDisabled(bool isDisabled)
    {
        this.bgm.SetVictoryDisabled(isDisabled);
        this.encounterPatcher.SetVictoryDisabled(isDisabled);
    }
}
