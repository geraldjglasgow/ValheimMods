using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Swap
{
    /// <summary>
    /// The bundle's animation clips in place of a live animator's clips of the same name, through an
    /// AnimatorOverrideController as the mods swap in the workshop's clips (and as /animate does on the stage). A
    /// controller that is already an override (a mod's clips over the game's) is rebuilt on the controller beneath it
    /// with that override's clips kept, so the bundle's clips go over the ones playing now. A live animator keeps its
    /// states, times and parameters across the change, so a creature mid-fight does not drop back to idle.
    /// </summary>
    internal static class ClipSwap
    {
        internal static void Put(SwapEntry entry, Animator animator, bool live)
        {
            RuntimeAnimatorController current = animator.runtimeAnimatorController;
            if (!current || (current is AnimatorOverrideController ours && entry.Overrides.ContainsKey(ours))) return;
            AnimatorOverrideController over = For(entry, current);
            if (over) Assign(animator, over, live);
        }

        /// <summary>The controller the animator had before the swap, when it plays one of ours.</summary>
        internal static void Revert(SwapEntry entry, Animator animator, bool live)
        {
            if (animator.runtimeAnimatorController is AnimatorOverrideController ours && entry.Overrides.TryGetValue(ours, out RuntimeAnimatorController source) && source)
                Assign(animator, source, live);
        }

        /// <summary>One override per controller, shared by the prefab and its copies; null when no clip of it is in the bundle.</summary>
        private static AnimatorOverrideController For(SwapEntry entry, RuntimeAnimatorController current)
        {
            if (entry.OverrideOf.TryGetValue(current, out AnimatorOverrideController made)) return made;
            made = Build(entry, current);
            entry.OverrideOf[current] = made;
            if (made) entry.Overrides[made] = current;
            return made;
        }

        // Named as the controller it stands in for, so a mod that recognises its own override by name still does.
        private static AnimatorOverrideController Build(SwapEntry entry, RuntimeAnimatorController current)
        {
            var existing = current as AnimatorOverrideController;
            RuntimeAnimatorController basis = existing != null ? existing.runtimeAnimatorController : current;
            if (!basis || basis is AnimatorOverrideController) return null; // overrides of overrides are not unpicked
            var over = new AnimatorOverrideController(basis) { name = current.name };
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            (existing != null ? existing : over).GetOverrides(pairs);
            if (Override(entry, pairs) > 0)
            {
                over.ApplyOverrides(pairs);
                return over;
            }
            Object.Destroy(over);
            return null;
        }

        /// <summary>Each clip playing now (the override's, else the controller's) that the bundle has a clip of the same name for.</summary>
        private static int Override(SwapEntry entry, List<KeyValuePair<AnimationClip, AnimationClip>> pairs)
        {
            int swapped = 0;
            for (int i = 0; i < pairs.Count; i++)
            {
                AnimationClip playing = pairs[i].Value ? pairs[i].Value : pairs[i].Key;
                if (!playing || !entry.BundleClips.TryGetValue(playing.name, out AnimationClip clip)) continue;
                pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, clip);
                entry.ClipNames.Add(clip.name);
                swapped++;
            }
            return swapped;
        }

        /// <summary>A live animator onto another controller in the states, at the times and with the parameters it had.</summary>
        private static void Assign(Animator animator, RuntimeAnimatorController controller, bool live)
        {
            if (!live || !animator.isActiveAndEnabled || !animator.isInitialized)
            {
                animator.runtimeAnimatorController = controller;
                return;
            }
            AnimatorStateInfo[] states = Enumerable.Range(0, animator.layerCount).Select(animator.GetCurrentAnimatorStateInfo).ToArray();
            var values = animator.parameters.Where(p => p.type != AnimatorControllerParameterType.Trigger).Select(p => (p, Read(animator, p))).ToList();
            animator.runtimeAnimatorController = controller;
            foreach (var (parameter, value) in values) Write(animator, parameter, value);
            for (int layer = 0; layer < states.Length && layer < animator.layerCount; layer++)
                animator.Play(states[layer].fullPathHash, layer, states[layer].normalizedTime);
        }

        private static float Read(Animator animator, AnimatorControllerParameter parameter)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float: return animator.GetFloat(parameter.nameHash);
                case AnimatorControllerParameterType.Int: return animator.GetInteger(parameter.nameHash);
                default: return animator.GetBool(parameter.nameHash) ? 1f : 0f;
            }
        }

        private static void Write(Animator animator, AnimatorControllerParameter parameter, float value)
        {
            if (parameter.type == AnimatorControllerParameterType.Float) animator.SetFloat(parameter.nameHash, value);
            else if (parameter.type == AnimatorControllerParameterType.Int) animator.SetInteger(parameter.nameHash, Mathf.RoundToInt(value));
            else animator.SetBool(parameter.nameHash, value > 0.5f);
        }

        /// <summary>The bundle's clips by name (the first of a name).</summary>
        internal static Dictionary<string, AnimationClip> Of(AssetBundle bundle)
        {
            var clips = new Dictionary<string, AnimationClip>();
            foreach (AnimationClip clip in bundle.LoadAllAssets<AnimationClip>().Where(c => c))
            {
                if (!clips.ContainsKey(clip.name)) clips.Add(clip.name, clip);
            }
            return clips;
        }
    }
}
