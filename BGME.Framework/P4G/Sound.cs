using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Ryo.Interfaces;

namespace BGME.Framework.P4G;

internal unsafe class Sound : BaseSound, IGameHook
{
    private const double BATTLE_SFX_LIMIT_MS = 200;

    [Function(CallingConventions.Microsoft)]
    private delegate void PlaySound(int soundCategory, int soundId);
    private IFunction<PlaySound>? playSound;
    private IHook<PlaySound>? playSoundHook;

    private delegate nint PlayBattleSfx(ushort* param1);
    private IHook<PlayBattleSfx>? playBattleSfx;

    private delegate void SetPlayerVolumeCategory(nint playerHn, uint param2, int cueId);
    private IHook<SetPlayerVolumeCategory>? setPlayerVolumeCategoryHook;

    private readonly ICriAtomEx criAtomEx;
    private DateTime prevBattleSfxTime = DateTime.Now;

    public Sound(ICriAtomEx criAtomEx, MusicService music)
        : base(music)
    {
        this.criAtomEx = criAtomEx;
    }

    protected override int VictoryBgmId { get; } = 7;

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan(nameof(PlaySound), "48 63 E9 89 D6", result =>
        {
            this.playSound = hooks.CreateFunction<PlaySound>(result - 30);
            this.playSoundHook = this.playSound.Hook(this.PlaySoundImpl).Activate();
        });
        
        scanner.Scan(nameof(PlayBattleSfx), "40 57 48 83 EC 20 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F B7 0F", result =>
        {
            this.playBattleSfx = hooks.CreateHook<PlayBattleSfx>(this.PlayBattleSfxImpl, result).Activate();
        });

        scanner.Scan(nameof(SetPlayerVolumeCategory), "40 53 56 57 48 81 EC E0 00 00 00", result =>
        {
            this.setPlayerVolumeCategoryHook = hooks.CreateHook<SetPlayerVolumeCategory>(this.SetPlayerCategoryVolumeImpl, result).Activate();
        });
    }

    protected override void PlayBgm(int bgmId)
    {
        this.playSound?.GetWrapper()(0, bgmId);
    }

    private void PlaySoundImpl(int playerId, int cueId)
    {
        if (playerId != 0)
        {
            this.playSoundHook!.OriginalFunction(playerId, cueId);
            return;
        }

        var currentBgmId = this.GetGlobalBgmId(cueId);
        if (currentBgmId == null)
        {
            return;
        }

        Log.Debug($"Playing BGM ID: {currentBgmId}");
        this.playSoundHook!.OriginalFunction(playerId, (int)currentBgmId);
    }

    private nint PlayBattleSfxImpl(ushort* param1)
    {
        var now = DateTime.Now;
        var elapsed = now - this.prevBattleSfxTime;
        if (elapsed.TotalMilliseconds >= BATTLE_SFX_LIMIT_MS)
        {
            this.prevBattleSfxTime = now;
            return this.playBattleSfx!.OriginalFunction(param1);
        }

        Log.Verbose($"Limiting battle SFX to 1 per {BATTLE_SFX_LIMIT_MS}ms to fix BGM muting.");
        return 1;
    }

    private void SetPlayerCategoryVolumeImpl(nint playerHn, uint param2, int cueId)
    {
        // Unsets categories after Ryo applies them, breaking Ryo audio.
        // Skip when running on the BGM player.
        var player = this.criAtomEx.GetPlayerByHn(playerHn);
        if (player?.Id == 0)
        {
            return;
        }

        // Still required for some audio/players, strangely enough.
        // For example, door opening SFX is muted without it.
        this.setPlayerVolumeCategoryHook!.OriginalFunction(playerHn, param2, cueId);
    }
}
