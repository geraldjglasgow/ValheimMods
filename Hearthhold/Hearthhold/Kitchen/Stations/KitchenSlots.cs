using HarmonyLib;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// One slot of a kitchen cooking station or oven as Hearthhold records it, beside the game's own keys ("slot" + i
    /// holds the item name and the cooked time, "slotstatus" + i the status): the cook's level and the raw food's stars
    /// when it went on, and the dish's stars once rolled. The game keeps a slot only in the station's ZDO and changes it
    /// on the ZDO owner, so these keys live in the same ZDO, are written there and reach every peer with it. Every way
    /// the game empties a slot goes through CookingStation.SetSlot with an empty name (taking a dish off, a missing
    /// recipe, the station breaking); the postfix there resets the keys. Resetting writes the defaults, because ZDO
    /// removals do not raise the data revision and would never reach other peers, and a write that changes nothing is
    /// skipped, so a station never cooked on by Hearthhold gains no data. Key hashes, the game's included, are worked
    /// out once per slot, so reading a slot allocates nothing (UpdateCooking reads every second).
    /// </summary>
    public static class KitchenSlots
    {
        /// <summary>Slots whose key hashes are worked out ahead; a station with more hashes the rest on each use.</summary>
        private const int Hashed = 16;

        private const string GameName = "slot";
        private const string GameStatus = "slotstatus";

        private static readonly int[] NameKeys = Hashes(GameName);
        private static readonly int[] StatusKeys = Hashes(GameStatus);
        private static readonly int[] LevelKeys = Hashes(KitchenKeys.SlotLevel);
        private static readonly int[] InputKeys = Hashes(KitchenKeys.SlotInput);
        private static readonly int[] StarKeys = Hashes(KitchenKeys.SlotStars);

        /// <summary>The slot's item prefab name as the game stores it, "" when empty.</summary>
        public static string Name(ZDO zdo, int slot) => zdo.GetString(Key(NameKeys, GameName, slot));

        public static CookingStation.Status Status(ZDO zdo, int slot) => (CookingStation.Status)zdo.GetInt(Key(StatusKeys, GameStatus, slot));

        /// <summary>The cook's Cooking level when the food went on, 0 when unknown.</summary>
        public static float Level(ZDO zdo, int slot) => StarOdds.Sane(zdo.GetFloat(Key(LevelKeys, KitchenKeys.SlotLevel, slot)));

        /// <summary>The raw food's stars, 0 to 3.</summary>
        public static float InputStars(ZDO zdo, int slot) =>
            Mathf.Clamp(StarOdds.Sane(zdo.GetFloat(Key(InputKeys, KitchenKeys.SlotInput, slot))), 0f, Stars.Max);

        /// <summary>The stars a done dish from this slot carries, 0 until rolled.</summary>
        public static int DishStars(ZDO zdo, int slot) => Mathf.Clamp(zdo.GetInt(Key(StarKeys, KitchenKeys.SlotStars, slot)), 0, Stars.Max);

        /// <summary>The stars the raw food in this slot carries, as a whole number.</summary>
        public static int RawStars(ZDO zdo, int slot) => Mathf.Clamp(Mathf.RoundToInt(InputStars(zdo, slot)), 0, Stars.Max);

        /// <summary>Records a freshly filled slot; its dish is not rolled yet.</summary>
        public static void Fill(ZDO zdo, int slot, float level, float inputStars)
        {
            Write(zdo, Key(LevelKeys, KitchenKeys.SlotLevel, slot), StarOdds.Sane(level));
            Write(zdo, Key(InputKeys, KitchenKeys.SlotInput, slot), Mathf.Clamp(StarOdds.Sane(inputStars), 0f, Stars.Max));
            Write(zdo, Key(StarKeys, KitchenKeys.SlotStars, slot), 0);
        }

        public static void SetStars(ZDO zdo, int slot, int stars) =>
            Write(zdo, Key(StarKeys, KitchenKeys.SlotStars, slot), Mathf.Clamp(stars, 0, Stars.Max));

        public static void Reset(ZDO zdo, int slot) => Fill(zdo, slot, 0f, 0f);

        private static void Write(ZDO zdo, int key, float value)
        {
            if (zdo.GetFloat(key) != value)
                zdo.Set(key, value);
        }

        private static void Write(ZDO zdo, int key, int value)
        {
            if (zdo.GetInt(key) != value)
                zdo.Set(key, value);
        }

        private static int[] Hashes(string key)
        {
            int[] hashes = new int[Hashed];
            for (int slot = 0; slot < Hashed; slot++)
                hashes[slot] = (key + slot).GetStableHashCode();
            return hashes;
        }

        /// <summary>The hash of <paramref name="key"/> followed by the slot number, as ZDO.Get/Set with a name would hash it.</summary>
        private static int Key(int[] hashes, string key, int slot) =>
            slot >= 0 && slot < hashes.Length ? hashes[slot] : (key + slot).GetStableHashCode();

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.SetSlot))]
        private static class Emptied
        {
            [HarmonyPostfix]
            private static void Postfix(CookingStation __instance, int slot, string itemName)
            {
                if (string.IsNullOrEmpty(itemName) && Kitchen.IsKitchen(__instance))
                    HookGuard.Run("kitchen slot reset", static args => ResetSlot(args.Item1, args.Item2), (__instance, slot));
            }
        }

        private static void ResetSlot(CookingStation station, int slot)
        {
            ZNetView nview = station.m_nview;
            if (nview != null && nview.IsValid())
                Reset(nview.GetZDO(), slot);
        }
    }
}
