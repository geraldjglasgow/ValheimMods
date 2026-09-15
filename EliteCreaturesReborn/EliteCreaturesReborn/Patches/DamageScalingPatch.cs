using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Scales a hit as it lands, on the victim's owner - the single choke point every damage source passes through.
    /// The attacker's traits raise or lower what it deals (star attack, Splintering, Plated's rising bite, Devouring's
    /// eaten damage), summed additively; the victim's Plated armour softens what comes in, using the game's own armour
    /// curve. Two more attacker-mutation hit tweaks live here because this is the prefix that owns the outgoing hit:
    /// Miasmic adds vanilla Poison when it hits a player, and Devouring zeroes knockback so its prey never tumbles away.
    /// Hits with no attacker - poison clouds, explosions, reflected damage - carry no traits and pass through.
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class DamageScalingPatch
    {
        private static void Prefix(Character __instance, HitData hit) =>
            Guard.Run("Character.RPC_Damage scaling", () => Scale(__instance, hit));

        private static void Scale(Character victim, HitData hit)
        {
            ZNetView nview = victim.GetComponent<ZNetView>();
            if (hit == null || nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }
            ApplyOutgoing(hit);
            ApplyIncoming(victim, hit);
            ApplyAttackerMutations(victim, hit);
        }

        // Attacker-mutation tweaks to the outgoing hit, applied after scaling so they are not multiplied by it.
        private static void ApplyAttackerMutations(Character victim, HitData hit)
        {
            Character attacker = hit.GetAttacker();
            EliteController? controller = attacker != null ? attacker.GetComponent<EliteController>() : null;
            if (controller == null || !controller.Ready)
            {
                return;
            }
            if (controller.Traits.Has(Mutation.Devouring))
            {
                hit.m_pushForce = 0f; // NEVER knocks anything back, players included: prey must stay where it was hit
            }
            if (controller.Traits.Has(Mutation.Miasmic) && victim.IsPlayer())
            {
                // Its attacks poison players with the game's own Poison, strength = cloud damage. RPC_Damage converts a
                // hit's poison portion into the SE_Poison itself, exactly as a blob does; creatures/tames are never hit.
                hit.m_damage.m_poison +=
                    Enhance.Magnitude(controller.Rules, controller.Traits, Mutation.Miasmic, Fields.CloudDamage);
            }
        }

        private static void ApplyOutgoing(HitData hit)
        {
            Character attacker = hit.GetAttacker();
            if (attacker == null || attacker.IsPlayer())
            {
                return;
            }
            EliteController controller = attacker.GetComponent<EliteController>();
            if (controller == null || !controller.Ready)
            {
                return;
            }
            CreatureTraits traits = controller.Traits;
            hit.ApplyModifier(DamageMath.OutgoingMultiplier(controller.Rules, traits, attacker.GetHealthPercentage()));
            hit.m_damage.m_blunt += DamageMath.DevouredFlatDamage(traits, controller.View.GetZDO());
        }

        private static void ApplyIncoming(Character victim, HitData hit)
        {
            EliteController controller = victim.GetComponent<EliteController>();
            if (controller == null || !controller.Ready)
            {
                return;
            }
            float armour = DamageMath.PlatedArmour(controller.Rules, controller.Traits, victim.GetHealthPercentage());
            if (armour > 0f)
            {
                hit.ApplyArmor(armour);
            }
        }
    }
}
