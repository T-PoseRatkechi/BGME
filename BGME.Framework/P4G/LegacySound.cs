using BGME.Framework.Models;
using BGME.Framework.Music;
using PersonaMusicScript.Types.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P4G;

internal unsafe class LegacySound : BaseSound
{
    // Constants.
    private const int WAVEFORM_ENTRY_SIZE = 20;
    private const int EXTENDED_BGM_ID = 64;

    private const ushort SONG_CUE_ID_1 = 58;
    private const ushort SONG_WAVEFORM_INDEX_1 = 57;
    private const ushort SONG_AWB_INDEX_1 = 80;

    private const ushort SONG_CUE_ID_2 = 61;
    private const ushort SONG_WAVEFORM_INDEX_2 = 60;
    private const ushort SONG_AWB_INDEX_2 = 82;

    // Shell songs.
    private static readonly ShellCue SHELL_SONG_1 = new(SONG_CUE_ID_1, SONG_WAVEFORM_INDEX_1);
    private static readonly ShellCue SHELL_SONG_2 = new(SONG_CUE_ID_2, SONG_WAVEFORM_INDEX_2);

    [Function(CallingConventions.Microsoft)]
    private delegate void PlaySoundFunction(int soundCategory, int soundId, nint param3, nint param4);
    private IFunction<PlaySoundFunction>? playSoundFunction;

    private IHook<PlaySoundFunction> playSoundHook;

    [Function(new Register[] { Register.rbx, Register.rax }, Register.rax, true)]
    private delegate void GetBgmAcbPtr(nint acbStrPtr, nint acbHndlPtr);
    private IReverseWrapper<GetBgmAcbPtr>? bgmAcbWrapper;
    private IAsmHook? bgmAcbHook;
    private nint acbAddress;

    private ShellCue currentShellSong = SHELL_SONG_1;
    private ushort currentAwbIndex = 0;

    public LegacySound(IReloadedHooks hooks, IStartupScanner scanner, MusicService music)
        : base(music)
    {
        scanner.Scan("Play Sound Function", "48 63 E9 89 D6", result =>
        {
            this.playSoundFunction = hooks.CreateFunction<PlaySoundFunction>(result - 30);
            this.playSoundHook = this.playSoundFunction.Hook(this.PlaySoundImpl).Activate();
        });

        scanner.Scan(
            "Get BGM.ACB Pointer Hook",
            "48 85 C0 41 0F 44 FF 48 89 83 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 45 33 C0 41 8B D7 FF 15 ?? ?? ?? ?? 85 FF 75 ?? C7 43 ?? 03 00 00 00 48 81 C6 38 02 00 00 48 81 C3 38 02 00 00 48 83 ED 01 0F 85 ?? ?? ?? ?? 33 D2",
            result =>
        {
            var patch = new string[]
            {
                "use64",
                Utilities.PushCallerRegisters,
                "push rax",
                "sub rsp, 8",
                hooks.Utilities.GetAbsoluteCallMnemonics(this.GetBgmAcbPtrImpl, out this.bgmAcbWrapper),
                "add rsp, 8",
                "pop rax",
                Utilities.PopCallerRegisters,
            };

            this.bgmAcbHook = hooks.CreateAsmHook(patch, result).Activate();
        });

        this.playSoundHook = default!;
    }

    private void GetBgmAcbPtrImpl(nint acbStrPtr, nint acbHndlPtr)
    {
        var acbString = Marshal.PtrToStringAnsi(acbStrPtr + 4);
        if (acbString == "app0:/data/sound/adx2/bgm/snd00_bgm.acb")
        {
            this.acbAddress = *(nint*)(acbHndlPtr + sizeof(nint) * 3);
            this.bgmAcbHook!.Disable();
            Log.Debug($"ACB Handle: {acbHndlPtr:X}");
            Log.Debug($"BGM.ACB found at: {this.acbAddress:X}");
        }
    }

    /// <summary>
    /// Gets address to waveform table in ACB.
    /// </summary>
    private unsafe nint WaveformAddress
    {
        get
        {
            var tableAddress = this.acbAddress + 0xAF77;
            Log.Debug($"Waveform Table Address: {tableAddress:X}");
            return tableAddress;
        }
    }

    protected override int VictoryBgmId { get; } = 7;

    protected override void PlayBgm(int bgmId)
    {
        this.playSoundFunction?.GetWrapper()(0, bgmId, 0, 0);
    }

    public void PlayMusic(IMusic music)
    {
        var bgmId = MusicUtils.CalculateMusicId(music);
        if (bgmId == null)
        {
            return;
        }
        else if (music is Sound sound)
        {
            Log.Debug($"PlaySound({sound.Setting_1}, {bgmId}, {sound.Setting_2}, {sound.Setting_3})");
            this.playSoundFunction?.GetWrapper()(sound.Setting_1, (int)bgmId, sound.Setting_2, sound.Setting_3);
        }
        else
        {
            Log.Debug($"PlaySound(0, {bgmId}, 0, 0)");
            this.playSoundFunction?.GetWrapper()(0, (int)bgmId, 0, 0);
        }
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
            this.playSoundHook.OriginalFunction(soundCategory, soundId, param3, param4);
            return;
        }

        var currentBgmId = this.GetGlobalBgmId(soundId);
        if (currentBgmId == null)
        {
            return;
        }

        // Issues with S-Link scenes replaying the last Cue ID, in BGME songs case
        // it'll trigger an incorrect reset.
        //if (currentBgmId == SHELL_SONG_1.CueId || currentBgmId == SHELL_SONG_2.CueId)
        //{
        //    this.ResetShellSongs();
        //}

        if (currentBgmId >= EXTENDED_BGM_ID)
        {
            // Swap shell cue ID to trigger a song change.
            if (this.currentAwbIndex != currentBgmId)
            {
                this.SwapShellCue();
                this.currentAwbIndex = (ushort)currentBgmId;
            }

            // Pointer to AWB property of shell cue ID.
            var entryAwbIndexPtr = (ushort*)(this.WaveformAddress + (WAVEFORM_ENTRY_SIZE * this.currentShellSong.WaveTableIndex) + 16);
            Log.Verbose($"Entry AWB Address: {(nint)entryAwbIndexPtr:X}");
            *entryAwbIndexPtr = this.currentAwbIndex.ToBigEndian();

            Log.Debug($"Playing AWB index {this.currentAwbIndex} using Cue ID {this.currentShellSong.CueId}.");
            currentBgmId = this.currentShellSong.CueId;
        }

        Log.Debug($"Playing BGM ID: {currentBgmId}");
        this.playSoundHook.OriginalFunction(soundCategory, (int)currentBgmId, param3, param4);
    }

    /// <summary>
    /// Resets shell songs data.
    /// </summary>
    private void ResetShellSongs()
    {
        var shellAwbPtr1 = (ushort*)(this.WaveformAddress + (WAVEFORM_ENTRY_SIZE * SHELL_SONG_1.WaveTableIndex) + 16);
        *shellAwbPtr1 = SONG_AWB_INDEX_1.ToBigEndian();

        var shellAwbPtr2 = (ushort*)(this.WaveformAddress + (WAVEFORM_ENTRY_SIZE * SHELL_SONG_2.WaveTableIndex) + 16);
        *shellAwbPtr2 = SONG_AWB_INDEX_2.ToBigEndian();

        Log.Debug("Reset shell songs data.");
    }

    private void SwapShellCue()
    {
        this.currentShellSong = (this.currentShellSong == SHELL_SONG_1) ? SHELL_SONG_2 : SHELL_SONG_1;
    }
}
