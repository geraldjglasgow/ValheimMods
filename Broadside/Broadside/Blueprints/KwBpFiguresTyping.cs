using HarmonyLib;
using TMPro;

namespace Broadside
{
    /// <summary>
    /// While one of the figures panel's number boxes has the keys (<see cref="KwBpFiguresView.Typing"/>), the game treats
    /// it as its own text box, as it does the sizes window's (<see cref="KwFormTyping"/>, a postfix of its own beside this
    /// one): <c>TextInput.IsVisible</c> answers true, so typing a number walks nowhere, uses no hotbar slot, opens no chat
    /// or map, and the table's window neither closes on Esc (the box gives the typing up instead) nor deletes the picked
    /// ship on Delete (the library's window keeps its keys while the game's text box is up).
    /// </summary>
    [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
    internal static class KwBpFiguresTyping
    {
        /// <summary>A box takes digits, one decimal point or comma and a minus first.</summary>
        public static readonly TMP_InputField.OnValidateInput Rule = Accept;

        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (!__result && KwBpFiguresView.Typing)
                __result = true;
        }

        private static char Accept(string text, int at, char added)
        {
            if (added >= '0' && added <= '9')
                return added;
            if ((added == '.' || added == ',') && text.IndexOf('.') < 0 && text.IndexOf(',') < 0)
                return added;
            return added == '-' && at == 0 && text.IndexOf('-') < 0 ? added : '\0';
        }
    }

    /// <summary>The inventory key (Tab by default) opens no inventory while a figures box has the keys.</summary>
    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class KwBpFiguresInventoryKey
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            if (KwBpFiguresView.Typing && ZInput.instance != null)
                ZInput.ResetButtonStatus("Inventory");
        }
    }
}
