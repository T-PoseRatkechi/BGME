using BGME.Framework.Models;
using PersonaMusicScript.Library.Models;
using Serilog;
using System.Diagnostics;

namespace BGME.Framework;

internal static class Utilities
{
    public static Random Random = new();
    public static string PushCallerRegisters = "push rcx\npush rdx\npush r8\npush r9";
    public static string PopCallerRegisters = "pop r9\npop r8\npop rdx\npop rcx";
    public static nint BaseAddress = Process.GetCurrentProcess().MainModule?.BaseAddress ?? 0;

    public static int CalculateMusicId(IMusic music)
    {
        if (music is Song song)
        {
            Log.Debug("Song ID: {id}", song.Id);
            return song.Id;
        }
        else if (music is RandomSong randomSong)
        {
            var randomId = Random.Next(randomSong.MinSongId, randomSong.MaxSongId);
            Log.Debug("Random Song ID from ({min}, {max}): {id}", randomSong.MinSongId, randomSong.MaxSongId, randomId);
            return randomId;
        }

        return -1;
    }

    public static int CalculateMusicId(IMusic music, EncounterContext context)
    {
        if (music is BattleBgm battleBgm)
        {
            Log.Debug("Battle BGM with Context: {context}", context);
            if (context == EncounterContext.Normal
                && battleBgm.NormalMusic != null)
            {
                return CalculateMusicId(battleBgm.NormalMusic);
            }

            if (context == EncounterContext.Advantage
                && battleBgm.AdvantageMusic != null)
            {
                return CalculateMusicId(battleBgm.AdvantageMusic);
            }

            if (context == EncounterContext.Disadvantage
                && battleBgm.DisadvantageMusic != null)
            {
                return CalculateMusicId(battleBgm.DisadvantageMusic);
            }
        }

        return CalculateMusicId(music);
    }
}
