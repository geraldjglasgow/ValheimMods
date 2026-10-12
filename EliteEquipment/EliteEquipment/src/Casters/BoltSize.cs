using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A charged bolt is bigger (user, 2026-10-11: "the actual spell thrown should be larger/smaller depending on how long
    /// you hold"): on the caster's machine, as the game sets up the bolt it fired, from <see cref="Smallest"/> times its size
    /// at a tap to <see cref="Largest"/> at full charge (<see cref="WindUp.Thrown"/>), its hit width with it. The game
    /// shares the size with every machine (<c>ZNetView.SetLocalScale</c>, the bolt syncing its initial scale).
    /// </summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    public static class BoltSize
    {
        private const float Smallest = 0.8f;
        private const float Largest = 2f;

        [HarmonyPostfix]
        private static void Postfix(Projectile __instance, Character owner, ItemDrop.ItemData item)
        {
            if (owner == null || owner != Player.m_localPlayer || !CasterGearWorn.IsWand(item) || __instance.m_nview == null)
                return;
            float scale = Mathf.Lerp(Smallest, Largest, WindUp.Thrown);
            __instance.m_nview.SetLocalScale(__instance.transform.localScale * scale);
            __instance.m_rayRadius *= scale;
        }
    }
}
