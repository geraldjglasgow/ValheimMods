using System;
using System.Collections.Generic;

namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// What other mods fixed for their creatures through the API (<c>features/api.md</c>), per prefab name: the mutations
    /// it always carries, the aspects it rolls among, the attack items Portalbound may send through its portals, and the
    /// creatures Summoner calls. Code, not data: every peer's mod makes the same registrations, so nothing here is synced.
    /// Only a creature's owner reads it, at the creature's roll (<see cref="Runtime.CreatureRoll"/>), and what the roll
    /// gives goes into the ZDO like any roll. Names are checked by the API before they get here; the body and the rule
    /// file are checked again at every roll (<see cref="RegisteredMutations"/>), since both can change after a
    /// registration. An empty list clears its part, and a prefab with nothing left is forgotten. Main thread only.
    /// </summary>
    public static class Registrations
    {
        private sealed class Entry
        {
            public int Mutations;
            public Aspect[]? Aspects;
            public string[]? PortalAttacks;
            public SummonPick[]? Summons;

            public bool Empty => Mutations == 0 && Aspects == null && PortalAttacks == null && Summons == null;
        }

        private static readonly Dictionary<string, Entry> ByPrefab = new Dictionary<string, Entry>(StringComparer.Ordinal);

        /// <summary>True when another mod registered anything for this prefab.</summary>
        public static bool Has(string prefab) => ByPrefab.ContainsKey(prefab);

        /// <summary>The fixed mutations, packed like a trait mask; 0 when none are registered.</summary>
        public static int MutationsOf(string prefab) => Find(prefab)?.Mutations ?? 0;

        /// <summary>The aspects it rolls among (one: always that one); null when none are registered.</summary>
        public static Aspect[]? AspectsOf(string prefab) => Find(prefab)?.Aspects;

        /// <summary>The attack item prefab names Portalbound carries for it; null when none are registered.</summary>
        public static string[]? PortalAttacksOf(string prefab) => Find(prefab)?.PortalAttacks;

        /// <summary>What its Summoner calls, in place of the rule file's list; null when none is registered.</summary>
        public static SummonPick[]? SummonsOf(string prefab) => Find(prefab)?.Summons;

        public static void SetMutations(string prefab, int mask) => Change(prefab, entry => entry.Mutations = mask);

        public static void SetAspects(string prefab, Aspect[] aspects) =>
            Change(prefab, entry => entry.Aspects = aspects.Length > 0 ? aspects : null);

        public static void SetPortalAttacks(string prefab, string[] attacks) =>
            Change(prefab, entry => entry.PortalAttacks = attacks.Length > 0 ? attacks : null);

        public static void SetSummons(string prefab, SummonPick[] summons) =>
            Change(prefab, entry => entry.Summons = summons.Length > 0 ? summons : null);

        /// <summary>Forgets everything registered for the prefab.</summary>
        public static void Clear(string prefab) => ByPrefab.Remove(prefab);

        private static Entry? Find(string prefab) =>
            prefab != null && ByPrefab.TryGetValue(prefab, out Entry entry) ? entry : null;

        private static void Change(string prefab, Action<Entry> change)
        {
            if (!ByPrefab.TryGetValue(prefab, out Entry entry))
            {
                entry = new Entry();
            }
            change(entry);
            if (entry.Empty)
            {
                ByPrefab.Remove(prefab);
            }
            else
            {
                ByPrefab[prefab] = entry;
            }
        }
    }
}
