using System.Collections.Generic;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Which status effects Cleanse takes away (user, 2026-11-02: "removes all debuffs from ally or self"): the game's
    /// harmful ones by name (checked against the game's item database, 2026-11-02: burning, spirit fire, poison, frost,
    /// lightning, smoke, tar, wet, slime, cold, freezing, the harpoon's pull and the three holds), our own mods' published
    /// harmful ones (Elite Creatures Pack's mire, bleeding and kraken ink, Elite Creatures Reborn's bat fiend screech and
    /// chill, EliteCrafting's hamstring and paralysis; nothing when those mods are absent), and any effect of the game's
    /// harm-only classes (<c>SE_Burning</c>, <c>SE_Poison</c>, <c>SE_Frost</c>, <c>SE_Smoke</c>, <c>SE_Harpooned</c>), so a
    /// mod's own fire or poison over time goes too. Nothing else is touched: rested, shelter, resting, the campfire, warmth,
    /// food, every mead and potion, the chants, set bonuses, the Guardian powers, the corpse run and soft death stay. Cold,
    /// freezing, wet and smoke come straight back while their cause lasts (the game puts them on again at once).
    /// </summary>
    public static class CleanseList
    {
        private static readonly HashSet<int> harmful = Hashes(
            "Burning", "Spirit", "Poison", "Frost", "Lightning", "Smoked", "Tared", "Wet", "Slimed", "Cold", "Freezing",
            "Harpooned", "Immobilized", "ImmobilizedLong", "ImmobilizedAshlands",
            "ECP_Mired", "ECP_Bleeding", "ECP_KrakenInk", "ecr_batfiend_screech", "ecr_batfiend_chill",
            "ECF_Hamstring", "ECF_Paralyze");

        private static readonly List<int> found = new List<int>();

        public static bool Harmful(StatusEffect effect) =>
            effect != null && (harmful.Contains(effect.NameHash()) || effect is SE_Burning || effect is SE_Poison
                || effect is SE_Frost || effect is SE_Smoke || effect is SE_Harpooned);

        /// <summary>Takes every harmful effect off, without their stop messages; on the character's owner, where they live.</summary>
        public static void Strip(SEMan seman)
        {
            found.Clear();
            foreach (StatusEffect effect in seman.GetStatusEffects())
            {
                if (Harmful(effect))
                    found.Add(effect.NameHash());
            }
            foreach (int hash in found)
                seman.RemoveStatusEffect(hash, quiet: true);
            found.Clear();
        }

        private static HashSet<int> Hashes(params string[] names)
        {
            var hashes = new HashSet<int>();
            foreach (string name in names)
                hashes.Add(name.GetStableHashCode());
            return hashes;
        }
    }
}
