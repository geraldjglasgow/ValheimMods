using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cart's bed in the cart's own frame: between its four walls (x across, z along, +z toward the shafts) and the top
    /// of its floor, measured once on the first cart that wakes from its own colliders (the game's cart: 1.41 m across,
    /// 1.94 m along, the floor 0.52 m over the cart's origin), and split into four cells, two across and two along. Bit
    /// <c>row * 2 + column</c> is a cell, row 0 at the back, column 0 on the left; a crate takes one cell, a row (across the
    /// cart), a column (along it) or the whole bed, whichever is the smallest its footprint fits in, turned square to the
    /// cart. With the game's cart and Broadside's crates: a small crate (0.90 x 0.62 m, slatted 0.75 x 0.60) takes a cell,
    /// a large or cargo crate (1.39 x 0.91 m, slatted 1.25 x 0.95) a row, turned across the cart: four small, two large,
    /// or one large and two small.
    /// </summary>
    public readonly struct KwCartShape
    {
        /// <summary>The game's cart as measured on 2026-11-03, used when the measuring finds no bed.</summary>
        public static readonly KwCartShape Game = new KwCartShape(-0.703f, 0.703f, -0.9865f, 0.9525f, 0.5235f);

        /// <summary>The shapes a crate may take, smallest first: (cells across, cells along, the masks of each place).</summary>
        public static readonly (int Across, int Along, int[] Masks)[] Shapes =
        {
            (1, 1, new[] { 1, 2, 4, 8 }), (2, 1, new[] { 3, 12 }), (1, 2, new[] { 5, 10 }), (2, 2, new[] { 15 }),
        };

        private const float Room = 0.02f, Over = 3f, Hand = 0.25f;

        public readonly float MinX, MaxX, MinZ, MaxZ, Floor;

        public KwCartShape(float minX, float maxX, float minZ, float maxZ, float floor) =>
            (MinX, MaxX, MinZ, MaxZ, Floor) = (minX, maxX, minZ, maxZ, floor);

        public float Cell(bool across) => across ? (MaxX - MinX) * 0.5f : (MaxZ - MinZ) * 0.5f;

        /// <summary>
        /// The turn a footprint (half sizes along the crate's own x and z) takes in a shape: 0 (its x across the cart), 90 (its x
        /// along it), or -1 when it fits neither way.
        /// </summary>
        public float Fit(int across, int along, Vector2 half)
        {
            float wide = across * Cell(true) + Room, deep = along * Cell(false) + Room;
            if (2f * half.x <= wide && 2f * half.y <= deep)
                return 0f;
            return 2f * half.y <= wide && 2f * half.x <= deep ? 90f : -1f;
        }

        /// <summary>The middle of a mask's cells on the floor, in the cart's frame.</summary>
        public Vector3 Middle(int mask)
        {
            int lowColumn = (mask & 5) != 0 ? 0 : 1, highColumn = (mask & 10) != 0 ? 1 : 0;
            int lowRow = (mask & 3) != 0 ? 0 : 1, highRow = (mask & 12) != 0 ? 1 : 0;
            float x = MinX + Cell(true) * (lowColumn + highColumn + 1) * 0.5f;
            float z = MinZ + Cell(false) * (lowRow + highRow + 1) * 0.5f;
            return new Vector3(x, Floor, z);
        }

        /// <summary>
        /// The bed of a cart from its own solid colliders (not its storage's box, nor its wheels): a ray down the middle finds
        /// the floor, four rays out from just over it find the walls; <see cref="Game"/> when that finds no bed, or one more
        /// than 0.25 m off it anywhere (a game update that changed the cart a little is followed; a measure gone wrong is not).
        /// </summary>
        public static KwCartShape Measure(Transform cart, Rigidbody body)
        {
            List<Collider> own = Solid(cart, body);
            float? floor = Cast(cart, own, new Vector3(0f, Over, 0f), Vector3.down);
            if (!floor.HasValue)
                return Game;
            float y = Over - floor.Value, side = y + 0.15f;
            float? right = Cast(cart, own, new Vector3(0f, side, 0f), Vector3.right), left = Cast(cart, own, new Vector3(0f, side, 0f), Vector3.left);
            float? front = Cast(cart, own, new Vector3(0f, side, 0f), Vector3.forward), back = Cast(cart, own, new Vector3(0f, side, 0f), Vector3.back);
            if (!right.HasValue || !left.HasValue || !front.HasValue || !back.HasValue)
                return Game;
            KwCartShape found = new KwCartShape(-left.Value, right.Value, -back.Value, front.Value, y);
            return found.Near(Game) ? found : Game;
        }

        // Within a hand's breadth of another bed everywhere (a measure gone wrong falls back to the game's known cart).
        private bool Near(KwCartShape other) =>
            Mathf.Abs(MinX - other.MinX) < Hand && Mathf.Abs(MaxX - other.MaxX) < Hand && Mathf.Abs(MinZ - other.MinZ) < Hand
            && Mathf.Abs(MaxZ - other.MaxZ) < Hand && Mathf.Abs(Floor - other.Floor) < Hand;

        private static List<Collider> Solid(Transform cart, Rigidbody body)
        {
            List<Collider> found = new List<Collider>();
            foreach (Collider collider in cart.GetComponentsInChildren<Collider>())
            {
                if (!collider.isTrigger && collider.enabled && collider.attachedRigidbody == body && collider.GetComponent<Container>() == null)
                    found.Add(collider);
            }
            return found;
        }

        // The nearest of the cart's own colliders along a ray from a point in its frame (a way in its frame), or null.
        private static float? Cast(Transform cart, List<Collider> own, Vector3 from, Vector3 way)
        {
            Ray ray = new Ray(cart.TransformPoint(from), cart.TransformDirection(way));
            float? best = null;
            foreach (Collider collider in own)
            {
                if (collider.Raycast(ray, out RaycastHit hit, Over) && (!best.HasValue || hit.distance < best.Value))
                    best = hit.distance;
            }
            return best;
        }
    }
}
