using PersonaMusicScript.Types.Music;

namespace BGME.Framework.Music;

public abstract class BaseSound
{
    private readonly MusicService music;

    private int prevOriginalBgmId;
    private int? currentBgmId;
    private bool isVictoryDisabled;

    public BaseSound(MusicService music)
    {
        this.music = music;
    }

    protected abstract int VictoryBgmId { get; }

    public void RefreshBgm()
    {
        this.PlayBgm(this.prevOriginalBgmId);
    }

    public void SetVictoryDisabled(bool isDisabled)
        => this.isVictoryDisabled = isDisabled;

    protected abstract void PlayBgm(int bgmId);

    protected int? GetGlobalBgmId(int originalBgmId)
    {
        if (originalBgmId == this.VictoryBgmId && this.isVictoryDisabled)
        {
            return null;
        }

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
