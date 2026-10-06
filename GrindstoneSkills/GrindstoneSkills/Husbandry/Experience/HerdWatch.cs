using System.Collections.Generic;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// Husbandry experience for tending animals, earned on each keeper's own client with no network traffic. Every two
    /// seconds the local player's client snapshots each tameable creature within Keeper Range (<see cref="HerdSnapshot"/>,
    /// from ZDO values the creature's owner writes and the game replicates) and credits what changed since the last
    /// snapshot (<see cref="HerdExperience"/>), times the creature's tier. A creature seen for the first time earns
    /// nothing, so walking into a farm or loading one is no windfall; one out of range or unloaded is forgotten.
    /// Everyone near earns: tending a herd together trains everyone tending it.
    /// </summary>
    public static class HerdWatch
    {
        private const float Interval = 2f;

        private static readonly Dictionary<ZDOID, HerdSnapshot> seen = new Dictionary<ZDOID, HerdSnapshot>();
        private static readonly Dictionary<ZDOID, HerdSnapshot> next = new Dictionary<ZDOID, HerdSnapshot>();
        private static float timer;

        /// <summary>Every frame for the local player (<see cref="LocalPlayerTick"/>).</summary>
        public static void Tick(Player player, float dt)
        {
            timer += dt;
            if (timer < Interval)
                return;
            timer = 0f;
            Guard.Run("herd watch", static p => Scan(p), player);
        }

        private static void Scan(Player player)
        {
            next.Clear();
            float experience = 0f;
            if (HusbandrySkill.Active)
                foreach (Character creature in Character.GetAllCharacters())
                    experience += Watch(player, creature);
            seen.Clear();
            foreach (KeyValuePair<ZDOID, HerdSnapshot> entry in next)
                seen[entry.Key] = entry.Value;
            HusbandryXp.Raise(player, experience);
        }

        private static float Watch(Player player, Character creature)
        {
            Tameable tameable = Herd.TameableOf(creature);
            float range = HusbandrySettings.KeeperRange.Value;
            if (tameable == null || tameable.m_nview == null || !tameable.m_nview.IsValid()
                || (creature.transform.position - player.transform.position).sqrMagnitude > range * range)
                return 0f;
            ZDOID id = creature.GetZDOID();
            HerdSnapshot now = HerdSnapshot.Of(tameable);
            next[id] = now;
            if (!seen.TryGetValue(id, out HerdSnapshot before))
                return 0f;
            string prefab = Herd.PrefabName(creature);
            return HerdExperience.Between(before, now, player, prefab) * Herd.Tier(prefab);
        }
    }
}
