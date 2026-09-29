namespace Workshop.SkelArsenal
{
    /// <summary>One bone weapon in a player's hands: its prefab, its hand, the game's animation state for it, and what it does.</summary>
    public sealed class PlayerRoutine
    {
        public string Label, Prefab, Spent, Bow, Secondary;
        public bool LeftHand, Crossbow;
        public int State;                      // ItemDrop.ItemData.AnimationState, as the game sets statei/statef
        public string[] Triggers = new string[0];
        public float Scale = 1f;
    }

    /// <summary>
    /// The players' bone weapons (Elite Creatures Pack), each played the way the game plays the item it is a copy of: its
    /// animation state and its primary attack's trigger for every step of its combo (read from the game's items: the copper
    /// knife's knife_stab, the bronze sword's and mace's swing_longsword, the bronze axe's swing_axe, the bronze spear's
    /// spear_poke, the bronze atgeir's atgeir_attack, the fine bow's bow_aim and bow_fire), and the Bone Crossbow as the
    /// Arbalest (crossbow_fire, then reload_crossbow), 1.25 times the crossbowmen's crossbow as the mod makes it.
    /// </summary>
    public static class ArsenalPlayerRoutines
    {
        private const string Arsenal = "Assets/Bundles/ecp_skel_arsenal/", Xbow = "Assets/Bundles/ecp_crossbowman/";

        public static readonly PlayerRoutine[] All =
        {
            Melee("Dagger", "ecp_skel_dagger", 0, "knife_stab", 3, "knife_secondary"),
            Melee("Sword", "ecp_skel_sword", 1, "swing_longsword", 3, "sword_secondary"),
            Melee("Axe", "ecp_skel_axe", 1, "swing_axe", 3, "axe_secondary"),
            Melee("Mace", "ecp_skel_mace", 1, "swing_longsword", 3, "mace_secondary"),
            new PlayerRoutine { Label = "Spear", Prefab = Weapon("ecp_skel_spear"), State = 1, Triggers = new[] { "spear_poke" }, Secondary = "spear_throw" },
            Melee("Atgeir", "ecp_skel_atgeir", 7, "atgeir_attack", 3, "atgeir_secondary"),
            new PlayerRoutine { Label = "Bow", Prefab = Weapon("ecp_skel_bow_player"), LeftHand = true, State = 3, Bow = "ecp_skel_bow_player" },
            new PlayerRoutine
            {
                Label = "Crossbow", Prefab = Xbow + "ecp_xbow_item_rig.prefab", Crossbow = true, LeftHand = true, State = 10, Scale = 1.25f,
            },
        };

        private static PlayerRoutine Melee(string label, string weapon, int state, string attack, int steps, string secondary)
        {
            var triggers = new string[steps];
            for (int i = 0; i < steps; i++)
                triggers[i] = attack + i;
            return new PlayerRoutine { Label = label, Prefab = Weapon(weapon), State = state, Triggers = triggers, Secondary = secondary };
        }

        private static string Weapon(string name) => $"{Arsenal}{name}/{name}.prefab";
    }
}
