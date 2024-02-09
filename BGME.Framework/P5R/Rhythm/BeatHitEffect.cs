using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P5R.Rhythm;

internal unsafe class BeatHitEffect : IGameHook
{
    [Function(Register.rbx, Register.rax, true)]
    private delegate int SetPostDamageHp(int originalHp);
    private IReverseWrapper<SetPostDamageHp>? setHpWrapper;
    private IAsmHook? setHpHook;

    private readonly IP5RLib p5rLib;
    private readonly EffectsHook effectsHook;
    private readonly bool* effectEnabled;

    private readonly BattleHooks battle = new();
    private Conductor? conductor;

    private float lastHitTime;

    public BeatHitEffect(IP5RLib p5rLib, EffectsHook effectsHook)
    {
        this.p5rLib = p5rLib;
        this.effectsHook = effectsHook;
        effectEnabled = (bool*)Marshal.AllocHGlobal(sizeof(bool));
        *effectEnabled = false;

        battle.ParticipantActed += (arg1, arg2) => OnParticipantActed((nint*)arg1, (void*)arg2);
    }

    private void OnParticipantActed(nint* participantPtr, void* param2)
    {
        // Act event can fire before the hit window is set.
        // Save last hit time then check if success in update.
        lastHitTime = conductor?.SongPositionInSeconds ?? 0;
        Log.Information($"Hit Time: {lastHitTime} || {(nint)participantPtr:X} / {*participantPtr:X} || {(nint)param2:X}");
    }

    private float hitTimeActual;

    public float Activate(Conductor conductor)
    {
        this.conductor = conductor;
        hitTimeActual = (float)(conductor.SongPositionInSeconds + HitWindows.GoodWindow.TotalSeconds);

        var currentWholeBeat = Math.Round(conductor.SongPositionInBeats);
        var nextActivation = (float)((currentWholeBeat + 2) * this.conductor.SecPerBeat) - HitWindows.GoodWindow.TotalSeconds;
        return (float)nextActivation;
    }

    public void Deactivate()
    {
        *effectEnabled = false;
    }

    public void Update()
    {
        if (lastHitTime != 0 && IsHitSuccessful(HitWindows.GoodWindow))
        {
            lastHitTime = 0;
            p5rLib.FlowCaller.SET_COUNT(500, 1);
            // this.p5rLib.FlowCaller.FLD_EFFECT_BANK_FREE(1);
            //if (this.IsHitSuccessful(this.hitWindows.PerfectWindow))
            //{
            //    Log.Information("Perfect!");
            //    this.p5rLib.FlowCaller.FLD_EFFECT_BANK_LOAD(1, 51);
            //}
            //else if (this.IsHitSuccessful(this.hitWindows.GreatWindow))
            //{
            //    Log.Information("Great!");
            //    this.p5rLib.FlowCaller.FLD_EFFECT_BANK_LOAD(1, 55);
            //}
            //else
            //{
            //    Log.Information("Good!");
            //    this.p5rLib.FlowCaller.FLD_EFFECT_BANK_LOAD(1, 54);
            //}

            //this.p5rLib.FlowCaller.FLD_EFFECT_BANK_SYNC(1);
            //this.currentEffect = this.p5rLib.FlowCaller.FLD_EFFECT_BANK_START(1);
            //this.successEffect = true;
            //var x = this.p5rLib.FlowCaller.FLD_CAMERA_GET_X_POS();
            //var y = this.p5rLib.FlowCaller.FLD_CAMERA_GET_Y_POS();
            //var z = this.p5rLib.FlowCaller.FLD_CAMERA_GET_Z_POS();
            //this.p5rLib.FlowCaller.FLD_EFFECT_SET_POS((int)this.currentEffect, x, y, z);

            //var bcd = this.p5rLib.FlowCaller.BTL_CUTSCENE_LOAD(779, 1);
            //this.p5rLib.FlowCaller.BTL_CUTSCENE_LOADSYNC(bcd);
            //this.p5rLib.FlowCaller.BTL_CUTSCENE_PLAY(bcd, 1, 0, 2, 0);
            //this.p5rLib.FlowCaller.BTL_CUTSCENE_END(bcd);
            //var bcd = this.p5rLib.FlowCaller.SCRIPT_READ(1, 2, 3);
            //this.p5rLib.FlowCaller.SCRIPT_READ_SYNC(bcd);
            //this.p5rLib.FlowCaller.SCRIPT_EXEC(bcd, 0);
            //this.p5rLib.FlowCaller.SCRIPT_FREE(bcd);
        }
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        battle.Initialize(scanner, hooks);

        scanner.Scan("Set Post-Damage HP Hook", "89 5F ?? 41 F7 85 ?? ?? ?? ?? 00 80 00 00", result =>
        {
            var patch = new string[]
            {
                "use64",
                $"mov rax, {(nint)effectEnabled}",
                "mov al, [rax]",
                "test al, al",
                "jz original",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(SetPostDamageHpImpl, out setHpWrapper),
                Utilities.PopCallerRegisters,
                "mov ebx, eax",
                "original:"
            };

            setHpHook = hooks.CreateAsmHook(patch, result).Activate();
        });
    }

    private int SetPostDamageHpImpl(int originalHp)
    {
        return 0;
        return originalHp;
    }

    private bool IsHitSuccessful(TimeSpan hitWindow)
    {
        var min = hitTimeActual - hitWindow.TotalSeconds;
        var max = hitTimeActual + hitWindow.TotalSeconds;

        if (lastHitTime >= min && lastHitTime <= max)
        {
            return true;
        }

        return false;
    }
}
