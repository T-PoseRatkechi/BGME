﻿using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Ryo.Interfaces;

namespace BGME.Framework.P4G;

internal class BgmeService : IBgmeService, IGameHook
{
    private readonly MusicService music;

    private readonly Sound sound;
    private readonly EncounterBgm encounterPatcher;
    private readonly FloorBgm floorPatcher;
    private readonly EventBgm eventBgm;
    private LegacySound? legacySound;

    public BgmeService(ICriAtomEx criAtomEx, MusicService music)
    {
        this.music = music;

        this.sound = new(criAtomEx, this.music);
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

        // Legacy BGM handler for new BGM in snd00_bgm.
        this.legacySound = new LegacySound(hooks, scanner, this.music);
    }

    public void SetVictoryDisabled(bool isDisabled)
    {
        this.sound.SetVictoryDisabled(isDisabled);
        this.encounterPatcher.SetVictoryDisabled(isDisabled);
    }
}
