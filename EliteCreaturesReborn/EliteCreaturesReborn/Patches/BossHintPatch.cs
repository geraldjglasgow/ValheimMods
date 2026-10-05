using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Hints;
using EliteCreaturesReborn.Traits;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// A boss hint, asked for on the boss's owner as it dies, while its ZDO still says what it was: never for a Phantom
    /// copy, a decoy, and for a Tethered pair only when the last of the two falls, as the damage board. Both of a Twin
    /// pair ask; the server answers once. Never throws: nothing here may stop a boss dying.
    /// </summary>
    [HarmonyPatch(typeof(Character), "OnDeath")]
    public static class BossHintDeathPatch
    {
        private static void Prefix(Character __instance) =>
            SafeCall.Run("Character.OnDeath boss hint", () => Ask(__instance));

        private static void Ask(Character victim)
        {
            ZNetView nview = victim.m_nview;
            if (!victim.IsBoss() || nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }
            ZDO zdo = nview.GetZDO();
            if (AspectStore.GetPhantomOf(zdo) == ZDOID.None && !TetherPair.Standing(AspectStore.GetTether(zdo)))
            {
                BossHintRpc.Request(Utils.GetPrefabName(victim.gameObject), victim.transform.position);
            }
        }
    }

    /// <summary>World start, on every machine: the hint's question and answer are registered before any boss can fall.</summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    public static class BossHintStartPatch
    {
        private static void Postfix() => SafeCall.Run("ZoneSystem.Start boss hints", BossHintRpc.EnsureRegistered);
    }
}
