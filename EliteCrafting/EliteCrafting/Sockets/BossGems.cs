using System;
using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Sockets
{
    /// <summary>
    /// What each boss adds of the socket stones (sockets.md section 7), on top of its YAML drops: the Dvergr Chisel one
    /// time in four from every boss, and gems, any of the eleven at random, at a chance that grows boss by boss (user
    /// 2026-10-07: "all bosses should be able to drop all gems, its just more common to get a gem at later bosses";
    /// before, each boss had its own one or two), rolled once for every player near the boss when it dies (user: "roll the
    /// chance multiple times per player participating in the fight", <see cref="Loot.BossParty"/>). Fixed in code so every server has them whatever its own economy file
    /// says. Rolled on the creature's owner with the rest of the boss's drops, while <c>Rune drops</c> and
    /// <c>Gems and sockets</c> are on.
    /// </summary>
    internal static class BossGems
    {
        private const float ChiselChance = 25f;

        // Percent chance of one gem, by boss in the game's order.
        private static readonly Dictionary<string, float> GemChance = new Dictionary<string, float>(StringComparer.Ordinal)
        {
            ["Eikthyr"] = 15f, ["gd_king"] = 20f, ["Bonemass"] = 30f, ["Dragon"] = 40f,
            ["GoblinKing"] = 50f, ["SeekerQueen"] = 65f, ["Fader"] = 80f,
        };

        private static readonly List<StoneDef> Gems = new List<StoneDef>();

        /// <summary>The boss's gems (one roll per player) and chisel, each rolled on its own; a disabled stone never drops.</summary>
        public static void Add(EconomyRules economy, string? bossPrefab, int players, Random random, List<StoneDef> into)
        {
            if (!SocketSwitch.On || bossPrefab == null || !GemChance.TryGetValue(bossPrefab, out float gemChance))
            {
                return;
            }
            for (int i = 0; i < Math.Max(1, players); i++)
            {
                if (Rolls(random, gemChance))
                {
                    AnyGem(economy, random, into);
                }
            }
            StoneDef? chisel = economy.Stone(StoneCatalog.ChiselId);
            if (chisel != null && chisel.Enabled && Rolls(random, ChiselChance))
            {
                into.Add(chisel);
            }
        }

        private static void AnyGem(EconomyRules economy, Random random, List<StoneDef> into)
        {
            Gems.Clear();
            foreach (string id in StoneCatalog.GemIds)
            {
                StoneDef? gem = economy.Stone(id);
                if (gem != null && gem.Enabled)
                {
                    Gems.Add(gem);
                }
            }
            if (Gems.Count > 0)
            {
                into.Add(Gems[random.Next(Gems.Count)]);
            }
        }

        private static bool Rolls(Random random, float percent) => random.NextDouble() * 100.0 < percent;
    }
}
