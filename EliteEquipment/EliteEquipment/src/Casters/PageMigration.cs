using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Before the spellbook (until 2026-11-02) spells were learned into the character, kept in player custom data
    /// <see cref="OldKey"/>. A character who has that record gets one page of each of its spells, and of Mend (which every
    /// character knew then without a record; a new book holds no page since 2026-11-02), into the inventory, or at their
    /// feet when it is full, and the record is removed, so it happens once. Looked at once per
    /// local player with the switch on, from the HUD tick (the character's own machine; never a dedicated server).
    /// </summary>
    public static class PageMigration
    {
        public const string OldKey = "EliteEquipment.spells";

        private static Player looked;

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            if (player == looked || player == null || !CastersSettings.On || ObjectDB.instance == null)
                return;
            looked = player;
            if (!player.m_customData.TryGetValue(OldKey, out string keys))
                return;
            player.m_customData.Remove(OldKey);
            if (Give(player, keys) > 0)
                player.Message(MessageHud.MessageType.Center, Lang.Get("ee_page_migrated"));
        }

        private static int Give(Player player, string keys)
        {
            int given = 0;
            string[] learned = keys.Split(',');
            string all = System.Array.IndexOf(learned, Spells.MendKey) >= 0 ? keys : Spells.MendKey + "," + keys;
            foreach (string key in all.Split(','))
            {
                Spell spell = Spells.Find(key);
                GameObject page = spell != null ? ObjectDB.instance.GetItemPrefab(spell.Item) : null;
                if (page == null)
                    continue;
                if (!player.GetInventory().AddItem(page, 1))
                    Object.Instantiate(page, player.transform.position + Vector3.up, Quaternion.identity);
                given++;
            }
            return given;
        }
    }
}
