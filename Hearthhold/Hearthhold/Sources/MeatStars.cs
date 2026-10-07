using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Stars on meat (Husbandry), on the creature's owner. Character.OnDeath does the whole death there, and its drops
    /// take one of two paths:
    /// <list type="bullet">
    /// <item>a creature whose death ragdoll drops the loot (most animals): Ragdoll.Setup, inside OnDeath, stores the
    /// drop list (CharacterDrop.GenerateDropList) on the ragdoll's ZDO and turns the CharacterDrop's own drops off; a few
    /// seconds later the ragdoll's owner spawns the list in Ragdoll.SpawnLoot (CharacterDrop.DropItems: Instantiate). The
    /// killer is long forgotten by then, so a postfix on Setup writes the killer's Husbandry level to the ragdoll's ZDO
    /// (<see cref="SourceKeys.KillerHusbandry"/>) beside the creature level the game stores there, and SpawnLoot rolls
    /// in a scope from both;</item>
    /// <item>any other creature: CharacterDrop.OnDeath (one of OnDeath's death callbacks) generates and spawns the drops
    /// at once, inside a scope.</item>
    /// </list>
    /// The roll's bonus is the creature's stars (<see cref="StarOdds.CreatureBonus"/>), its level the killer's Husbandry:
    /// the attacker of the game's last hit, which RPC_Damage records on the owner for every hit on a living creature,
    /// when that is a player (GrindstoneSkills publishes its own skills to the player's ZDO, so a remote killer's level
    /// reads too). No player killer: level 0, the creature's stars still count. Only meat rolls (<see cref="SourceRoll"/>);
    /// GrindstoneSkills' Butcher Yield only changes the amounts in the list, so its extra meat rolls as well.
    /// </summary>
    public static class MeatStars
    {
        private const StarSource Source = StarSource.Meat;
        private static readonly int KillerHash = SourceKeys.KillerHusbandry.GetStableHashCode();

        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.OnDeath))]
        private static class Death
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(CharacterDrop __instance, out SourceRoll.Scope __state) =>
                __state = __instance.m_dropsEnabled
                    ? HookGuard.Run("meat stars", () => OpenScope(__instance.m_character), default(SourceRoll.Scope))
                    : default;

            [HarmonyFinalizer]
            private static void Finalizer(SourceRoll.Scope __state) => __state.Close();
        }

        [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.Setup))]
        private static class RagdollSetup
        {
            [HarmonyPostfix]
            private static void Postfix(Ragdoll __instance, CharacterDrop characterDrop)
            {
                if (characterDrop != null && __instance.m_dropItems)
                    HookGuard.Run("meat killer", static call => SaveKiller(call.ragdoll, call.drop), (ragdoll: __instance, drop: characterDrop));
            }
        }

        [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.SpawnLoot))]
        private static class RagdollLoot
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Ragdoll __instance, out SourceRoll.Scope __state) =>
                __state = HookGuard.Run("ragdoll meat stars", () => OpenScope(__instance), default(SourceRoll.Scope));

            [HarmonyFinalizer]
            private static void Finalizer(SourceRoll.Scope __state) => __state.Close();
        }

        private static SourceRoll.Scope OpenScope(Character creature)
        {
            ZNetView nview = creature != null ? creature.m_nview : null;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
                return default;
            return SourceRoll.Open(Source, KillerLevel(creature), StarOdds.CreatureBonus(creature.GetLevel()));
        }

        private static SourceRoll.Scope OpenScope(Ragdoll ragdoll)
        {
            ZNetView nview = ragdoll.m_nview;
            ZDO zdo = nview != null && nview.IsValid() && nview.IsOwner() ? nview.GetZDO() : null;
            if (zdo == null || zdo.GetInt(ZDOVars.s_drops) <= 0)
                return default;
            return SourceRoll.Open(Source, zdo.GetFloat(KillerHash), StarOdds.CreatureBonus(zdo.GetInt(ZDOVars.s_level, 1)));
        }

        /// <summary>The killer's Husbandry on the ragdoll's ZDO, while the ragdoll is fresh and owned here; nothing for 0.</summary>
        private static void SaveKiller(Ragdoll ragdoll, CharacterDrop drop)
        {
            ZNetView nview = ragdoll.m_nview;
            Character creature = drop.GetComponent<Character>();
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || creature == null)
                return;
            float level = KillerLevel(creature);
            if (level > 0f)
                nview.GetZDO().Set(KillerHash, level);
        }

        /// <summary>The Husbandry level of the player who landed the last hit on the creature; 0 for anyone else.</summary>
        private static float KillerLevel(Character creature)
        {
            Player killer = creature.m_lastHit != null ? creature.m_lastHit.GetAttacker() as Player : null;
            return killer != null ? StarOdds.Sane(GrindstoneLink.LevelOf(killer, StarSources.Skill(Source))) : 0f;
        }
    }
}
