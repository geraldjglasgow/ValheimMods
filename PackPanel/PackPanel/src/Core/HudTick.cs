using HarmonyLib;
using PackPanel.Consume;
using PackPanel.Panels;

namespace PackPanel.Core
{
    /// <summary>
    /// PackPanel's one <c>Hud.Update</c> postfix (one patch per hot method): the Food and Mead bar
    /// (<see cref="ConsumeBar"/>), then the weight and stat boxes beside the minimap (<see cref="HudWeight"/>).
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    public static class HudTick
    {
        [HarmonyPostfix]
        public static void Postfix(Hud __instance)
        {
            ConsumeBar.Tick(__instance);
            HudWeight.Tick();
        }
    }
}
