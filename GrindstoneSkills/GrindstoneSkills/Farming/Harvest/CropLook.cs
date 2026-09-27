using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A ripe crop's size: starred crops stand 6% taller per star, giants Giant Crop Size times. The owner sizes the new
    /// crop when it ripens (<see cref="CropRipening"/>) with ZNetView.SetLocalScale, which the game sends to every client
    /// only for prefabs that sync their scale (m_syncInitialScale: most crops, not magecap). For the others every client
    /// sizes the crop itself when it loads (Pickable.Awake), from its ZDO keys and the prefab's scale.
    /// </summary>
    public static class CropLook
    {
        private const float SizePerStar = 0.06f;

        /// <summary>How many times its normal size a crop with these stars stands.</summary>
        public static float Size(int stars, bool giant) =>
            giant ? Mathf.Max(1f, FarmingSettings.GiantSize.Value) : 1f + SizePerStar * Mathf.Clamp(stars, 0, Stars.Max);

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Awake))]
        private static class Loaded
        {
            [HarmonyPostfix]
            private static void Postfix(Pickable __instance)
            {
                ZNetView nview = __instance.m_nview;
                if (nview == null || !nview.IsValid() || nview.m_syncInitialScale)
                    return;
                float size = Size(CropKeys.Stars(nview), CropKeys.Giant(nview));
                if (size > 1f)
                    __instance.transform.localScale *= size;
            }
        }
    }
}
