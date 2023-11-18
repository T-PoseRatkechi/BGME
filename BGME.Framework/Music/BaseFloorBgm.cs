namespace BGME.Framework.Music;

internal abstract class BaseFloorBgm
{
    private readonly MusicService music;

    public BaseFloorBgm(MusicService music)
    {
        this.music = music;
    }

    protected int GetFloorBgm(int floorId)
    {
        Log.Debug($"Floor: {floorId}");
        if (this.music.Floors.TryGetValue(floorId, out var floorMusic))
        {
            Log.Debug("Floor uses BGME");
            return Utilities.CalculateMusicId(floorMusic) ?? -1;
        }

        return -1;
    }
}
