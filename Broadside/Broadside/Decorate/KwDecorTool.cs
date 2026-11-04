using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Auto-decorate tool in the hammer (<c>BS_tool_decorate</c>): a copy of the game's wood floor stripped to the
    /// placing box (<see cref="KwPrefabs.Anchor"/>), never placed itself, free, needing no station (the decorations it
    /// places need theirs), not turning (so the game leaves the plain wheel to the camera's zoom; Alt + wheel is the tool's,
    /// <see cref="KwToolWheel"/>), wearing the lobster pots' icon. In Broadside's build menu entry and the game's
    /// Decoration filter, after the dockside decorations (<see cref="KwHammer"/>). Made once when the network scene first
    /// wakes, after the decorations, and registered every wake on every peer.
    /// </summary>
    public static class KwDecorTool
    {
        public const string Prefab = "BS_tool_decorate";
        private const string Source = "wood_floor", IconFrom = "BS_dock_creels";
        private static GameObject? made;

        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside auto-decorate tool", () => Build(scene)));

        private static void Build(ZNetScene scene)
        {
            made ??= Make(scene);
            if (made == null)
                return;
            NetPrefabs.Register(scene, made);
            KwHammer.Refresh();
        }

        private static GameObject? Make(ZNetScene scene)
        {
            GameObject? source = scene.GetPrefab(Source);
            if (source == null || source.GetComponent<Piece>() == null)
            {
                Log.Warn($"Broadside: the game's {Source} was not found; no {KwDecorWords.Name} tool.");
                return null;
            }
            GameObject copy = PrefabBench.Copy(source, Prefab);
            KwPrefabs.Strip(copy);
            Object.DestroyImmediate(copy.GetComponent<WearNTear>());
            GameObject model = new GameObject("model") { layer = copy.layer };
            model.transform.SetParent(copy.transform, false);
            KwPrefabs.Anchor(model);
            Configure(copy.GetComponent<Piece>());
            return copy;
        }

        private static void Configure(Piece piece)
        {
            (piece.m_name, piece.m_description) = (KwDecorWords.Name, KwDecorWords.Description);
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_usage = Piece.UsageTagFlags.Transport | Piece.UsageTagFlags.Decor;
            (piece.m_enabled, piece.m_canRotate, piece.m_randomInitBuildRotation) = (true, false, false);
            piece.m_resources = new Piece.Requirement[0];
            piece.m_craftingStation = null;
            GameObject? pots = KwDockside.All.Count > 0 ? (System.Linq.Enumerable.FirstOrDefault(KwDockside.All, p => p.name == IconFrom) ?? KwDockside.All[0]) : null;
            if (pots != null)
                piece.m_icon = pots.GetComponent<Piece>().m_icon;
        }

        /// <summary>The tool, once made.</summary>
        public static GameObject? Made => made;

        /// <summary>Whether a piece (or the hammer's selection) is the tool.</summary>
        public static bool Is(Piece? piece) => piece != null && piece.name == Prefab;

        /// <summary>The tool in this player's hammer.</summary>
        public static bool Held(Player? player) => player != null && player.InPlaceMode() && Is(player.GetSelectedPiece());

        /// <summary>Into the hammer's pieces, once.</summary>
        public static void AddTo(List<GameObject> pieces)
        {
            if (made != null && !pieces.Contains(made))
                pieces.Add(made);
        }
    }
}
