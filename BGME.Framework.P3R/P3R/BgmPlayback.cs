using BGME.Framework.Music;
using Project.Utils;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Ryo.Interfaces;

namespace BGME.Framework.P3R.P3R;

internal unsafe class BgmPlayback : BaseSound, IGameHook
{
    [Function(CallingConventions.Microsoft)]
    private delegate void PlayBgmFunction(int bgmId);
    private IHook<PlayBgmFunction>? playBgmHook;

    private readonly ICriAtomEx criAtomEx;

    public BgmPlayback(ICriAtomEx criAtomEx, MusicService music)
        : base(music)
    {
        this.criAtomEx = criAtomEx;
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan(
            "Play Aria of the Soul Function",
            "40 53 48 83 EC 20 F3 0F 58 49",
        result =>
        {
            var thunkFunc = *(int*)(result + 0x4a + 1) + result + 0x4a + 5;
            thunkFunc = *(int*)(thunkFunc + 1) + thunkFunc + 5;
            var actual = thunkFunc = *(int*)(thunkFunc + 1) + thunkFunc + 5;
            this.playBgmHook = hooks.CreateHook<PlayBgmFunction>(this.PlayBgm, actual).Activate();
        });
    }

    protected override void PlayBgm(int bgmId)
    {
        Log.Debug($"{nameof(PlayBgm)} || Cue ID: {bgmId}");
        var currentBgmId = this.GetGlobalBgmId(bgmId);
        if (currentBgmId == null)
        {
            return;
        }

        if (currentBgmId >= 400 && !IsDlcBgm(bgmId))
        {
            // Manually play.
            var player = this.criAtomEx.GetPlayerById(0)!;
            var strPtr = StringsCache.GetStringPtr($"{currentBgmId}");
            this.criAtomEx.Player_SetCueName(player.PlayerHn, 0, (byte*)strPtr);
            this.criAtomEx.Player_Start(player.PlayerHn);
        }
        else
        {
            this.playBgmHook!.OriginalFunction((int)currentBgmId);
        }
    }

    private static bool IsDlcBgm(int bgmId)
        => bgmId >= 1000 && bgmId <= 1100;
}
