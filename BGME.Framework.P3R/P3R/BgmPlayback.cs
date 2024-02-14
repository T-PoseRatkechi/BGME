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

    [Function(CallingConventions.Microsoft)]
    private delegate void PlayCueId(nint param1, int param2, int param3, int cueId);
    private IHook<PlayCueId>? playCueHook;

    private readonly ICriAtomEx criAtomEx;

    public BgmPlayback(ICriAtomEx criAtomEx, MusicService music)
        : base(music)
    {
        this.criAtomEx = criAtomEx;
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan(
            "PlayBgm Function",
            "89 4C 24 ?? 48 83 EC 48 E8 ?? ?? ?? ?? 48 89 44 24 ?? 48 8B 4C 24 ?? E8 ?? ?? ?? ?? 48 89 44 24 ?? 48 8B 4C 24 ?? E8 ?? ?? ?? ?? 48 89 44 24 ?? 48 83 7C 24 ?? 00 74 ?? 44 8B 4C 24 ?? C6 05 ?? ?? ?? ?? 82",
            result =>
        {
            this.playBgmHook = hooks.CreateHook<PlayBgmFunction>(this.PlayBgm, result);
        });

        scanner.Scan(
            "PlayCueId Function",
            "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 45 31 D2",
        result =>
        {
            this.playCueHook = hooks.CreateHook<PlayCueId>(this.PlayCueIdImpl, result).Activate();
        });
    }

    private void PlayCueIdImpl(nint param1, int param2, int param3, int cueId)
    {
        if (param2 != 0 || param3 != 0)
        {
            this.playCueHook!.OriginalFunction(param1, param2, param3, cueId);
            return;
        }

        var currentBgmId = this.GetGlobalBgmId(cueId);
        if (currentBgmId == null)
        {
            return;
        }

        if (cueId >= 400)
        {
            // Manually play.
            var player = this.criAtomEx.GetPlayerById(0);
            var strPtr = StringsCache.GetStringPtr($"{currentBgmId}");
            this.criAtomEx.Player_SetCueName(player.PlayerHn, 0, (byte*)strPtr);
            this.criAtomEx.Player_Start(player.PlayerHn);
        }
        else
        {
            this.playCueHook!.OriginalFunction(param1, param2, param3, (int)currentBgmId);
        }
    }

    protected override void PlayBgm(int bgmId)
    {
        var currentBgmId = this.GetGlobalBgmId(bgmId);
        if (currentBgmId == null)
        {
            return;
        }

        if (bgmId >= 400)
        {
            // Manually play.
            var player = this.criAtomEx.GetPlayerById(0);
            var strPtr = StringsCache.GetStringPtr($"{bgmId}");
            this.criAtomEx.Player_SetCueName(player.PlayerHn, 0, (byte*)strPtr);
            this.criAtomEx.Player_Start(player.PlayerHn);
        }
        else
        {
            this.playBgmHook!.OriginalFunction(bgmId);
        }
    }
}
