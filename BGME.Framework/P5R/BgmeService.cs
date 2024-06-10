using BGME.Framework.CRI;
using BGME.Framework.CRI.Types;
using BGME.Framework.Music;
using BGME.Framework.P5R.Rhythm;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using static BGME.Framework.CRI.CriAtomExFunctions;

namespace BGME.Framework.P5R;

internal class BgmeService : IBgmeService
{
    private const int EXTENDED_BGM_ID = 10000;

    private readonly IP5RLib p5rLib;
    private readonly CriAtomEx criAtomEx;
    private readonly BgmPlayback bgm;
    private readonly EncounterBgm encounterBgm;
    private IHook<criAtomExPlayer_SetCueId>? setCueIdHook;
    private PlayerConfig? bgmPlayer;

    private readonly RhythmGame? rhythmGame;

    public BgmeService(IP5RLib p5rLib, CriAtomEx criAtomEx, MusicService music)
    {
        this.p5rLib = p5rLib;
        this.criAtomEx = criAtomEx;
        this.bgm = new(criAtomEx, music);
        this.encounterBgm = new(music);

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
        this.encounterBgm.Initialize(scanner, hooks);
    }

    private unsafe void CriAtomExPlayer_SetCueId(nint player, nint acbHn, int cueId)
    {
        this.bgmPlayer ??= this.criAtomEx.GetPlayerByAcbPath("SOUND/BGM.ACB");

        if (player == this.bgmPlayer?.PlayerHn && cueId >= 1000)
        {
            Log.Debug($"{nameof(CriAtomExPlayer_SetCueId)}|BGME: {this.bgmPlayer.PlayerHn:X} || {acbHn:X} || {cueId}");

            var bgmFile = $"BGME/P5R/{cueId}.adx";
            var format = Path.GetExtension(bgmFile) == ".hca" ? CRIATOM_FORMAT.HCA : CRIATOM_FORMAT.ADX;
            var ptr = StringsCache.GetStringPtr(bgmFile);

            this.criAtomEx.Player_SetFile(this.bgmPlayer.PlayerHn, IntPtr.Zero, (byte*)ptr);
            this.criAtomEx.Player_SetFormat(this.bgmPlayer.PlayerHn, format);
            this.criAtomEx.Player_SetNumChannels(this.bgmPlayer.PlayerHn, 2);
            this.criAtomEx.Player_SetSamplingRate(this.bgmPlayer.PlayerHn, 48000);
            this.criAtomEx.Player_SetCategoryById(this.bgmPlayer.PlayerHn, 1);
            this.criAtomEx.Player_SetCategoryById(this.bgmPlayer.PlayerHn, 8);
        }
        else
        {
            this.setCueIdHook!.OriginalFunction(player, acbHn, cueId);
        }
    }

    public void SetVictoryDisabled(bool isDisabled)
    {
        Log.Debug($"Disable Victory BGM: {isDisabled}");
        this.bgm.SetVictoryDisabled(isDisabled);
        this.encounterBgm.SetVictoryDisabled(isDisabled);
    }
}
