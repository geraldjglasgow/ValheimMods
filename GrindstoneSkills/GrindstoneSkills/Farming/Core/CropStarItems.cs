using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Which crops carry stars. A crop plant whose seed or crops matter to a kitchen makes its seed and all its crops
    /// star items (<see cref="Kitchen.AddStarItem"/>): edible, a kitchen ingredient or input, or milled into something
    /// that is (<see cref="KitchenUses"/>). Then every Smelter conversion whose input carries stars gives its output stars
    /// too, and that Smelter becomes a star mill (<see cref="MillQueue"/>): the windmill (barley flour, oats, oat flour).
    /// Flax is none of these, so it has no stars and the spinning wheel is left alone. Runs last after ZNetScene.Awake
    /// (with the catalog) and ObjectDB.Awake, whichever comes second finds both; adding is idempotent.
    /// </summary>
    public static class CropStarItems
    {
        private const int MaxChain = 4;

        private static readonly HashSet<string> starMills = new HashSet<string>();
        private static string reported;

        /// <summary>Whether this Smelter prefab processes items that carry stars.</summary>
        public static bool IsStarMill(string smelterPrefab) => smelterPrefab != null && starMills.Contains(smelterPrefab);

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabaseAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => Discover();
        }

        public static void Discover()
        {
            if (ZNetScene.instance == null || ObjectDB.instance == null)
                return;
            // The kitchen's own items first (idempotent): dish values and intermediates feed the uses and values here.
            KitchenDiscovery.Discover();
            KitchenUses.Rebuild();
            foreach (CropPlant crop in CropCatalog.All)
                Mark(crop);
            AddMillOutputs();
            CropValues.Clear();
            Report();
        }

        /// <summary>Logs what was found, once per change: how many crop plants, how many carry stars, the star mills.</summary>
        private static void Report()
        {
            int crops = 0, starred = 0;
            foreach (CropPlant crop in CropCatalog.All)
            {
                crops++;
                starred += crop.CarriesStars ? 1 : 0;
            }
            string text = $"Farming: {crops} crop plants, {starred} carry stars; star mills: {string.Join(", ", starMills)}.";
            if (text != reported)
                GrindstoneSkills.Log.LogInfo(reported = text);
        }

        private static void Mark(CropPlant crop)
        {
            foreach (ItemDrop item in crop.Items())
                crop.CarriesStars |= Matters(item, 0);
            if (!crop.CarriesStars)
                return;
            foreach (ItemDrop item in crop.Items())
                Kitchen.AddStarItem(item);
        }

        /// <summary>Whether a kitchen cares about the item: edible, an ingredient, or milled into something that is.</summary>
        private static bool Matters(ItemDrop item, int depth)
        {
            if (item == null || depth > MaxChain)
                return false;
            if (Kitchen.Value(item.m_itemData.m_shared) > 0f || KitchenUses.IsIngredient(item.name))
                return true;
            foreach (KitchenUses.Conversion conversion in KitchenUses.Conversions)
            {
                if (conversion.From == item && Matters(conversion.To, depth + 1))
                    return true;
            }
            return false;
        }

        /// <summary>Outputs of conversions whose input carries stars, repeated so chains (oat seeds, oats, oat flour) follow.</summary>
        private static void AddMillOutputs()
        {
            for (int pass = 0; pass < MaxChain; pass++)
            {
                foreach (KitchenUses.Conversion conversion in KitchenUses.Conversions)
                {
                    if (!Kitchen.IsKitchenItem(conversion.From.m_itemData))
                        continue;
                    Kitchen.AddStarItem(conversion.To);
                    starMills.Add(conversion.Mill);
                }
            }
        }
    }
}
