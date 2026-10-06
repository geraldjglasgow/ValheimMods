using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The ghost of a loaded construction site, drawn on every machine that has its post loaded: plain copies of the
    /// unbuilt pieces' looks (<see cref="PieceShapes"/> templates, like the placing preview, no collider, nothing
    /// networked) under one root standing at the site's frame, drawn see-through and pale (<see cref="GhostLook"/>) so
    /// they read as a plan.
    /// A copy goes away when its piece's bit is set in the ZDO; with more than <see cref="BlueprintRules.FullPreviewLimit"/>
    /// unbuilt pieces only what stands within <see cref="BlueprintRules.OutlineHeight"/> of the ground is drawn, the rest
    /// once the unbuilt count falls below the limit. Pieces glow brighter under a key (the planner's selection, a hovered
    /// queue row): the last key set that holds a piece decides its glow. Copies are made within a time budget each frame
    /// (<see cref="FrameBudget"/>, shared by every site). While this player has the Construction ghosts switch off
    /// (<see cref="GhostSwitch.Visible"/>, the Site planner in hand shows them anyway) every ghost's root is inactive, no
    /// copies are made and nothing can be picked; the posts are untouched.
    /// </summary>
    public sealed class SiteGhost
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Dictionary<SiteMarker, SiteGhost> ghosts = new Dictionary<SiteMarker, SiteGhost>();
        private static readonly List<Renderer> renderers = new List<Renderer>();
        private static MaterialPropertyBlock block;

        private readonly SiteMarker site;
        private readonly Blueprint bp;
        private readonly GameObject root;
        private readonly GameObject[] copies;
        private readonly List<int> todo = new List<int>();
        private readonly List<(string Key, HashSet<int> Pieces, Color Colour)> glows = new List<(string, HashSet<int>, Color)>();
        private int made;
        private bool outlineOnly;
        private uint revision = uint.MaxValue;

        private SiteGhost(SiteMarker site)
        {
            this.site = site;
            bp = site.State.Blueprint;
            copies = new GameObject[bp.Pieces.Count];
            root = new GameObject("OpenKeep Site Ghost");
            root.SetActive(GhostSwitch.Visible);
            BuildFrame frame = site.State.Frame;
            root.transform.SetPositionAndRotation(frame.Origin, frame.Rotation);
            outlineOnly = Unbuilt() > BlueprintRules.FullPreviewLimit;
            Queue();
        }

        /// <summary>The ghost of a loaded site (made on first use), or null when the site has no blueprint.</summary>
        public static SiteGhost For(SiteMarker site)
        {
            if (site == null || site.State?.Blueprint == null)
                return null;
            if (!ghosts.TryGetValue(site, out SiteGhost ghost))
                ghosts[site] = ghost = new SiteGhost(site);
            return ghost;
        }

        /// <summary>Per frame (SiteHooks): every loaded site's ghost follows its ZDO and makes its next copies (never on a dedicated server).</summary>
        public static void TickAll()
        {
            if (ZNet.instance == null || ZNet.instance.IsDedicated())
                return;
            double until = FrameBudget.Until();
            bool visible = GhostSwitch.Visible;
            foreach (SiteMarker site in SiteMarker.Loaded)
            {
                SiteGhost ghost = For(site);
                if (ghost == null)
                    continue;
                if (ghost.root.activeSelf != visible)
                    ghost.root.SetActive(visible);
                if (visible)
                    ghost.Update(until);
            }
        }

        /// <summary>Every ghost's pieces take their colour again (after <see cref="GhostLook.Tune"/>).</summary>
        public static void RepaintAll()
        {
            foreach (SiteGhost ghost in ghosts.Values)
            {
                for (int i = 0; i < ghost.copies.Length; i++)
                {
                    if (ghost.copies[i] != null)
                        ghost.Paint(i);
                }
            }
        }

        /// <summary>The post went away (taken down, finished, unloaded): its ghost goes with it.</summary>
        public static void Forget(SiteMarker site)
        {
            if (site == null || !ghosts.TryGetValue(site, out SiteGhost ghost))
                return;
            ghosts.Remove(site);
            if (ghost.root != null)
                Object.Destroy(ghost.root);
        }

        /// <summary>The pieces glow in the colour (replacing any earlier glow set under the same key; the same glow again costs nothing).</summary>
        public void SetGlow(string key, ICollection<int> pieces, Color colour)
        {
            int at = glows.FindIndex(g => g.Key == key);
            if (at >= 0 && glows[at].Colour == colour && glows[at].Pieces.SetEquals(pieces))
                return;
            HashSet<int> changed = new HashSet<int>(pieces);
            if (at >= 0)
            {
                changed.UnionWith(glows[at].Pieces);
                glows.RemoveAt(at);
            }
            glows.Add((key, new HashSet<int>(pieces), colour));
            Repaint(changed);
        }

        public void ClearGlow(string key)
        {
            int at = glows.FindIndex(g => g.Key == key);
            if (at < 0)
                return;
            HashSet<int> changed = glows[at].Pieces;
            glows.RemoveAt(at);
            Repaint(changed);
        }

        /// <summary>The nearest unbuilt ghost piece of any loaded site along the ray, within the distance.</summary>
        public static bool Pick(Ray ray, float distance, out SiteMarker site, out int piece)
        {
            site = null;
            piece = -1;
            if (!GhostSwitch.Visible)
                return false;
            float best = distance;
            foreach (SiteMarker candidate in SiteMarker.Loaded)
            {
                SiteGhost ghost = For(candidate);
                if (ghost != null && GhostPick.Nearest(ghost.bp, candidate.State, ghost.Shown, ray, ref best, out int hit))
                {
                    site = candidate;
                    piece = hit;
                }
            }
            return site != null;
        }

        /// <summary>The piece is drawn (or will be): not built, and not left out of a large site's outline.</summary>
        private bool Shown(int i) => !Built(i) && (!outlineOnly || bp.Pieces[i].Y <= BlueprintRules.OutlineHeight);

        private bool Built(int i)
        {
            bool[] built = site.State.Built;
            return i < built.Length && built[i];
        }

        private void Update(double until)
        {
            if (site.State.Zdo.DataRevision != revision)
                Refresh();
            while (made < todo.Count && FrameBudget.Left(until))
                Copy(todo[made++]);
        }

        /// <summary>The ZDO changed: copies of built pieces go, and a large site draws in full once few enough pieces are left.</summary>
        private void Refresh()
        {
            revision = site.State.Zdo.DataRevision;
            for (int i = 0; i < copies.Length; i++)
            {
                if (copies[i] != null && Built(i))
                {
                    Object.Destroy(copies[i]);
                    copies[i] = null;
                }
            }
            if (outlineOnly && Unbuilt() <= BlueprintRules.FullPreviewLimit)
            {
                outlineOnly = false;
                Queue();
            }
        }

        /// <summary>Queues every shown piece without a copy, in blueprint order (at the start, and again when the outline opens up).</summary>
        private void Queue()
        {
            todo.Clear();
            made = 0;
            for (int i = 0; i < copies.Length; i++)
            {
                if (copies[i] == null && Shown(i))
                    todo.Add(i);
            }
        }

        private int Unbuilt()
        {
            bool[] built = site.State.Built;
            int n = 0;
            for (int i = 0; i < copies.Length; i++)
                n += i < built.Length && built[i] ? 0 : 1;
            return n;
        }

        private void Copy(int i)
        {
            if (copies[i] != null || !Shown(i))
                return;
            BlueprintPiece p = bp.Pieces[i];
            PieceShape shape = PieceShapes.Of(p.Prefab);
            if (shape?.Template == null)
                return;
            GameObject copy = Object.Instantiate(shape.Template, root.transform, false);
            copy.transform.localPosition = new Vector3(p.X, p.Y, p.Z);
            copy.transform.localRotation = Quaternion.Euler(0f, p.Yaw, 0f);
            copies[i] = copy;
            GhostLook.Apply(copy, renderers);
            Paint(i);
        }

        private void Repaint(IEnumerable<int> pieces)
        {
            foreach (int i in pieces)
            {
                if (i >= 0 && i < copies.Length && copies[i] != null)
                    Paint(i);
            }
        }

        /// <summary>The copy's renderers get the ghost's colour: its glow (the last key holding the piece), else pale.</summary>
        private void Paint(int i)
        {
            if (block == null)
                block = new MaterialPropertyBlock();
            block.Clear();
            block.SetColor(ColorId, GhostLook.ColourOf(GlowOf(i)));
            copies[i].GetComponentsInChildren(renderers);
            foreach (Renderer renderer in renderers)
                renderer.SetPropertyBlock(block);
        }

        private Color? GlowOf(int i)
        {
            for (int g = glows.Count - 1; g >= 0; g--)
            {
                if (glows[g].Pieces.Contains(i))
                    return glows[g].Colour;
            }
            return null;
        }
    }
}
