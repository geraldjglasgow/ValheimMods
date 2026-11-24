using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Each boss drops its own spell's page (<see cref="Spell.Boss"/>; Eikthyr: Stag's Swiftness, the Elder: Elder's Volley), one per player present
    /// (the game's own <c>m_onePerPlayer</c> drop), while the switch is on: added to the boss's prefab once the scene has
    /// woken and whenever the switch turns on, taken off when it turns off, on every peer (the boss's owner decides the
    /// drop). Not scaled by level. A boss already summoned keeps the drops it was made with; bosses whose spells are not
    /// built yet drop none.
    /// </summary>
    public static class BossPages
    {
        /// <summary>Follows the switch; nothing to do before a world's scene exists (a server's value arriving at the menu).</summary>
        public static void Refresh(ZNetScene scene)
        {
            if (scene == null)
                return;
            foreach (Spell spell in Spells.All)
            {
                if (spell.Boss != null)
                    Refresh(scene, spell);
            }
        }

        private static void Refresh(ZNetScene scene, Spell spell)
        {
            GameObject item = scene.GetPrefab(spell.Item);
            CharacterDrop drops = scene.GetPrefab(spell.Boss)?.GetComponent<CharacterDrop>();
            if (item == null || drops == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: {spell.Boss} drops no {spell.Item} (boss or item missing)");
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
