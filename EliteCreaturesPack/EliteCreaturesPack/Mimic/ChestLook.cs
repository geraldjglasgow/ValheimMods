using UnityEngine;

namespace EliteCreaturesPack.Mimic
{
    /// <summary>
    /// What a crypt chest says when you look at it, taken from the game's own chest so a dormant mimic reads exactly
    /// like one: its name, and the same "[E] Open" line (with the stack-all hint) the game writes for a full chest.
    /// </summary>
    public static class ChestLook
    {
        private static string? _key;

        public static string Name() => Localization.instance.Localize(Key);

        public static string HoverText() =>
            Localization.instance.Localize(Key + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open $msg_stackall_hover");

        private static string Key => _key ??= FindKey();

        private static string FindKey()
        {
            GameObject? chest = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(MimicPrefabs.Chest) : null;
            Container? container = chest != null ? chest.GetComponent<Container>() : null;
            return container != null ? container.m_name : "$piece_chest";
        }
    }
}
