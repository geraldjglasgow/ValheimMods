using HarmonyLib;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The bolt's burst where it lands: the game runs a projectile's hits on its owner's machine (<c>Projectile.OnHit</c>
    /// from its owner's <c>FixedUpdate</c>, read 2026-10-09), which sends the burst to the players near
    /// (<see cref="CasterFx.BoltHit"/>). Every other projectile is left alone after a name test.
    /// </summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
    public static class BoltHit
    {
        [HarmonyPostfix]
        private static void Postfix(Projectile __instance, UnityEngine.Vector3 hitPoint)
        {
            if (__instance.name.StartsWith(WandBolt.Prefab) && __instance.m_nview != null && __instance.m_nview.IsOwner())
                CasterSafe.Run("bolt hit", () => CasterFx.Send(CasterFx.BoltHit, hitPoint, ZDOID.None, null));
        }
    }
}
