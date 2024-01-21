using BGME.Framework.P5R;
using Reloaded.Hooks.Definitions.X64;

namespace BGME.Framework.CRI;

internal unsafe static class CriAdx
{
    [Function(CallingConventions.Microsoft)]
    public delegate CriBool criAtomExPlayer_GetNumPlayedSamples(uint playbackId, ulong* numSamples, uint* samplingRate);

    [Function(CallingConventions.Microsoft)]
    public delegate void* criAtomExAcb_LoadAcbFile(nint acbBinder, byte* acbPathStr, nint awbBinder, byte* awbPathStr, void* work, int workSize);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetCueId(nint player, void* acbHn, int cueId);

    [Function(CallingConventions.Microsoft)]
    public delegate uint criAtomExPlayer_Start(nint player);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetFile(nint player, nint criBinderHn, byte* path);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetFormat(nint player, CRIATOM_FORMAT format);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetSamplingRate(nint player, int samplingRate);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetNumChannels(nint player, int numChannels);

    [Function(CallingConventions.Microsoft)]
    public delegate float criAtomExCategory_GetVolumeById(uint categoryId);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetVolume(nint player, float volume);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetCategoryById(nint player, uint id);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetStartTime(nint player, uint startTimeMs);

    [Function(CallingConventions.Microsoft)]
    public delegate uint criAtomExPlayback_GetTimeSyncedWithAudio(uint playbackId);

    [Function(CallingConventions.Microsoft)]
    public delegate nint criAtomExPlayer_Create(CriAtomExPlayerConfigTag* config, void* work, int workSize);
}
