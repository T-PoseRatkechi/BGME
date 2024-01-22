using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using static BGME.Framework.CRI.CriAtomExFunctions;
using System.Runtime.InteropServices;
using BGME.Framework.CRI.Types;
using PersonaModdingMetadata.Shared.Games;
using Reloaded.Hooks.Definitions;

namespace BGME.Framework.CRI;

#pragma warning disable IDE1006 // Naming Styles
internal unsafe class CriAtomEx : IGameHook
{
    private const int EXTENDED_BGM_ID = 10000;

    private IHook<criAtomExPlayer_GetNumPlayedSamples>? getNumPlayedSamplesHook;
    private IHook<criAtomExAcb_LoadAcbFile>? loadAcbFileHook;
    private IHook<criAtomExPlayer_SetCueId>? setCueIdHook;
    private IHook<criAtomExPlayer_Start>? startHook;
    private IHook<criAtomExPlayer_SetFile>? setFileHook;
    private IHook<criAtomExPlayer_SetFormat>? setFormatHook;
    private IHook<criAtomExPlayer_SetSamplingRate>? setSamplingRateHook;
    private IHook<criAtomExPlayer_SetNumChannels>? setNumChannelsHook;
    private IHook<criAtomExCategory_GetVolumeById>? getVolumeByIdHook;
    private IHook<criAtomExPlayer_SetVolume>? setVolumeHook;
    private IHook<criAtomExPlayer_SetCategoryById>? setCategoryByIdHook;
    private IHook<criAtomExPlayer_SetStartTime>? setStartTimeHook;
    private IHook<criAtomExPlayback_GetTimeSyncedWithAudio>? getTimeSyncedWithAudioHook;
    private IHook<criAtomExPlayer_Create>? createHook;

    private readonly Game game;
    private readonly CriAtomExPatterns patterns;

    private Dictionary<int, CriAtomExPlayerConfigTag> playerConfigs = new();
    private readonly List<PlayerConfig> players = new();
    private readonly List<AcbConfig> acbs = new();

