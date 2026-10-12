using System.Collections.Generic;
using EliteEquipment.Core;
using UnityEngine;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// The game's own craftable weapons of the eleven filled lines (swords, bows, maces, knives, greatswords, spears,
    /// atgeirs, axes, greataxes, dual axes, fists) shown with icons remade the way the new ones are (the weapons' bundle)
    /// while Weapons is on (<see cref="GameIcons"/>). The Abyssal Harpoon (SpearChitin) keeps its own icon (the user,
    /// 2026-10-10): its rope coil does not come out of a render of its mesh.
    /// </summary>
    public static class WeaponIcons
    {
        private static readonly GameIcons icons = new GameIcons(
            "SwordBronze", "SwordIron", "SwordSilver", "SwordBlackmetal", "SwordMistwalker", "SwordNiedhogg", "SwordDyrnwyn",
            "SwordGold", "SwordWood",
            "Bow", "BowFineWood", "BowHuntsman", "BowDraugrFang", "BowSpineSnap", "BowAshlands", "BowAshlandsRoot",
            "BowAshlandsStorm", "BowGold",
            "Club", "MaceBronze", "MaceIron", "MaceSilver", "MaceNeedle", "MaceEldner", "MaceGold",
            "KnifeFlint", "KnifeCopper", "KnifeButcher", "KnifeChitin", "KnifeSilver", "KnifeBlackMetal", "KnifeSkollAndHati",
            "KnifeGold",
            "THSwordKrom", "THSwordSlayer", "THSwordGold",
            "SpearFlint", "SpearBronze", "SpearElderbark", "SpearWolfFang", "SpearCarapace", "SpearSplitner", "SpearGold",
            "AtgeirBronze", "AtgeirIron", "AtgeirBlackmetal", "AtgeirHimminAfl", "AtgeirGold",
            "AxeStone", "AxeFlint", "AxeBronze", "AxeIron", "AxeBlackMetal", "AxeJotunBane", "AxeGold",
            "Battleaxe", "BattleaxeCrystal", "BattleaxeBlackmetal", "BattleaxeSkullSplittur", "BattleaxeGold",
            "AxeEarly", "AxeBerzerkr",
            "FistBjornClaw", "FistFenrirClaw", "FistBjornUndeadClaw", "FistGold");

        public static void Prepare(List<GameObject> prefabs, AssetBundle bundle) => icons.Prepare(prefabs, bundle);

        public static void Apply(bool on) => icons.Apply(on);
    }
}
