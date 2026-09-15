using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The Devouring bite, resolved on the VICTIM's owner - where RPC_Damage runs and whose ZDO writes stick. A landed
    /// attack on a non-boss creature is an instant kill when the devourer is off its cooldown:
    /// <list type="bullet">
    /// <item>The <b>prefix</b> commits the kill before vanilla resolves the hit - it marks the prey with the devourer's id
    /// (so the prey's death path feeds that one devourer, see <see cref="DeathPatch"/>) and fires the sound-and-effect
    /// tell, once. Marking here, not in the postfix, means a well-fed devourer whose ordinary damage would have finished
    /// the prey outright still routes its death as a devour.</item>
    /// <item>The <b>postfix</b> forces the kill if the prey survived the hit's own damage, by draining it and running the
    /// death check - so it dies this bite with no health bar and no fight, whatever its health was.</item>
    /// </list>
    /// The postfix also handles the reverse role: a player striking the devourer <see cref="DevourBehaviour.Provoke"/>s
    /// it, turning it on that player. Players are never devoured; Devouring's zeroed knockback lives in DamageScalingPatch.
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class DevourHitPatch
    {
        private static void Prefix(Character __instance, HitData hit) =>
            Guard.Run("Character.RPC_Damage devour commit", () => Commit(__instance, hit));

        private static void Postfix(Character __instance, HitData hit) =>
            Guard.Run("Character.RPC_Damage devour finish", () => Finish(__instance, hit));

        // Prefix (prey's owner): decide whether this bite is a devour and, if so, commit it - mark the prey and tell.
        private static void Commit(Character prey, HitData hit)
        {
            ZNetView nview = prey.GetComponent<ZNetView>();
            if (hit == null || nview == null || !nview.IsValid() || !nview.IsOwner() || prey.IsDead())
            {
                return;
            }
            if (prey.IsPlayer() || prey.IsBoss())
            {
                return; // prey is other creatures only; a player is never devoured
            }
            Character? devourer = ReadyDevourer(hit);
            if (devourer != null)
            {
                Devour(prey, nview, devourer);
            }
        }

        // The attacker if it is a resolved Devouring creature that is off its cooldown; null otherwise.
        private static Character? ReadyDevourer(HitData hit)
        {
            Character attacker = hit.GetAttacker();
            EliteController? dc = attacker != null ? attacker.GetComponent<EliteController>() : null;
            if (attacker == null || dc == null || !dc.Ready || !dc.Traits.Has(Mutation.Devouring))
            {
                return null;
            }
            return OffCooldown(attacker) ? attacker : null;
        }

        private static bool OffCooldown(Character devourer)
        {
            ZDO zdo = devourer.GetComponent<ZNetView>().GetZDO();
            double now = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : 0.0;
            return zdo != null && (long)(now * 1000.0) >= TraitStore.GetDevourReadyAt(zdo);
        }

        private static void Devour(Character prey, ZNetView nview, Character devourer)
        {
            TraitStore.MarkDevouredBy(nview.GetZDO(), devourer.GetZDOID());
            // The tell rides the DEVOURER's own ZNetView, so every client that can see the fight draws and hears it at the
            // prey - a distant player watches the creature cease. Scoped by the engine to clients holding the devourer.
            CreatureRpc.FireFlash(devourer.GetComponent<ZNetView>(), prey.GetCenterPoint(),
                Mathf.Max(prey.GetRadius() * 2f, 2f), "devour");
            Log.Diag($"{devourer.name} devours {prey.name} in one bite");
        }

        // Postfix (prey's owner): force the marked prey's death if its own damage did not, then handle player provocation.
        private static void Finish(Character victim, HitData hit)
        {
            ZNetView nview = victim.GetComponent<ZNetView>();
            if (hit == null || nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }
            ForceKill(victim, nview, hit);
            Provoke(victim, hit);
        }

        private static void ForceKill(Character prey, ZNetView nview, HitData hit)
        {
            Character attacker = hit.GetAttacker();
            if (prey.IsDead() || attacker == null || TraitStore.GetDevouredBy(nview.GetZDO()) != attacker.GetZDOID())
            {
                return; // not the bite we just marked as a devour (or the hit's own damage already killed it)
            }
            prey.SetHealth(0f);
            Traverse.Create(prey).Method("CheckDeath").GetValue(); // routes through OnDeath with this hit as the last one
        }

        // A player hitting the devourer turns it on them; runs on the devourer's owner, where its AI is simulated.
        private static void Provoke(Character devourer, HitData hit)
        {
            Character attacker = hit.GetAttacker();
            if (attacker == null || !attacker.IsPlayer() || hit.GetTotalDamage() <= 0f)
            {
                return;
            }
            DevourBehaviour? behaviour = devourer.GetComponent<DevourBehaviour>();
            if (behaviour != null)
            {
                behaviour.Provoke(attacker);
            }
        }
    }
}
