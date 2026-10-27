using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The bowsprit tool: click the top of the bow's stem post (any keel's post, its "Post top"); a window
    /// (<see cref="KwBowspritForm"/>) takes the bowsprit's length and rise and its jibs, and <see cref="KwPreview"/> shows the
    /// spar and the jibs see-through as they change (the jibs flat, in their patterned look, <see cref="KwSailPreview"/>),
    /// the jibs cut to the stay from the foremast found there
    /// (<see cref="KwJibStay"/>); OK builds it on the post (a networked generated part, <see cref="KwGenerated"/>, free, its
    /// spec saved in its ZDO, <see cref="KwBowsprit"/>), running out along the keel past the post. A new tool rather than a
    /// choice on the sail window, because a bowsprit stands on the stem, which a mast's text knows nothing of, and is saved,
    /// costed and carried in blueprints as a part of its own; its jibs set with the foremast's sails (<see cref="KwJibRig"/>).
    /// A click on a bowsprit edits it instead (<see cref="KwBowspritEdit"/>, <see cref="Edit"/>): the same window and preview,
    /// on that bowsprit in its place.
    /// </summary>
    public static class KwBowspritTool
    {
        public const string Tool = KwBowsprit.Tool;

        private static KwSite? site;

        /// <summary>
        /// Where the open window's bowsprit and jibs are shown in place of the dock's preview: on a launched ship, its own
        /// ghost (<see cref="KwSeaRigWindows"/>); null on the dock.
        /// </summary>
        internal static System.Action<KwBowsprit, KwJibStay?>? Elsewhere;

        public static void Click(Player player)
        {
            if (KwForm.IsOpen)
                return;
            KwClick? aim = KwTools.Aim(player);
            site = aim != null ? Site(aim.Value) : null;
            if (site == null)
            {
                Say(player, "Click the top of the bow's stem post");
                return;
            }
            KwBowspritForm.Open(site, spec => Done(player, spec));
        }

        // The clicked keel's post top nearest the click (within 2 m), the bowsprit running out along the keel's line the way
        // the tool's placeholder points (the wheel turns it), so either end of a keel can be the bow.
        internal static KwSite? Site(KwClick aim)
        {
            if (!KwKeel.IsKeel(aim.Piece))
                return null;
            Transform? top = null;
            float best = 4f;
            foreach (Transform child in aim.Piece.transform)
            {
                float sq = (child.position - aim.Point).sqrMagnitude;
                if (child.name == "Post top" && child.CompareTag("snappoint") && sq < best)
                    (top, best) = (child, sq);
            }
            Vector3 axis = aim.Piece.transform.forward;
            axis.y = 0f;
            return top != null && axis.sqrMagnitude > 1e-4f ? new KwSite(top.position, KwShapeSites.Facing(axis, KwShapeSites.Way()), false, KwKeel.BodyOf(aim.Piece)) : null;
        }

        /// <summary>
        /// The window on a bowsprit already built, for an edit (<see cref="KwBowspritEdit"/>): the preview shows it in its place
        /// as the window changes it; every change goes to changed, and the bowsprit on OK (null cancelled) to done.
        /// </summary>
        public static void Edit(Transform at, KwBowsprit spec, string title, System.Func<KwBowsprit, string> changed, System.Action<KwBowsprit?> done)
        {
            KwSite on = new KwSite(at.position, at.rotation, false, KwKeel.Standard);
            site = on;
            KwBowspritForm.OpenOn(on, spec, title, changed, edited => Edited(edited, done));
        }

        private static void Edited(KwBowsprit? spec, System.Action<KwBowsprit?> done)
        {
            site = null;
            Hide();
            done(spec);
        }

        /// <summary>Shows the bowsprit and jibs the open window would build; nothing while it is not open.</summary>
        public static void Preview()
        {
            if (Elsewhere != null)
            {
                if (KwForm.IsOpen)
                    Elsewhere(KwBowspritForm.Spec, KwBowspritForm.Stay);
                return;
            }
            KwSite? at = site;
            if (at == null || !KwForm.IsOpen)
            {
                Hide();
                return;
            }
            KwBowsprit spec = KwBowspritForm.Spec;
            KwJibStay? stay = KwBowspritForm.Stay;
            string key = Tool + "|" + spec + "|" + (stay != null ? stay.Key : "") + "|" + at.Point.ToString("F2");
            KwPreview.Show(key, at.Point, at.Turn, _ => Spar(spec), false);
            KwSailPreview.Show(key, at.Point, at.Turn, () => Jibs(spec, stay));
        }

        // The spar alone.
        internal static Mesh Spar(KwBowsprit spec)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            spec.Def().Shape!(b);
            return b.ToMesh(KwBowsprit.Made + "_preview");
        }

        // The jibs as flat triangles, each in its look, its texture on it as on the jib's cloth: its own triangle in the
        // texture at tack, clew and head (<see cref="KwJibUv"/>), unstretched.
        internal static List<(Mesh Mesh, Material? Look, Matrix4x4 At)> Jibs(KwBowsprit spec, KwJibStay? stay)
        {
            List<(Mesh Mesh, Material? Look, Matrix4x4 At)> jibs = new List<(Mesh Mesh, Material? Look, Matrix4x4 At)>();
            foreach (KwJibCut cut in stay != null ? KwJibPlan.Of(spec, stay) : new List<KwJibCut>())
            {
                MeshBuilder b = new MeshBuilder(Vector3.zero);
                KwJibUv uv = KwJibUv.Of(KwJibDress.Frame(cut));
                b.Face(new List<Vector3> { cut.Tack, cut.Clew, cut.Head }, new List<Vector2> { uv.Tack, uv.Clew, uv.Head });
                jibs.Add((b.ToMesh(KwBowsprit.Made + "_jib"), KwJibDress.Look(cut, spec), Matrix4x4.identity));
            }
            return jibs;
        }

        private static void Hide()
        {
            KwPreview.Hide();
            KwSailPreview.Hide();
        }

        private static void Done(Player player, KwBowsprit? spec)
        {
            KwSite? at = site;
            site = null;
            Hide();
            if (spec == null || at == null || player == null)
                return;
            KwTools.Spawn(player, Tool, at.Point, at.Turn, spec.Value.ToString());
            KwJibStay? stay = KwJibStay.OnDock(at.Point, at.Turn, out _);
            float area = KwJibPlan.Area(spec.Value, stay);
            Say(player, area > 0f ? $"Bowsprit built with {KwJibPlan.Of(spec.Value, stay!).Count} jib(s) ({area:0} m² of sail)" : "Bowsprit built");
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
