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
    /// reach other peers. Each key's hash ("grindstone_level" + slot, as ZDO.Get/Set with a name would hash it) is worked
    /// out once per slot rather than built and hashed on every read.
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

        /// <summary>Slots whose key hashes are worked out ahead; a station with more hashes the rest on each use.</summary>
        private const int HashedSlots = 16;

        private static readonly int[] CookKeys = Hashes(Keys.SlotCook);
        private static readonly int[] LevelKeys = Hashes(Keys.SlotLevel);
        private static readonly int[] InputKeys = Hashes(Keys.SlotInput);
        private static readonly int[] StarKeys = Hashes(Keys.SlotStars);

        /// <summary>Player ID of the cook, 0 when unknown.</summary>
        public static long CookId(ZDO zdo, int slot) => zdo.GetLong(Key(CookKeys, Keys.SlotCook, slot));

        /// <summary>The cook's Cooking level when the food went on, 0 when unknown.</summary>
        public static float Level(ZDO zdo, int slot) => zdo.GetFloat(Key(LevelKeys, Keys.SlotLevel, slot));

        /// <summary>Stars of the raw input, 0 for plain raw food.</summary>
        public static float InputStars(ZDO zdo, int slot) => zdo.GetFloat(Key(InputKeys, Keys.SlotInput, slot));

        /// <summary>The stars rolled when the dish turned done, -1 while not rolled.</summary>
        public static int RolledStars(ZDO zdo, int slot) => zdo.GetInt(Key(StarKeys, Keys.SlotStars, slot), -1);

        /// <summary>The stars a done dish from this slot carries: the rolled stars, 0 when never rolled.</summary>
        public static int DishStars(ZDO zdo, int slot) => Mathf.Clamp(RolledStars(zdo, slot), 0, Stars.Max);

        /// <summary>The stars the raw input in this slot carries, as a whole number of stars.</summary>
        public static int RawStars(ZDO zdo, int slot) => Mathf.Clamp(Mathf.RoundToInt(InputStars(zdo, slot)), 0, Stars.Max);

        /// <summary>Stores the cook of a freshly filled slot; its stars are not rolled yet.</summary>
        public static void Write(ZDO zdo, int slot, Cook cook)
        {
            zdo.Set(Key(CookKeys, Keys.SlotCook, slot), cook.PlayerId);
            zdo.Set(Key(LevelKeys, Keys.SlotLevel, slot), cook.Level);
            zdo.Set(Key(InputKeys, Keys.SlotInput, slot), cook.InputStars);
            zdo.Set(Key(StarKeys, Keys.SlotStars, slot), -1);
        }

        public static void SetStars(ZDO zdo, int slot, int stars) =>
            zdo.Set(Key(StarKeys, Keys.SlotStars, slot), Mathf.Clamp(stars, 0, Stars.Max));

        public static void Reset(ZDO zdo, int slot) => Write(zdo, slot, default);

        private static int[] Hashes(string key)
        {
            int[] hashes = new int[HashedSlots];
            for (int slot = 0; slot < HashedSlots; slot++)
                hashes[slot] = (key + slot).GetStableHashCode();
            return hashes;
        }

        /// <summary>The hash of <paramref name="key"/> followed by the slot number, from <paramref name="hashes"/> when it has it.</summary>
        private static int Key(int[] hashes, string key, int slot) =>
            slot >= 0 && slot < hashes.Length ? hashes[slot] : (key + slot).GetStableHashCode();

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
