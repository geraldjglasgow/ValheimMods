using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A cast lands: FishingFloat.Setup runs on the caster's client, which made the float and owns it. For the local
    /// player's cast the postfix stamps the angler's level on the float's ZDO (<see cref="Angler"/>), attaches the cast's
    /// state (<see cref="FloatFight"/>) and lengthens the line (<see cref="Tackle"/>).
    /// </summary>
    public static class FloatSetup
    {
        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Setup))]
        private static class Landed
        {
            [HarmonyPostfix]
            private static void Postfix(FishingFloat __instance, Character owner)
            {
                if (FishSkill.Active && owner != null && owner == Player.m_localPlayer && __instance.m_nview.IsValid())
                    HookGuard.Run("fishing float setup", () => Setup(__instance, (Player)owner));
            }
        }

        private static void Setup(FishingFloat fishingFloat, Player angler)
        {
            float level = FishSkill.Of(angler);
            Angler.Stamp(fishingFloat, level);
            FloatFight.Attach(fishingFloat, level);
            Tackle.OnLanded(fishingFloat, level);
        }
    }
}
