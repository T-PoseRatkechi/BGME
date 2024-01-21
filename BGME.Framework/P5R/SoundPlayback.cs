using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;

namespace BGME.Framework.P5R;

internal unsafe class SoundPlayback : BaseSound
{
    private const int EXTENDED_BGM_ID = 10000;

    [Function(CallingConventions.Microsoft)]
    private delegate CriBool criAtomExPlayer_GetNumPlayedSamples(uint playbackId, ulong* numSamples, uint* samplingRate);
    private IHook<criAtomExPlayer_GetNumPlayedSamples> getNumPlayedSamplesHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void* criAtomExAcb_LoadAcbFile(nint acbBinder, byte* acbPathStr, nint awbBinder, byte* awbPathStr, void* work, int workSize);
    private IHook<criAtomExAcb_LoadAcbFile> loadAcbFileHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void criAtomExPlayer_SetCueId(nint player, void* acbHn, int cueId);
    private IHook<criAtomExPlayer_SetCueId> setCueIdHook;

    [Function(CallingConventions.Microsoft)]
    private delegate uint criAtomExPlayer_Start(nint player);
    private IHook<criAtomExPlayer_Start> startHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void criAtomExPlayer_SetFile(nint player, nint criBinderHn, byte* path);
    private IHook<criAtomExPlayer_SetFile> setFileHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void criAtomExPlayer_SetFormat(nint player, CRIATOM_FORMAT format);
    private IHook<criAtomExPlayer_SetFormat> setFormatHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void criAtomExPlayer_SetSamplingRate(nint player, int samplingRate);
    private IHook<criAtomExPlayer_SetSamplingRate> setSamplingRateHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void criAtomExPlayer_SetNumChannels(nint player, int numChannels);
    private IHook<criAtomExPlayer_SetNumChannels> setNumChannelsHook;

    [Function(CallingConventions.Microsoft)]
    private delegate float criAtomExCategory_GetVolumeById(uint categoryId);
    private IHook<criAtomExCategory_GetVolumeById> getVolumeByIdHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void criAtomExPlayer_SetVolume(nint player, float volume);
    private IHook<criAtomExPlayer_SetVolume> setVolumeHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void criAtomExPlayer_SetCategoryById(nint player, uint id);
    private IHook<criAtomExPlayer_SetCategoryById> setCategoryByIdHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void criAtomExPlayer_SetStartTime(nint player, uint startTimeMs);
    private IHook<criAtomExPlayer_SetStartTime> setStartTimeHook;

    [Function(CallingConventions.Microsoft)]
    private delegate uint criAtomExPlayback_GetTimeSyncedWithAudio(uint playbackId);
    private IHook<criAtomExPlayback_GetTimeSyncedWithAudio> getTimeSyncedWithAudioHook;

    [Function(CallingConventions.Microsoft)]
    private delegate nint criAtomExPlayer_Create(CriAtomExPlayerConfigTag* config, void* work, int workSize);
    private IHook<criAtomExPlayer_Create> createHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void PlayBgmFunction(nint param1, nint param2, int bgmId, nint param4, nint param5);
    private IHook<PlayBgmFunction>? playBgmHook;

    private void* bgmAcbHn = (void*)IntPtr.Zero;
    private void* playerHn = (void*)IntPtr.Zero;

