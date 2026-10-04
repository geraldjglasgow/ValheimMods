using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Swap
{
    /// <summary>What a swapped renderer draws with: the live material wearing the bundle's textures, the bundle's own, or its own.</summary>
    internal enum MaterialMode
    {
        Textures,
        Replace,
        Keep,
    }

    /// <summary>
    /// One prefab whose look comes from a bundle prefab: what was asked, which live renderer takes which bundle renderer,
    /// and everything needed to put the originals back - the first look of each renderer it changed, and the meshes,
    /// materials, textures and controllers it handed out (a copy made from the swapped prefab carries those, not its own).
    /// </summary>
    internal sealed class SwapEntry
    {
        internal GameObject Prefab;
        internal string PrefabName;
        internal int Hash;
        internal string Bundle;
        internal string Asset;
        internal MaterialMode Materials;
        internal bool Clips;
        internal string Map;
        internal DateTime At;

        /// <summary>Why it is off while its bundle is away (reloading, a failed build, the asset gone); null while on.</summary>
        internal string Waiting;

        /// <summary>"9 swapped, 2 kept, 1 failed", from the last time it was applied.</summary>
        internal string Summary = "";

        /// <summary>The last error reaching new copies, logged once rather than every second.</summary>
        internal string SweepError;

        internal List<Pair> Pairs = new List<Pair>();
        internal List<string> Missing = new List<string>();
        internal Dictionary<string, AnimationClip> BundleClips;
        internal readonly SortedSet<string> ClipNames = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>Each renderer's look the first time the swap reached it, while it still drew its own.</summary>
        internal readonly Dictionary<Renderer, Look> Own = new Dictionary<Renderer, Look>();

        /// <summary>The bundle meshes handed out: a renderer drawing one carries the swap.</summary>
        internal readonly HashSet<Mesh> Meshes = new HashSet<Mesh>();

        /// <summary>Copies of live materials wearing the bundle's textures, each with the material it was copied from.</summary>
        internal readonly Dictionary<Material, Material> Made = new Dictionary<Material, Material>();
        internal readonly Dictionary<(Material Source, Material Bundle), Material> Copies = new Dictionary<(Material, Material), Material>();

        /// <summary>Bundle materials put on outright (materials=replace).</summary>
        internal readonly HashSet<Material> Placed = new HashSet<Material>();

        /// <summary>The bundle's textures handed out, on copies of ours or on materials the game copied from them.</summary>
        internal readonly HashSet<Texture> Textures = new HashSet<Texture>();

        /// <summary>Override controllers made, with the controller each overrides; and per controller the one made for it (or none).</summary>
        internal readonly Dictionary<AnimatorOverrideController, RuntimeAnimatorController> Overrides =
            new Dictionary<AnimatorOverrideController, RuntimeAnimatorController>();
        internal readonly Dictionary<RuntimeAnimatorController, AnimatorOverrideController> OverrideOf =
            new Dictionary<RuntimeAnimatorController, AnimatorOverrideController>();

        /// <summary>Every object the swap was applied to: the prefab, live copies, worn item visuals.</summary>
        internal readonly Dictionary<GameObject, Target> Covered = new Dictionary<GameObject, Target>();

        internal bool On => Waiting == null;

        internal int Count(string kind) => Covered.Values.Count(t => t.Root && t.Kind == kind);

        /// <summary>After the originals are back: the copies and overrides made go, and what was handed out is forgotten.</summary>
        internal void Clear()
        {
            foreach (Material made in Made.Keys.Where(m => m)) Object.Destroy(made);
            foreach (AnimatorOverrideController over in Overrides.Keys.Where(o => o)) Object.Destroy(over);
            Own.Clear();
            Meshes.Clear();
            Made.Clear();
            Copies.Clear();
            Placed.Clear();
            Textures.Clear();
            Overrides.Clear();
            OverrideOf.Clear();
            Covered.Clear();
            ClipNames.Clear();
            Pairs = new List<Pair>();
            BundleClips = null;
        }
    }

    /// <summary>A renderer of the live prefab and the bundle renderer it takes its look from (none: it is kept).</summary>
    internal sealed class Pair
    {
        /// <summary>Its path from the prefab's root, the same in every copy of the prefab.</summary>
        internal string Path;

        internal Renderer Live;
        internal Renderer Bundle;
        internal string How;

        /// <summary>On the prefab: why the swap left it as it was (null when swapped), triangles before and after, and the materials.</summary>
        internal string Failure;
        internal int Before;
        internal int After;
        internal string Materials;

        internal Pair To(Renderer bundle, string how)
        {
            Bundle = bundle;
            How = how;
            return this;
        }
    }
}
