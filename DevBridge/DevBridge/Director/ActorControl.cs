using System.Collections;
using System.Linq;
using DevBridge.Server;
using HarmonyLib;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// What the director does with a creature: hold it (its AI skipped: it stands, turns to a spot or walks to one),
    /// release it on a target, or make it use one of its attacks now.
    /// </summary>
    internal static class ActorControl
    {
        /// <summary>Replaces one AI update for a held actor on its owner; false leaves the AI to run as usual.</summary>
        internal static bool Drive(BaseAI ai, float dt)
        {
            Actor actor = Cast.For(ai);
            if (actor == null || !actor.Held || !ai.m_nview.IsValid() || !ai.m_nview.IsOwner()) return false;
            if (actor.MoveTo != null)
            {
                if (Walk(ai, actor, dt)) actor.MoveTo = null;
            }
            else ai.StopMoving();
            if (actor.MoveTo == null && actor.Face != null) ai.LookAt(actor.Face.Resolve(Cast.Frame));
            return true;
        }

        /// <summary>One step toward MoveTo; true once there (then it stands).</summary>
        private static bool Walk(BaseAI ai, Actor actor, float dt)
        {
            Vector3 to = actor.MoveTo.Resolve(Cast.Frame);
            if (!actor.Straight) return ai.MoveTo(dt, to, actor.Arrive, actor.Run);
            Vector3 flat = Vector3.ProjectOnPlane(to - ai.transform.position, Vector3.up);
            if (flat.magnitude < actor.Arrive) { ai.StopMoving(); return true; }
            ai.MoveTowards(flat.normalized, actor.Run);
            return false;
        }

        /// <summary>Lets the AI run again, hunting the target when one is given.</summary>
        internal static void Release(Actor actor, Character target)
        {
            actor.Held = false;
            actor.MoveTo = null;
            if (!(actor.Ai is MonsterAI monster) || !target) return;
            monster.m_sleeping = false;
            monster.m_targetCreature = target;
            monster.m_timeSinceSensedTargetCreature = 0f;
            monster.m_lastKnownTargetPos = target.transform.position;
            monster.SetAlerted(true);
        }

        /// <summary>Equips the attack whose prefab or name contains item (or keeps the current one) and starts it on the target.</summary>
        internal static bool Attack(Actor actor, string item, Character target)
        {
            Humanoid humanoid = actor.Character as Humanoid ?? throw new BridgeException($"actor {actor.Name} cannot attack");
            if (!string.IsNullOrEmpty(item))
            {
                ItemDrop.ItemData weapon = humanoid.GetInventory().GetAllItems().FirstOrDefault(i => Matches(i, item))
                    ?? throw new BridgeException($"actor {actor.Name} has no attack {item}; it has {string.Join(", ", humanoid.GetInventory().GetAllItems().Select(Name))}");
                humanoid.EquipItem(weapon, false);
            }
            if (target && actor.Ai) actor.Ai.LookAt(target.transform.position);
            if (target && actor.Ai is MonsterAI monster) (monster.m_targetCreature, monster.m_sleeping) = (target, false);
            return humanoid.StartAttack(target, false);
        }

        /// <summary>
        /// The attack as soon as the creature can take it: a creature still waking, staggered or finishing another move
        /// refuses one, so it is tried each frame for up to two seconds of film; skip (if not negative) then starts its
        /// animation that far in.
        /// </summary>
        internal static IEnumerator AttackSoon(Actor actor, string item, Character target, float skip)
        {
            for (float waited = 0f; waited < 2f; waited += FilmClock.Delta)
            {
                if (actor.Go && Attack(actor, item, target))
                {
                    if (skip >= 0f) Skip(actor, skip);
                    yield break;
                }
                yield return null;
            }
            ShotRunner.Report($"{actor.Name} would not start its {item} attack within two seconds");
        }

        /// <summary>
        /// Starts the attack's animation `seconds` in (a wind-up skipped): two frames after the attack begins, the state
        /// it is entering is played again from that point.
        /// </summary>
        internal static void Skip(Actor actor, float seconds) => DevBridgePlugin.Instance.StartCoroutine(SkipAhead(actor, seconds));

        private static IEnumerator SkipAhead(Actor actor, float seconds)
        {
            yield return null;
            yield return null;
            Animator animator = actor.Character ? actor.Character.m_animator : null;
            if (!animator) yield break;
            AnimatorStateInfo state = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.fullPathHash, 0, Mathf.Clamp01(seconds / Mathf.Max(0.01f, state.length)));
        }

        private static bool Matches(ItemDrop.ItemData item, string text) =>
            Name(item).IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0;

        private static string Name(ItemDrop.ItemData item) => item.m_dropPrefab ? item.m_dropPrefab.name : item.m_shared.m_name;
    }

    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterHoldPatch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(MonsterAI __instance, float dt, ref bool __result) => !(__result = ActorControl.Drive(__instance, dt));
    }

    [HarmonyPatch(typeof(AnimalAI), nameof(AnimalAI.UpdateAI))]
    internal static class AnimalHoldPatch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(AnimalAI __instance, float dt, ref bool __result) => !(__result = ActorControl.Drive(__instance, dt));
    }
}
