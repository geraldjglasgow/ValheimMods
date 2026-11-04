using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Dock tool and the dock sections it builds, made once when the network scene first wakes, on every peer (the
    /// dedicated server too), registered every time it wakes; then the hammer is refreshed (<see cref="KwPierHammer"/>).
    /// Each is a copy of the game's wood floor (its Piece, WearNTear, network view, place and break effects), stripped of
    /// its look and colliders (<see cref="KwPrefabs.Strip"/>). A section (<c>BS_pier_wood</c>, <c>_corewood</c>,
    /// <c>_finewood</c>, never in the hammer) has an empty model its shape fills (<see cref="KwPier"/>), its material's health
    /// and support rules, its wear states on the model and no flying fragments when it breaks, at a workbench for removal
    /// as the game's pieces are. The tool (<c>BS_tool_pier</c>) wears a small wood dock as its look and hammer icon, at a
    /// workbench, never placed itself (<see cref="KwPierTool"/>), its wheel the tool's (it never turns). The dock ladders
    /// the ladder tool hangs from a dock's edge are made here too (<see cref="KwPierLadderPrefabs"/>).
    /// </summary>
    public static class KwPierPrefabs
    {
        public const string Prefix = "BS_pier_", Tool = "BS_tool_pier";
        private const string Source = "wood_floor", Workbench = "piece_workbench";
        private static readonly List<GameObject> built = new List<GameObject>();
        private static KwPierPlan? shownFor;
        private static Piece.Requirement[] shown = new Piece.Requirement[0];

        /// <summary>The tool and the three section prefabs, once made.</summary>
        public static IReadOnlyList<GameObject> All => built;

        public static GameObject? ToolPrefab { get; private set; }

        /// <summary>No requirements: the tool at rest.</summary>
        public static readonly Piece.Requirement[] None = new Piece.Requirement[0];

        /// <summary>Once, from the plugin, after the dockside decorations.</summary>
        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside docks", () => Build(scene)));

        private static void Build(ZNetScene scene)
        {
            if (built.Count == 0)
                Create(scene);
            foreach (GameObject prefab in built)
                NetPrefabs.Register(scene, prefab);
            KwHammer.Refresh();
        }

        private static void Create(ZNetScene scene)
        {
            GameObject? source = scene.GetPrefab(Source);
            if (source == null || source.GetComponent<Piece>() == null || source.GetComponent<WearNTear>() == null)
            {
                Log.Warn($"Broadside: the game's {Source} was not found; no docks.");
                return;
            }
            CraftingStation? bench = scene.GetPrefab(Workbench)?.GetComponent<CraftingStation>();
            foreach (KwPierKind kind in KwPierKinds.All)
                built.Add(Section(source, kind, bench));
            built.AddRange(KwPierLadderPrefabs.Make(source, bench));
            ToolPrefab = KwPierToolLook.Make(source, bench);
            built.Add(ToolPrefab);
            Log.Info($"Broadside: the Dock tool, {KwPierKinds.All.Length} dock materials and their ladders made.");
        }

        private static GameObject Section(GameObject source, KwPierKind kind, CraftingStation? bench)
        {
            GameObject copy = PrefabBench.Copy(source, kind.Prefab);
            KwPrefabs.Strip(copy);
            GameObject model = Model(copy);
            Configure(copy.GetComponent<Piece>(), $"Dock ({kind.Label.ToLowerInvariant()})", bench);
            WearNTear wear = copy.GetComponent<WearNTear>();
            (wear.m_health, wear.m_materialType, wear.m_noSupportWear, wear.m_autoCreateFragments) = (kind.Health, kind.Wear, true, false);
            (wear.m_new, wear.m_worn, wear.m_broken, wear.m_wet, wear.m_fragmentRoots) = (model, model, model, null, null);
            copy.AddComponent<KwPier>().Kind = kind.Id;
            return copy;
        }

        /// <summary>The empty "model" child a piece draws on, on its layer, casting the game's shadows.</summary>
        public static GameObject Model(GameObject piece)
        {
            GameObject model = new GameObject("model") { layer = piece.layer };
            model.transform.SetParent(piece.transform, false);
            model.AddComponent<MeshFilter>();
            model.AddComponent<MeshRenderer>();
            return model;
        }

        /// <summary>Named, in Broadside's build menu entry (Transportation's flag), at a workbench, costing nothing until set.</summary>
        public static void Configure(Piece piece, string name, CraftingStation? bench)
        {
            (piece.m_name, piece.m_description) = (name, "A dock built with the Dock tool. Removing it gives back all it cost.");
            (piece.m_category, piece.m_usage, piece.m_enabled) = (Piece.PieceCategory.Misc, Piece.UsageTagFlags.Transport, true);
            (piece.m_comfort, piece.m_comfortGroup, piece.m_comfortObject) = (0, Piece.ComfortGroup.None, null);
            piece.m_resources = new Piece.Requirement[0];
            if (bench != null)
                piece.m_craftingStation = bench;
            else
                Log.Warn($"Broadside: the game's {Workbench} was not found; {piece.name} keeps the wood floor's station.");
        }

        /// <summary>The tool's requirements while a dock it may build is aimed at: that dock's cost (one array per plan).</summary>
        public static Piece.Requirement[] Shown(KwPierPlan plan)
        {
            if (plan != shownFor)
                (shownFor, shown) = (plan, KwPierCost.Requirements(plan.Kind, plan.Cost, false));
            return shown;
        }

        /// <summary>The tool as it rests in the hammer: its description and no requirements.</summary>
        public static void Rest()
        {
            Piece? tool = ToolPrefab != null ? ToolPrefab.GetComponent<Piece>() : null;
            if (tool == null)
                return;
            if (tool.m_description != KwPierWords.Description)
                tool.m_description = KwPierWords.Description;
            if (tool.m_resources.Length > 0)
                tool.m_resources = None;
        }
    }
}