    public SoundPlayback(IStartupScanner scanner, IReloadedHooks hooks, MusicService music)
        : base(music)
    {
        scanner.Scan(nameof(criAtomExPlayer_Create), "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 55 41 54 41 55 41 56 41 57 48 8B EC 48 83 EC 40 45 33 ED", result =>
        {
            this.createHook = hooks.CreateHook<criAtomExPlayer_Create>(this.criAtomExPlayer_CreateImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetStartTime), "48 85 C9 74 ?? 48 85 D2 78 ?? B8 FF FF FF FF 48 3B D0 48 0F 4C C2", result =>
        {
            this.setStartTimeHook = hooks.CreateHook<criAtomExPlayer_SetStartTime>(this.criAtomExPlayer_SetStartTimeImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayback_GetTimeSyncedWithAudio), "48 83 EC 28 E8 ?? ?? ?? ?? 4C 8B C0 48 85 C0 7E", result =>
        {
            this.getTimeSyncedWithAudioHook = hooks.CreateHook<criAtomExPlayback_GetTimeSyncedWithAudio>(this.criAtomExPlayback_GetTimeSyncedWithAudioImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_GetNumPlayedSamples), "33 C0 4C 8D 54 24", result =>
        {
            this.getNumPlayedSamplesHook = hooks.CreateHook<criAtomExPlayer_GetNumPlayedSamples>(this.criAtomExPlayer_GetNumPlayedSamplesImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExAcb_LoadAcbFile), "48 8B C4 48 89 58 ?? 48 89 68 ?? 48 89 70 ?? 48 89 78 ?? 41 54 41 56 41 57 48 83 EC 40 49 8B E9", result =>
        {
            this.loadAcbFileHook = hooks.CreateHook<criAtomExAcb_LoadAcbFile>(this.criAtomExAcb_LoadAcbFileImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetCueId), "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 49 63 F8 48 8B F2 48 8B D9", result =>
        {
            this.setCueIdHook = hooks.CreateHook<criAtomExPlayer_SetCueId>(this.criAtomExPlayer_SetCueIdImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_Start), "48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 48 85 C9 75 ?? 44 8D 41 ?? 48 8D 15 ?? ?? ?? ?? E8 ?? ?? ?? ?? 83 C8 FF EB ?? E8 ?? ?? ?? ?? 33 D2", result =>
        {
            this.startHook = hooks.CreateHook<criAtomExPlayer_Start>(this.criAtomExPlayer_StartImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetFile), "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 49 8B F0 48 8B EA 48 8B F9", result =>
        {
            this.setFileHook = hooks.CreateHook<criAtomExPlayer_SetFile>(this.criAtomExPlayer_SetFileImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetFormat), "48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 48 85 C9 75 ?? 48 8D 15", result =>
        {
            this.setFormatHook = hooks.CreateHook<criAtomExPlayer_SetFormat>(this.criAtomExPlayer_SetFormatImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetSamplingRate), "48 89 5C 24 ?? 57 48 83 EC 20 8B FA 48 8B D9 48 85 C9 74 ?? 85 D2", result =>
        {
            this.setSamplingRateHook = hooks.CreateHook<criAtomExPlayer_SetSamplingRate>(this.criAtomExPlayer_SetSamplingRateImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetNumChannels), "48 89 5C 24 ?? 57 48 83 EC 20 8B FA 48 8B D9 48 85 C9 74 ?? 8D 42", result =>
        {
            this.setNumChannelsHook = hooks.CreateHook<criAtomExPlayer_SetNumChannels>(this.criAtomExPlayer_SetNumChannelsImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExCategory_GetVolumeById), "40 53 48 83 EC 20 8B D9 33 C9 E8 ?? ?? ?? ?? 85 C0 75 ?? 48 8D 15 ?? ?? ?? ?? 33 C9 E8 ?? ?? ?? ?? F3 0F 10 05", result =>
        {
            this.getVolumeByIdHook = hooks.CreateHook<criAtomExCategory_GetVolumeById>(this.criAtomExCategory_GetVolumeByIdImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetVolume), "48 85 C9 75 ?? 44 8D 41 ?? 48 8D 15 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 89 ?? ?? ?? ?? 33 D2", result =>
        {
            this.setVolumeHook = hooks.CreateHook<criAtomExPlayer_SetVolume>(this.criAtomExPlayer_SetVolumeImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetCategoryById), "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 50 48 8B F9 8B F2", result =>
        {
            this.setCategoryByIdHook = hooks.CreateHook<criAtomExPlayer_SetCategoryById>(this.criAtomExPlayer_SetCategoryByIdImpl, result).Activate();
        });

        scanner.Scan("Play BGM Function", "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 80 7C 24 ?? 00", result =>
        {
            this.playBgmHook = hooks.CreateHook<PlayBgmFunction>(this.PlayBgm, result).Activate();
        });
    }

    private int numPlayers;
    private nint criAtomExPlayer_CreateImpl(CriAtomExPlayerConfigTag* config, void* work, int workSize)
    {
        Log.Debug($"Config: {(nint)config:X} || Work: {(nint)work:X} || WorkSize: {workSize}");

        // Creating BGM player.
        if (numPlayers == 255)
        {
            var newConfig = new CriAtomExPlayerConfigTag()
            {
                maxPathStrings = 2,
                maxPath = 256,
                enableAudioSyncedTimer = true,
                updatesTime = true,
            };

            var ptr = Marshal.AllocHGlobal(sizeof(CriAtomExPlayerConfigTag));
            Marshal.StructureToPtr(newConfig, ptr, false);
            return this.createHook.OriginalFunction((CriAtomExPlayerConfigTag*)ptr, work, workSize);
        }

        var result = this.createHook.OriginalFunction(config, work, workSize);
        Log.Information($"Player: {result:X} || Config: {(nint)config:X} || NumPlayer: {numPlayers++}");
        return result;
    }

    private uint criAtomExPlayback_GetTimeSyncedWithAudioImpl(uint playbackId)
    {
        Log.Information("GetTime");
        return this.getTimeSyncedWithAudioHook.OriginalFunction(playbackId);
    }

    private void criAtomExPlayer_SetStartTimeImpl(nint player, uint startTimeMs)
    {
        if (player == (nint)this.playerHn)
        {
            Log.Information($"Set Start Time|BGM: {startTimeMs}");
        }

        this.setStartTimeHook.OriginalFunction(player, startTimeMs);
    }

    private uint bgmPlaybackId;
    private uint currentBgmTime;

    private void PlayBgm(nint param1, nint param2, int bgmId, nint param4, nint param5)
    {
        Log.Debug($"{param1:X} || {param2:X} || {bgmId:X} || {param4:X} || {param5:X}");
        var currentBgmId = this.GetGlobalBgmId(bgmId);
        if (currentBgmId == null)
        {
            return;
        }

        if (bgmId == 341)
        {
            this.currentBgmTime = this.criAtomExPlayback_GetTimeSyncedWithAudioImpl(this.bgmPlaybackId);
            Log.Debug($"Saved BGM Time: {currentBgmTime}");
            this.criAtomExPlayer_SetCueIdImpl((nint)this.playerHn, this.bgmAcbHn, 341);
            this.criAtomExPlayer_StartImpl((nint)this.playerHn);
        }
        else if (this.currentBgmTime != 0)
        {
            this.criAtomExPlayer_SetCueIdImpl((nint)this.playerHn, this.bgmAcbHn, (int)currentBgmId);
            this.criAtomExPlayer_SetStartTimeImpl((nint)this.playerHn, this.currentBgmTime);
            this.criAtomExPlayer_StartImpl((nint)this.playerHn);
            this.currentBgmTime = 0;
        }
        else
        {
            Log.Debug($"Playing BGM ID: {currentBgmId}");
            this.playBgmHook!.OriginalFunction(param1, param2, (int)currentBgmId, param4, param5);
        }
    }

    private uint criAtomExPlayer_StartImpl(nint player)
    {
        var playbackId = this.startHook.OriginalFunction(player);
        if (player == (nint)this.playerHn)
        {
            Log.Information($"BGM Playback || Player: {player:X} || Playback ID: {playbackId}");
            this.bgmPlaybackId = playbackId;
        }

        return playbackId;
    }

    private void criAtomExPlayer_SetCategoryByIdImpl(nint player, uint id)
    {
        this.setCategoryByIdHook.OriginalFunction(player, id);
    }

    private void criAtomExPlayer_SetVolumeImpl(nint player, float volume)
    {
        this.setVolumeHook.OriginalFunction(player, volume);
    }

    private float criAtomExCategory_GetVolumeByIdImpl(uint categoryId)
    {
        return this.getVolumeByIdHook.OriginalFunction(categoryId);
    }

    private CriBool criAtomExPlayer_GetNumPlayedSamplesImpl(uint playbackId, ulong* numSamples, uint* samplingRate)
    {
        Log.Information($"{playbackId} || {*numSamples} || {*samplingRate}");
        return this.getNumPlayedSamplesHook.OriginalFunction(playbackId, numSamples, samplingRate);
    }

    private void* criAtomExAcb_LoadAcbFileImpl(nint acbBinder, byte* acbPathStr, nint awbBinder, byte* awbPathStr, void* work, int workSize)
    {
        var acbPath = Marshal.PtrToStringAnsi((nint)acbPathStr);
        var result = this.loadAcbFileHook.OriginalFunction(acbBinder, acbPathStr, awbBinder, awbPathStr, work, workSize);

        Log.Debug($"{nameof(criAtomExAcb_LoadAcbFile)}: {acbPath} || {(nint)result:X}");
        if (acbPath == "SOUND/BGM.ACB")
        {
            this.bgmAcbHn = result;
            Log.Information("BGM.ACB handle saved.");
        }

        return result;
    }

    private readonly Dictionary<string, nint> stringCache = new();

    private void criAtomExPlayer_SetCueIdImpl(nint player, void* acbHn, int cueId)
    {
        if (this.playerHn == null && acbHn == this.bgmAcbHn)
        {
            this.playerHn = (void*)player;
        }

        if (acbHn == this.bgmAcbHn && cueId >= EXTENDED_BGM_ID)
        {
            Log.Debug($"{nameof(criAtomExPlayer_SetCueId)}|BGME: {player:X} || {(nint)acbHn:X} || {cueId}");
            var bgmFile = $"FEmulator/AWB/BGME_42.AWB/{cueId - EXTENDED_BGM_ID}.adx";
            if (!this.stringCache.TryGetValue(bgmFile, out var ptr))
            {
                var strPtr = Marshal.StringToHGlobalAnsi(bgmFile);
                this.stringCache[bgmFile] = strPtr;
                ptr = strPtr;
            }

            this.criAtomExPlayer_SetFileImpl(player, IntPtr.Zero, (byte*)ptr);
            this.criAtomExPlayer_SetFormatImpl(player, CRIATOM_FORMAT.ADX);
            this.criAtomExPlayer_SetNumChannelsImpl(player, 2);
            this.criAtomExPlayer_SetSamplingRateImpl(player, 48000);
            this.criAtomExPlayer_SetCategoryByIdImpl(player, 1);
        }
        else
        {
            this.setCueIdHook.OriginalFunction(player, acbHn, cueId);
        }
    }

    private void criAtomExPlayer_SetFileImpl(nint player, nint criBinderHn, byte* path)
    {
        this.setFileHook.OriginalFunction(player, criBinderHn, path);
    }

    private void criAtomExPlayer_SetFormatImpl(nint player, CRIATOM_FORMAT format)
    {
        this.setFormatHook.OriginalFunction(player, format);
    }

    private void criAtomExPlayer_SetSamplingRateImpl(nint player, int samplingRate)
    {
        this.setSamplingRateHook.OriginalFunction(player, samplingRate);
    }

    private void criAtomExPlayer_SetNumChannelsImpl(nint player, int numChannels)
    {
        this.setNumChannelsHook.OriginalFunction(player, numChannels);
    }

    protected override void PlayBgm(int bgmId)
    {
        throw new NotImplementedException();
    }
}

internal enum CriBool
{
    CRI_TRUE,
    CRI_FALSE,
}

internal enum CRIATOM_FORMAT
    : uint
{
    NONE,
    ADX,
    HCA = 3,
    HCA_MX,
    WAVE,
    RAW_PCM,
    AIFF,
}

[StructLayout(LayoutKind.Sequential)]
internal struct CriAtomExPlayerConfigTag
{
    public int voiceAllocationMethod;
    public int maxPathStrings;
    public int maxPath;
    public byte maxAisacs;
    public bool updatesTime;
    public bool enableAudioSyncedTimer;
}