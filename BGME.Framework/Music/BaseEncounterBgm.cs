using BGME.Framework.Models;
using PersonaMusicScript.Types.MusicCollections.Entries;

namespace BGME.Framework.Music;

public abstract class BaseEncounterBgm
{
    private readonly MusicService music;
    private EncounterMusic? currentEncounterMusic;
    private bool isVictoryDisabled;

    public BaseEncounterBgm(MusicService music)
    {
        this.music = music;
    }

    public void SetVictoryDisabled(bool isDisabled)
        => this.isVictoryDisabled = isDisabled;

    protected int GetVictoryMusic()
    {
        if (currentEncounterMusic?.Encounter.VictoryMusic != null && this.isVictoryDisabled == false)
        {
            Log.Debug("Victory Music uses BGME");
            var musicId = MusicUtils.CalculateMusicId(currentEncounterMusic.Encounter.VictoryMusic, currentEncounterMusic.Context);
            return musicId;
        }

        return -1;
    }

    protected int GetBattleMusic(int encounterId, EncounterContext context)
    {
        this.SetCurrentEncounter(encounterId, context);
        if (this.currentEncounterMusic?.Encounter.BattleMusic != null)
        {
            Log.Debug("Battle Music uses BGME");
            var musicId = MusicUtils.CalculateMusicId(this.currentEncounterMusic.Encounter.BattleMusic, context);
            return musicId;
        }

        return -1;
    }

    private void SetCurrentEncounter(int encounterId, EncounterContext context)
    {
        // Reset encounter music.
        this.currentEncounterMusic = null;

        Log.Debug($"Encounter: {encounterId}");
        Log.Debug($"Context: {context}");
        if (music.Encounters.TryGetValue(encounterId, out var encounter))
        {
            Log.Debug("Encounter uses BGME");
            this.currentEncounterMusic = new(encounter, context);
        }
    }

    protected record EncounterMusic(EncounterEntry Encounter, EncounterContext Context);
}
