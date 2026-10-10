using System.Collections.Generic;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// Plays what a node holds from the start, on this machine only: its particle systems (cleared, then played), its
    /// animators (the current state, or state=, from its first frame), legacy animations and sounds (the game's ZSFX
    /// players, or a plain audio source). Inactive objects are skipped, as Unity plays nothing there.
    /// </summary>
    internal static class StudioPlayer
    {
        internal static bool CanPlay(Transform node) =>
            node.GetComponentInChildren<ParticleSystem>(true) || node.GetComponentInChildren<Animator>(true)
            || node.GetComponentInChildren<Animation>(true) || node.GetComponentInChildren<AudioSource>(true);

        internal static List<string> Play(Transform node, string state)
        {
            var played = new List<string>();
            Particles(node, played);
            Animators(node, state, played);
            Animations(node, played);
            Sounds(node, played);
            if (played.Count == 0) throw new BridgeException($"{node.name} has nothing active that plays (particles, animator, animation or sound)");
            return played;
        }

        private static void Particles(Transform node, List<string> played)
        {
            ParticleSystem[] systems = node.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem system in systems) system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (ParticleSystem system in systems) system.Play(false);
            if (systems.Length > 0) played.Add($"{systems.Length} particle system{(systems.Length == 1 ? "" : "s")}");
        }

        private static void Animators(Transform node, string state, List<string> played)
        {
            foreach (Animator animator in node.GetComponentsInChildren<Animator>())
            {
                if (!animator.runtimeAnimatorController) continue;
                int hash = state != null ? Animator.StringToHash(state) : animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
                if (state != null && !animator.HasState(0, hash)) throw new BridgeException($"{animator.name} has no state {state} on its first layer");
                animator.Play(hash, 0, 0f);
                played.Add($"animator {animator.name}" + (state != null ? $" ({state})" : ""));
            }
        }

        private static void Animations(Transform node, List<string> played)
        {
            foreach (Animation animation in node.GetComponentsInChildren<Animation>())
            {
                animation.Stop();
                if (animation.Play()) played.Add($"animation {animation.name}");
            }
        }

        private static void Sounds(Transform node, List<string> played)
        {
            foreach (AudioSource source in node.GetComponentsInChildren<AudioSource>())
            {
                ZSFX sfx = source.GetComponent<ZSFX>();
                if (sfx) sfx.Play();
                else
                {
                    source.Stop();
                    source.Play();
                }
                played.Add($"sound {source.name}");
            }
        }
    }
}
