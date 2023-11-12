using PersonaMusicScript.Library.Models;

namespace BGME.Framework.Music;

internal abstract class BaseSound
{
    private readonly MusicService music;

    private int prevOriginalBgmId;
    private int newBgmId;

    public BaseSound(MusicService music)
    {
        this.music = music;
    }

    protected int GetGlobalBgmId(int originalBgmId)
    {
        if (this.music.Global.TryGetValue(originalBgmId, out var newMusic))
        {
            Log.Debug($"Global BGM overwriting BGM ID: {originalBgmId}");
            if (this.prevOriginalBgmId == originalBgmId && newMusic.Type == MusicType.RandomSong)
            {
                Log.Debug("Reusing previous random song.");
                return this.newBgmId;
            }

            this.prevOriginalBgmId = originalBgmId;
            this.newBgmId = Utilities.CalculateMusicId(newMusic);

            return this.newBgmId;
        }

        this.prevOriginalBgmId = originalBgmId;
        return originalBgmId;
    }
}
