using UnityEngine;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// A human's own fists. A Humanoid with empty hands fights with its unarmed weapon (a bow-only human closed in on, a
    /// definition with no weapon), and the player's is the game's PlayerUnarmed item itself, one item data every player
    /// shares: its AI values could not be fitted for humans alone, and all humans would share its last-attack time, so
    /// one punching would hold back every other. Each human gets its own copy instead, on an inactive child of the
    /// prefab (never woken, never networked; instantiating the prefab gives every creature its own copy), fitted by
    /// <see cref="HumanWeaponAi"/>.
    /// </summary>
    internal static class HumanFists
    {
        private const string Holder = "ECP_HumanFists";

        public static void Give(GameObject shell, Humanoid human)
        {
            ItemDrop? game = human.m_unarmedWeapon;
            if (game == null)
            {
                return;
            }
            GameObject holder = new GameObject(Holder);
            holder.SetActive(false);
            holder.transform.SetParent(shell.transform, false);
            ItemDrop own = holder.AddComponent<ItemDrop>();
            own.m_itemData = game.m_itemData.Clone();
            own.m_itemData.m_shared = HumanWeaponAi.Copy(game.m_itemData.m_shared);
            HumanWeaponAi.Fit(own.m_itemData.m_shared);
            human.m_unarmedWeapon = own;
        }
    }
}
