using System.Globalization;
using System.Linq;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A fish's tooltip tells what the local player's angler's log says about its species: the levels landed and the
    /// record, or that it is not in the log yet; a legendary fish also says so. The static ItemData.GetTooltip builds
    /// every item tooltip before localization; the lines go at the end. A second item's tooltip appended to this one is a
    /// call of its own with appending set and is skipped.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    public static class FishTooltip
    {
        [HarmonyPostfix]
        private static void Postfix(ItemDrop.ItemData item, bool appending, ref string __result)
        {
            if (appending || __result == null || !FishSkill.Active || !FishInfo.IsFishItem(item))
                return;
            __result += HookGuard.Run("fish tooltip", static fish => Lines(fish), item, "");
        }

        private static string Lines(ItemDrop.ItemData item)
        {
            string lines = FishInfo.IsLegendary(item.m_quality) ? "\n<color=#8cffd9>Legendary</color>" : "";
            Player player = Player.m_localPlayer;
            if (player == null)
                return lines;
            string prefab = item.m_dropPrefab.name;
            int levels = AnglerLog.Levels(player, prefab);
            if (levels == 0)
                return lines + "\nNot in your angler's log yet";
            lines += "\nLanded levels: " + string.Join(" ", AnglerLog.LevelsIn(levels).Select(level => level.ToString(CultureInfo.InvariantCulture)));
            if (AnglerLog.Best(player, prefab, out float weight, out int best))
                lines += $"\nYour record: {weight.ToString("0.0", CultureInfo.InvariantCulture)} kg (level {best})";
            return lines;
        }
    }
}
