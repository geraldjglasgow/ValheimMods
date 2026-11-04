using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Whether a piece rests on a dock section's deck (<see cref="KwPierLadderSupport"/>): one of its boxes, as the game's
    /// support check reads them (<c>WearNTear.m_bounds</c>: each collider's box grown 0.15 m all round), meets a built
    /// section's deck box (from the cap beams' underside to its curb's top, across its whole width and length). The game
    /// finds the section the same way, but then measures the support it gives from the section's origin, at its start, up
    /// to 12 m away, so a piece far along a deck (more than about 7 m in wood, 9 m in core wood) had too little and broke;
    /// resting on the deck it has its own material's full support instead, as on the ground. A dock section itself, and
    /// anything not near a loaded dock, is not asked (a level distance first, then the boxes). Plain maths.
    /// </summary>
    public static class KwPierRest
    {
        private const float Near = 3f, Below = 25f, Above = 30f;

        /// <summary>Whether the piece's boxes meet the deck of a built dock section loaded here.</summary>
        public static bool On(WearNTear wear)
        {
            if (KwPier.Loaded.Count == 0)
                return false;
            Vector3 at = wear.transform.position;
            foreach (KwPier pier in KwPier.Loaded)
            {
                KwPierSpec? spec = pier != null ? pier.Spec : null;
                if (pier == null || spec == null || !Close(pier, at) || wear.gameObject == pier.gameObject)
                    continue;
                if (Meets(wear, pier, spec))
                    return true;
            }
            return false;
        }

        // Within a few metres of the deck, level (from its middle, no call into the engine), and within the height a piece on
        // it or hung from it could reach.
        private static bool Close(KwPier pier, Vector3 at)
        {
            Vector3 d = at - pier.Middle;
            float reach = pier.Reach + Near;
            return d.x * d.x + d.z * d.z <= reach * reach && d.y >= -Below && d.y <= Above;
        }

        // Any of the piece's boxes overlapping the deck's box, both seen in the section's own frame.
        private static bool Meets(WearNTear wear, KwPier pier, KwPierSpec spec)
        {
            if (wear.GetComponent<KwPier>() != null)
                return false;
            if (wear.m_colliders == null)
                wear.SetupColliders();
            KwPierKind kind = pier.Material;
            Bounds deck = new Bounds();
            deck.SetMinMax(new Vector3(-spec.Width * 0.5f, kind.CapFoot, 0f), new Vector3(spec.Width * 0.5f, kind.Curb.y, spec.Length));
            Matrix4x4 toDeck = pier.transform.worldToLocalMatrix;
            foreach (WearNTear.BoundData bound in wear.m_bounds)
            {
                if (deck.Intersects(Local(toDeck, bound)))
                    return true;
            }
            return false;
        }

        // A turned box (centre, half size, turn) as the box round it in the section's frame.
        private static Bounds Local(Matrix4x4 toDeck, WearNTear.BoundData bound)
        {
            Matrix4x4 turn = toDeck * Matrix4x4.Rotate(bound.m_rot);
            Vector3 half = bound.m_size, extent = Vector3.zero;
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 column = turn.GetColumn(axis);
                extent += new Vector3(Mathf.Abs(column.x), Mathf.Abs(column.y), Mathf.Abs(column.z)) * half[axis];
            }
            return new Bounds(toDeck.MultiplyPoint3x4(bound.m_pos), extent * 2f);
        }
    }
}
