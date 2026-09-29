using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The skills window as a book: wider and taller (<see cref="BookLayout"/>), with an info pane beside the list
    /// (<see cref="BookPane"/>). Clicking an entry shows that skill's page there; with a gamepad the page follows the
    /// selected entry. The entries' own hover tooltips are emptied, since the pane replaces them. Read from the game code
    /// (2026-09-28): <c>SkillsDialog.Setup</c> runs every time the window opens, reuses its entries and rewrites each
    /// entry's tooltip; each entry is a Button calling <c>SkillClicked</c> with itself; <c>Update</c> moves
    /// <c>m_selectionIndex</c> with a gamepad and opens that entry's tooltip. Local only; nothing is sent.
    /// </summary>
    public static class SkillBook
    {
        [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
        private static class Open
        {
            [HarmonyPostfix]
            private static void Postfix(SkillsDialog __instance, Player player) =>
                HookGuard.Run("skill book", () => Opened(__instance, player));
        }

        [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.SkillClicked))]
        private static class Click
        {
            [HarmonyPostfix]
            private static void Postfix(SkillsDialog __instance, GameObject selectedObject) =>
                HookGuard.Run("skill book", () => PaneOf(__instance)?.ShowEntry(selectedObject));
        }

        [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Update))]
        private static class Gamepad
        {
            [HarmonyPostfix]
            private static void Postfix(SkillsDialog __instance)
            {
                if (ZInput.IsExclusiveGamepadActive() && !ZInput.IsTouchActive())
                    HookGuard.Run("skill book", () => PaneOf(__instance)?.Follow(__instance.m_selectionIndex));
            }
        }

        private static void Opened(SkillsDialog dialog, Player player)
        {
            if (player == null)
                return;
            Silence(dialog);
            BookLayout.PaneOf(dialog)?.Open(player);
        }

        /// <summary>The pane once the window has one; the window makes it as it opens.</summary>
        private static BookPane PaneOf(SkillsDialog dialog)
        {
            Transform frame = dialog.skillListScrollRect != null ? dialog.skillListScrollRect.transform.parent?.parent : null;
            Transform pane = frame != null ? frame.Find(BookLayout.PaneName) : null;
            return pane != null ? pane.GetComponent<BookPane>() : null;
        }

        /// <summary>Empties every entry's tooltip, which then never shows, and hides one the game already began to show.</summary>
        private static void Silence(SkillsDialog dialog)
        {
            foreach (GameObject entry in dialog.m_elements)
            {
                UITooltip tooltip = entry != null ? entry.GetComponentInChildren<UITooltip>() : null;
                if (tooltip == null)
                    continue;
                tooltip.m_text = "";
                tooltip.m_topic = "";
            }
            UITooltip.HideTooltip();
        }
    }
}
