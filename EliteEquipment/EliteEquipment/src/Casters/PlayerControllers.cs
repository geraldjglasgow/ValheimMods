using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Each player's own copy of the wand's swapped controller (<see cref="WandAnimations"/>), so the charged staff's cast
    /// slot can hold one player's spell motion (<see cref="SpellMotion"/>) while another player's casts and bolts play on:
    /// a copy made as the player wakes, on every machine that draws, and destroyed with them. Changing the slot keeps every
    /// layer where it was (the workshop's check found the animator keeps its states; a layer that has reset all the same is
    /// played back at its state and time).
    /// </summary>
    public static class PlayerControllers
    {
        public const string CastSlot = "Charge Staff Attack 2";
        private const int MostLayers = 8;

        private sealed class Own
        {
            public AnimatorOverrideController Controller;
            public AnimationClip Casts;
        }

        private static readonly Dictionary<Player, Own> copies = new Dictionary<Player, Own>();
        private static readonly List<KeyValuePair<AnimationClip, AnimationClip>> pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        private static readonly int[] states = new int[MostLayers];
        private static readonly float[] times = new float[MostLayers];

        /// <summary>This player's own copy of the swapped controller (the same game controller, the same swapped clips).</summary>
        public static AnimatorOverrideController Give(Player player, AnimatorOverrideController swapped)
        {
            var own = new AnimatorOverrideController(swapped.runtimeAnimatorController) { name = swapped.name };
            pairs.Clear();
            swapped.GetOverrides(pairs);
            own.ApplyOverrides(pairs);
            pairs.Clear();
            copies[player] = new Own { Controller = own, Casts = own[CastSlot] };
            return own;
        }

        /// <summary>The player's animator plays their own copy (not the game's, nor another mod's).</summary>
        public static bool Owns(Player player, RuntimeAnimatorController controller) =>
            controller != null && copies.TryGetValue(player, out Own own) && own.Controller == controller;

        /// <summary>A clip in the player's cast slot; false when their animator does not play their own copy.</summary>
        public static bool Put(Player player, AnimationClip clip)
        {
            Animator animator = player != null ? player.m_animator : null;
            if (clip == null || animator == null || !copies.TryGetValue(player, out Own own) || own.Controller != animator.runtimeAnimatorController)
                return false;
            if (own.Controller[CastSlot] == clip)
                return true;
            int layers = Save(animator);
            own.Controller[CastSlot] = clip;
            Restore(animator, layers);
            return true;
        }

        /// <summary>The wand's casts back in the player's cast slot, for the next bolt or combo move.</summary>
        public static void Reset(Player player)
        {
            if (player != null && copies.TryGetValue(player, out Own own))
                Put(player, own.Casts);
        }

        private static int Save(Animator animator)
        {
            int layers = Mathf.Min(animator.layerCount, MostLayers);
            for (int i = 0; i < layers; i++)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(i);
                states[i] = state.fullPathHash;
                times[i] = state.normalizedTime;
            }
            return layers;
        }

        private static void Restore(Animator animator, int layers)
        {
            for (int i = 0; i < layers; i++)
            {
                if (animator.GetCurrentAnimatorStateInfo(i).fullPathHash != states[i])
                    animator.Play(states[i], i, times[i]);
            }
        }

        [HarmonyPatch(typeof(Player), "OnDestroy")]
        private static class Gone
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (!copies.TryGetValue(__instance, out Own own))
                    return;
                copies.Remove(__instance);
                Object.Destroy(own.Controller);
            }
        }
    }
}
