using System.Collections.Generic;
using EliteEquipment.Core;
using UnityEngine;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// The game's own bucklers, round and tower shields shown with icons remade the way the new ones are (the shields'
    /// bundle: the same view, light, shine and size per kind) while Shields is on (<see cref="GameIcons"/>).
    /// </summary>
    public static class ShieldIcons
    {
        private static readonly GameIcons icons = new GameIcons(
            "ShieldBronzeBuckler", "ShieldIronBuckler", "ShieldCarapaceBuckler", "ShieldGoldBuckler",
            "ShieldWood", "ShieldBanded", "ShieldRoots", "ShieldSerpentscale", "ShieldSilver", "ShieldBlackmetal",
            "ShieldCarapace", "ShieldFlametal", "ShieldGold",
            "ShieldWoodTower", "ShieldBoneTower", "ShieldIronTower", "ShieldBlackmetalTower", "ShieldFlametalTower",
            "ShieldGoldTower");

        public static void Prepare(List<GameObject> prefabs, AssetBundle bundle) => icons.Prepare(prefabs, bundle);

        public static void Apply(bool on) => icons.Apply(on);
    }
}
