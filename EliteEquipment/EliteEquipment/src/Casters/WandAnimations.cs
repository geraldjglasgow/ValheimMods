using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The wand's own charge and throw (user, 2026-10-11: "when charging the character should make a motion to put wand kinda
    /// by its side a little lower than face, then when casting or releasing charge, he should pull wand back behind head
    /// then straight forward toward where aiming"). The bolt plays the game's charged-staff states on the player's upper
    /// body (<see cref="WindUp"/>: the bool <c>staff_charging</c>, the trigger <c>staff_charge_attack</c>, which the game
    /// shares with every machine), states only creatures use. Every player's animator, on every machine that draws, gets
    /// a copy of the game's controller with those three clips swapped for the workshop's (<c>ee_casteranim</c>): a short
    /// raise to the charge pose behind the caster, a living hold, and the casts (the combo's three moves and the charged
    /// release as segments, <see cref="WandMoves"/>), each with a <c>Hit</c> that fires the bolt as the game's clip did.
    /// Without the bundle the game's clips stay. The game's controller has no way from the raise into the hold loop, and a
    /// charge broken off without the throw stays in the hold, so every frame, on every machine that draws (the bool is
    /// shared), the upper-body layer is moved on (<see cref="Tick"/>; the workshop's ee_casteranim README, checked there).
    /// </summary>
    [HarmonyPatch(typeof(Player), "Awake")]
    public static class WandAnimations
    {
        private const string Bundle = "ee_casteranim";
        private const float LeaveHold = 0.25f;
        private const string CastTrigger = "staff_charge_attack";
        private const string Casts = "ee_anim_wand_casts";
        private const string ChainEvent = "Chain";
        // The bool going off and the release's move reach other machines separately (the ZDO and an RPC): the hold is left
        // only once the bool has been off this long with no move taking it.
        private const float LetGoGrace = 0.2f;

        private static readonly int Charging = Animator.StringToHash("staff_charging");
        private static readonly int Raise = Animator.StringToHash("staff_charge_start");
        private static readonly int Hold = Animator.StringToHash("staff_charge_loop");
        private static readonly int None = Animator.StringToHash("None");
        private static readonly int AttackTag = Animator.StringToHash("attack");
        private static readonly Dictionary<Animator, float> offSince = new Dictionary<Animator, float>();

        private static readonly Dictionary<string, string> Swaps = new Dictionary<string, string>
        {
            { "Charge Staff Start Charge", "ee_anim_wand_raise" },
            { "Charge Staff Loop", "ee_anim_wand_hold" },
            { "Charge Staff Attack 2", Casts },
        };

        private static readonly Dictionary<RuntimeAnimatorController, AnimatorOverrideController> made =
            new Dictionary<RuntimeAnimatorController, AnimatorOverrideController>();

        [HarmonyPostfix]
        private static void Postfix(Player __instance) => CasterSafe.Run("wand animations", () => Apply(__instance));

        /// <summary>
        /// The wand's attack names the cast state's trigger (the game needs a name to call it an attack), but a player's casts
        /// are cross-faded into their segments by <see cref="WandMoves"/>: the trigger would restart the clip from its first
        /// move, and one left set would fire on the next charge. On the caster's machine, which sends it to all, it is held back.
        /// </summary>
        /// <summary>
        /// The charged staff's hold is tagged as an attack in the game's controller, so while a caster holds a charge the game
        /// would think them mid-attack and refuse the release's cast until the hold had faded out (and its press, queued
        /// half a second, could expire first). On the caster's machine, which starts the cast, the raise and the hold are not
        /// an attack: the release's cast starts at once, from the hold, as a chained swing would.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.InAttack))]
        private static class HoldIsNoAttack
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, ref bool __result)
            {
                if (__result && __instance == Player.m_localPlayer && InHold(__instance.m_animator))
                    __result = false;
            }
        }

        /// <summary>The upper body in the raise or the hold (not leaving it for a cast), the base layer in no attack.</summary>
        private static bool InHold(Animator animator)
        {
            if (animator == null || animator.layerCount < 2 || animator.GetCurrentAnimatorStateInfo(0).tagHash == AttackTag)
                return false;
            AnimatorStateInfo state = animator.IsInTransition(1) ? animator.GetNextAnimatorStateInfo(1) : animator.GetCurrentAnimatorStateInfo(1);
            return state.shortNameHash == Hold || state.shortNameHash == Raise;
        }

        [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetTrigger))]
        private static class NoTrigger
        {
            [HarmonyPrefix]
            private static bool Prefix(ZSyncAnimation __instance, string name) =>
                name == null || !name.StartsWith(CastTrigger) || __instance.GetComponent<Player>() == null;
        }

        private static void Apply(Player player)
        {
            Animator animator = player.m_animator;
            if (animator == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            AnimatorOverrideController ours = For(animator.runtimeAnimatorController);
            if (ours != null)
                animator.runtimeAnimatorController = ours;
        }

        /// <summary>Every player's upper body moved on from the raise into the hold, and out of a hold let go without a throw.</summary>
        public static void Tick()
        {
            foreach (Player player in Player.GetAllPlayers())
            {
                Animator animator = player.m_animator;
                if (animator != null && animator.runtimeAnimatorController is AnimatorOverrideController ours && made.ContainsValue(ours))
                    Step(animator, animator.GetLayerIndex("upperbody"));
            }
        }

        private static void Step(Animator animator, int layer)
        {
            if (layer < 0 || animator.IsInTransition(layer))
                return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
            bool charging = animator.GetBool(Charging);
            if (charging && state.shortNameHash == Raise && state.normalizedTime >= 1f)
                animator.CrossFadeInFixedTime(Hold, 0f, layer);
            if (charging || state.shortNameHash != Hold)
            {
                offSince.Remove(animator);
                return;
            }
            if (!offSince.TryGetValue(animator, out float since))
                offSince[animator] = Time.time;
            else if (Time.time - since >= LetGoGrace)
                LetGo(animator, layer);
        }

        private static void LetGo(Animator animator, int layer)
        {
            offSince.Remove(animator);
            animator.CrossFadeInFixedTime(None, LeaveHold, layer);
        }

        /// <summary>The swapped copy of a controller, made once (another mod's copy is wrapped too); null for our own.</summary>
        private static AnimatorOverrideController For(RuntimeAnimatorController game)
        {
            if (game == null || (game is AnimatorOverrideController copy && made.ContainsValue(copy)))
                return null;
            if (made.TryGetValue(game, out AnimatorOverrideController ours))
                return ours;
            AssetBundle bundle = CasterBundles.Get(Bundle);
            ours = bundle != null ? Swapped(game, bundle) : null;
            made[game] = ours;
            return ours;
        }

        private static AnimatorOverrideController Swapped(RuntimeAnimatorController game, AssetBundle bundle)
        {
            var ours = new AnimatorOverrideController(game) { name = game.name + "_ee_wand" };
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(ours.overridesCount);
            ours.GetOverrides(pairs);
            int swapped = 0;
            for (int i = 0; i < pairs.Count; i++)
            {
                AnimationClip clip = Swaps.TryGetValue(pairs[i].Key.name, out string name) ? bundle.LoadAsset<AnimationClip>(name) : null;
                if (clip == null)
                    continue;
                if (name == Casts)
                    AddChainPoints(clip);
                pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, clip);
                swapped++;
            }
            ours.ApplyOverrides(pairs);
            Plugin.Log.LogInfo($"EliteEquipment: wand animations, {swapped} of {Swaps.Count} clips swapped");
            return ours;
        }

        /// <summary>The game's <c>Chain</c> event (a sword's "the next swing may start") at each move's chain point, once.</summary>
        private static void AddChainPoints(AnimationClip casts)
        {
            foreach (AnimationEvent existing in casts.events)
            {
                if (existing.functionName == ChainEvent)
                    return;
            }
            foreach (float time in WandMoves.ChainPoints)
                casts.AddEvent(new AnimationEvent { functionName = ChainEvent, time = time });
        }
    }
}
