using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Ryo.Interfaces;
using SharedScans.Interfaces;
using static Ryo.Definitions.Functions.CriAtomExFunctions;

namespace BGME.Framework.P4G;

internal unsafe class Sound : BaseSound, IGameHook
{
    private const double BATTLE_SFX_LIMIT_MS = 200;

    [Function(CallingConventions.Microsoft)]
    private delegate void PlaySound(int soundCategory, int soundId);
    private IFunction<PlaySound>? playSound;
    private IHook<PlaySound>? playSoundHook;

    [Function(CallingConventions.Microsoft)]
    private delegate nint PlayBattleSfx(ushort* param1);
    private IHook<PlayBattleSfx>? playBattleSfx;

    private delegate void SetPlayerVolumeCategory(nint playerHn, uint param2, int cueId);
    private IHook<SetPlayerVolumeCategory>? setPlayerVolumeCategoryHook;

    private readonly ICriAtomRegistry criAtomRegistry;
    private readonly HookContainer<criAtomConfig_GetCategoryIndexById> getCategoryInfoByIndex;

    public Sound(ISharedScans scans, ICriAtomRegistry criAtomRegistry, MusicService music)
        : base(music)
    {
        this.criAtomRegistry = criAtomRegistry;
        this.getCategoryInfoByIndex = scans.CreateHook<criAtomConfig_GetCategoryIndexById>(this.GetCategoryIndexById, Mod.NAME);
    }

    protected override int VictoryBgmId { get; } = 7;

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan(nameof(PlaySound), "48 63 E9 89 D6", result =>
        {
            this.playSound = hooks.CreateFunction<PlaySound>(result - 30);
            this.playSoundHook = this.playSound.Hook(this.PlaySoundImpl).Activate();
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

    private void SetPlayerCategoryVolumeImpl(nint playerHn, uint param2, int cueId)
    {
        // Unsets categories after Ryo applies them, breaking Ryo audio.
        // Skip when running on the BGM player.
        var player = this.criAtomRegistry.GetPlayerByHn(playerHn);
        if (player?.Id == 0)
        {
            return;
        }

        // Still required for some audio/players, strangely enough.
        // For example, door opening SFX is muted without it.
        this.setPlayerVolumeCategoryHook!.OriginalFunction(playerHn, param2, cueId);
    }

    private ushort GetCategoryIndexById(uint id)
    {
        // Redirect problematic cues to more limited category.
        if (id == 5 || id == 3 || id == 4)
        {
            return this.getCategoryInfoByIndex.Hook!.OriginalFunction(0);
        }

        return this.getCategoryInfoByIndex.Hook!.OriginalFunction(id);
    }
}
