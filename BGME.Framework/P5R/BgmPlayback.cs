using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Timer = System.Timers.Timer;

namespace BGME.Framework.P5R;

internal unsafe class BgmPlayback : BaseSound, IGameHook
{
    [Function(CallingConventions.Microsoft)]
    public delegate void PlayBgmCue(nint param1, nint param2, int bgmId, nint param4, nint param5);
    private IHook<PlayBgmCue>? playBgmHook;

    [Function(CallingConventions.Microsoft)]
    public delegate void PlayBgmFunction(int cueId);
    private PlayBgmFunction? playBgm;

    private readonly Timer holdupBgmBuffer = new(TimeSpan.FromMilliseconds(1000)) { AutoReset = false };
    private bool holdupBgmQueued;

    public BgmPlayback(MusicService music)
        : base(music)
    {
        this.holdupBgmBuffer.Elapsed += (sender, args) =>
        {
            this.PlayBgm(341);
            this.holdupBgmQueued = false;
        };
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan(
            nameof(PlayBgmCue),
            "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 80 7C 24 ?? 00",
            result => this.playBgmHook = hooks.CreateHook<PlayBgmCue>(this.PlayBgmCueImpl, result).Activate());

        scanner.Scan(
            nameof(PlayBgmCue),
            "40 53 48 83 EC 30 89 CB",
            result => this.playBgm = hooks.CreateWrapper<PlayBgmFunction>(result, out _));
    }

    protected override int VictoryBgmId { get; } = 340;

    protected override void PlayBgm(int cueId) => this.playBgm!(cueId);

    private void PlayBgmCueImpl(nint param1, nint param2, int cueId, nint param4, nint param5)
    {
        var currentBgmId = this.GetGlobalBgmId(cueId);
        if (currentBgmId == null)
        {
            return;
        }

        // Buffer playing hold up music so it doesn't
        // interrupt battle BGM if quick AOA.
        if (cueId == 341 && this.holdupBgmQueued == false)
        {
            this.holdupBgmBuffer.Start();
            this.holdupBgmQueued = true;
            return;
        }
        else
        {
            this.holdupBgmBuffer.Stop();
            this.holdupBgmQueued = false;

            Log.Debug($"Playing BGM ID: {currentBgmId}");
            this.playBgmHook!.OriginalFunction(param1, param2, (int)currentBgmId, param4, param5);
        }
    }
}
