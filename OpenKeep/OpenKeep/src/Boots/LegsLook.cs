using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using Object = UnityEngine.Object;

namespace OpenKeep.Boots
{
    /// <summary>
    /// The split leggings' look while Separate Boots is on: the game attaches its own leggings skin as always, and each of
    /// its skinned meshes then draws only the trousers part (<see cref="SkinSplit"/>, the workshop's cut of the game's own
    /// triangles), so the footwear is left to the boots; the body paint follows in <see cref="BodyPaint"/>. Players only:
    /// armour stands and other characters keep the whole leggings. Every client draws it the same, as the switch is
    /// synced; uncut leggings (no mesh, or a mesh the split does not fit) keep the game's look.
    /// </summary>
    public static class LegsLook
    {
        /// <summary>Keeps the set's trousers parts; the leggings wear them while the switch is on.</summary>
        public static void Add(BootSet set, Dictionary<Mesh, Mesh[]> cuts)
        {
            foreach (KeyValuePair<Mesh, Mesh[]> cut in cuts)
                set.Trousers[cut.Key] = cut.Value[0];
        }

        /// <summary>The switch changed: every character's leggings are attached again, the split ones or the game's.</summary>
        public static void Apply()
        {
            foreach (VisEquipment vis in Object.FindObjectsByType<VisEquipment>(FindObjectsSortMode.None))
            {
                // The next visuals update sees a change and attaches the leggings again (their old pieces destroyed,
                // the body paint set afresh).
                if (vis.m_currentLegItemHash != 0)
                    vis.m_currentLegItemHash = 0;
            }
        }

        private static void Wear(GameObject skin, Dictionary<Mesh, Mesh> trousers)
        {
            foreach (SkinnedMeshRenderer part in skin.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (part.sharedMesh != null && trousers.TryGetValue(part.sharedMesh, out Mesh cut))
                    part.sharedMesh = cut;
            }
        }

        /// <summary>After the game attached a split leggings' skin to a player: its meshes swapped for their trousers parts.</summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachArmor))]
        private static class AttachPatch
        {
            [HarmonyPostfix]
            private static void Postfix(VisEquipment __instance, int itemHash, List<GameObject> __result)
            {
                if (__result == null || !BootsSettings.On || !__instance.m_isPlayer || __instance.m_isArmorStand)
                    return;
                BootSet set = BootSets.ByLegsHash(itemHash);
                if (set == null || set.Trousers.Count == 0)
                    return;
                foreach (GameObject piece in __result)
                {
                    if (piece != null && piece.name.StartsWith(SkinSplit.Skin, StringComparison.Ordinal))
                        Wear(piece, set.Trousers);
                }
            }
        }
    }
}
