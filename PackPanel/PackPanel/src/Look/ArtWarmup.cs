using HarmonyLib;
using PackPanel.Core;

namespace PackPanel.Look
{
    /// <summary>
    /// <c>FejdStartup.Start()</c> postfix (private; the main menu, once per game start, before any world): PackPanel's
    /// panel images are decoded here (<see cref="SkinArt.Prewarm"/>), where a short stall is hidden by the menu loading,
    /// instead of when the inventory first opens. Skipped with PackPanel off; a failure only logs, the images then load
    /// on first use as before.
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
    public static class ArtWarmup
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!InventorySettings.Enabled.Value)
                return;
            try
            {
                SkinArt.Prewarm();
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"PackPanel: could not prepare the panel images early: {e.Message}");
            }
        }
    }
}
