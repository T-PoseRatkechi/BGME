using BGME.Framework.CRI;
using BGME.Framework.CRI.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using static BGME.Framework.CRI.CriAtomExFunctions;

namespace BGME.Framework.P3R;

internal unsafe class BgmeService : IBgmeService
{
    private readonly CriAtomEx criAtomEx;
    private IHook<criAtomExPlayer_SetCueName>? setCueName;

    private nint songBuffer;
    private int bufferSize;

    public BgmeService(CriAtomEx criAtomEx, string songFile)
    {
        this.criAtomEx = criAtomEx;

        var songData = File.ReadAllBytes(songFile);
        this.songBuffer = Marshal.AllocHGlobal(songData.Length);
        this.bufferSize = songData.Length;
        Marshal.Copy(songData, 0, this.songBuffer,this.bufferSize);

        criAtomEx.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(criAtomEx.SetCueName))
            {
                this.setCueName = criAtomEx.SetCueName!.Hook(this.CriAtomExPlayer_SetCueName).Activate();
            }
        };
    }

    private void CriAtomExPlayer_SetCueName(nint playerHn, nint acbHn, byte* cueName)
    {
        //var bgmPlayer = this.criAtomEx.GetPlayerByPreviousCueName("Title_Env_Dark");
        var cueNameStr = Marshal.PtrToStringAnsi((nint)cueName);
        if (cueNameStr == "Title_Env_Dark")
        {
            // Play test song.
            Log.Debug($"Playing test song.");
            this.criAtomEx.Player_SetData(playerHn, (byte*)this.songBuffer, this.bufferSize);
            this.criAtomEx.Player_SetFormat(playerHn, CRIATOM_FORMAT.HCA);
            this.criAtomEx.Player_SetNumChannels(playerHn, 2);
            this.criAtomEx.Player_SetSamplingRate(playerHn, 41000);
        }
        else
        {
            this.setCueName!.OriginalFunction(playerHn, acbHn, cueName);
        }
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
    }
}
