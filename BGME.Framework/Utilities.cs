﻿using BGME.Framework.Models;
using PersonaMusicScript.Types.Music;
using System.Diagnostics;

namespace BGME.Framework;

internal static class Utilities
{
    public static string PushCallerRegisters = "push rcx\npush rdx\npush r8\npush r9";
    public static string PopCallerRegisters = "pop r9\npop r8\npop rdx\npop rcx";
    public static nint BaseAddress = Process.GetCurrentProcess().MainModule?.BaseAddress ?? 0;

    public static ushort ToBigEndian(this ushort value)
    {
        var bigEndianValue = BitConverter.ToUInt16(BitConverter.GetBytes(value).Reverse().ToArray());
        return bigEndianValue;
    }

    /// <summary>
    /// Calculate the BGM ID to play given <paramref name="music"/>.
    /// </summary>
    /// <param name="music">Music to play.</param>
    /// <returns>BGM ID to play, or null if music is disabled.</returns>
    public static int? CalculateMusicId(IMusic music)
    {
        if (music is Song song)
        {
            Log.Debug($"Song ID: {song.Id}");
            return song.Id;
        }
        else if (music is RandomSong randomSong)
        {
            var randomId = randomSong.GetRandomId();

            // Random BGM from list or range log.
            if (randomSong.BgmIds != null)
            {
                Log.Debug($"Random Song ID from [{string.Join(", ", randomSong.BgmIds.Select(x => x.ToString()))}]: {randomId}");
            }
            else
            {
                Log.Debug($"Random Song ID from ({randomSong.MinSongId}, {randomSong.MaxSongId}): {randomId}");
            }

            return randomId;
        }
        else if (music is Sound sound)
        {
            return CalculateMusicId(sound.Music);
        }
        else if (music is DisableMusic)
        {
            Log.Debug("Music is disabled.");
            return null;
        }

        return -1;
    }

    /// <summary>
    /// Calculate the Encounter BGM ID to play given <paramref name="music"/> and the <paramref name="context"/>.
    /// </summary>
    /// <param name="music">Music to play.</param>
    /// <param name="context">Encounter context.</param>
    /// <returns>BGM ID to play, -1 if none set or disabled.</returns>
    public static int CalculateMusicId(IMusic music, EncounterContext context)
    {
        if (music is BattleBgm battleBgm)
        {
            Log.Debug($"Battle BGM with Context: {context}");
            if (context == EncounterContext.Normal
                && battleBgm.NormalMusic != null)
            {
                return CalculateMusicId(battleBgm.NormalMusic) ?? -1;
            }

            if (context == EncounterContext.Advantage
                && battleBgm.AdvantageMusic != null)
            {
                return CalculateMusicId(battleBgm.AdvantageMusic) ?? -1;
            }

            if (context == EncounterContext.Disadvantage
                && battleBgm.DisadvantageMusic != null)
            {
                return CalculateMusicId(battleBgm.DisadvantageMusic) ?? -1;
            }

            return -1;
        }

        return CalculateMusicId(music) ?? -1;
    }
}
