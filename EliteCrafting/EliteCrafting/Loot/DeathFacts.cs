using System;
using EliteCrafting.Config;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// Everything the drop roll reads from a dying creature, taken once at the death hook while its ZDO is still valid
    /// (the game destroys the object at the end of its death handling). Readable on any peer for <c>LootPreview</c>;
    /// the roll itself only ever uses it on the ZDO owner.
    /// </summary>
    public readonly struct DeathFacts
    {
        private DeathFacts(Character character, ZDO zdo)
        {
            PrefabHash = zdo.GetPrefab();
            Stars = Math.Max(0, character.GetLevel() - 1);
            Ally = LootKeys.IsPlayerAlly(character);
            PlayerHit = LootKeys.PlayerHit(zdo);
            AllyHit = LootKeys.AllyHitSet(zdo);
            Cheated = zdo.GetBool(ZDOVars.s_cheated);
            FlaggedBoss = character.IsBoss();
            Position = character.transform.position;
            Center = character.GetCenterPoint();
            Ecr = EcrFacts.Read(zdo, ModSettings.EcrSynergy.Value);
        }

        public int PrefabHash { get; }

        /// <summary>Vanilla level minus one (0 = unstarred).</summary>
        public int Stars { get; }

        /// <summary>
        /// Elite Creatures Reborn's keys on the creature (ecr-integration.md): default when ECR is absent; the star and
        /// tier keys only when the synced synergy switch is on.
        /// </summary>
        public EcrFacts Ecr { get; }

        /// <summary>The star count the roll uses: ECR's for an ECR-resolved creature with the synergy on, else <see cref="Stars"/>.</summary>
        public int RollStars => Ecr.UsesStars ? Ecr.Stars : Stars;

        /// <summary>Tamed, or summoned by a player (treated like tamed: a summon is a player's own creature).</summary>
        public bool Ally { get; }

        public bool PlayerHit { get; }
        public bool AllyHit { get; }
        public bool Cheated { get; }

        /// <summary>The game marks it as a boss (a boss not in the boss map still uses the boss rarity rows).</summary>
        public bool FlaggedBoss { get; }

        public Vector3 Position { get; }
        public Vector3 Center { get; }

        /// <summary>A living non-player creature with a valid ZDO; false for players and despawned objects.</summary>
        public static bool TryRead(Character character, out DeathFacts facts)
        {
            facts = default;
            if (character == null || character.IsPlayer() || character.m_nview == null || !character.m_nview.IsValid())
            {
                return false;
            }
            facts = new DeathFacts(character, character.m_nview.GetZDO());
            return true;
        }

        /// <summary>Whether this death may drop our loot (drops.md section 2); <paramref name="reason"/> says why not.</summary>
        public bool Qualifies(DropRules drops, out string reason)
        {
            if (Ally && !drops.Tamed)
            {
                reason = "tamed or summoned";
                return false;
            }
            if (drops.RequirePlayer && !PlayerHit && !AllyHit)
            {
                reason = "no player or pet hit it";
                return false;
            }
            if (Ecr.Worthless && drops.Ecr.SkipWorthless)
            {
                reason = "Elite Creatures Reborn marks it worthless (Cloven twin or Phantom husk)";
                return false;
            }
            reason = "";
            return true;
        }
    }
}
