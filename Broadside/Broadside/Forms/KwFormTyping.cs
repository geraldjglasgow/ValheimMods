using HarmonyLib;

namespace Broadside
{
    /// <summary>
    /// While a <see cref="KwForm"/> takes the keys (open, and for the rest of the frame it closed in), the game treats it as
    /// its own text box: <c>TextInput.IsVisible</c> answers true. That is the game's own check while a sign or a name is
    /// typed, so the player does not walk, jump or look around (<c>PlayerController.TakeInput</c>), uses no hotbar, emote
    /// or other player key (<c>Player.TakeInput</c>), Enter opens no chat, the map, minimap zoom and menu keys do nothing,
    /// and the cursor stays free. Tab moves between the form's fields, and the inventory screen reads its key without
    /// that check, so the key's press is cleared before the screen looks at it (<see cref="KwFormInventoryKey"/>).
    /// </summary>
    [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
    internal static class KwFormTyping
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (!__result && KwForm.TakesKeys)
                __result = true;
        }
    }

    /// <summary>The inventory key (Tab by default) opens no inventory while a <see cref="KwForm"/> takes the keys.</summary>
    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class KwFormInventoryKey
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            if (KwForm.TakesKeys && ZInput.instance != null)
                ZInput.ResetButtonStatus("Inventory");
        }
    }
}
