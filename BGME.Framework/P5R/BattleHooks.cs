using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace BGME.Framework.P5R;

internal unsafe class BattleHooks : IGameHook
{
    [Function(CallingConventions.Microsoft)]
    private delegate nint SetParticipantAction(nint* participantPtr, nint param2);
    private IHook<SetParticipantAction>? setParticipantActionHook;

    public Action<nint, nint>? ParticipantActed;

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan("SetParticipantAction Function", "48 89 54 24 ?? 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24", result =>
        {
            this.setParticipantActionHook = hooks.CreateHook<SetParticipantAction>(this.SetParticipantActionImpl, result).Activate();
        });
    }

    private nint SetParticipantActionImpl(nint* participantPtr, nint param2)
    {
        Log.Debug($"ParticipantActionSet || {(nint)participantPtr:X} || {param2:X}");
        this.ParticipantActed?.Invoke((nint)participantPtr, param2);
        return this.setParticipantActionHook!.OriginalFunction(participantPtr, param2);
    }
}
