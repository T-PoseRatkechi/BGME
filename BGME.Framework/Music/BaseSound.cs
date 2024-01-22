using PersonaMusicScript.Types.Music;

namespace BGME.Framework.Music;

internal abstract class BaseSound
{
    private readonly MusicService music;

    private int prevOriginalBgmId;
    private int? currentBgmId;

    public BaseSound(MusicService music)
    {
        this.music = music;
    }

    public void RefreshBgm()
    {
        this.PlayBgm(this.prevOriginalBgmId);
    }

    protected abstract void PlayBgm(int bgmId);

    protected int? GetGlobalBgmId(int originalBgmId)
    {
        if (this.music.Global.TryGetValue(originalBgmId, out var newMusic))
        {
            Log.Debug($"Global BGM overwriting BGM ID: {originalBgmId}");
            if (this.prevOriginalBgmId == originalBgmId && newMusic.Type == MusicType.RandomSong)
            {
                Log.Debug("Reusing previous random song.");
                return this.currentBgmId;
            }

            this.prevOriginalBgmId = originalBgmId;
            this.currentBgmId = MusicUtils.CalculateMusicId(newMusic);
            if (this.currentBgmId == null)
            {
                Log.Debug($"BGM ID: {originalBgmId} is disabled.");
            }

            return this.currentBgmId;
        }

        this.prevOriginalBgmId = originalBgmId;
        return originalBgmId;
    }
}
