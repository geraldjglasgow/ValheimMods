using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the ladder tool aims at (<see cref="KwLadderTool"/>): the piece under the crosshair and the point on it, as the
    /// game's placement ray sees it; a hatch hit, or the nearest open hatch whose opening (or rim) the aim passes through
    /// before what it hits below, with the point where it crosses the deck's top. A ladder hit stays the ladder (a click on
    /// it edits it, <see cref="KwLadderEdit"/>). Local to the player aiming.
    /// </summary>
    public readonly struct KwLadderAim
    {
        private const float Reach = 50f;

        public KwLadderAim(Piece? piece, KwHatchCover? hatch, Vector3 point)
        {
            Piece = piece;
            Hatch = hatch;
            Point = point;
        }

        public Piece? Piece { get; }
        public KwHatchCover? Hatch { get; }
        public Vector3 Point { get; }

        public static KwLadderAim Of(Player player)
        {
            Transform eye = GameCamera.instance.transform;
            Ray ray = new Ray(eye.position, eye.forward);
            bool hit = Physics.Raycast(ray, out RaycastHit h, Reach, player.m_placeRayMask, QueryTriggerInteraction.Ignore);
            Piece? piece = hit ? h.collider.GetComponentInParent<Piece>() : null;
            KwHatchCover? hatch = piece != null ? piece.GetComponent<KwHatchCover>() : null;
            if (hatch != null || (piece != null && KwLadderTool.IsLadder(piece)))
                return new KwLadderAim(piece, hatch, h.point);
            KwHatchCover? crossed = Crossed(ray, hit ? h.distance : Reach, out Vector3 at);
            if (crossed != null)
                return new KwLadderAim(crossed.GetComponent<Piece>(), crossed, at);
            return new KwLadderAim(piece, null, hit ? h.point : Vector3.zero);
        }

        // The nearest hatch whose opening and rim the aim crosses, at the deck's top, before it hits anything.
        private static KwHatchCover? Crossed(Ray ray, float before, out Vector3 at)
        {
            KwHatchCover? best = null;
            at = Vector3.zero;
            foreach (KwHatchCover hatch in KwHatchCover.All)
            {
                Transform t = hatch.transform;
                if (hatch == null || !new Plane(t.up, t.position).Raycast(ray, out float along) || along > before)
                    continue;
                Vector3 kit = KwMesh.ToKit(t.InverseTransformPoint(ray.GetPoint(along)));
                if (Mathf.Abs(kit.x) > hatch.Opening.x / 2f + KwHatch.Rim || Mathf.Abs(kit.y) > hatch.Opening.y / 2f + KwHatch.Rim)
                    continue;
                (best, at, before) = (hatch, ray.GetPoint(along), along);
            }
            return best;
        }
    }
}
