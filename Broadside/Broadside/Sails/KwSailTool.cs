using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The sail tool: click a deck, a cabin's roof or a keel where the mast should stand; a window (<see cref="KwSailForm"/>) takes the
    /// mast's height and its sails one by one (square sails stacked on yards, a gaff or triangle sail behind the mast),
    /// and <see cref="KwPreview"/> shows the mast and every sail see-through as they change (the cloths in their own
    /// patterned looks, <see cref="KwSailPreview"/>); OK builds it there (a
    /// networked generated part, <see cref="KwGenerated"/>, free, its mast and sails saved in its ZDO as one text,
    /// <see cref="KwMastSpec"/>), its square sails facing along the piece clicked the way the player looks. E on the mast
    /// sets or furls all its sails (<see cref="KwSailRig"/>). The window opens with the mast made last. A click on a mast
    /// edits it instead (<see cref="KwMastEdit"/>, <see cref="Edit"/>): the same window and preview, on that mast in its place.
    /// </summary>
    public static class KwSailTool
    {
        public const string Tool = "sail", Made = "KW_gen_sail";

        private static KwSite? site;

        /// <summary>
        /// Where the open window's mast is shown in place of the dock's preview: on a launched ship, its own ghost
        /// (<see cref="KwSeaRigWindows"/>); null on the dock.
        /// </summary>
        internal static System.Action<KwMastSpec>? Elsewhere;

        public static void Click(Player player)
        {
            if (KwForm.IsOpen)
                return;
            KwClick? aim = KwTools.Aim(player);
            site = aim != null ? KwShapeSites.Mast(aim.Value) : null;
            if (site == null)
            {
                Say(player, "Click a deck, a cabin's roof or a keel where the mast should stand");
                return;
            }
            KwSailForm.Open(mast => Done(player, mast));
        }

        /// <summary>
        /// The window on a mast already built, for an edit (<see cref="KwMastEdit"/>): the preview shows the mast in its place as
        /// the window changes it; every change goes to changed, and the mast on OK (null cancelled) to done.
        /// </summary>
        public static void Edit(Transform at, KwMastSpec mast, string title, System.Func<KwMastSpec, string> changed, System.Action<KwMastSpec?> done)
        {
            site = new KwSite(at.position, at.rotation, false, KwKeel.Standard);
            KwSailForm.OpenOn(mast, title, changed, edited => Edited(edited, done));
        }

        private static void Edited(KwMastSpec? mast, System.Action<KwMastSpec?> done)
        {
            site = null;
            Hide();
            done(mast);
        }

        /// <summary>Shows the mast and sails the open window would build; nothing while it is not open.</summary>
        public static void Preview()
        {
            if (Elsewhere != null)
            {
                if (KwForm.IsOpen)
                    Elsewhere(KwSailForm.Mast);
                return;
            }
            KwSite? at = site;
            if (at == null || !KwForm.IsOpen)
            {
                Hide();
                return;
            }
            KwMastSpec mast = KwSailForm.Mast;
            string key = Tool + "|" + KwSailForm.Saved + "|" + at.Point.ToString("F2");
            KwPreview.Show(key, at.Point, at.Turn, preview => KwSailShapes.Whole(mast), false);
            KwSailPreview.Show(key, at.Point, at.Turn, () => KwRigGhost.Preview(Cloths(mast), mast, at.Point, at.Turn));
        }

        // Every sail's cloth, fully set, where it hangs on the mast, in its own look.
        private static List<(Mesh Mesh, Material? Look, Matrix4x4 At)> Cloths(KwMastSpec mast) =>
            KwSailLayout.Of(mast).ConvertAll(s => (KwSailCanvas.Cloth(s, 1f), KwSailLooks.Of(s.Spec), Matrix4x4.Translate(KwMesh.ToUnity(s.ClothOrigin))));

        private static void Hide()
        {
            KwPreview.Hide();
            KwSailPreview.Hide();
        }

        private static void Done(Player player, KwMastSpec? mast)
        {
            KwSite? at = site;
            site = null;
            Hide();
            if (mast == null || at == null || player == null)
                return;
            KwTools.Spawn(player, Tool, at.Point, at.Turn, mast.ToString());
            KwSailSpec first = mast.Sails[0];
            Say(player, mast.Sails.Count == 1
                ? $"Mast and sail built ({first.Width:0.#} × {first.Height:0.#} m sail)"
                : $"Mast built with {mast.Sails.Count} sails ({Area(mast):0} m² of sail)");
        }

        private static float Area(KwMastSpec mast)
        {
            float area = 0f;
            foreach (KwSailSpec sail in mast.Sails)
                area += sail.Area;
            return area;
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
