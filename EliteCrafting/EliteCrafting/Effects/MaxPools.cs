using System;
using HarmonyLib;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// Flat maximum health, stamina and eitr (effects <c>max_health</c>, <c>max_stamina</c>, <c>max_eitr</c>, plus Stout
    /// Heart's health and Restless Mind's percentage eitr loss, applied after the flat eitr): added to
    /// the food totals in a postfix on <c>Player.GetTotalFoodValue</c>, which the game calls once per food tick and turns
    /// into the maximum pools. Local player only (the pools of a player are computed on its own client).
    /// <para>
    /// The pools read the unconditional set only: a health-critical max-health bonus would move the threshold it is
    /// gated on and flicker. No Phase 1 affix has one; an owner-made one sums but is not applied (judgement call).
    /// </para>
    /// </summary>
    internal static class MaxPools
    {
        /// <summary>The totals the pools depend on, taken before a rebuild.</summary>
        public readonly struct Snapshot
        {
            public Snapshot(float health, float stamina, float eitr, float eitrLoss)
            {
                Health = health;
                Stamina = stamina;
                Eitr = eitr;
                EitrLoss = eitrLoss;
            }

            public float Health { get; }
            public float Stamina { get; }
            public float Eitr { get; }
            public float EitrLoss { get; }

            public bool Same(Snapshot other) =>
                Health == other.Health && Stamina == other.Stamina && Eitr == other.Eitr && EitrLoss == other.EitrLoss;
        }

        public static Snapshot Take()
        {
            AggregateValues v = AggregateBuilder.Normal;
            return new Snapshot(v[EffectKind.MaxHealth], v[EffectKind.MaxStamina], v[EffectKind.MaxEitr], v[EffectKind.EitrForRegen]);
        }

        /// <summary>After a rebuild: resize the pools now instead of on the next food tick, only when a total changed.</summary>
        public static void RefreshIfChanged(Player player, Snapshot before)
        {
            if (Take().Same(before) || player.IsDead())
            {
                return;
            }
            // The same three calls the game's food tick makes, without ticking the foods down (UpdateFood(0, true) would).
            player.GetTotalFoodValue(out float hp, out float st, out float ei);
            player.SetMaxHealth(hp, true);
            player.SetMaxStamina(st, true);
            player.SetMaxEitr(ei, true);
        }

        internal static void AddTo(Player player, ref float hp, ref float stamina, ref float eitr)
        {
            if (!ReferenceEquals(player, Player.m_localPlayer) || !ItemEffects.Enabled)
            {
                return;
            }
            AggregateValues v = AggregateBuilder.Normal;
            hp += v[EffectKind.MaxHealth];
            stamina += v[EffectKind.MaxStamina];
            eitr = (eitr + v[EffectKind.MaxEitr]) * Math.Max(0f, 1f - v[EffectKind.EitrForRegen]);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    internal static class MaxPoolsPatch
    {
        // Runs where the player's food ticks run: its own client.
        private static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr)
        {
            MaxPools.AddTo(__instance, ref hp, ref stamina, ref eitr);
        }
    }
}
