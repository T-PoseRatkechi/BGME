using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P5R;

internal unsafe class Sound : BaseSound
{
    private readonly DlcBgmHook dlcBgmHook;
    private readonly SoundPlayback playback;

    public Sound(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
        : base(music)
    {
        this.dlcBgmHook = new(scanner, hooks);
        this.playback = new(scanner, hooks, music);
    }

    protected override void PlayBgm(int bgmId)
    {
        //this.PlayBgm(0, 0, bgmId, 0);
    }
}
