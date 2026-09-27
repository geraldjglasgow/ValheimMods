using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// GrindstoneSkills' keys for one slot of a cooking station or oven, beside the game's own ("slot" + i holds the item
    /// name and the cooked time, "slotstatus" + i the status). The game keeps a slot only in the station's ZDO and
    /// changes it on the ZDO owner, so these keys live in the same ZDO, are written where the game writes its slots,
    /// and reach every peer with it. A slot filled by a plain vanilla add reads as cook 0, level 0, no input stars and
    /// not rolled. Every way the game empties a slot goes through CookingStation.SetSlot with an empty name (taking a
    /// dish off, a missing recipe, the station breaking, GrindstoneSkills' trash filter); the postfix there resets our keys.
    /// Keys are reset by writing the defaults: ZDO.Remove* does not raise the data revision, so a removal would never
    /// reach other peers.
    /// </summary>
    public static class StationSlots
    {
        /// <summary>Who put the food on and with what: stored when the slot is filled.</summary>
        public struct Cook
        {
            public long PlayerId;
            public float Level;
            public float InputStars;
        }

        /// <summary>Player ID of the cook, 0 when unknown.</summary>
        public static long CookId(ZDO zdo, int slot) => zdo.GetLong(Keys.SlotCook + slot);

        /// <summary>The cook's Cooking level when the food went on, 0 when unknown.</summary>
        public static float Level(ZDO zdo, int slot) => zdo.GetFloat(Keys.SlotLevel + slot);

        /// <summary>Stars of the raw input, 0 for plain raw food.</summary>
        public static float InputStars(ZDO zdo, int slot) => zdo.GetFloat(Keys.SlotInput + slot);

        /// <summary>The stars rolled when the dish turned done, -1 while not rolled.</summary>
        public static int RolledStars(ZDO zdo, int slot) => zdo.GetInt(Keys.SlotStars + slot, -1);

        /// <summary>The stars a done dish from this slot carries: the rolled stars, 0 when never rolled.</summary>
        public static int DishStars(ZDO zdo, int slot) => Mathf.Clamp(RolledStars(zdo, slot), 0, Stars.Max);

        /// <summary>The stars the raw input in this slot carries, as a whole number of stars.</summary>
        public static int RawStars(ZDO zdo, int slot) => Mathf.Clamp(Mathf.RoundToInt(InputStars(zdo, slot)), 0, Stars.Max);

        /// <summary>Stores the cook of a freshly filled slot; its stars are not rolled yet.</summary>
        public static void Write(ZDO zdo, int slot, Cook cook)
        {
            zdo.Set(Keys.SlotCook + slot, cook.PlayerId);
            zdo.Set(Keys.SlotLevel + slot, cook.Level);
            zdo.Set(Keys.SlotInput + slot, cook.InputStars);
            zdo.Set(Keys.SlotStars + slot, -1);
        }

        public static void SetStars(ZDO zdo, int slot, int stars) => zdo.Set(Keys.SlotStars + slot, Mathf.Clamp(stars, 0, Stars.Max));

        public static void Reset(ZDO zdo, int slot) => Write(zdo, slot, default);

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.SetSlot))]
        private static class ClearSlot
        {
            [HarmonyPostfix]
            private static void Postfix(CookingStation __instance, int slot, string itemName)
            {
                ZNetView nview = __instance.m_nview;
                if (string.IsNullOrEmpty(itemName) && Kitchen.IsKitchen(__instance) && nview != null && nview.IsValid())
                    Reset(nview.GetZDO(), slot);
            }
        }
    }
}
