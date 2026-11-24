using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Each wand's bolt charge in its stone's colour (features/casters.md, "Spell animations"): the workshop built
    /// <c>ee_fx_charge</c> again per stone (same shape, timing and scaling), so the little spell at the tip
    /// (<see cref="ChargeOrb"/>) is the Gnarled Wand's amber, the Frostsilver Wand's ice and the Rootbound Wand's earth. The
    /// Greywood Wand's is its own (user, 2026-11-02: "the light at the end of the wand should be more greenish with little
    /// rock"): <c>ee_fx_charge_rock</c>, a mossy green glow with tiny stone chips round it, the pale blue tint while the
    /// bundle lacks it. The Blackmetal Wand has no stone colour of its own and keeps the white, as does any wand
    /// while the bundle lacks its tint. Read on every machine from the wand the game shows in the caster's hand. The full
    /// charge's glow and flash and the hand's stream stay white, but for the Frostsilver Wand's own ice (Full, Ready, Stream).
    /// </summary>
    public static class BoltTint
    {
        private static Dictionary<int, string[]> tints, fulls, readies, streams;

        /// <summary>The prepared charge for the wand in this player's hand: its stone's, else the white one.</summary>
        public static GameObject Charge(Player player) => CasterFx.Prepared(Pick(player, Tints(), CasterFx.Charge));

        /// <summary>
        /// The full charge's glow, its flash and the other hand's stream for the wand in this player's hand: the
        /// Frostsilver Wand's own ice (user, 2026-11-02: "more ice like instead of reusing the meadows wand stuff"), else the
        /// white ones every other wand keeps.
        /// </summary>
        public static GameObject Full(Player player) => CasterFx.Prepared(Pick(player, Fulls(), CasterFx.ChargeFull));

        public static string Ready(Player player) => Pick(player, Readies(), CasterFx.ChargeReady);

        public static GameObject Stream(Player player) => CasterFx.Prepared(Pick(player, Streams(), CasterFx.ChargeStream));

        /// <summary>The first of the wand's own effects the bundle has, else the white one.</summary>
        private static string Pick(Player player, Dictionary<int, string[]> table, string white)
        {
            int wand = player != null && player.m_visEquipment != null ? player.m_visEquipment.m_rightItem : 0;
            if (table.TryGetValue(wand, out string[] names))
            {
                foreach (string name in names)
                {
                    if (CasterFx.Prepared(name) != null)
                        return name;
                }
            }
            return white;
        }

        private static Dictionary<int, string[]> Tints() => tints ?? (tints = new Dictionary<int, string[]>
        {
            { CasterGear.Wand.Prefab.GetStableHashCode(), new[] { "ee_fx_charge_tint_amber" } },
            { CasterGear.Greywood.Prefab.GetStableHashCode(), new[] { "ee_fx_charge_rock", "ee_fx_charge_tint_paleblue" } },
            { CasterGear.Frostsilver.Prefab.GetStableHashCode(), new[] { "ee_fx_charge_frost", "ee_fx_charge_tint_ice" } },
            { CasterGear.Rootbound.Prefab.GetStableHashCode(), new[] { "ee_fx_charge_tint_earth" } },
        });

        private static Dictionary<int, string[]> Fulls() => fulls ?? (fulls = Frost("ee_fx_charge_frost_full"));

        private static Dictionary<int, string[]> Readies() => readies ?? (readies = Frost("ee_fx_charge_frost_ready"));

        private static Dictionary<int, string[]> Streams() => streams ?? (streams = Frost("ee_fx_frost_stream"));

        private static Dictionary<int, string[]> Frost(string name) =>
            new Dictionary<int, string[]> { { CasterGear.Frostsilver.Prefab.GetStableHashCode(), new[] { name } } };
    }
}
