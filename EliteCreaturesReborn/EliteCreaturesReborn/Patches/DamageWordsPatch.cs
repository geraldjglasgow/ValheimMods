using HarmonyLib;
using PatchGuard;
using Translations;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// A text over a creature sent as words (<see cref="LangWire"/>: Frostbound's heal number, "+30 frost") is worded
    /// here, on each machine that shows it, in that machine's language, before the game localizes and draws it. The
    /// game sends every damage number to everyone near, so the sender's language must not travel; anything the game
    /// itself sends passes untouched after a one-character test.
    /// </summary>
    [HarmonyPatch(typeof(DamageText), "AddInworldText")]
    public static class DamageWordsPatch
    {
        private static void Prefix(ref string text)
        {
            if (LangWire.IsMessage(text))
            {
                text = Guard.Run("DamageText words", static message => LangWire.Read(message), text);
            }
        }
    }
}
