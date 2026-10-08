using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// Everybody sees the worn pair. The character's owner names it in its ZDO (<c>OpenKeep.boots</c>, the boots prefab's
    /// hash, 0 for none) whenever the game sets up its equipment visuals, written only when it changes; a death ragdoll
    /// gets its own copy the same way. Every client's visuals update (each frame, as the game reads its own equipment
    /// keys) attaches the boots' skin through the game's own armour attach when the hash changes, and takes it off when
    /// it goes, and lays the body paint for it (<see cref="BodyPaint"/>). Without a ZDO (the main menu's character) the
    /// hash is kept on the character itself. Armour stands are left alone.
    /// </summary>
    public static class BootsShow
    {
        public static readonly int Key = "OpenKeep.boots".GetStableHashCode();

        private static readonly ConditionalWeakTable<VisEquipment, Shown> shown = new ConditionalWeakTable<VisEquipment, Shown>();

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
        private static class Setup
        {
            [HarmonyPostfix]
            private static void Postfix(Humanoid __instance, VisEquipment visEq)
            {
                if (visEq == null || !__instance.IsPlayer())
                    return;
                ItemDrop.ItemData pair = WornBoots.Of(__instance);
                BootSet set = BootSets.Of(pair);
                int hash = set != null ? set.Hash : 0;
                ZDO zdo = visEq.m_nview != null ? visEq.m_nview.GetZDO() : null;
                if (zdo == null)
                    shown.GetOrCreateValue(visEq).Local = hash;
                else if (visEq.m_nview.IsOwner() && zdo.GetInt(Key) != hash)
                    zdo.Set(Key, hash);
            }
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
        private static class Draw
        {
            [HarmonyPostfix]
            private static void Postfix(VisEquipment __instance)
            {
                if (!__instance.m_isPlayer || __instance.m_isArmorStand || __instance.m_bodyModel == null)
                    return;
                Shown state = shown.GetOrCreateValue(__instance);
                ZDO zdo = __instance.m_nview != null ? __instance.m_nview.GetZDO() : null;
                int hash = zdo != null ? zdo.GetInt(Key) : state.Local;
                if (hash != state.Hash)
                    state.Swap(__instance, hash);
                BodyPaint.Update(__instance, hash != 0 ? BootSets.ByHash(hash) : null);
            }
        }

        private sealed class Shown
        {
            private List<GameObject> pieces;

            /// <summary>What the menu's character wears (no ZDO to carry it).</summary>
            public int Local { get; set; }

            /// <summary>What is attached now.</summary>
            public int Hash { get; private set; }

            public void Swap(VisEquipment vis, int hash)
            {
                if (pieces != null)
                {
                    foreach (GameObject piece in pieces)
                        Object.Destroy(piece);
                    pieces = null;
                }
                Hash = hash;
                if (hash != 0 && ObjectDB.instance != null && ObjectDB.instance.GetItemPrefab(hash) != null)
                    pieces = vis.AttachArmor(hash);
                vis.UpdateLodgroup();
            }
        }
    }
}
