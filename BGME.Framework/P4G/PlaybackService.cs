using Ryo.Definitions.Enums;
using Ryo.Interfaces;
using SharedScans.Interfaces;
using System.Collections.Concurrent;
using static Ryo.Definitions.Functions.CriAtomExFunctions;

namespace BGME.Framework.P4G;

internal class PlaybackService
{
    private const int REWIND_MS = -150;

    private readonly ICriAtomRegistry atomRegistry;
    private readonly HookContainer<criAtomExPlayer_SetCueId> setCueId;
    private readonly HookContainer<criAtomExPlayer_Start> start;
    private readonly WrapperContainer<criAtomExPlayback_GetTimeSyncedWithAudioMicro> getTimeSyncWithAudioMicro;
    private readonly WrapperContainer<criAtomExPlayer_SetStartTime> setStartTime;
    private readonly WrapperContainer<criAtomExPlayer_GetStatus> getStatus;
    private Player? _bgmPlayer;

    private readonly ConcurrentDictionary<int, int> cuePlaybackTimes = new();
    private Action<PlaybackInfo>? bgmPlayed;

    public PlaybackService(
        ISharedScans scans,
        ICriAtomRegistry atomRegistry)
    {
        this.atomRegistry = atomRegistry;
        this.setCueId = scans.CreateHook<criAtomExPlayer_SetCueId>(this.SetCueId, Mod.NAME);
        this.start = scans.CreateHook<criAtomExPlayer_Start>(this.Start, Mod.NAME);
        this.getTimeSyncWithAudioMicro = scans.CreateWrapper<criAtomExPlayback_GetTimeSyncedWithAudioMicro>(Mod.NAME);
        this.setStartTime = scans.CreateWrapper<criAtomExPlayer_SetStartTime>(Mod.NAME);
        this.getStatus = scans.CreateWrapper<criAtomExPlayer_GetStatus>(Mod.NAME);

        this.bgmPlayed += this.OnBgmPlayed;
    }

    private int currentCueId;

    private void OnBgmPlayed(PlaybackInfo info)
    {
        Log.Information($"Playback ID: {info.PlaybackId} || Cue ID: {info.CueId}");
        Task.Run(() =>
        {
            while (true)
            {
                var status = this.getStatus.Wrapper(this.BgmPlayer.Handle);
                if (status == CriAtomExPlayerStatusTag.CRIATOMEXPLAYER_STATUS_PLAYING)
                {
                    var currentTimeMicro = this.getTimeSyncWithAudioMicro.Wrapper(info.PlaybackId);
                    var currentTimeMs = currentTimeMicro / 1000;

                    if (currentTimeMs > 0)
                    {
                        this.cuePlaybackTimes[info.CueId] = currentTimeMicro;
                    }
                }

                if (status > CriAtomExPlayerStatusTag.CRIATOMEXPLAYER_STATUS_PLAYING)
                {
                    return;
                }

                Thread.Sleep(200);
            }
        });
    }

    private uint Start(nint playerHn)
    {
        if (this.BgmPlayer.Handle == playerHn)
        {
            if (this.cuePlaybackTimes.TryGetValue(this.currentCueId, out var timeMicro))
            {
                var timeMs = timeMicro / 1000;
                var newTimeMs = Math.Max(0, timeMs - REWIND_MS);

                Log.Information($"{this.currentCueId}: Set time to {newTimeMs}ms");
                this.setStartTime.Wrapper(playerHn, newTimeMs);
            }
            else
            {
                this.setStartTime.Wrapper(playerHn, 0);
                Log.Information($"{this.currentCueId}: Set time to 0ms");
            }

        }

        var playbackId = this.start.Hook!.OriginalFunction(playerHn);
        if (this.BgmPlayer.Handle == playerHn)
        {
            this.bgmPlayed?.Invoke(new(playbackId, this.currentCueId));
        }

        return playbackId;
    }

    public Player BgmPlayer
    {
        get
        {
            if (this._bgmPlayer == null)
            {
                this._bgmPlayer = this.atomRegistry.GetPlayerById(0)!;
            }

            return this._bgmPlayer;
        }
    }

    private void SetCueId(nint playerHn, nint acbHn, int cueId)
    {
        if (playerHn == this.BgmPlayer.Handle)
        {
            this.currentCueId = cueId;

            if (cueId == 2000)
            {
                this.currentCueId = 33;
            }
        }

        this.setCueId.Hook!.OriginalFunction(playerHn, acbHn, cueId);
    }

    private record PlaybackInfo(uint PlaybackId, int CueId);
}
