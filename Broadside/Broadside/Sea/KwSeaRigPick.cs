using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hammer's remove aimed at a launched ship's rig (<see cref="KwSeaRemove"/>): a mast (its column, yards, booms,
    /// gaffs or sails), a bowsprit or a post cap whose hit collider the aim met first is picked at 0 m; else, where the aim met
    /// the ship, the rig piece whose main spar's surface is nearest (a fallen one's stump), so a click on the deck at a mast's
    /// foot is a near miss and never breaks the ship up. Its removal makes the piece's entry a tombstone: its sails, ropes,
    /// jibs and marks go with it, and a bowsprit whose foremast it was loses its jibs (their cloth given back).
    /// </summary>
    public static class KwSeaRigPick
    {
        public static KwSeaPick? Of(KwSeaKind kind, KwSeaAim aim)
        {
            KwBlueprint blueprint = aim.Ship.Blueprint;
            int index = aim.Rig != null ? aim.Rig.Piece : -1;
            if (index >= 0 && index < blueprint.Pieces.Count && KwSeaRig.Is(blueprint.Pieces[index].Prefab))
                return new KwSeaPick(kind, index, -1, 0f, KwSeaRigWords.What(blueprint.Pieces[index].Prefab), Reached(aim, index));
            return aim.Hit ? Nearest(kind, aim.Ship, aim.Point) : null;
        }

        // The rig piece whose spar's surface comes nearest a point of the ship's frame, within the near miss's reach.
        private static KwSeaPick? Nearest(KwSeaKind kind, KwSeaShip ship, Vector3 point)
        {
            KwRigDamage? damage = ship.GetComponent<KwRigDamage>();
            KwSeaPick? best = null;
            foreach (KwRigPiece piece in damage != null ? damage.Parts.Pieces : KwRigParts.Of(ship.Blueprint).Pieces)
            {
                if (piece.Kind == KwRigPiece.RudderKind)
                    continue;
                float off = Off(piece, Standing(damage, piece), point);
                if (off < KwSeaRemove.Near && (best == null || off < best.Distance))
                    best = new KwSeaPick(kind, piece.Index, -1, off, KwSeaRigWords.What(piece.Piece.Prefab), point);
            }
            return best;
        }

        // The spar a piece stands by now: its main spar, or a fallen one's stump.
        private static KwRigSpar Standing(KwRigDamage? damage, KwRigPiece piece)
        {
            KwRigMark? fall = damage != null && damage.Condition != null ? damage.Condition.FallOf(piece.Index) : null;
            return fall.HasValue ? KwRigHitFind.Stump(piece.Main, fall.Value.A) : piece.Main;
        }

        /// <summary>
        /// Where a rig piece is reached from the aim's eye, for the building rules' reach: where the aim met its hit box, or its
        /// foot when that is nearer (a sail high up is changed or removed from the deck by its mast).
        /// </summary>
        public static Vector3 Reached(KwSeaAim aim, int index)
        {
            Vector3 foot = aim.Ship.Blueprint.Pieces[index].Position;
            if (aim.Rig == null || aim.Rig.Piece != index)
                return foot;
            return (aim.RigPoint - aim.From).sqrMagnitude < (foot - aim.From).sqrMagnitude ? aim.RigPoint : foot;
        }

        /// <summary>How far a point of the ship's frame is from a piece's spar's surface (0 on or in it).</summary>
        public static float Off(KwRigPiece piece, KwRigSpar spar, Vector3 point)
        {
            Vector3 kit = piece.ToKit(point);
            float s = Mathf.Clamp(spar.Along(kit), 0f, spar.Length);
            return Mathf.Max(0f, (kit - spar.At(s)).magnitude - spar.Radius(s));
        }

        /// <summary>The edit that removes a rig piece (a tombstone in its place).</summary>
        public static KwSeaEdit Removal(KwSeaShip ship, int index)
        {
            string what = KwSeaRigWords.What(ship.Blueprint.Pieces[index].Prefab);
            KwRigDamage? damage = ship.GetComponent<KwRigDamage>();
            bool down = damage != null && damage.Condition != null && damage.Condition.Fallen(index);
            return new KwSeaEdit(KwSeaRig.Kind, ship.Revision, KwSeaRigWords.Removed(what, down), new List<KwSeaPart> { KwSeaPart.Remove(index) });
        }
    }
}
