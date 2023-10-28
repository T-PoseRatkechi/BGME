using BGME.Framework.Music;
using PersonaMusicScript.Library.Models;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Serilog;
using System.Runtime.InteropServices;

namespace BGME.Framework.P4G;

/// <summary>
/// Based on p4g64.EventLogger by Secre-C.
/// </summary>
internal unsafe class EventBgm
{
    [Function(CallingConventions.Microsoft)]
    private delegate void* RunCommandFunction(nint frameAddress);
    private IHook<RunCommandFunction>? runCommandHook;

    [Function(CallingConventions.Microsoft)]
    private delegate void RunEventCommandsFunction(int frame, nint param2, int pass, nint param4);
    private IHook<RunEventCommandsFunction>? runEventCommandsHook;

    private IAsmHook? setCurrentEventHook;

    private readonly SoundPatcher sound;
    private readonly MusicService music;

    private readonly int* currentMajorId = (int*)NativeMemory.AllocZeroed(sizeof(int));
    private readonly int* currentMinorId = (int*)NativeMemory.AllocZeroed(sizeof(int));
    private int currentFrame = -1;

    public EventBgm(
        IReloadedHooks hooks,
        IStartupScanner scanner,
        SoundPatcher sound,
        MusicService music)
    {
        this.sound = sound;
        this.music = music;

        scanner.AddMainModuleScan("48 89 6C 24 ?? 56 57 41 56 48 83 EC 30 89 CD", (result) =>
        {
            if (!result.Found)
            {
                throw new Exception("Could not find Function RunEventCommands");
            }

            nint address = Utilities.BaseAddress + result.Offset;
            this.runEventCommandsHook = hooks.CreateHook<RunEventCommandsFunction>(this.RunFrameCommands, address).Activate();
        });

        scanner.AddMainModuleScan("40 53 48 83 EC 20 48 8B D9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F BE 53 ??", (result) =>
        {
            if (!result.Found)
            {
                throw new Exception("Could not find RunCommand pattern.");
            }

            nint address = Utilities.BaseAddress + result.Offset;
            this.runCommandHook = hooks.CreateHook<RunCommandFunction>(this.RunCommand, address).Activate();
        });

        // Format event PM file path hook.
        scanner.AddMainModuleScan("F7 EE 89 7C 24 20", result =>
        {
            if (!result.Found)
            {
                throw new Exception("Could not find event PM file path pattern.");
            }

            nint address = Utilities.BaseAddress + result.Offset;

            var patch = new string[]
            {
                "use64",
                $"push rcx",
                $"mov rcx, {(nint)this.currentMajorId}",
                $"mov [rcx], rsi",
                $"mov rcx, {(nint)this.currentMinorId}",
                $"mov [rcx], rdi",
                $"pop rcx"
            };

            this.setCurrentEventHook = hooks.CreateAsmHook(patch, address, Reloaded.Hooks.Definitions.Enums.AsmHookBehaviour.ExecuteFirst).Activate();
        });
    }

    private void RunFrameCommands(int frame, nint param2, int pass, nint param4)
    {
        this.currentFrame = frame;

        // BGM added through script.
        // Only check on first pass(?)
        if (pass == 1 && this.music.GetEventFrame(*this.currentMajorId, *this.currentMinorId) is EventFrame eventFrame)
        {
            Log.Debug("Frame: {frame}", this.currentFrame);

            // Current frame has music added from script.
            if (eventFrame.FrameMusic.TryGetValue(this.currentFrame, out var music))
            {
                if (music != null)
                {
                    Log.Debug("Frame {frame} uses BGME.", this.currentFrame);
                    this.sound.PlayMusic(music);
                }
            }
        }

        this.runEventCommandsHook?.OriginalFunction(frame, param2, pass, param4);
    }

    private void* RunCommand(nint commandPtr)
    {
        int commandTypeId = *(int*)(commandPtr + 0x38);
        CommandObjType commandType = (CommandObjType)commandTypeId;

        int commandId = *(ushort*)commandPtr;
        if (this.currentFrame == commandId)
        {
            // Check if original frame bgm was disabled by script.
            if (commandType == CommandObjType.BGM)
            {
                if (this.music.GetEventFrame(*this.currentMajorId, *this.currentMinorId) is EventFrame frame)
                {
                    ushort* bgmId = (ushort*)(commandPtr + 0x12);
                    Log.Debug("Command: {type} || Frame: {frame} || BGM ID: {id}", commandType, commandId, *bgmId);

                    if (frame.FrameMusic.TryGetValue(this.currentFrame, out var frameMusic))
                    {
                        if (frameMusic == null)
                        {
                            // *bgmId = 0;
                            Log.Debug("Original frame BGM disabled.");
                            return null;
                        }
                    }
                }
            }
        }

        return this.runCommandHook!.OriginalFunction(commandPtr);
    }

    private enum CommandObjType
    {
        STAGE = 0,
        UNIT = 1,
        CAMERA = 2,
        EFFECT = 3,
        MESSAGE = 4,
        SE = 5,
        FADE = 6,
        QUAKE = 7,
        BLUR = 8,
        LIGHT = 9,
        SLIGHT = 10,
        SFOG = 11,
        SKY = 12,
        BLUR2 = 13,
        MBLUR = 14,
        DBLUR = 15,
        FILTER = 16,
        MFILTER = 17,
        BED = 18,
        BGM = 19,
        MG1 = 20,
        MG2 = 21,
        FB = 22,
        RBLUR = 23,
        TMX = 24,
        EPL = 26,
        HBLUR = 27,
        PADACT = 28,
        MOVIE = 29,
        TIMEI = 30,
        RENDERTEX = 31,
        BISTA = 32,
        CTLCAM = 33,
        WAIT = 34,
        B_UP = 35,
        CUTIN = 36,
        EVENT_EFFECT = 37,
        JUMP = 38,
        KEYFREE = 39,
        RANDOMJUMP = 40,
        CUSTOMEVENT = 41,
        CONDJUMP = 42,
        COND_ON = 43,
        COMULVJUMP = 44,
        COUNTJUMP = 45,
        HOLYJUMP = 46,
        FIELDOBJ = 47,
        PACKMODEL = 48,
        FIELDEFF = 49,
        SPUSE = 50,
        SCRIPT = 51,
        BLURFILTER = 52,
        FOG = 53,
        ENV = 54,
        FLDSKY = 55,
        FLDNOISE = 56,
        CAMERA_STATE = 57
    }
}
