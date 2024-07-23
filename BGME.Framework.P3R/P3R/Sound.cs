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
        scanner.Scan(
            nameof(RequestSound),
            "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 4C 89 74 24 ?? 45 31 D2",
            result => this.requestSoundHook = hooks.CreateHook<RequestSound>(this.RequestSoundImpl, result).Activate());
    }

    private void RequestSoundImpl(UPlayAdxControl* self, int playerMajorId, int playerMinorId, int cueId, nint param5)
    {
        Log.Verbose($"{nameof(RequestSound)} || Player: {playerMajorId} / {playerMinorId} || Cue ID: {cueId} || param5: {param5}");
        if (playerMajorId != 0 || playerMinorId != 0)
        {
            this.requestSoundHook!.OriginalFunction(self, playerMajorId, playerMinorId, cueId, param5);
            return;
        }

        var currentBgmId = this.GetGlobalBgmId(cueId);
        if (currentBgmId == null)
        {
            return;
        }

        Log.Debug($"Playing BGM ID: {currentBgmId}");
        this.requestSoundHook!.OriginalFunction(self, playerMajorId, playerMinorId, (int)currentBgmId, param5);
    }

    protected override void PlayBgm(int bgmId)
        => this.playBgm!(bgmId);

    private static bool IsDlcBgm(int bgmId)
        => bgmId >= 1000 && bgmId <= 1100;
}
