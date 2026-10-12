using System.Collections.Generic;
using EliteEquipment.Core;
using UnityEngine;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// The 22 weapons that fill the gaps in the game's weapon lines (<c>EliteEquipment/features/weapon_gaps.txt</c>). Damage
    /// sits on the line between the game's same-type weapons either side (read in game 2026-10-10; WEAPONS.md has the
    /// neighbours); silver weapons add a little spirit as the game's silver gear does. Each recipe sits beside a same-tier
    /// game weapon at a comparable cost. To be checked with the valheim-weapon-dps skill in game.
    /// </summary>
    public static class WeaponSets
    {
        private const string Sword = "Viking_Sword (1)";

        private static HitData.DamageTypes Hit(float slash = 0f, float pierce = 0f, float blunt = 0f, float chop = 0f, float spirit = 0f) =>
            new HitData.DamageTypes { m_slash = slash, m_pierce = pierce, m_blunt = blunt, m_chop = chop, m_spirit = spirit };

        private static RecipePart P(string item, int amount, int perLevel = 0) => new RecipePart(item, amount, perLevel);

        public static readonly IReadOnlyList<WeaponSet> All = new List<WeaponSet>
        {
            Greatsword("Bronze", "SwordBronze", Sword, Hit(slash: 46), P("Wood", 4, 2), P("Bronze", 12, 6), P("LeatherScraps", 3, 2)),
            Greatsword("Iron", "SwordIron", Sword, Hit(slash: 72), P("Wood", 4, 2), P("Iron", 30, 15), P("LeatherScraps", 4, 2)),
            Greatsword("Silver", "SwordSilver", Sword, Hit(slash: 98, spirit: 30), P("Wood", 4, 2), P("Silver", 60, 30), P("Iron", 8, 4), P("LeatherScraps", 4, 2)),
            Greatsword("Black Metal", "SwordBlackmetal", "default", Hit(slash: 124), P("FineWood", 4), P("BlackMetal", 30, 15), P("LinenThread", 8, 5)),

            Dual("Bronze", "AxeBronze", "Viking_Axe", "AxeBronze", Hit(slash: 48, chop: 42), P("Wood", 8), P("Bronze", 12, 6), P("LeatherScraps", 4, 2)),
            Dual("Iron", "AxeIron", "Viking_Axe", "AxeIron", Hit(slash: 67, chop: 50), P("Wood", 8), P("Iron", 30, 15), P("LeatherScraps", 4, 2)),
            Dual("Silver", "KnifeSilver", "default", "SwordSilver", Hit(slash: 85, chop: 57, spirit: 15), P("Wood", 6), P("Silver", 40, 20), P("LeatherScraps", 4, 2)),
            Dual("Black Metal", "AxeBlackMetal", "Model", "AxeBlackMetal", Hit(slash: 103, chop: 65), P("FineWood", 8), P("BlackMetal", 30, 15), P("LinenThread", 6, 4)),
            Dual("Carapace", "SpearCarapace", "carapacespear", "SpearCarapace", Hit(slash: 122, chop: 72), P("YggdrasilWood", 8), P("Carapace", 16, 8), P("Mandible", 2, 1)),
            Dual("Bloodgold", "AxeGold", "Model", "AxeGold", Hit(slash: 158, chop: 88), P("Gold", 30, 15), P("Frostwood", 15, 5), P("MoldAxe", 1)),

            Fist("ee_fist_flint", "Flint Knuckles", "Flint shards lashed to a wooden knuckle bar.", "KnifeFlint", "knife", "AxeFlint", Hit(blunt: 14), P("Wood", 4, 2), P("Flint", 6, 3), P("LeatherScraps", 4, 2)),
            Fist("ee_fist_iron", "Iron Claws", "Three hooked iron claws on each hand.", "SwordIron", Sword, "SwordIron", Hit(slash: 42), P("Wood", 2), P("Iron", 20, 10), P("LeatherScraps", 6, 3)),
            Fist("ee_fist_flametal", "Flametal Claws", "Barbed flametal claws, red at the tips.", "ShieldFlametal", "default", "THSwordSlayer", Hit(slash: 100), P("FlametalNew", 20, 10), P("AskHide", 4, 2), P("MorgenSinew", 2, 1)),

            Held("ee_knife_iron", "Iron Dagger", "A broad iron dagger, the iron sword's short brother.", "KnifeSilver", "SwordIron", Sword, "SwordIron", Hit(slash: 18, pierce: 18), P("Wood", 2, 1), P("Iron", 10, 5), P("LeatherScraps", 2, 1)),
            Held("ee_knife_flametal", "Flametal Dagger", "A toothed flametal seax.", "KnifeBlackMetal", "THSwordSlayer", "default", "SpearSplitner", Hit(slash: 60, pierce: 60), P("Blackwood", 4), P("FlametalNew", 10, 5), P("AskHide", 2, 1)),
            Held("ee_atgeir_silver", "Silver Atgeir", "A silver glaive with a curling fang.", "AtgeirBlackmetal", "SwordSilver", Sword, "SwordSilver", Hit(pierce: 85, spirit: 15), P("Wood", 10), P("Silver", 30, 15), P("Iron", 4, 2), P("LeatherScraps", 3, 1)),
            Held("ee_atgeir_flametal", "Flametal Atgeir", "A jagged flametal blade on a charred haft.", "AtgeirBlackmetal", "SpearSplitner", "default", "SpearSplitner", Hit(pierce: 150), P("Blackwood", 10), P("FlametalNew", 30, 15), P("AskHide", 4, 2)),
            Trail(Held("ee_axe_silver", "Silver Axe", "A bearded silver axe with a scroll on its back.", "AxeBlackMetal", "ShieldSilver", "model", "SwordSilver", Hit(slash: 80, chop: 55, spirit: 15), P("Wood", 4), P("Silver", 20, 10), P("Iron", 2, 1), P("LeatherScraps", 2, 1)), new Vector3(0.24f, 0f, 0.50f), new Vector3(0.25f, 0f, 0.78f)),
            Trail(Held("ee_axe_flametal", "Flametal Axe", "A toothed flametal axe with a back spike.", "AxeBlackMetal", "ShieldFlametal", "default", "SpearSplitner", Hit(slash: 140, chop: 80), P("Blackwood", 4), P("FlametalNew", 20, 10), P("AskHide", 2, 1)), new Vector3(0.26f, 0f, 0.48f), new Vector3(0.27f, 0f, 0.80f)),
            Trail(Held("ee_battleaxe_flametal", "Flametal Greataxe", "A broad flametal greataxe with flame-hooked edges.", "BattleaxeBlackmetal", "AxeBerzerkr", "default", "THSwordSlayer", Hit(slash: 160, chop: 60), P("Blackwood", 10), P("FlametalNew", 30, 15), P("AskHide", 5, 3)), new Vector3(0.20f, 0f, 1.00f), new Vector3(0.43f, 0f, 1.40f)),
            Held("ee_bow_blackmetal", "Black Metal Bow", "A black metal recurve with hooked blades on its limbs.", "BowDraugrFang", "AtgeirBlackmetal", "default", "SwordBlackmetal", Hit(pierce: 60), P("FineWood", 10, 5), P("BlackMetal", 15, 8), P("LinenThread", 10, 5)),
            Throw(Held("ee_spear_blackmetal", "Black Metal Spear", "A barbed black metal spear.", "SpearWolfFang", "AtgeirBlackmetal", "default", "SwordBlackmetal", Hit(pierce: 95), P("FineWood", 10, 5), P("BlackMetal", 10, 5), P("LinenThread", 4, 2)), new Vector3(0f, 0f, -0.854f), new Vector3(0f, 180f, 0f)),
        };

        private static WeaponSet Held(string asset, string name, string text, string game, string look, string part, string beside, HitData.DamageTypes damage, params RecipePart[] cost) =>
            new WeaponSet { Prefab = "EE_" + asset.Substring(3), Asset = asset, Name = name, Description = text, Base = game, Look = look, LookPart = part, Beside = beside, Damage = damage, Cost = cost };

        private static WeaponSet Greatsword(string tier, string sword, string part, HitData.DamageTypes damage, params RecipePart[] cost) =>
            Held("ee_greatsword_" + Key(tier), tier + " Greatsword", $"A long {tier.ToLowerInvariant()} blade for two hands.", "THSwordKrom", sword, part, sword, damage, cost);

        private static WeaponSet Dual(string tier, string look, string part, string beside, HitData.DamageTypes damage, params RecipePart[] cost)
        {
            WeaponSet set = Held("ee_dualaxes_" + Key(tier), tier + " Dual Axes", $"A pair of {tier.ToLowerInvariant()} axes, one for each hand.", "AxeBerzerkr", look, part, beside, damage, cost);
            set.Kind = WeaponKind.Dual;
            return set;
        }

        private static WeaponSet Fist(string asset, string name, string text, string look, string part, string beside, HitData.DamageTypes damage, params RecipePart[] cost)
        {
            WeaponSet set = Held(asset, name, text, "FistFenrirClaw", look, part, beside, damage, cost);
            set.Kind = WeaponKind.Fist;
            return set;
        }

        private static WeaponSet Trail(WeaponSet set, Vector3 trailBase, Vector3 trailTip)
        {
            set.TrailBase = trailBase;
            set.TrailTip = trailTip;
            return set;
        }

        private static WeaponSet Throw(WeaponSet set, Vector3 position, Vector3 rotation)
        {
            set.Thrown = true;
            set.ThrownPosition = position;
            set.ThrownRotation = rotation;
            return set;
        }

        private static string Key(string tier) => tier.Replace(" ", "").ToLowerInvariant();
    }
}
