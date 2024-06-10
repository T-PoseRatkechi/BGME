using BGME.Framework.CRI;
using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P4G;

internal unsafe class BgmPlayback : BaseSound, IGameHook
{
    [Function(CallingConventions.Microsoft)]
    private delegate void PlaySoundFunction(int soundCategory, int soundId, nint param3, nint param4);
    private IFunction<PlaySoundFunction>? playSoundFunction;
    private IHook<PlaySoundFunction>? playSoundHook;

    [Function(new Register[] { Register.rbx, Register.rax }, Register.rax, true)]
    private delegate void GetBgmAcbPtr(nint acbStrPtr, nint acbHndlPtr);
    private IReverseWrapper<GetBgmAcbPtr>? bgmAcbWrapper;
    private IAsmHook? bgmAcbHook;

    private readonly CriAtomEx criAtomEx;
    private PlayerConfig? bgmPlayer;

    public BgmPlayback(CriAtomEx criAtomEx, MusicService music)
        : base(music)
    {
        this.criAtomEx = criAtomEx;
    }

    public PlayerConfig BgmPlayer
    {
        get
        {
            this.bgmPlayer ??= this.criAtomEx.GetPlayerByAcbPath("/sound/adx2/bgm/snd00_bgm.acb");
            return this.bgmPlayer!;
        }
    }

    protected override int VictoryBgmId { get; } = 7;

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan("Play Sound Function", "48 63 E9 89 D6", result =>
        {
            this.playSoundFunction = hooks.CreateFunction<PlaySoundFunction>(result - 30);
            this.playSoundHook = this.playSoundFunction.Hook(this.PlaySoundImpl).Activate();
        });
    }

    protected override void PlayBgm(int bgmId)
    {
        this.playSoundFunction?.GetWrapper()(0, bgmId, 0, 0);
    }

    /// <summary>
    /// Play sound function.
    /// </summary>
    /// <param name="soundCategory">Sound category? 0 to 3 = BGM, 4 = SFX</param>
    /// <param name="soundId">Sound ID to play? Equal to Cue ID for BGM, unknown value for others.</param>
    /// <param name="param3">Unknown param.</param>
    /// <param name="param4">Unknown param.</param>
    /// <returns></returns>
    private void PlaySoundImpl(int soundCategory, int soundId, nint param3, nint param4)
    {
        if (soundCategory != 0)
        {
            Log.Verbose($"Playing Sound ID: {soundId}");
            this.playSoundHook!.OriginalFunction(soundCategory, soundId, param3, param4);
            return;
        }

        var currentBgmId = this.GetGlobalBgmId(soundId);
        if (currentBgmId == null)
        {
            return;
        }

        Log.Debug($"Playing BGM ID: {currentBgmId}");
        this.playSoundHook!.OriginalFunction(soundCategory, (int)currentBgmId, param3, param4);
    }
}
