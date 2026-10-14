using System.Collections.Generic;
using EliteCraftingLink;
using EliteCreaturesReborn.Creatures.BatFiend;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The hall's guardians: the very first time anyone is in Hel's hall, in this world, three Bat-fiends wait on the rune
    /// circle at its centre, facing the gate (the user, 2026-10-13: "ONLY the very first time someone goes into Hel's
    /// domain"). The core's owner decides it (<see cref="ArenaCore"/>, every step, before the trial's), so two players
    /// arriving together never bring six: it spawns them, which makes it their owner, and marks the core's ZDO
    /// (<see cref="ArenaKeys.GuardiansCame"/>), saved with the world, so they never come again - not after they die, not
    /// for a later player, not after a restart. Not while a trial runs. They are no arena creatures (no trial tag): they
    /// keep the Bat-fiend's own drops, and each also drops a Recasting Rune and a Sealed Rune when EliteCrafting is
    /// installed (<see cref="Drops"/>, from the drop list's patch, on the dying guardian's owner).
    /// </summary>
    internal static class ArenaGuardians
    {
        private const int Count = 3;
        private const float Ring = 3f;   // metres from the hall's centre, a triangle on the rune circle

        private static readonly int CameKey = ArenaKeys.GuardiansCame.GetStableHashCode();
        private static readonly int GuardianKey = ArenaKeys.Guardian.GetStableHashCode();
        private static readonly string[] Runes = { CraftingPrefabs.Recasting, CraftingPrefabs.Sealed };

        /// <summary>On the core's owner: brings the guardians if nobody ever has and a player is in the hall now.</summary>
        public static void Step(ArenaCore core)
        {
            ZDO? zdo = core.Zdo;
            if (zdo == null || zdo.GetBool(CameKey) || !ArenaHall.Built || core.State.Running || HallPlayers.PeersNow().Count == 0)
            {
                return;
            }
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(BatFiendPrefabs.Creature) : null;
            if (prefab == null)
            {
                return; // the Bat-fiend could not be built here; nothing is marked, so a machine that has it brings them
            }
            zdo.Set(CameKey, true);
            for (int i = 0; i < Count; i++)
            {
                Spawn(prefab, i);
            }
            Util.Log.Info($"Hel's hall: its {Count} Bat-fiend guardians came, the first time anyone was in the hall.");
        }

        private static void Spawn(GameObject prefab, int index)
        {
            float angle = (90f + index * 360f / Count) * Mathf.Deg2Rad;
            Vector3 at = ArenaHall.Origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Ring + Vector3.up * 0.2f;
            GameObject guardian = Object.Instantiate(prefab, at, Quaternion.LookRotation(Vector3.forward));
            ZNetView? nview = guardian.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid())
            {
                nview.GetZDO().Set(GuardianKey, true);
            }
        }

        /// <summary>A dying guardian's drops: its own, and one of each rune (only with EliteCrafting: else the runes are not items).</summary>
        public static void Drops(CharacterDrop drop, List<KeyValuePair<GameObject, int>> result)
        {
            ZNetView? nview = drop != null && drop.m_character != null ? drop.m_character.m_nview : null;
            if (result == null || nview == null || !nview.IsValid() || !nview.GetZDO().GetBool(GuardianKey) || ObjectDB.instance == null)
            {
                return;
            }
            foreach (string rune in Runes)
            {
                GameObject? item = ObjectDB.instance.GetItemPrefab(rune);
                if (item != null)
                {
                    result.Add(new KeyValuePair<GameObject, int>(item, 1));
                }
            }
        }
    }
}
