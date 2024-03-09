using BGME.Framework.Music;
using Project.Utils;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Ryo.Definitions.Structs;
using Ryo.Interfaces;
using System.Runtime.InteropServices;

namespace BGME.Framework.P3R.P3R;

internal unsafe class Sound : BaseSound, IGameHook
{
    [Function(CallingConventions.Microsoft)]
    private delegate void PlayBgmFunction(int bgmId);
    private IHook<PlayBgmFunction>? playBgmHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void RequestSound(UPlayAdxControl* self, int playerMajorId, int playerMinorId, int cueId);

    private readonly ICriAtomEx criAtomEx;
    private int currentNewBgm = -1;
    private IHook<RequestSound>? requestSoundHook;

    public Sound(ICriAtomEx criAtomEx, MusicService music)
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
            //this.playBgmHook = hooks.CreateHook<PlayBgmFunction>(this.PlayBgm, actual).Activate();
        });

        scanner.Scan(
            nameof(RequestSound),
            "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 45 31 D2",
            result => this.requestSoundHook = hooks.CreateHook<RequestSound>(this.RequestSoundImpl, result).Activate());
    }

    private void RequestSoundImpl(UPlayAdxControl* self, int playerMajorId, int playerMinorId, int cueId)
    {
        Log.Debug($"{nameof(RequestSound)} || Player: {playerMajorId} / {playerMinorId} || Cue ID: {cueId}");
        if (playerMajorId != 0 || playerMinorId != 0)
        {
            this.requestSoundHook!.OriginalFunction(self, playerMajorId, playerMinorId, cueId);
            return;
        }

        Log.Debug($"Playing BGM with Cue ID: {cueId}");
        var currentBgmId = this.GetGlobalBgmId(cueId);
        if (currentBgmId == null)
        {
            return;
        }

        if (currentBgmId >= 400 && !IsDlcBgm((int)currentBgmId))
        {
            if (this.currentNewBgm == currentBgmId)
            {
                return;
            }

            // Manually play.
            var player = this.criAtomEx.GetPlayerById(0)!;
            var strPtr = StringsCache.GetStringPtr($"{currentBgmId}");
            this.criAtomEx.Player_SetCueName(player.PlayerHn, 0, (byte*)strPtr);
            this.criAtomEx.Player_Start(player.PlayerHn);
        }
        else
        {
            this.requestSoundHook!.OriginalFunction(self, playerMajorId, playerMinorId, cueId);
        }
    }

    protected override void PlayBgm(int bgmId)
    {
        Log.Debug($"{nameof(PlayBgm)} || Cue ID: {bgmId}");
        var currentBgmId = this.GetGlobalBgmId(bgmId);
        if (currentBgmId == null)
        {
            return;
        }

        if (currentBgmId >= 400 && !IsDlcBgm((int)currentBgmId))
        {
            if (this.currentNewBgm == currentBgmId)
            {
                return;
            }

            // Manually play.
            var player = this.criAtomEx.GetPlayerById(0)!;
            var strPtr = StringsCache.GetStringPtr($"{currentBgmId}");
            this.criAtomEx.Player_SetCueName(player.PlayerHn, 0, (byte*)strPtr);
            this.criAtomEx.Player_Start(player.PlayerHn);

            this.currentNewBgm = (int)currentBgmId;
        }
        else
        {
            this.playBgmHook!.OriginalFunction((int)currentBgmId);
            this.currentNewBgm = -1;
        }
    }

    private static bool IsDlcBgm(int bgmId)
        => bgmId >= 1000 && bgmId <= 1100;
}
