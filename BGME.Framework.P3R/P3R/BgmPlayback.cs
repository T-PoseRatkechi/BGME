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
            "PlayBgm Function",
            "89 4C 24 ?? 48 83 EC 48 E8 ?? ?? ?? ?? 48 89 44 24 ?? 48 8B 4C 24 ?? E8 ?? ?? ?? ?? 48 89 44 24 ?? 48 8B 4C 24 ?? E8 ?? ?? ?? ?? 48 89 44 24 ?? 48 83 7C 24 ?? 00 74 ?? 44 8B 4C 24 ?? C6 05 ?? ?? ?? ?? 82",
            result =>
        {
            this.playBgmHook = hooks.CreateHook<PlayBgmFunction>(this.PlayBgm, result).Activate();
        });
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
