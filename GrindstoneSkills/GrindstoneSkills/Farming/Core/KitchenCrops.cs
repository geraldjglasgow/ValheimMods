using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The crops a kitchen uses, for the compost bin (<see cref="CompostDigest"/>). A crop plant whose seed or crops matter
    /// to a kitchen adds its seed and all its crops: edible, a kitchen ingredient or input, or milled into something that
    /// is (<see cref="KitchenUses"/>). Then every Smelter conversion whose input is one of them adds its output too: the
    /// windmill's barley flour, oats and oat flour. Flax is none of these. Keyed by the shared name. Runs last after
    /// ZNetScene.Awake (with the catalog) and ObjectDB.Awake, whichever comes second finds both; adding is idempotent.
    /// </summary>
    public static class KitchenCrops
    {
        private const int MaxChain = 4;

        private static readonly HashSet<string> names = new HashSet<string>();
        private static string reported;

        /// <summary>Whether the item is a crop, seed or milled crop a kitchen uses.</summary>
        public static bool Contains(ItemDrop.ItemData item) => item?.m_shared != null && names.Contains(item.m_shared.m_name);

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
            int crops = 0, used = 0;
            foreach (CropPlant crop in CropCatalog.All)
            {
                crops++;
                used += Mark(crop) ? 1 : 0;
            }
            AddMillOutputs();
            CropValues.Clear();
            Report(crops, used);
        }

        /// <summary>Logs what was found, once per change: how many crop plants, how many a kitchen uses.</summary>
        private static void Report(int crops, int used)
        {
            string text = $"Farming: {crops} crop plants, {used} used in kitchens.";
            if (text != reported)
                GrindstoneSkills.Log.LogInfo(reported = text);
        }

        /// <summary>Adds the plant's items when a kitchen uses any of them; true when it does.</summary>
        private static bool Mark(CropPlant crop)
        {
            bool used = false;
            foreach (ItemDrop item in crop.Items())
                used |= Matters(item, 0);
            if (!used)
                return false;
            foreach (ItemDrop item in crop.Items())
                Add(item);
            return true;
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

        /// <summary>Outputs of conversions whose input is a kitchen crop, repeated so chains (oat seeds, oats, oat flour) follow.</summary>
        private static void AddMillOutputs()
        {
            for (int pass = 0; pass < MaxChain; pass++)
            {
                foreach (KitchenUses.Conversion conversion in KitchenUses.Conversions)
                {
                    if (Contains(conversion.From.m_itemData))
                        Add(conversion.To);
                }
            }
        }

        private static void Add(ItemDrop item)
        {
            if (item?.m_itemData?.m_shared != null)
                names.Add(item.m_itemData.m_shared.m_name);
        }
    }
}
