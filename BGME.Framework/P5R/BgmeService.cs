using BGME.Framework.CRI;
using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P5R;

internal class BgmeService : IBgmeService, IGameHook
{
    private readonly SoundPlayback sound;
    private readonly EncounterBgm encounterBgm;

    public BgmeService(CriAtomEx criAtomEx, MusicService music)
    {
        this.sound = new(criAtomEx, music);
        this.encounterBgm = new(music);
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        this.sound.Initialize(scanner, hooks);
        this.encounterBgm.Initialize(scanner, hooks);
    }
}
