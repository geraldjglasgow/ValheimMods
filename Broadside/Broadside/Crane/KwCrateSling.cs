using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The rope sling a crate hangs in from a crane's hook (the user's words, 2026-11-02: "When you hook onto a cargo crate,
    /// rope should appear around the crate with a like loop on top that the crane hook is hooked into"), drawn on every
    /// screen from what is already replicated (the crate hangs, <see cref="KwCrate.HangingFrom"/>): nothing is sent and a
    /// dedicated server draws nothing. Two bands go round the crate's body like a parcel's string, one along it and one
    /// across, each under its bottom (the one across under the one along) and up two opposite faces, standing clear of what
    /// the model has there (<see cref="KwCrateSlingFit"/>); from where they reach the top edges, four legs run up to an eye
    /// above the middle of the top, and the crane's hook goes through the eye: its top rests in the hook's bowl (the crate's
    /// lift point, <see cref="KwCrate.LiftLocal"/>), the eye standing across the hook's own plane. Three line renderers in
    /// the game's rope (the crane's, <see cref="KwCargoModel.Line"/>), in the crate's own frame under <see cref="Name"/>,
    /// made the first time the crate hangs on this screen, shown only while it hangs; only the eye is set again, when the
    /// crate's turn against the turret changes.
    /// </summary>
    public sealed class KwCrateSling
    {
        public const string Name = "kwc_sling";

        /// <summary>The rope's half thickness (the crane's rope is 3.5 cm), the eye's radius to the rope's middle (m).</summary>
        public const float RopeHalf = 0.0175f, Eye = 0.07f;
        private const float Clear = 0.005f;
        private const int EyePoints = 12;

        private readonly LineRenderer along, across, eye;
        private readonly Vector3 eyeCentre;
        private readonly Vector3[] ring = new Vector3[EyePoints];
        private float shownYaw = float.NaN;

        /// <summary>
        /// A crate's lift point made from its box when its model has none: the eye's bottom as high over the middle of the
        /// top as 0.9 times the longer half side (0.3 to 1 m, so the legs stand steep), the eye's top in the hook's bowl.
        /// </summary>
        public static Vector3 LiftOver(Vector3 centre, Vector3 half) =>
            centre + Vector3.up * (half.y + Mathf.Clamp(0.9f * Mathf.Max(half.x, half.z), 0.3f, 1f) + 2f * Eye - RopeHalf);

        private KwCrateSling(KwCrate crate, LineRenderer along, LineRenderer across, LineRenderer eye)
        {
            (this.along, this.across, this.eye) = (along, across, eye);
            eyeCentre = crate.LiftLocal + Vector3.up * (RopeHalf - Eye);
            KwCrateSlingFit fit = KwCrateSlingFit.Of(crate);
            Vector3 knot = eyeCentre + Vector3.down * Eye;
            Set(along, Band(fit, 0, crate.BoxCentre, knot, RopeHalf));
            Set(across, Band(fit, 2, crate.BoxCentre, knot, 3f * RopeHalf));
        }

        /// <summary>The sling under a crate, hidden; null on a dedicated server or without the game's rope.</summary>
        public static KwCrateSling? Make(KwCrate crate)
        {
            if (!KwCargoModel.Screens || ZNetScene.instance == null)
                return null;
            GameObject holder = new GameObject(Name) { layer = crate.gameObject.layer };
            holder.transform.SetParent(crate.transform, false);
            LineRenderer? along = Line(holder.transform, "along"), across = Line(holder.transform, "across"), eye = Line(holder.transform, "eye");
            if (along == null || across == null || eye == null || KwCargoModel.Rope == null)
            {
                Object.Destroy(holder);
                return null;
            }
            eye.loop = true;
            return new KwCrateSling(crate, along, across, eye);
        }

        private static LineRenderer? Line(Transform holder, string name)
        {
            LineRenderer? line = KwCargoModel.Line(holder, name, ZNetScene.instance);
            if (line != null)
                (line.useWorldSpace, line.numCornerVertices, line.enabled) = (false, 3, false);
            return line;
        }

        private static void Set(LineRenderer line, Vector3[] points)
        {
            line.positionCount = points.Length;
            line.SetPositions(points);
        }

        // One band and its two legs: from the eye down to one top edge, down that side, under the bottom (this far below
        // it), up the opposite side and back up to the eye. Sides 0 and 1 face along x, 2 and 3 along z.
        private static Vector3[] Band(KwCrateSlingFit fit, int side, Vector3 centre, Vector3 knot, float under)
        {
            List<Vector2> down = fit.Sides[side], up = fit.Sides[side + 1];
            List<Vector3> path = new List<Vector3> { knot };
            for (int i = down.Count - 1; i >= 0; i--)
                path.Add(At(down[i], side, centre, Lift(i, down.Count, under)));
            for (int i = 0; i < up.Count; i++)
                path.Add(At(up[i], side + 1, centre, Lift(i, up.Count, under)));
            path.Add(knot);
            return path.ToArray();
        }

        // How far a side's point is raised so the rope lies round the edge: under the bottom edge, over the top edge.
        private static float Lift(int index, int count, float under) => index == 0 ? -under : index == count - 1 ? RopeHalf : 0f;

        // A side's point in the crate's frame: the rope's middle its half thickness out from what it lies on.
        private static Vector3 At(Vector2 point, int side, Vector3 centre, float raise)
        {
            float reach = (point.y + RopeHalf + Clear) * (side % 2 == 0 ? 1f : -1f), y = centre.y + point.x + raise;
            return side < 2 ? new Vector3(centre.x + reach, y, centre.z) : new Vector3(centre.x, y, centre.z + reach);
        }

        /// <summary>Shown, the eye across the hook: the hook's plane is the turret's, turned <paramref name="hangYaw"/> from the crate's.</summary>
        public void Show(float hangYaw)
        {
            if (hangYaw != shownYaw)
                Ring(shownYaw = hangYaw);
            if (!along.enabled)
                along.enabled = across.enabled = eye.enabled = true;
        }

        public void Hide()
        {
            if (along.enabled)
                along.enabled = across.enabled = eye.enabled = false;
        }

        // The eye round its middle in the plane of the turret's right and up, so the hook, which lies along the turret, threads it.
        private void Ring(float hangYaw)
        {
            Vector3 right = Quaternion.Euler(0f, -hangYaw, 0f) * Vector3.right;
            for (int i = 0; i < ring.Length; i++)
            {
                float angle = i * 2f * Mathf.PI / ring.Length;
                ring[i] = eyeCentre + Eye * (Mathf.Cos(angle) * right + Mathf.Sin(angle) * Vector3.up);
            }
            Set(eye, ring);
        }
    }
}
