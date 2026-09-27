using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A cast lands: FishingFloat.Setup runs on the caster's client, which made the float and owns it. For the local
    /// player's cast the postfix stamps the angler's level and the bait's stars on the float's ZDO (<see cref="Angler"/>),
    /// attaches the cast's state (<see cref="FloatFight"/>) and lengthens the line (<see cref="Tackle"/>). The bait's
    /// stars come from the ammo item the cast used up (Attack's last used ammo, passed through the thrown projectile):
    /// baits are crafted at the prep table, a kitchen, so they roll Cooking stars like dishes.
    /// </summary>
    public static class FloatSetup
    {
        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Setup))]
        private static class Landed
        {
            [HarmonyPostfix]
            private static void Postfix(FishingFloat __instance, Character owner, ItemDrop.ItemData ammo)
            {
                if (FishSkill.Active && owner != null && owner == Player.m_localPlayer && __instance.m_nview.IsValid())
                    HookGuard.Run("fishing float setup", () => Setup(__instance, (Player)owner, ammo));
            }
        }

        private static void Setup(FishingFloat fishingFloat, Player angler, ItemDrop.ItemData ammo)
        {
            float level = FishSkill.Of(angler);
            int baitStars = Stars.Get(ammo);
            Angler.Stamp(fishingFloat, level, baitStars);
            FloatFight.Attach(fishingFloat, level, baitStars);
            Tackle.OnLanded(fishingFloat, level);
        }
    }
}
