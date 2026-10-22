using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Eikthyr drops Stag's Swiftness, one per player near (the game's own <c>m_onePerPlayer</c> drop), while the switch is
    /// on: added to his prefab once the scene has woken and whenever the switch turns on, taken off when it turns off, on
    /// every peer (the server or host decides the drop). Not scaled by his level. An Eikthyr already summoned keeps the
    /// drops he was made with.
    /// </summary>
    public static class EikthyrDrop
    {
        private const string Boss = "Eikthyr";

        /// <summary>Follows the switch; nothing to do before a world's scene exists (a server's value arriving at the menu).</summary>
        public static void Refresh(ZNetScene scene)
        {
            if (scene == null)
                return;
            GameObject item = scene.GetPrefab(Spells.Swiftness.Item);
            CharacterDrop drops = scene.GetPrefab(Boss)?.GetComponent<CharacterDrop>();
            if (item == null || drops == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: {Boss} drops no {Spells.Swiftness.Item} (boss or item missing)");
                return;
            }
            drops.m_drops.RemoveAll(d => d.m_prefab == item);
            if (!CastersSettings.On)
                return;
            drops.m_drops.Add(new CharacterDrop.Drop
            {
                m_prefab = item, m_amountMin = 1, m_amountMax = 1, m_chance = 1f, m_onePerPlayer = true,
                m_levelMultiplier = false, m_dontScale = true,
            });
        }
    }
}
