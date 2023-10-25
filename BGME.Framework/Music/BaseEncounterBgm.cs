using BGME.Framework.Models;
using PersonaMusicScript.Library.Models;
using Serilog;

namespace BGME.Framework.Music;

internal abstract class BaseEncounterBgm
{
    private readonly MusicService music;
    private EncounterMusic? currentEncounterMusic;

    public BaseEncounterBgm(MusicService music)
    {
        this.music = music;
    }

    protected int GetVictoryMusic()
    {
        if (currentEncounterMusic?.Encounter.VictoryMusic != null)
        {
            Log.Debug("Victory Music uses BGME");
            var musicId = Utilities.CalculateMusicId(currentEncounterMusic.Encounter.VictoryMusic, currentEncounterMusic.Context);
            currentEncounterMusic = null;
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
            var musicId = Utilities.CalculateMusicId(this.currentEncounterMusic.Encounter.BattleMusic, context);
            return musicId;
        }

        return -1;
    }

    private void SetCurrentEncounter(int encounterId, EncounterContext context)
    {
        Log.Debug("Encounter: {id}", encounterId);
        Log.Debug("Context: {context}", context);
        if (music.Encounters.TryGetValue(encounterId, out var encounter))
        {
            Log.Debug("Encounter uses BGME");
            this.currentEncounterMusic = new(encounter, context);
        }
    }

    protected record EncounterMusic(Encounter Encounter, EncounterContext Context);
}
