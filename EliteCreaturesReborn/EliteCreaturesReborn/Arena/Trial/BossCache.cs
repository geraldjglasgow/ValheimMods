using System.Collections.Generic;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The boss cache (features/arena.md section 3, "Boss cache"): when a boss of the trial falls (the last of Kall's
    /// phases, not a copy), 60% of the time healing meads drop for its players - for each player of the trial 1 to 3, of
    /// the boss's tier (Meadows and Black Forest the minor mead, Swamp and Mountains the medium, Plains and Mistlands the
    /// major, Ashlands and the Deep North the lingering, or the best the game has below that) - on the rune circle near
    /// where it died, with a burst of cold soul-fire every player of the trial sees. They are real items on the floor, made
    /// once on the boss's owner, which holds the core and so knows the players: the one thing an arena creature leaves.
    /// </summary>
    internal static class BossCache
    {
        private static readonly string[] Meads = { "MeadHealthMinor", "MeadHealthMedium", "MeadHealthMajor", "MeadHealthLingering" };

        /// <summary>The size of the soul-fire burst over the cache.</summary>
        private const float BurstSize = 0.6f;

        /// <summary>A boss of tier <paramref name="tier"/> has fallen, on its owner.</summary>
        public static void Drop(Character boss, int tier, ZDO core)
        {
            GameObject? mead = Mead(tier);
            if (mead == null || !Dice.Percent(ArenaTable.CacheChance))
            {
                return;
            }
            Vector3 at = OnCircle(boss.transform.position);
            List<long> players = TrialPlayers.All(core);
            int total = 0;
            for (int i = 0; i < players.Count; i++)
            {
                int count = Random.Range(ArenaTable.CacheMeadsMin, ArenaTable.CacheMeadsMax + 1);
                Place(mead, count, at);
                total += count;
            }
            ArenaNet.Flare(core, at, BurstSize);
            Log.Info($"Hel's Trial: the boss left a cache of {total} {mead.name} for {players.Count} player(s)");
        }

        // The tier's mead, or the best the game has below it; null when it has none.
        private static GameObject? Mead(int tier)
        {
            ObjectDB db = ObjectDB.instance;
            for (int i = Mathf.Clamp((tier - 1) / 2, 0, Meads.Length - 1); i >= 0 && db != null; i--)
            {
                GameObject? prefab = db.GetItemPrefab(Meads[i]);
                if (prefab != null && prefab.GetComponent<ItemDrop>() != null)
                {
                    return prefab;
                }
            }
            return null;
        }

        // On the rune circle: where the boss fell, drawn in to the circle when it fell further out.
        private static Vector3 OnCircle(Vector3 fell)
        {
            Vector3 flat = fell - ArenaHall.Origin;
            flat.y = 0f;
            Vector3 at = ArenaHall.Origin + Vector3.ClampMagnitude(flat, ArenaTable.CacheCircle) + Vector3.up;
            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(at, out float floor))
            {
                at.y = floor + 0.3f;
            }
            return at;
        }

        // One player's meads, one stack, a little apart from the others: a networked item this machine owns until taken.
        private static void Place(GameObject prefab, int count, Vector3 at)
        {
            Vector2 aside = Random.insideUnitCircle * 1.5f;
            GameObject made = Object.Instantiate(prefab, at + new Vector3(aside.x, 0.5f, aside.y), Quaternion.identity);
            ItemDrop item = made.GetComponent<ItemDrop>();
            item.m_itemData.m_worldLevel = (byte)Game.m_worldLevel;
            item.SetStack(count);
        }
    }
}
