using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a rudder's linkage is made of (<see cref="KwRudderLink"/>), in the rudder's own frame (Unity axes): the blade it
    /// follows, the deck tiller that turns with it on its own post, the two links (a line or a rod each) from the yoke's
    /// eyes to the deck tiller's, and where the helmsman's hand holds it.
    /// </summary>
    public sealed class KwRudderLinkParts
    {
        public Transform? Helm { get; set; }

        /// <summary>The yoke's eyes in the blade's frame, and the deck tiller's in its own.</summary>
        public Vector3[] YokeEnds { get; } = new Vector3[2];
        public Vector3[] DeckEnds { get; } = new Vector3[2];

        /// <summary>Each link: a line through both eyes, or a rod moved along with the eye it hangs from.</summary>
        public LineRenderer?[] Lines { get; } = new LineRenderer?[2];
        public Transform?[] Rods { get; } = new Transform?[2];

        /// <summary>The idle hand's place on what it holds: in the deck tiller's frame, or the blade's for a tiller.</summary>
        public Vector3 Hand { get; set; }

        /// <summary>Whether a helmsman can stand at it.</summary>
        public bool Steered { get; set; }
    }

    /// <summary>
    /// A rudder's linkage following its blade, on the dock (the blade amidships) and on a launched ship (the game's ship
    /// turning it with the helm): the yoke turns with the stock, the deck tiller turns with it by the same angle (its yoke
    /// is as long as the head's, so the two links stay parallel and keep their length), so its handle swings the opposite
    /// way to the blade's after edge as a tiller does, and the links follow both yokes. The helmsman's hands find the grip
    /// from the blade's turn (<see cref="HandAt"/>, <see cref="BarAt"/>), his feet from the helm's steady turn
    /// (<see cref="KwTillerGrip"/>). Every peer sees the blade turned by replicated state (the ship's rudder value), so
    /// every peer draws the same.
    /// </summary>
    public sealed class KwRudderLinkage : MonoBehaviour
    {
        // A hand holding the bar beside the peg: this far back along it from the grip toward the pivot, on the bar.
        private static readonly Vector3 BarBack = KwMesh.ToUnity(new Vector3(-0.14f, 0f, KwRudderLay.GripHeight - KwRudderLay.Hand));

        private Transform? blade;
        private KwRudderLinkParts parts = new KwRudderLinkParts();
        private Quaternion last;
        private bool dirty;

        /// <summary>Whether a helmsman can stand at it: a tiller to his hand, or lines to a deck tiller.</summary>
        public bool Steered => blade != null && parts.Steered;

        /// <summary>Where the idle hand is now, and the feet under it to port, in the rudder's frame.</summary>
        public Vector3 HandLocal => HandAt(Turn);
        public Vector3 FeetLocal => HandLocal + KwMesh.ToUnity(new Vector3(0f, KwRudderLay.Side, -KwRudderLay.Hand));

        /// <summary>The blade's turn as drawn now (none amidships).</summary>
        public Quaternion Turn => blade != null ? blade.localRotation : Quaternion.identity;

        /// <summary>Where the idle hand holds the peg with the blade turned so, in the rudder's frame.</summary>
        public Vector3 HandAt(Quaternion turn) => At(turn, parts.Hand);

        /// <summary>A point on the bar a hand's width back from the grip with the blade turned so, in the rudder's frame.</summary>
        public Vector3 BarAt(Quaternion turn) => At(turn, parts.Hand + BarBack);

        /// <summary>The linkage built again: what it follows and moves.</summary>
        public void Set(Transform blade, KwRudderLinkParts parts)
        {
            this.blade = blade;
            this.parts = parts;
            dirty = true;
        }

        private void LateUpdate()
        {
            if (blade == null)
                return;
            Quaternion turn = blade.localRotation;
            if (!dirty && turn == last)
                return;
            (last, dirty) = (turn, false);
            if (parts.Helm != null)
                parts.Helm.localRotation = turn;
            for (int i = 0; i < 2; i++)
                Link(i);
        }

        // A link from the yoke's eye to the deck tiller's: a line through both, or a rod moved with the yoke's eye (the two
        // yokes turn alike, so the rod keeps its length and its heading).
        private void Link(int i)
        {
            if (parts.Helm == null)
                return;
            Vector3 yoke = On(blade, parts.YokeEnds[i]), deck = On(parts.Helm, parts.DeckEnds[i]);
            LineRenderer? line = parts.Lines[i];
            if (line != null)
            {
                line.SetPosition(0, yoke);
                line.SetPosition(1, deck);
            }
            Transform? rod = parts.Rods[i];
            if (rod != null)
                rod.localPosition = yoke;
        }

        // A point of a part (the blade, or the deck tiller under the linkage's holder, both in the rudder's frame) in the
        // rudder's frame.
        private static Vector3 On(Transform? part, Vector3 local) =>
            part == null ? local : part.localPosition + part.localRotation * local;

        // A point of what the hand holds (the deck tiller, turning as the blade does, or the blade's own tiller) with the
        // blade turned so, in the rudder's frame.
        private Vector3 At(Quaternion turn, Vector3 local)
        {
            Transform? part = parts.Helm != null ? parts.Helm : blade;
            return part == null ? local : part.localPosition + turn * local;
        }
    }
}
