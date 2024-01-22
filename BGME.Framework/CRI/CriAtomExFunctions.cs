﻿using BGME.Framework.CRI.Types;
using Reloaded.Hooks.Definitions.X64;

namespace BGME.Framework.CRI;

internal unsafe static class CriAtomExFunctions
{
    [Function(CallingConventions.Microsoft)]
    public delegate CriBool criAtomExPlayer_GetNumPlayedSamples(uint playbackId, ulong* numSamples, uint* samplingRate);

    [Function(CallingConventions.Microsoft)]
    public delegate nint criAtomExAcb_LoadAcbFile(nint acbBinder, byte* acbPathStr, nint awbBinder, byte* awbPathStr, void* work, int workSize);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetCueId(nint playerHn, nint acbHn, int cueId);

    [Function(CallingConventions.Microsoft)]
    public delegate uint criAtomExPlayer_Start(nint playerHn);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetFile(nint playerHn, nint criBinderHn, byte* path);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetFormat(nint playerHn, CRIATOM_FORMAT format);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetSamplingRate(nint plaplayerHnyer, int samplingRate);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetNumChannels(nint playerHn, int numChannels);

    [Function(CallingConventions.Microsoft)]
    public delegate float criAtomExCategory_GetVolumeById(uint categoryId);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetVolume(nint playerHn, float volume);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetCategoryById(nint playerHn, uint id);

    [Function(CallingConventions.Microsoft)]
    public delegate void criAtomExPlayer_SetStartTime(nint playerHn, int startTimeMs);

    [Function(CallingConventions.Microsoft)]
    public delegate int criAtomExPlayback_GetTimeSyncedWithAudio(uint playbackId);

    [Function(CallingConventions.Microsoft)]
    public delegate nint criAtomExPlayer_Create(CriAtomExPlayerConfigTag* config, void* work, int workSize);

    [Function(CallingConventions.Microsoft)]
    public delegate uint criAtomExPlayer_GetLastPlaybackId(nint playerHn);
}
