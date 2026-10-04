using System.Collections.Generic;
using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Loot
{
    /// <summary>
    /// Tethered: only the last of the pair to fall pays. The first to die drops nothing from its own table, trophies
    /// included - its partner, still standing, drops the whole reward when it falls. "Standing" is read from the
    /// partner's ZDO on the dying boss's owner, where the drop list is built: present, with health left (no health key
    /// means full). A partner whose ZDO is already gone has fallen, so this one pays. Rows another mod put in the list
    /// are left alone, as everywhere else in the loot engine.
    /// </summary>
    internal static class TetherLoot
    {
        /// <summary>True when the drop list was emptied of this boss's own rows because its partner still stands.</summary>
        public static bool Withhold(CharacterDrop drop, EliteController controller, List<KeyValuePair<GameObject, int>> result)
        {
            if (!controller.Traits.HasAspect(Aspect.Tethered) || !PartnerStanding(controller))
            {
                return false;
            }
            HashSet<GameObject> own = new HashSet<GameObject>();
            foreach (CharacterDrop.Drop row in drop.m_drops)
            {
                if (row.m_prefab != null)
                {
                    own.Add(row.m_prefab);
                }
            }
            result.RemoveAll(pair => own.Contains(pair.Key));
            return true;
        }

        private static bool PartnerStanding(EliteController controller)
        {
            ZDO? zdo = controller.View != null && controller.View.IsValid() ? controller.View.GetZDO() : null;
            return zdo != null && TetherPair.Standing(AspectStore.GetTether(zdo));
        }
    }
}
