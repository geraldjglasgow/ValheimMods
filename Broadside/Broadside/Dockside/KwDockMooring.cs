using System.Collections.Generic;
using System.Linq;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A mooring post (any wood tier) or the lantern pole on a dock (DOCKS.md, "Mooring posts on a dock"): placed on a dock
    /// section's deck within <see cref="Zone"/> of a side edge, it snaps over the nearest piling station on that side
    /// (<see cref="Over"/>) so it reads as the piling carried on up through the deck's edge, square to the edge: the lantern
    /// pole's post on the piling's axis, its lamp (the model's -X) in over the deck toward the dock's middle; a mooring post,
    /// whose foot is thicker than the piling, moved in until its back is flush with the deck's side, its rope coil (the model's
    /// +Z) in over the deck. Anywhere else it places as before. A station where another dock branches off, a dock ladder
    /// hangs or another post or lantern stands is passed over for the next one. Each one loaded in the world (never a placing
    /// ghost) is listed (<see cref="Loaded"/>), so the section under it leaves its curb out round it, as round a dock ladder
    /// (<see cref="KwPierGaps"/>), told when it comes or goes; nothing is saved or sent: every peer works it out from where
    /// the pieces and sections stand. Put on the mooring posts with their <see cref="KwDockDef.Fit"/>, on the lantern pole
    /// with its light (<see cref="KwDockLantern.Fit"/>).
    /// </summary>
    public sealed class KwDockMooring : MonoBehaviour
    {
        public const float Zone = 0.8f;
        private const float Level = 0.35f, Reach = 1.6f, Apart = 0.3f, Joined = 0.35f, Back = 0.19f, Seat = 0.05f;
        private const string Lantern = KwDockside.Prefix + "lanternpole";
        private static readonly List<KwDockMooring> loaded = new List<KwDockMooring>();
        private bool listed;

        /// <summary>Every mooring post and lantern pole placed and loaded on this peer.</summary>
        public static IReadOnlyList<KwDockMooring> Loaded => loaded;

        /// <summary>Put on a mooring post's piece (<see cref="KwDockDef.Fit"/>, every tier).</summary>
        public static void Fit(GameObject piece, GameObject look) => piece.AddComponent<KwDockMooring>();

        /// <summary>Whether a prefab is a mooring post of any tier.</summary>
        public static bool Is(string prefab) => prefab.StartsWith(KwDockside.Prefix + "mooringpost", System.StringComparison.Ordinal);

        /// <summary>Whether a prefab is seated on a dock's piling when placed on a dock near a side: a mooring post or the lantern pole.</summary>
        public static bool OnPiling(string prefab) => Is(prefab) || prefab == Lantern;

        /// <summary>Its side out of the deck, level: a mooring post's back (-Z), the lantern pole's +X (its lamp hangs to -X, in).</summary>
        public Vector3 Outward
        {
            get
            {
                Vector3 back = Utils.GetPrefabName(gameObject) == Lantern ? transform.right : -transform.forward;
                return new Vector3(back.x, 0f, back.z).normalized;
            }
        }

        // Listed once it is a real piece (a ghost has no valid network view); the section under it looks at its gaps.
        private void Start()
        {
            ZNetView? view = GetComponent<ZNetView>();
            if (view == null || !view.IsValid())
                return;
            loaded.Add(this);
            listed = true;
            KwPierGaps.TouchLadder(transform.position);
        }

        private void OnDestroy()
        {
            if (!listed)
                return;
            loaded.Remove(this);
            Guard.Run<Vector3>("Broadside mooring post", KwPierGaps.TouchLadder, transform.position);
        }

        /// <summary>
        /// The hammer's ghost of a mooring post or the lantern pole snapped over a piling station when it stands on a dock near
        /// a side; the lantern pole's drawn without its cairn there (<see cref="KwDockTrim"/>).
        /// </summary>
        public static void Hover(GameObject ghost, string prefab)
        {
            (Vector3 At, Quaternion Turn)? pose = Snap(ghost.transform.position, prefab);
            if (pose != null)
                ghost.transform.SetPositionAndRotation(pose.Value.At, pose.Value.Turn);
            if (prefab == Lantern)
                KwDockTrim.Set(ghost, pose != null);
        }

        /// <summary>A post standing at a point on a built dock section's deck near a side: where it snaps; null anywhere else.</summary>
        public static (Vector3 At, Quaternion Turn)? Snap(Vector3 point, string prefab)
        {
            foreach (KwPierDeck deck in KwPierDeck.Near(point, 0.05f))
            {
                (Vector3 At, Quaternion Turn)? pose = Mathf.Abs(point.y - deck.Top) <= Level ? Over(deck, point, null, prefab) : null;
                if (pose != null)
                    return pose;
            }
            return null;
        }

        /// <summary>
        /// Over the free piling station nearest a point on a deck within <see cref="Zone"/> of a side (and within
        /// <see cref="Reach"/> along it), facing in; null when the point is farther from a side or no station there is free.
        /// <paramref name="taken"/> are more posts' places counted as standing (the Auto-decorate tool's plans).
        /// </summary>
        public static (Vector3 At, Quaternion Turn)? Over(KwPierDeck deck, Vector3 point, List<Vector3>? taken, string prefab)
        {
            Vector3 d = point - deck.Start;
            float along = Vector3.Dot(d, deck.Forward), across = Vector3.Dot(d, deck.Right);
            if (along < 0f || along > deck.Length || deck.Width * 0.5f - Mathf.Abs(across) > Zone)
                return null;
            bool right = across >= 0f, lantern = prefab == Lantern;
            // the lantern pole's post on the piling's axis; a mooring post's thicker foot moved in until its back is flush
            // with the deck's side, over the piling still, so nothing of it hangs past the edge
            float side = right ? 1f : -1f, x = side * (deck.Width * 0.5f - Mathf.Max(deck.PilingIn, lantern ? 0f : Back));
            float? z = Free(deck, right, along, x, taken);
            if (z == null)
                return null;
            Quaternion turn = Quaternion.LookRotation(-deck.Right * side) * (lantern ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity);
            return (deck.At(x, z.Value), turn);
        }

        /// <summary>Whether a piece's origin stands on a built dock section's deck right over one of its pilings (as seated).</summary>
        public static bool Seated(Vector3 at)
        {
            foreach (KwPierDeck deck in KwPierDeck.Near(at, 0.05f))
            {
                if (Mathf.Abs(at.y - deck.Top) > Seat)
                    continue;
                foreach (bool right in new[] { false, true })
                {
                    float x = (right ? 1f : -1f) * (deck.Width * 0.5f - deck.PilingIn);
                    if (deck.Pilings(right).Any(z => Flat(deck.At(x, z) - at) < Seat))
                        return true;
                }
            }
            return false;
        }

        // The station with a piling on that side nearest along, within reach, not where a dock branches off, a ladder hangs or a post stands.
        private static float? Free(KwPierDeck deck, bool right, float along, float x, List<Vector3>? taken)
        {
            float? best = null;
            IReadOnlyList<Vector2> joins = deck.Edges[right ? KwPierGaps.Right : KwPierGaps.Left].Joins;
            foreach (float z in deck.Pilings(right))
            {
                bool nearer = best == null || Mathf.Abs(z - along) < Mathf.Abs(best.Value - along);
                if (nearer && Mathf.Abs(z - along) <= Reach && !Joins(joins, z) && !Stands(deck.At(x, z), taken))
                    best = z;
            }
            return best;
        }

        private static bool Joins(IReadOnlyList<Vector2> joins, float z)
        {
            foreach (Vector2 join in joins)
            {
                if (join.x < z + Joined && join.y > z - Joined)
                    return true;
            }
            return false;
        }

        // Another post, a placed one or one in the list, or a dock ladder, at that place.
        private static bool Stands(Vector3 at, List<Vector3>? taken)
        {
            foreach (KwDockMooring post in loaded)
            {
                if (post != null && Flat(post.transform.position - at) < Apart)
                    return true;
            }
            foreach (KwPierLadder ladder in KwPierLadder.Loaded)
            {
                if (ladder != null && Flat(ladder.transform.position - at) < KwPierGaps.LadderSpan + Apart)
                    return true;
            }
            return taken != null && taken.Exists(p => Flat(p - at) < Apart);
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
