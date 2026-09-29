using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>
    /// A placed object's Animator driven by hand: play a state, fire triggers, set parameters, change speed, and swap
    /// the controller's clips for a bundle's (an AnimatorOverrideController, as the mods swap in the workshop's clips).
    /// </summary>
    internal static class Poses
    {
        internal static Animator Of(Placement placement) =>
            placement.Root.GetComponentsInChildren<Animator>(true).FirstOrDefault(a => a.runtimeAnimatorController)
            ?? throw new BridgeException($"{placement.Name} (id {placement.Id}) has no animator with a controller");

        internal static Dictionary<string, object> Describe(Animator animator) => new Dictionary<string, object>
        {
            ["controller"] = animator.runtimeAnimatorController.name,
            ["speed"] = Fmt.R(animator.speed),
            ["layers"] = Enumerable.Range(0, animator.layerCount).Select(l => Layer(animator, l)).ToList(),
            ["parameters"] = animator.parameters.Select(p => $"{p.name} ({p.type}) = {Value(animator, p)}").ToList(),
            ["clips"] = animator.runtimeAnimatorController.animationClips.Where(c => c).Select(c => c.name).Distinct().OrderBy(n => n).ToList(),
        };

        private static string Layer(Animator animator, int layer)
        {
            string clips = string.Join(" + ", animator.GetCurrentAnimatorClipInfo(layer).Select(c => $"{c.clip.name} {c.weight:0.##}"));
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
            return $"{layer} {animator.GetLayerName(layer)} (weight {animator.GetLayerWeight(layer):0.##}): {(clips.Length > 0 ? clips : "no clip")} at {state.normalizedTime:0.##}";
        }

        private static string Value(Animator animator, AnimatorControllerParameter parameter)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float: return animator.GetFloat(parameter.nameHash).ToString("0.###", CultureInfo.InvariantCulture);
                case AnimatorControllerParameterType.Int: return animator.GetInteger(parameter.nameHash).ToString(CultureInfo.InvariantCulture);
                default: return animator.GetBool(parameter.nameHash) ? "true" : "false";
            }
        }

        /// <summary>Plays a state by name ("attack", or "Base Layer.attack") on the layer that has it: from `at` (0 to 1), or faded in over `fade` seconds.</summary>
        internal static void Play(Animator animator, string state, int layer, float fade, float at)
        {
            int hash = Animator.StringToHash(state);
            if (layer < 0) layer = Enumerable.Range(0, animator.layerCount).FirstOrDefault(l => animator.HasState(l, hash));
            if (!animator.HasState(layer, hash))
                throw new BridgeException($"no state {state} in {animator.runtimeAnimatorController.name} (states are mostly named like their clips; /animate lists the clips)");
            if (fade > 0f) animator.CrossFadeInFixedTime(hash, fade, layer);
            else animator.Play(hash, layer, at);
        }

        internal static void Trigger(Animator animator, string names)
        {
            foreach (string name in List(names)) animator.SetTrigger(Parameter(animator, name, AnimatorControllerParameterType.Trigger).nameHash);
        }

        /// <summary>name:value pairs, each set by the parameter's own type (float, int, bool).</summary>
        internal static void Set(Animator animator, string pairs)
        {
            foreach (string pair in List(pairs))
            {
                string[] parts = pair.Split(':');
                if (parts.Length != 2) throw new BridgeException($"set= takes name:value pairs, not {pair}");
                AnimatorControllerParameter parameter = Parameter(animator, parts[0], null);
                float value = parts[1] == "true" ? 1f : parts[1] == "false" ? 0f : Fmt.Numbers(parts[1], 1, parts[0])[0];
                if (parameter.type == AnimatorControllerParameterType.Float) animator.SetFloat(parameter.nameHash, value);
                else if (parameter.type == AnimatorControllerParameterType.Int) animator.SetInteger(parameter.nameHash, Mathf.RoundToInt(value));
                else if (parameter.type == AnimatorControllerParameterType.Bool) animator.SetBool(parameter.nameHash, value != 0f);
                else if (value != 0f) animator.SetTrigger(parameter.nameHash);
            }
        }

        private static AnimatorControllerParameter Parameter(Animator animator, string name, AnimatorControllerParameterType? type) =>
            animator.parameters.FirstOrDefault(p => p.name == name && (type == null || p.type == type))
            ?? throw new BridgeException($"{animator.runtimeAnimatorController.name} has no {(type == null ? "" : type.ToString().ToLowerInvariant() + " ")}parameter {name}");

        private static IEnumerable<string> List(string text) => text.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0);

        /// <summary>
        /// "gameClip:bundleClip,..." puts each bundle clip in place of the controller clip of that name. The first swap
        /// remembers the controller; swaps build on it afresh each time, and reset puts it back.
        /// </summary>
        internal static List<string> Swap(Placement placement, Animator animator, string spec)
        {
            if (!placement.Controller) placement.Controller = animator.runtimeAnimatorController;
            var over = new AnimatorOverrideController(placement.Controller) { name = placement.Controller.name + " (swapped)" };
            var bundles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> done = List(spec).Select(entry => SwapOne(over, placement.Controller, entry, bundles)).ToList();
            animator.runtimeAnimatorController = over;
            placement.SwapSpec = spec;
            placement.SwapBundles.Clear();
            placement.SwapBundles.UnionWith(bundles);
            return done;
        }

        // As the mods swap them: the controller's clip of that name, overridden by the bundle's.
        private static string SwapOne(AnimatorOverrideController over, RuntimeAnimatorController controller, string entry, HashSet<string> bundles)
        {
            string[] parts = entry.Split(':');
            if (parts.Length != 2) throw new BridgeException($"swap= takes gameClip:bundleClip pairs, not {entry}");
            AnimationClip original = controller.animationClips.FirstOrDefault(c => c && c.name == parts[0])
                ?? throw new BridgeException($"{controller.name} has no clip {parts[0]} (/animate lists its clips)");
            AnimationClip clip = Bundles.Asset<AnimationClip>(parts[1], null, out LoadedBundle from);
            bundles.Add(from.Name);
            over[original] = clip;
            return $"{parts[0]} -> {clip.name} ({from.Name})";
        }

        /// <summary>The animator's own controller back; forget the swap unless it is to be re-applied after a reload.</summary>
        internal static void Unswap(Placement placement, bool keepSpec)
        {
            Animator animator = placement.Alive ? placement.Root.GetComponentsInChildren<Animator>(true).FirstOrDefault(a => a.runtimeAnimatorController) : null;
            if (animator && placement.Controller) animator.runtimeAnimatorController = placement.Controller;
            if (keepSpec) return;
            placement.SwapSpec = null;
            placement.SwapBundles.Clear();
        }
    }
}
