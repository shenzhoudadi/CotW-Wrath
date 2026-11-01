using System.Collections.Generic;
using System.Linq;
using Kingmaker.UI.MVVM._VM.Tooltip.Bricks;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using Owlcat.Runtime.UI.Tooltips;

namespace Way_of_the_shield.NewComponents
{
    public abstract class BuffDynamicDescriptionComponent_Base : UnitBuffComponentDelegate
    {
        public abstract string GenerateDescription();
        [HarmonyPatch(typeof(TooltipTemplateBuff), nameof(TooltipTemplateBuff.GetBody))]
        public static class Transpiler
        {
            // Decorate the public enumerable. No private compiler-generated iterator/stack rewrite.
            [HarmonyPostfix]
            public static void AppendDynamicDescription(TooltipTemplateBuff __instance, ref IEnumerable<ITooltipBrick> __result)
            {
                if (__instance?.Buff == null) return;
                __result = Describe(__result, __instance.Buff);
            }
            static IEnumerable<ITooltipBrick> Describe(IEnumerable<ITooltipBrick> original, Buff buff)
            {
                foreach (var brick in original ?? Enumerable.Empty<ITooltipBrick>()) yield return brick;
                string description = null;
                buff.CallComponents<BuffDynamicDescriptionComponent_Base>(c => description = c.GenerateDescription());
                if (!string.IsNullOrEmpty(description)) yield return new TooltipBrickText(description, TooltipTextType.Simple);
            }
        }
    }
}
