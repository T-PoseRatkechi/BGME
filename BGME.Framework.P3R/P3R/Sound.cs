using BGME.Framework.Music;
using Project.Utils;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Ryo.Definitions.Structs;
using Ryo.Interfaces;

namespace BGME.Framework.P3R.P3R;

internal unsafe class Sound : BaseSound, IGameHook
{
    [Function(CallingConventions.Microsoft)]
    private delegate void PlayBgmFunction(int bgmId);
    private PlayBgmFunction? playBgm;

    [Function(CallingConventions.Microsoft)]
    private delegate void RequestSound(UPlayAdxControl* self, int playerMajorId, int playerMinorId, int cueId, nint param5);

    private readonly ICriAtomEx criAtomEx;
    private IHook<RequestSound>? requestSoundHook;

    public Sound(ICriAtomEx criAtomEx, MusicService music)
        : base(music)
    {
        this.criAtomEx = criAtomEx;
    }

    protected override int VictoryBgmId { get; } = 60;

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        //scanner.Scan(
        //    "Play Aria of the Soul Function",
        //    "40 53 48 83 EC 20 F3 0F 58 49",
        //result =>
        //{
        //    var thunkFunc = *(int*)(result + 0x4a + 1) + result + 0x4a + 5;
        //    thunkFunc = *(int*)(thunkFunc + 1) + thunkFunc + 5;
        //    var actual = thunkFunc = *(int*)(thunkFunc + 1) + thunkFunc + 5;
        //    //this.playBgmHook = hooks.CreateHook<PlayBgmFunction>(this.PlayBgm, actual).Activate();
        //    this.playBgm = hooks.CreateWrapper<PlayBgmFunction>(actual, out _);
        //});

        scanner.Scan(
            nameof(RequestSound),
            "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 4C 89 74 24 ?? 45 31 D2",
            result => this.requestSoundHook = hooks.CreateHook<RequestSound>(this.RequestSoundImpl, result).Activate());
    }

    private void RequestSoundImpl(UPlayAdxControl* self, int playerMajorId, int playerMinorId, int cueId, nint param5)
    {
        Log.Debug($"{nameof(RequestSound)} || Player: {playerMajorId} / {playerMinorId} || Cue ID: {cueId} || param5: {param5}");
        if (playerMajorId != 0 || playerMinorId != 0)
        {
            this.requestSoundHook!.OriginalFunction(self, playerMajorId, playerMinorId, cueId, param5);
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
            // Manually play.
            var player = this.criAtomEx.GetPlayerById(0)!;
            var strPtr = StringsCache.GetStringPtr($"{currentBgmId}");
            this.criAtomEx.Player_SetCueName(player.PlayerHn, 0, (byte*)strPtr);
            this.criAtomEx.Player_Start(player.PlayerHn);
        }
        else
        {
            this.requestSoundHook!.OriginalFunction(self, playerMajorId, playerMinorId, (int)currentBgmId, param5);
        }
    }

    protected override void PlayBgm(int bgmId)
        => this.playBgm!(bgmId);

    private static bool IsDlcBgm(int bgmId)
        => bgmId >= 1000 && bgmId <= 1100;
}
