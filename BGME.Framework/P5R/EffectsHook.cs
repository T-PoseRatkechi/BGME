using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace BGME.Framework.P5R;

internal unsafe class EffectsHook : IGameHook
{
    [Function(Register.r8, Register.rax, true)]
    private delegate nint SetFieldEffect(int id);
    private IReverseWrapper<SetFieldEffect>? setFieldWrapper;
    private IAsmHook? setFieldHook;

    private readonly Dictionary<int, string> assignedEffects = new()
    {
        [50] = "EFFECT/EVENT/EE695_030.EPL",
        [51] = "BATTLE/EVENT/BCD/BATONTOUCH/bes_btn_touch_yuka4.EPL",
        [52] = "BATTLE/EVENT/BCD/HOLD_UP/ICON/BES_H_01.EPL",
        [53] = "BATTLE/EVENT/BCD/SP_GUN/bes_sp_bang_shita.EPL",
        [54] = "BATTLE/EVENT/BCD/BATONTOUCH/bes_btn_touch_yuka.EPL",
        [55] = "BATTLE/EVENT/BCD/BATONTOUCH/bes_btn_touch_yuka3.EPL",
    };

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan("Field Effect Hook", "E8 ?? ?? ?? ?? 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? E8", result =>
        {
            var patch = new string[]
            {
                "use64",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(this.SetFieldEffectImpl, out this.setFieldWrapper),
                Utilities.PopCallerRegisters,
                "test rax, rax",
                "jz original",
                "mov rdx, rax",
                "original:"
            };

            this.setFieldHook = hooks.CreateAsmHook(patch, result).Activate();
        });
    }

    private nint SetFieldEffectImpl(int id)
    {
        if (this.assignedEffects.TryGetValue(id, out var newPath))
        {
            return StringsCache.GetStringPtr(newPath);
        }

        return IntPtr.Zero;
    }
}
