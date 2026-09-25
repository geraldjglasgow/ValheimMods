using UnityEngine;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// The mead affixes, all on the local player's own client:
    /// Reflex Draught drinks the best healing mead in the inventory when the player becomes health-critical, through
    /// the game's own consume path (so its cooldown, the status effect's category, still applies); Swift Draught makes
    /// a healing mead drunk while critical heal its whole over-time amount at once; Brewer's Haste shortens the
    /// restoring status effect a mead adds, which is what blocks the next mead of its kind.
    /// A "healing mead" is a consumable whose status effect heals up front or over time (judgement call: resistance
    /// meads, whose status effect is the benefit itself, are never shortened).
    /// </summary>
    internal static class Meads
    {
        private static bool _drinkPending;

        /// <summary>The health-critical state turned on (called from the aggregate's tick): drink on the next frame.</summary>
        public static void OnBecameCritical() => _drinkPending = true;

        /// <summary>Once per frame, outside every status-effect update (drinking adds a status effect).</summary>
        public static void Tick(Player player)
        {
            if (!_drinkPending)
            {
                return;
            }
            _drinkPending = false;
            if (AggregateHost.Current[EffectKind.AutoMead] > 0f && !player.IsDead())
            {
                ItemDrop.ItemData? mead = BestHealingMead(player);
                if (mead != null)
                {
                    player.ConsumeItem(player.GetInventory(), mead);
                }
            }
        }

        /// <summary>A status effect added while the local player drinks.</summary>
        public static void OnDrunk(SE_Stats effect)
        {
            if (!Restores(effect))
            {
                return;
            }
            AggregateValues v = AggregateHost.Current;
            if (v[EffectKind.MeadBurst] > 0f && effect.m_healthOverTimeTicks > 0f && effect.m_character != null)
            {
                effect.m_character.Heal(effect.m_healthOverTimeTicks * effect.m_healthOverTimeTickHP);
                effect.m_healthOverTimeTicks = 0f;
            }
            float shorter = v[EffectKind.MeadCooldown];
            if (shorter > 0f && effect.m_ttl > 0f)
            {
                float floor = Mathf.Max(effect.m_healthOverTimeDuration, Mathf.Max(effect.m_staminaOverTimeDuration, effect.m_eitrOverTimeDuration));
                effect.m_ttl = Mathf.Max(floor, effect.m_ttl * (1f - shorter));
            }
        }

        private static bool Restores(SE_Stats effect) =>
            effect.m_healthOverTime > 0f || effect.m_healthUpFront > 0f || effect.m_staminaOverTime > 0f
            || effect.m_staminaUpFront > 0f || effect.m_eitrOverTime > 0f || effect.m_eitrUpFront > 0f;

        private static bool Heals(SE_Stats effect) => effect.m_healthOverTime > 0f || effect.m_healthUpFront > 0f;

        // The largest total heal among meads the game would let the player drink now (same checks as CanConsumeItem,
        // done first so a blocked mead never shows the game's "can't consume" message).
        private static ItemDrop.ItemData? BestHealingMead(Player player)
        {
            ItemDrop.ItemData? best = null;
            float bestHeal = 0f;
            SEMan seman = player.GetSEMan();
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable || item.m_shared.m_food > 0f
                    || !(item.m_shared.m_consumeStatusEffect is SE_Stats effect) || !Heals(effect)
                    || seman.HaveStatusEffect(effect.NameHash()) || seman.HaveStatusEffectCategory(effect.m_category))
                {
                    continue;
                }
                float heal = effect.m_healthUpFront + effect.m_healthOverTime;
                if (heal > bestHeal)
                {
                    best = item;
                    bestHeal = heal;
                }
            }
            return best;
        }
    }
}