    public CriAtomEx(Game game)
    {
        this.game = game;
        this.patterns = CriAtomExGames.GetGamePatterns(game);
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan(nameof(criAtomExPlayer_Create), this.patterns.CriAtomExPlayer_Create, result =>
        {
            this.createHook = hooks.CreateHook<criAtomExPlayer_Create>(this.criAtomExPlayer_CreateImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetStartTime), this.patterns.CriAtomExPlayer_SetStartTime, result =>
        {
            this.setStartTimeHook = hooks.CreateHook<criAtomExPlayer_SetStartTime>(this.criAtomExPlayer_SetStartTimeImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayback_GetTimeSyncedWithAudio), this.patterns.CriAtomExPlayback_GetTimeSyncedWithAudio, result =>
        {
            this.getTimeSyncedWithAudioHook = hooks.CreateHook<criAtomExPlayback_GetTimeSyncedWithAudio>(this.criAtomExPlayback_GetTimeSyncedWithAudioImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_GetNumPlayedSamples), this.patterns.CriAtomExPlayer_GetNumPlayedSamples, result =>
        {
            this.getNumPlayedSamplesHook = hooks.CreateHook<criAtomExPlayer_GetNumPlayedSamples>(this.criAtomExPlayer_GetNumPlayedSamplesImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExAcb_LoadAcbFile), this.patterns.CriAtomExAcb_LoadAcbFile, result =>
        {
            this.loadAcbFileHook = hooks.CreateHook<criAtomExAcb_LoadAcbFile>(this.criAtomExAcb_LoadAcbFileImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetCueId), this.patterns.CriAtomExPlayer_SetCueId, result =>
        {
            this.setCueIdHook = hooks.CreateHook<criAtomExPlayer_SetCueId>(this.criAtomExPlayer_SetCueIdImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_Start), this.patterns.CriAtomExPlayer_Start, result =>
        {
            this.startHook = hooks.CreateHook<criAtomExPlayer_Start>(this.criAtomExPlayer_StartImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetFile), this.patterns.CriAtomExPlayer_SetFile, result =>
        {
            this.setFileHook = hooks.CreateHook<criAtomExPlayer_SetFile>(this.criAtomExPlayer_SetFileImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetFormat), this.patterns.CriAtomExPlayer_SetFormat, result =>
        {
            this.setFormatHook = hooks.CreateHook<criAtomExPlayer_SetFormat>(this.criAtomExPlayer_SetFormatImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetSamplingRate), this.patterns.CriAtomExPlayer_SetSamplingRate, result =>
        {
            this.setSamplingRateHook = hooks.CreateHook<criAtomExPlayer_SetSamplingRate>(this.criAtomExPlayer_SetSamplingRateImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetNumChannels), this.patterns.CriAtomExPlayer_SetNumChannels, result =>
        {
            this.setNumChannelsHook = hooks.CreateHook<criAtomExPlayer_SetNumChannels>(this.criAtomExPlayer_SetNumChannelsImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExCategory_GetVolumeById), this.patterns.CriAtomExCategory_GetVolumeById, result =>
        {
            this.getVolumeByIdHook = hooks.CreateHook<criAtomExCategory_GetVolumeById>(this.criAtomExCategory_GetVolumeByIdImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetVolume), this.patterns.CriAtomExPlayer_SetVolume, result =>
        {
            this.setVolumeHook = hooks.CreateHook<criAtomExPlayer_SetVolume>(this.criAtomExPlayer_SetVolumeImpl, result).Activate();
        });

        scanner.Scan(nameof(criAtomExPlayer_SetCategoryById), this.patterns.CriAtomExPlayer_SetCategoryById, result =>
        {
            this.setCategoryByIdHook = hooks.CreateHook<criAtomExPlayer_SetCategoryById>(this.criAtomExPlayer_SetCategoryByIdImpl, result).Activate();
        });
    }

    public PlayerConfig? GetPlayerByAcbPath(string acbPath)
        => this.players.FirstOrDefault(x => x.Acb.AcbPath == acbPath);

    public void SetPlayerConfigById(int id, CriAtomExPlayerConfigTag config)
        => this.playerConfigs[id] = config;

    public nint criAtomExPlayer_CreateImpl(CriAtomExPlayerConfigTag* config, void* work, int workSize)
    {
        Log.Verbose($"Create || Config: {(nint)config:X} || Work: {(nint)work:X} || WorkSize: {workSize}");

        var playerId = this.players.Count;
        var currentConfigPtr = config;
        if (this.playerConfigs.TryGetValue(playerId, out var newConfig))
        {
            var newConfigPtr = Marshal.AllocHGlobal(sizeof(CriAtomExPlayerConfigTag));
            Marshal.StructureToPtr(newConfig, newConfigPtr, false);
            Log.Information($"Using custom player config for: {playerId}");
        }

        var playerHn = this.createHook!.OriginalFunction(currentConfigPtr, work, workSize);
        this.players.Add(new()
        {
            PlayerHn = playerHn,
        });

        Log.Debug($"Player: {playerHn:X} || Config: {(nint)config:X} || ID: {playerId}");
        return playerHn;
    }

    public uint criAtomExPlayback_GetTimeSyncedWithAudioImpl(uint playbackId)
    {
        var result = this.getTimeSyncedWithAudioHook!.OriginalFunction(playbackId);
        Log.Debug($"GetTimeSyncedWithAudio || Playback ID: {playbackId} || Time: {result}ms");
        return result;
    }

    public void criAtomExPlayer_SetStartTimeImpl(nint player, uint startTimeMs)
    {
        this.setStartTimeHook!.OriginalFunction(player, startTimeMs);
    }

    public uint criAtomExPlayer_StartImpl(nint playerHn)
    {
        return this.startHook!.OriginalFunction(playerHn);
    }

    public void criAtomExPlayer_SetCategoryByIdImpl(nint player, uint id)
    {
        this.setCategoryByIdHook!.OriginalFunction(player, id);
    }

    public void criAtomExPlayer_SetVolumeImpl(nint player, float volume)
    {
        this.setVolumeHook!.OriginalFunction(player, volume);
    }

    public float criAtomExCategory_GetVolumeByIdImpl(uint categoryId)
    {
        return this.getVolumeByIdHook!.OriginalFunction(categoryId);
    }

    public CriBool criAtomExPlayer_GetNumPlayedSamplesImpl(uint playbackId, ulong* numSamples, uint* samplingRate)
    {
        Log.Information($"{playbackId} || {*numSamples} || {*samplingRate}");
        return this.getNumPlayedSamplesHook!.OriginalFunction(playbackId, numSamples, samplingRate);
    }

    public nint criAtomExAcb_LoadAcbFileImpl(nint acbBinder, byte* acbPathStr, nint awbBinder, byte* awbPathStr, void* work, int workSize)
    {
        var acbHn = this.loadAcbFileHook!.OriginalFunction(acbBinder, acbPathStr, awbBinder, awbPathStr, work, workSize);
        var acbPath = Marshal.PtrToStringAnsi((nint)acbPathStr)!;

        this.acbs.Add(new()
        {
            AcbHn = acbHn,
            AcbPath = acbPath,
        });

        Log.Debug($"{nameof(criAtomExAcb_LoadAcbFile)}: {acbPath} || {(nint)acbHn:X}");
        return acbHn;
    }

    public void criAtomExPlayer_SetCueIdImpl(nint playerHn, nint acbHn, int cueId)
    {
        // Update player ACB.
        var player = this.players.First(x => x.PlayerHn == playerHn);
        var acb = this.acbs.First(x => x.AcbHn == acbHn);
        player.Acb = acb;

        this.setCueIdHook!.OriginalFunction(playerHn, acbHn, cueId);

        //if (this.playerHn == null && acbHn == this.bgmAcbHn)
        //{
        //    this.playerHn = (void*)playerHn;
        //}

        //if (acbHn == this.bgmAcbHn && cueId >= EXTENDED_BGM_ID)
        //{
        //    Log.Debug($"{nameof(criAtomExPlayer_SetCueId)}|BGME: {playerHn:X} || {(nint)acbHn:X} || {cueId}");
        //    var bgmFile = $"FEmulator/AWB/BGM_42.AWB/{cueId - EXTENDED_BGM_ID}.adx";
        //    var ptr = StringsCache.GetStringPtr(bgmFile);

        //    this.criAtomExPlayer_SetFileImpl(playerHn, IntPtr.Zero, (byte*)ptr);
        //    this.criAtomExPlayer_SetFormatImpl(playerHn, CRIATOM_FORMAT.ADX);
        //    this.criAtomExPlayer_SetNumChannelsImpl(playerHn, 2);
        //    this.criAtomExPlayer_SetSamplingRateImpl(playerHn, 48000);
        //    this.criAtomExPlayer_SetCategoryByIdImpl(playerHn, 1);
        //}
        //else
        //{
        //    this.setCueIdHook!.OriginalFunction(playerHn, acbHn, cueId);
        //}
    }

    public void criAtomExPlayer_SetFileImpl(nint player, nint criBinderHn, byte* path)
    {
        this.setFileHook!.OriginalFunction(player, criBinderHn, path);
    }

    public void criAtomExPlayer_SetFormatImpl(nint player, CRIATOM_FORMAT format)
    {
        this.setFormatHook!.OriginalFunction(player, format);
    }

    public void criAtomExPlayer_SetSamplingRateImpl(nint player, int samplingRate)
    {
        this.setSamplingRateHook!.OriginalFunction(player, samplingRate);
    }

    public void criAtomExPlayer_SetNumChannelsImpl(nint player, int numChannels)
    {
        this.setNumChannelsHook!.OriginalFunction(player, numChannels);
    }
}

internal class PlayerConfig
{
    public nint PlayerHn { get; set; }

    public AcbConfig Acb { get; set; } = new();
}

internal class AcbConfig
{
    public nint AcbHn { get; set; }

    public string AcbPath { get; set; } = string.Empty;
}