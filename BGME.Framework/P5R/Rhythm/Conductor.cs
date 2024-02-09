namespace BGME.Framework.P5R.Rhythm;

internal class Conductor
{
    public float SongBpm { get; set; }

    public float SecPerBeat { get; set; }

    public float SongPositionInSeconds { get; set; }

    public float SongPositionInBeats { get; set; }

    public DateTime DspSongTime { get; set; }

    public void Start(float songBpm)
    {
        SongBpm = songBpm;
        SecPerBeat = 60f / songBpm;
        DspSongTime = DateTime.Now;
    }

    public void Update(int songPosMs)
    {
        SongPositionInSeconds = (float)TimeSpan.FromMilliseconds(songPosMs).TotalSeconds;
        SongPositionInBeats = SongPositionInSeconds / SecPerBeat;
    }
}
