using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// Epic Loot's rarity background behind a magic item's icon, the one its inventory grid and the game's recipe list
    /// show. The salvage rows and the selected stack's icon are drawn by OpenKeep, not by the game's AddRecipeToList, so
    /// Epic Loot's own recipe list patch never sees them; they are passed to the method Epic Loot publishes for this,
    /// <c>EpicLoot.API.ApplyMagicItemBackgroundToIcon</c>, found by reflection so nothing of Epic Loot is referenced.
    /// Epic Loot's client setting <c>Interface / Show Rarity In Recipe List</c> decides, as for the game's rows.
    /// Silent without Epic Loot.
    /// </summary>
    public static class SalvageRarity
    {
        private const string Guid = "randyknapp.mods.epicloot";

        private static bool looked;
        private static MethodInfo apply;
        private static ConfigEntry<bool> shown;

        /// <summary>The call's arguments, reused (asked every frame while the Salvage tab shows); cleared after each call.</summary>
        private static readonly object[] args = new object[2];

        /// <summary>Draws or hides the background behind an icon for this item (none for a non-magic item).</summary>
        public static void Apply(GameObject icon, ItemDrop.ItemData item)
        {
            if (icon == null || !Find() || (shown != null && !shown.Value))
                return;
            try
            {
                args[0] = icon;
                args[1] = item;
                apply.Invoke(null, args);
            }
            catch (Exception e)
            {
                apply = null;
                Plugin.Log.LogWarning($"OpenKeep: Epic Loot rarity backgrounds off, {e.GetType().Name}: {e.Message}");
            }
            finally
            {
                args[0] = args[1] = null;
            }
        }

        private static bool Find()
        {
            if (looked)
                return apply != null;
            looked = true;
            if (!Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) || info.Instance == null)
                return false;
            Type api = info.Instance.GetType().Assembly.GetType("EpicLoot.API");
            apply = api?.GetMethod("ApplyMagicItemBackgroundToIcon", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(GameObject), typeof(ItemDrop.ItemData) }, null);
            info.Instance.Config.TryGetEntry("Interface", "Show Rarity In Recipe List", out shown);
            return apply != null;
        }
    }
}
