using BGME.Framework.CRI;
using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P5R;

internal unsafe class SoundPlayback : BaseSound, IGameHook
{
    [Function(CallingConventions.Microsoft)]
    public delegate void PlayBgmFunction(nint param1, nint param2, int bgmId, nint param4, nint param5);
    private IHook<PlayBgmFunction>? playBgmHook;

    private readonly CriAtomEx cri;
    private PlayerConfig? bgmPlayer;

    private uint bgmPlaybackId;
    private uint currentBgmTime;

    public SoundPlayback(CriAtomEx criAtomEx, MusicService music)
        : base(music)
    {
        this.cri = criAtomEx;
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan("Play BGM Function", "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 80 7C 24 ?? 00", result =>
        {
            this.playBgmHook = hooks.CreateHook<PlayBgmFunction>(this.PlayBgm, result).Activate();
        });
    }

    public PlayerConfig BgmPlayer
    {
        get
        {
            if (this.bgmPlayer == null)
            {
                this.bgmPlayer = this.cri.GetPlayerByAcbPath("SOUND/BGM.AWB");
            }

            return this.bgmPlayer!;
        }
    }

    private void PlayBgm(nint param1, nint param2, int bgmId, nint param4, nint param5)
    {
        Log.Debug($"{param1:X} || {param2:X} || {bgmId:X} || {param4:X} || {param5:X}");
        var currentBgmId = this.GetGlobalBgmId(bgmId);
        if (currentBgmId == null)
        {
            return;
        }

        if (bgmId == 341)
        {
            this.currentBgmTime = this.cri.criAtomExPlayback_GetTimeSyncedWithAudioImpl(this.bgmPlaybackId);
            Log.Debug($"Saved BGM Time: {currentBgmTime}");
            this.cri.criAtomExPlayer_SetCueIdImpl(this.BgmPlayer.PlayerHn, this.BgmPlayer.Acb.AcbHn, 341);
            this.cri.criAtomExPlayer_StartImpl(this.BgmPlayer.PlayerHn);
        }
        else if (this.currentBgmTime != 0)
        {
            this.cri.criAtomExPlayer_SetCueIdImpl(this.BgmPlayer.PlayerHn, this.BgmPlayer.Acb.AcbHn, (int)currentBgmId);
            this.cri.criAtomExPlayer_SetStartTimeImpl(this.BgmPlayer.PlayerHn, this.currentBgmTime);
            this.cri.criAtomExPlayer_StartImpl(this.BgmPlayer.PlayerHn);
            this.currentBgmTime = 0;
        }
        else
        {
            Log.Debug($"Playing BGM ID: {currentBgmId}");
            this.playBgmHook!.OriginalFunction(param1, param2, (int)currentBgmId, param4, param5);
        }
    }

    protected override void PlayBgm(int bgmId)
    {
        throw new NotImplementedException();
    }
}
