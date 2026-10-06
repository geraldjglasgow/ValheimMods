using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Capacity
{
    /// <summary>
    /// "Hover Contents": Container.GetHoverText() gets a fill line and up to Hover Lines content lines appended,
    /// only for a container the local player could open: the game's own guard stone check
    /// (PrivateArea.CheckAccess when the container checks guard stones) and privacy check (Container.CheckAccess)
    /// are run, and the net view must be valid. The game asks for the hover text every frame; the lines are built
    /// again only when the hovered container, the revision its inventory was last loaded or saved at (the game's
    /// <c>m_lastRevision</c>) or the two settings changed, and at least every <see cref="MaxAge"/> seconds (a ward
    /// switched on, the language changed).
    /// </summary>
    public static class HoverText
    {
        private const float MaxAge = 0.5f;

        private static Container builtFor;
        private static uint builtRevision = uint.MaxValue;
        private static HoverFill builtFill;
        private static int builtLines = -1;
        private static float builtAt = float.MinValue;
        private static string builtExtra = "";

        public static string Append(Container container, string text)
        {
            if (!CapacitySettings.HoverContents.Value)
                return text;
            string extra = Extra(container);
            return extra.Length == 0 ? text : text + extra;
        }

        /// <summary>The localized lines to append for this container, empty for none; built again only when out of date.</summary>
        private static string Extra(Container container)
        {
            HoverFill fill = CapacitySettings.HoverFill.Value;
            int lines = CapacitySettings.HoverLines.Value;
            float now = Time.unscaledTime;
            if (container == builtFor && container.m_lastRevision == builtRevision && fill == builtFill && lines == builtLines
                && now - builtAt < MaxAge)
                return builtExtra;
            builtExtra = CanSee(container) ? Build(container.GetInventory(), lines) : "";
            builtFor = container;
            builtRevision = container != null ? container.m_lastRevision : uint.MaxValue;
            builtFill = fill;
            builtLines = lines;
            builtAt = now;
            return builtExtra;
        }

        private static string Build(Inventory inventory, int maxLines)
        {
            StringBuilder extra = new StringBuilder();
            string fill = FillLine(inventory);
            if (fill != null)
                extra.Append('\n').Append(fill);
            foreach (string line in ContentLines(inventory, maxLines))
                extra.Append('\n').Append(line);
            return extra.Length == 0 ? "" : Language.Localize(extra.ToString());
        }

        private static bool CanSee(Container container)
        {
            if (container == null || container.GetInventory() == null || container.m_nview == null || !container.m_nview.IsValid())
                return false;
            if (container.m_checkGuardStone && !PrivateArea.CheckAccess(container.transform.position, 0f, false))
                return false;
            if (Game.instance == null || Game.instance.GetPlayerProfile() == null)
                return false;
            return container.CheckAccess(Game.instance.GetPlayerProfile().GetPlayerID());
        }

        private static string FillLine(Inventory inventory)
        {
            int used = inventory.NrOfItems();
            int total = Mathf.Max(1, inventory.GetWidth() * inventory.GetHeight());
            switch (CapacitySettings.HoverFill.Value)
            {
                case HoverFill.Fraction: return $"{used} / {total} $ok_slots";
                case HoverFill.Percent: return $"{Mathf.RoundToInt(100f * used / total)}% $ok_full";
                default: return null;
            }
        }

        /// <summary>Stacks of the same item summed, most first; "name x count" lines, then "and n more".</summary>
        public static List<string> ContentLines(Inventory inventory, int maxLines)
        {
            List<KeyValuePair<string, int>> counts = CountByName(inventory).OrderByDescending(pair => pair.Value).ToList();
            List<string> lines = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts.Take(Mathf.Max(0, maxLines)))
                lines.Add($"{pair.Key} x {pair.Value}");
            int more = counts.Count - lines.Count;
            if (more > 0)
                lines.Add($"$ok_and {more} $ok_more");
            return lines;
        }

        private static Dictionary<string, int> CountByName(Inventory inventory)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item == null || item.m_shared == null)
                    continue;
                string name = string.IsNullOrEmpty(item.m_shared.m_name) ? "?" : item.m_shared.m_name;
                counts.TryGetValue(name, out int count);
                counts[name] = count + item.m_stack;
            }
            return counts;
        }

        [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
        private static class HoverPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Container __instance, ref string __result) => __result = Append(__instance, __result);
        }
    }
}
