using Serilog;

namespace BGME.Framework.Music;

internal abstract class BaseSound
{
    private readonly MusicService music;

    public BaseSound(MusicService music)
    {
        this.music = music;
    }

    protected int? GetGlobalBgmId(int bgmId)
    {
        if (this.music.Global.TryGetValue(bgmId, out var newMusic))
        {
            Log.Information("Global BGM overwriting BGM ID: {id}", bgmId);
            return Utilities.CalculateMusicId(newMusic);
        }

        return null;
    }
}
