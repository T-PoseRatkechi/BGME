using BGME.Framework.CRI;
using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Timer = System.Timers.Timer;

namespace BGME.Framework.P5R;

internal unsafe class BgmPlayback : BaseSound, IGameHook
{
    [Function(CallingConventions.Microsoft)]
    public delegate void PlayBgmFunction(nint param1, nint param2, int bgmId, nint param4, nint param5);
    private IHook<PlayBgmFunction>? playBgmHook;

    private readonly CriAtomEx criAtomEx;
    private PlayerConfig? bgmPlayer;

    private int currentBgmTime;
    private readonly Timer holdupBgmBuffer = new(TimeSpan.FromMilliseconds(1000)) { AutoReset = false };

    public BgmPlayback(CriAtomEx criAtomEx, MusicService music)
        : base(music)
    {
        this.criAtomEx = criAtomEx;
        this.criAtomEx.SetPlayerConfigById(255, new()
        {
            maxPathStrings = 2,
            maxPath = 256,
            enableAudioSyncedTimer = true,
            updatesTime = true,
        });

        this.holdupBgmBuffer.Elapsed += (sender, args) =>
        {
            this.currentBgmTime = this.criAtomEx.Playback_GetTimeSyncedWithAudio(this.criAtomEx.Player_GetLastPlaybackId(this.BgmPlayer.PlayerHn));
            Log.Debug($"Saved BGM Time: {currentBgmTime}");
            this.criAtomEx.Player_SetCueId(this.BgmPlayer.PlayerHn, this.BgmPlayer.Acb.AcbHn, 341);
            this.criAtomEx.Player_Start(this.BgmPlayer.PlayerHn);
        };
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
            this.bgmPlayer ??= this.criAtomEx.GetPlayerByAcbPath("SOUND/BGM.ACB");
            return this.bgmPlayer!;
        }
    }

    private void PlayBgm(nint param1, nint param2, int bgmId, nint param4, nint param5)
    {
        var currentBgmId = this.GetGlobalBgmId(bgmId);
        if (currentBgmId == null)
        {
            return;
        }

        Log.Debug($"Playing BGM ID: {currentBgmId}");

        // Buffer playing hold up music so it doesn't
        // interrupt battle BGM if quick AOA.
        if (bgmId == 341 && !this.holdupBgmBuffer.Enabled)
        {
            //this.holdupBgmBuffer.Start();
            return;
        }

        // Reset music to previous time after hold up music.
        else if (this.currentBgmTime != 0)
        {
            this.criAtomEx.Player_SetCueId(this.BgmPlayer.PlayerHn, this.BgmPlayer.Acb.AcbHn, (int)currentBgmId);
            this.criAtomEx.Player_SetStartTime(this.BgmPlayer.PlayerHn, this.currentBgmTime);
            this.criAtomEx.Player_Start(this.BgmPlayer.PlayerHn);
            this.currentBgmTime = 0;
        }
        else
        {
            if (this.holdupBgmBuffer.Enabled)
            {
                this.holdupBgmBuffer.Stop();
            }

            this.playBgmHook!.OriginalFunction(param1, param2, (int)currentBgmId, param4, param5);
        }
    }

    protected override void PlayBgm(int bgmId)
    {
        throw new NotImplementedException();
    }
}
