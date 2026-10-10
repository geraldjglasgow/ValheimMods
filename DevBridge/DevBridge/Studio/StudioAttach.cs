using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Studio
{
    /// <summary>
    /// Effects the studio hangs on a target's node: a harmless local copy (<see cref="LocalCopy.Harmless"/>, nothing that
    /// hurts, flies or syncs) parented to the node at a local position, kept until removed (its timed destruction and the
    /// particles' stop actions go) so it can be moved and played again. It goes with the target when that unloads. One
    /// from a workshop bundle is hung back on from the new build, at the same place, when that bundle reloads.
    /// </summary>
    internal static class StudioAttach
    {
        /// <summary>An effect taken off while its bundle reloads.</summary>
        internal sealed class Lifted
        {
            internal Transform Parent;
            internal int Index;
            internal string Source;
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal Vector3 Scale;
        }

        /// <summary>A copy of the source (from <see cref="StudioSources"/>) on the parent, at the local position.</summary>
        internal static Transform Attach(Transform parent, string source, Vector3 position)
        {
            GameObject prefab = StudioSources.Resolve(source);
            GameObject copy = LocalCopy.Harmless(prefab, parent.position, parent.rotation, Keep);
            copy.name = prefab.name;
            copy.transform.SetParent(parent, false);
            copy.transform.localPosition = position;
            copy.transform.localRotation = Quaternion.identity;
            StudioMark mark = copy.AddComponent<StudioMark>();
            (mark.Source, mark.Bundle) = (source, StudioSources.BundleOf(source));
            return copy.transform;
        }

        internal static bool IsAttached(Transform node) => node.GetComponent<StudioMark>();

        internal static void Detach(Transform node)
        {
            if (!IsAttached(node)) throw new BridgeException($"{node.name} was not attached by the studio; only attached effects can be removed");
            Object.Destroy(node.gameObject);
        }

        /// <summary>Takes off every effect from the bundle (at once: the bundle unloads next), remembering where each hung.</summary>
        internal static List<Lifted> Lift(string bundle)
        {
            var lifted = new List<Lifted>();
            foreach (StudioMark mark in Object.FindObjectsByType<StudioMark>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!mark || !string.Equals(mark.Bundle, bundle, StringComparison.OrdinalIgnoreCase)) continue; // gone with an outer one
                Transform node = mark.transform;
                lifted.Add(new Lifted
                {
                    Parent = node.parent, Index = node.GetSiblingIndex(), Source = mark.Source,
                    Position = node.localPosition, Rotation = node.localRotation, Scale = node.localScale,
                });
                Object.DestroyImmediate(node.gameObject);
            }
            return lifted;
        }

        /// <summary>Lifts the bundle's effects, runs the reload, and hangs them back on; returns how many came back.</summary>
        internal static int Across(string bundle, Action reload, List<string> lost)
        {
            List<Lifted> lifted = Lift(bundle);
            reload();
            return lifted.Where(one => one.Parent).Count(one => Restore(one, lost));
        }

        private static bool Restore(Lifted one, List<string> lost)
        {
            try
            {
                Transform copy = Attach(one.Parent, one.Source, one.Position);
                copy.SetSiblingIndex(one.Index);
                (copy.localRotation, copy.localScale) = (one.Rotation, one.Scale);
                return true;
            }
            catch (Exception error)
            {
                lost.Add($"studio effect {one.Source} on {one.Parent.name}: {error.Message}");
                return false;
            }
        }

        // Before it wakes, on the inactive bench: a template kept switched off is switched on (so it wakes with network
        // views disabled), nothing removes it on a timer, and its particles stay when they stop, ready to play again.
        private static void Keep(GameObject copy)
        {
            copy.SetActive(true);
            LocalCopy.Purge(copy.GetComponentsInChildren<TimedDestruction>(true));
            foreach (ParticleSystem system in copy.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.stopAction = ParticleSystemStopAction.None;
            }
        }
    }

    /// <summary>Marks an effect the studio attached: the source it came from, and its workshop bundle if any.</summary>
    internal sealed class StudioMark : MonoBehaviour
    {
        internal string Source;
        internal string Bundle;
    }
}
