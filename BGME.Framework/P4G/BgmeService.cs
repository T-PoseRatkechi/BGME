using BGME.Framework.CRI;
using BGME.Framework.CRI.Types;
using BGME.Framework.Music;
using BGME.Framework.Template.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using static BGME.Framework.CRI.CriAtomExFunctions;

namespace BGME.Framework.P4G;

internal class BgmeService : IBgmeService, IGameHook
{
    private readonly Config config;
    private readonly CriAtomEx criAtomEx;
    private readonly MusicService music;

    private BgmPlayback bgm;
    private EncounterBgm encounterPatcher;
    private FloorBgm floorPatcher;
    private EventBgm eventBgm;

    private IHook<criAtomExPlayer_SetCueId>? setCueIdHook;
    private PlayerConfig? bgmPlayer;

    public BgmeService(Config config, CriAtomEx criAtomEx, MusicService music)
    {
        this.config = config;
        this.criAtomEx = criAtomEx;
        this.music = music;

        this.bgm = new(this.criAtomEx, this.music);
        this.encounterPatcher = new(music);
        this.floorPatcher = new(music);
        this.eventBgm = new(this.bgm, music);

        this.criAtomEx.SetPlayerConfigById(0, new()
        {
            maxPath = 512,
            maxPathStrings = 2,
        });

        criAtomEx.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(criAtomEx.SetCueId))
            {
                this.setCueIdHook = criAtomEx.SetCueId!.Hook(this.CriAtomExPlayer_SetCueId).Activate();
            }
        };
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        this.bgm.Initialize(scanner, hooks);
        this.encounterPatcher.Initialize(scanner, hooks);
        this.floorPatcher.Initialize(scanner, hooks);
        this.eventBgm.Initialize(scanner, hooks);
    }

    private unsafe void CriAtomExPlayer_SetCueId(nint player, nint acbHn, int cueId)
    {
        this.bgmPlayer ??= this.criAtomEx.GetPlayerByAcbPath("/sound/adx2/bgm/snd00_bgm.acb");

        if (player == this.bgmPlayer?.PlayerHn && IsBgmeMusic(cueId))
        {
            Log.Debug($"{nameof(CriAtomExPlayer_SetCueId)}|BGME: {player:X} || {acbHn:X} || {cueId}");

            var bgmFile = this.GetBgmFile(cueId);
            var format = Path.GetExtension(bgmFile) == ".hca" ? CRIATOM_FORMAT.HCA : CRIATOM_FORMAT.ADX;
            var ptr = StringsCache.GetStringPtr(bgmFile);

            this.criAtomEx.Player_SetFile(player, IntPtr.Zero, (byte*)ptr);
            this.criAtomEx.Player_SetFormat(player, format);
            this.criAtomEx.Player_SetNumChannels(player, 2);
            this.criAtomEx.Player_SetSamplingRate(player, 48000);

            // BGM isn't stopped by SFX so maybe correct ID?
            // Does not respect music volume level though.
            // Does not respect ANY KIND OF VOLUME setting...
            this.criAtomEx.Player_SetCategoryById(player, 13);
        }
        else
        {
            this.setCueIdHook!.OriginalFunction(player, acbHn, cueId);
        }
    }

    private static bool IsBgmeMusic(int cueId)
    {
        if (cueId >= 2500)
        {
            return true;
        }

        if (cueId >= 678 && cueId <= 835)
        {
            return true;
        }

        return false;
    }

    private string GetBgmFile(int bgmId)
        => $"BGME/P4G/{bgmId}.hca";
}
