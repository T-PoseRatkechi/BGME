using BGME.Framework.Models;
using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;

namespace BGME.Framework.Metaphor;

internal unsafe class EncounterBgm : BaseEncounterBgm
{
    private delegate void PlayBattleBGM(Battle* battle, nint cueId, nint param3, nint param4);
    private IHook<PlayBattleBGM>? playBgmHoook;

    public EncounterBgm(MusicService music)
        : base(music)
    {
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan(
            nameof(PlayBattleBGM),
            "48 89 5C 24 ?? 57 48 83 EC 20 8B DA 48 8B F9 E8 ?? ?? ?? ?? 3B C3 74",
            result => this.playBgmHoook = hooks.CreateHook<PlayBattleBGM>(this.PlayBattleBgmImpl, result).Activate());
    }

    private void PlayBattleBgmImpl(Battle* battle, nint cueId, nint param3, nint param4)
    {
        var currentBgmId = cueId;

        var isVictoryBgm = cueId == 1090;
        if (isVictoryBgm)
        {
            currentBgmId = this.GetVictoryMusic();
        }
        else
        {
            currentBgmId = this.GetBattleMusic(battle->EncountId, battle->Context);
        }

        this.playBgmHoook!.OriginalFunction(battle, currentBgmId, param3, param4);
    }


    [StructLayout(LayoutKind.Explicit)]
    private unsafe struct Battle
    {
        [FieldOffset(0x25c)]
        public ushort EncountId;

        [FieldOffset(0x298)]
        public EncounterContext Context;

        [FieldOffset(0x7f4)]
        public int BgmId;
    }
}
