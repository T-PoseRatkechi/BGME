using p5rpc.lib.interfaces;

namespace BGME.Framework.P5R.Rhythm;

internal class RhythmGame
{
    private readonly IP5RLib p5rLib;
    private readonly BgmPlayback bgm;
    private readonly EffectsHook effectsHook;

    private uint initialPlaybackId;
    private uint? battleBgmPlaybackId;

    private Conductor conductor = new();
    private readonly BeatHitEffect beatHitEffect;

    private readonly HashSet<int> beatsPlayed = new();

    private float nextHitActivation = 0;
    private Task? rhythmGameLoop;
    private CancellationTokenSource rhythmGameLoopToken;

    public RhythmGame(
        IP5RLib p5rLib,
        //CriAtomEx criAtomEx,
        BgmPlayback bgm,
        BeatHitEffect beatHitEffect)
    {
        this.p5rLib = p5rLib;
        this.beatHitEffect = beatHitEffect;
        //this.criAtomEx = criAtomEx;
        this.bgm = bgm;
    }

    public void StartBattleBgm()
    {
        //Log.Information("Starting Battle Rhythm Game");
        //this.initialPlaybackId = this.criAtomEx.Player_GetLastPlaybackId(this.bgm.BgmPlayer.PlayerHn);
        //Log.Information($"Initial BGM: {initialPlaybackId}");
        //this.battleBgmPlaybackId = null;

        //this.rhythmGameLoopToken = new();
        //this.rhythmGameLoop = Task.Run(() =>
        //{
        //    var lastUpdate = DateTime.Now;
        //    while (true)
        //    {
        //        var current = DateTime.Now;
        //        var elapsedMs = (current - lastUpdate).TotalMilliseconds;
        //        if (elapsedMs > 1)
        //        {
        //            this.UpdateRhythmGame();
        //            lastUpdate = DateTime.Now;
        //        }
        //    }
        //}, this.rhythmGameLoopToken.Token);

        //this.rhythmGameLoopToken.Token.Register(() =>
        //{
        //    this.initialPlaybackId = 0;
        //    this.battleBgmPlaybackId = null;
        //    this.beatsPlayed.Clear();
        //    this.beatHitEffect.Deactivate();
        //    this.p5rLib.FlowCaller.FLD_DASH_EFFECT(0);
        //    this.nextHitActivation = 0;
        //    Log.Information("Stopped Battle Rhythm Game");
        //});
    }

    private void UpdateRhythmGame()
    {
        //var currentPlaybackId = this.criAtomEx.Player_GetLastPlaybackId(this.bgm.BgmPlayer.PlayerHn);

        //// Wait for BGM to change.
        //if (this.initialPlaybackId == currentPlaybackId)
        //{
        //    return;
        //}

        //// Set battle BGM playback ID.
        //else if (this.battleBgmPlaybackId == null)
        //{
        //    Log.Information($"Battle BGM: {currentPlaybackId}");
        //    this.battleBgmPlaybackId = currentPlaybackId;
        //    this.conductor = new();
        //    this.conductor.Start(125);
        //}

        //// End battle loop when BGM ends.
        //else if (currentPlaybackId != this.battleBgmPlaybackId)
        //{
        //    Log.Information($"Current BGM: {currentPlaybackId}");
        //    this.rhythmGameLoopToken.Cancel();
        //}

        //var currentBgmTime = this.criAtomEx.Playback_GetTimeSyncedWithAudio((uint)this.battleBgmPlaybackId!);
        //this.conductor.Update(currentBgmTime);
        //this.beatHitEffect.Update();

        //// Visual window logic.
        //var currentWholeBeat = (int)Math.Round(this.conductor.SongPositionInBeats);
        //if (!this.beatsPlayed.Contains(currentWholeBeat) && currentWholeBeat % 2 == 0)
        //{
        //    this.beatsPlayed.Add(currentWholeBeat);

        //    Log.Information($"Visual Beat: {this.conductor.SongPositionInSeconds}");
        //    Log.Information($"Next Hit Beat: {this.conductor.SongPositionInSeconds}");
        //    this.p5rLib.FlowCaller.FLD_DASH_EFFECT(1);
        //}
        //else
        //{
        //    this.p5rLib.FlowCaller.FLD_DASH_EFFECT(0);
        //}

        //// Hit window logic.
        //if (this.conductor.SongPositionInSeconds >= this.nextHitActivation)
        //{
        //    Log.Debug($"Current Hit: {this.nextHitActivation}");
        //    this.nextHitActivation = this.beatHitEffect.Activate(conductor);
        //}
    }
}
