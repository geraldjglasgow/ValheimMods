using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A giant crop stands Giant Crop Size times its normal size. The owner sizes the new crop when it ripens
    /// (<see cref="CropRipening"/>) with ZNetView.SetLocalScale, which the game sends to every client only for prefabs
    /// that sync their scale (m_syncInitialScale: most crops, not magecap). For the others every client sizes the crop
    /// itself when it loads (Pickable.Awake), from its ZDO and the prefab's scale.
    /// </summary>
    public static class CropLook
    {
        /// <summary>How many times its normal size a giant crop stands.</summary>
        public static float GiantSize => Mathf.Max(1f, FarmingSettings.GiantSize.Value);

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Awake))]
        private static class Loaded
        {
            [HarmonyPostfix]
            private static void Postfix(Pickable __instance)
            {
                ZNetView nview = __instance.m_nview;
                if (nview == null || !nview.IsValid() || nview.m_syncInitialScale || !CropKeys.Giant(nview))
                    return;
                if (GiantSize > 1f)
                    __instance.transform.localScale *= GiantSize;
            }
        }
    }
}
