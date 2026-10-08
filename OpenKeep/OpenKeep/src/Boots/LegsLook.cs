using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// The split leggings' look while boots are on: the game attaches the workshop's trousers (to the ankle, no boots) in
    /// place of the leggings' own skin. The game's body paint stays, as for any leggings (the trousers' lower legs sit
    /// partly inside the body and the paint under them keeps them whole; switched off, most looked like shorts in the
    /// workshop's renders), except on the feet (<see cref="LegsPaint"/>). The trousers' skin waits on the bench
    /// (<see cref="BootsSkin"/>): the game's prefabs keep their hierarchy, and <c>VisEquipment.AttachArmor</c> is answered
    /// for the 20 leggings' hashes only. Every client draws it the same, as the switch is synced. Without the bundle the
    /// leggings keep their own look.
    /// </summary>
    public static class LegsLook
    {
        private static readonly Dictionary<int, BootSet> byHash = new Dictionary<int, BootSet>();

        /// <summary>Makes the set's trousers skin on the bench; the leggings then wear it while boots are on.</summary>
        public static void Add(BootSet set, GameObject legs, SetLook look)
        {
            var holder = new GameObject("OpenKeep_Pants_" + set.Key);
            holder.transform.SetParent(PrefabBench.Root, false);
            set.PantsSkin = BootsSkin.Make(holder, BootsBundle.Pants(set), look);
            if (set.PantsSkin != null)
                byHash[legs.name.GetStableHashCode()] = set;
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

        /// <summary>The set whose split trousers to wear for a leggings hash while boots are on, or null for the game's own.</summary>
        public static BootSet Split(int itemHash) =>
            BootsSettings.On && byHash.TryGetValue(itemHash, out BootSet set) ? set : null;

        /// <summary>The game's skinned branch of AttachArmor, for the trousers' skin: under the body, bound to its bones.</summary>
        public static List<GameObject> Attach(VisEquipment vis, GameObject skin)
        {
            SkinnedMeshRenderer body = vis.m_bodyModel;
            Transform visual = body.transform.parent;
            GameObject worn = Object.Instantiate(skin, body.transform.position, visual.rotation, visual);
            worn.SetActive(true);
            foreach (SkinnedMeshRenderer part in worn.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                part.rootBone = body.rootBone;
                part.bones = body.bones;
            }
            VisEquipment.CleanupInstance(worn);
            vis.RefreshSnowLevel();
            return new List<GameObject> { worn };
        }

        /// <summary>The game set the leggings' body paint just before (SetLegEquipped); the feet are cleared after it.</summary>
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachArmor))]
        private static class AttachPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(VisEquipment __instance, int itemHash, ref List<GameObject> __result)
            {
                BootSet set = Split(itemHash);
                if (set == null || __instance.m_bodyModel == null)
                    return true;
                __result = Attach(__instance, set.PantsSkin);
                LegsPaint.Apply(__instance, set);
                return false;
            }
        }
    }
}
