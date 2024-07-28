using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Ryo.Interfaces;
using SharedScans.Interfaces;

namespace BGME.Framework.P4G;

internal class BgmeService : IBgmeService, IGameHook
{
    private readonly MusicService music;

    private readonly Sound sound;
    private readonly EncounterBgm encounterPatcher;
    private readonly FloorBgm floorPatcher;
    private readonly EventBgm eventBgm;

    public BgmeService(
        ISharedScans scans,
        ICriAtomRegistry criAtomRegistry,
        MusicService music)
    {
        this.music = music;

        this.sound = new(scans, criAtomRegistry, this.music);
        this.encounterPatcher = new(music);
        this.floorPatcher = new(music);
        this.eventBgm = new(this.sound, music);
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        this.sound.Initialize(scanner, hooks);
        this.encounterPatcher.Initialize(scanner, hooks);
        this.floorPatcher.Initialize(scanner, hooks);
        this.eventBgm.Initialize(scanner, hooks);
    }

    public void SetVictoryDisabled(bool isDisabled)
    {
        this.sound.SetVictoryDisabled(isDisabled);
        this.encounterPatcher.SetVictoryDisabled(isDisabled);
    }
}
