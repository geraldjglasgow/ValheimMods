using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One piece of a launched ship's rig that shots can damage (<see cref="KwRigDamage"/>), from its blueprint: a mast with
    /// its sails, a bowsprit, a post cap or a rudder; where it stands in the ship's frame, and its spars
    /// (<see cref="KwRigSpar"/>): a mast's column, each square sail's yard, a sail along the ship's boom and gaff; a
    /// bowsprit's spar; a cap's upright. Where each sail is fastened to its spars: a square sail's head lashed to its yard
    /// (<see cref="Lashings"/>, the rings <see cref="KwSailShapes"/> draws), a sail along the ship's luff in hoops up the
    /// mast (<see cref="Hoops"/>) and its foot laced to the boom (<see cref="Lacing"/>). The same on every peer: plain data.
    /// </summary>
    public sealed class KwRigPiece
    {
        public const string MastKind = "mast", SpritKind = "bowsprit", CapKind = "cap", RudderKind = "rudder";

        public KwRigPiece(int index, string kind, KwBpPiece piece, KwRigSpar main)
        {
            (Index, Kind, Piece, Main) = (index, kind, piece, main);
        }

        public int Index { get; }
        public string Kind { get; }
        public KwBpPiece Piece { get; }

        /// <summary>The spar the piece stands or falls by: a mast's column, a bowsprit's spar, a cap's upright (a rudder's: none).</summary>
        public KwRigSpar Main { get; }

        public KwMastSpec? Mast { get; set; }
        public List<KwSailLayout> Sails { get; } = new List<KwSailLayout>();
        public KwCapSpec? Cap { get; set; }
        public KwRudderTier? Tier { get; set; }
        public KwRudderFit Fit { get; set; }

        /// <summary>The cost of putting it back once fallen (<see cref="KwRigCost"/>, <see cref="KwCapSpec.Cost"/>).</summary>
        public List<(string Item, int Amount)> Cost { get; set; } = new List<(string Item, int Amount)>();

        /// <summary>For a bowsprit: the mast its jibs' stay comes from (its index), or -1.</summary>
        public int Foremast { get; set; } = -1;

        /// <summary>Which way its sails pull along the ship: 1 forward, -1 aft, 0 across (as <see cref="KwSailMix"/> counts them).</summary>
        public float Way { get; set; }

        /// <summary>For a bowsprit: its jibs' power (area × cloth drive) and the share of it set at half sail.</summary>
        public float JibPower { get; set; }
        public float JibHalf { get; set; }

        public bool IsMast => Kind == MastKind;

        /// <summary>A kit point of the piece in the ship's frame.</summary>
        public Vector3 ToShip(Vector3 kit) => Piece.Position + Piece.Rotation * KwMesh.ToUnity(kit);

        /// <summary>A point of the ship's frame in the piece's kit axes.</summary>
        public Vector3 ToKit(Vector3 ship) => KwMesh.ToKit(Back * (ship - Piece.Position));

        /// <summary>The piece's turn undone (plain arithmetic: its conjugate).</summary>
        public Quaternion Back => new Quaternion(-Piece.Rotation.x, -Piece.Rotation.y, -Piece.Rotation.z, Piece.Rotation.w);

        /// <summary>The piece's place in the ship's frame.</summary>
        public Matrix4x4 Matrix => Matrix4x4.TRS(Piece.Position, Piece.Rotation, Vector3.one);

        /// <summary>Its spar by part number (<see cref="KwRigSpar"/>), a gaff as set at full sail; null when it has none such.</summary>
        public KwRigSpar? Spar(int part)
        {
            if (part == KwRigSpar.Column)
                return Main;
            int i = part % 100, kind = part - i;
            if (i < 0 || i >= Sails.Count)
                return null;
            KwSailLayout s = Sails[i];
            if (kind == 0)
                return s.Square ? Yard(s, i) : (KwRigSpar?)null;
            if (s.Square)
                return null;
            return kind == KwRigSpar.Boom ? BoomOf(s, i) : kind == KwRigSpar.Gaff && s.Spec.Shape == 2 ? GaffOf(s, i, 1f) : (KwRigSpar?)null;
        }

        /// <summary>A square sail's yard: across the ship in front of the mast, thickest at the mast.</summary>
        public static KwRigSpar Yard(KwSailLayout s, int i)
        {
            Vector3 mid = new Vector3(s.YardX, 0f, s.At), half = new Vector3(0f, s.Width / 2f + 0.25f, 0f);
            return new KwRigSpar(i, mid - half, mid + half, s.Spar * 0.55f, s.Spar * 0.55f, s.Spar);
        }

        /// <summary>A sail along the ship's boom, aft of the mast (about the mast's axis, as it swings).</summary>
        public static KwRigSpar BoomOf(KwSailLayout s, int i) =>
            new KwRigSpar(KwRigSpar.Boom + i, new Vector3(-s.Radius, 0f, s.At), new Vector3(-s.Width - 0.2f, 0f, s.At), s.Spar, s.Spar * 0.7f);

        /// <summary>A gaff sail's gaff at hoist f, from its throat past its peak as it is drawn (<see cref="KwSailShapes.GaffSpar"/>).</summary>
        public static KwRigSpar GaffOf(KwSailLayout s, int i, float f)
        {
            (Vector3 head, Vector3 peak) = KwSailCanvas.Top(s, f);
            Vector3 a = s.ClothOrigin + head, way = (peak - head).normalized;
            return new KwRigSpar(KwRigSpar.Gaff + i, a, a + way * ((peak - head).magnitude + 0.2f), s.Spar * 0.9f, s.Spar * 0.6f);
        }

        /// <summary>A square sail's lashings: where each ring binds its head to its yard, along the yard.</summary>
        public static List<float> Lashings(KwSailLayout s)
        {
            List<float> at = new List<float>();
            int count = Mathf.Max(3, Mathf.RoundToInt(s.Width / 0.45f));
            for (int k = 0; k < count; k++)
                at.Add(0.25f + (k + 0.5f) * s.Width / count);
            return at;
        }

        /// <summary>A sail along the ship's hoops up the mast, along the mast's column (from its foot 0.1 m under the deck).</summary>
        public static List<float> Hoops(KwSailLayout s)
        {
            List<float> at = new List<float>();
            for (float z = s.At + 0.35f; z < s.At + KwSailShapes.Luff(s) - 0.05f; z += 0.6f)
                at.Add(z + 0.1f);
            return at;
        }

        /// <summary>The lacing binding a sail along the ship's foot to its boom, along the boom.</summary>
        public static List<float> Lacing(KwSailLayout s)
        {
            List<float> at = new List<float>();
            for (float x = -0.45f; x > -s.Width; x -= 0.5f)
                at.Add(-x - s.Radius);
            return at;
        }

        /// <summary>What the piece is called in a message.</summary>
        public string Called => Kind switch
        {
            MastKind => "The mast",
            SpritKind => "The bowsprit",
            CapKind => "The post cap",
            _ => "The rudder",
        };
    }

    /// <summary>
    /// A launched ship's rig pieces that shots can damage (<see cref="KwRigPiece"/>), read from its blueprint once: every
    /// mast, bowsprit, post cap and rudder, by its index in the blueprint.
    /// </summary>
    public sealed class KwRigParts
    {
        private readonly Dictionary<int, KwRigPiece> byIndex = new Dictionary<int, KwRigPiece>();

        public List<KwRigPiece> Pieces { get; } = new List<KwRigPiece>();

        public KwRigPiece? Get(int index) => byIndex.TryGetValue(index, out KwRigPiece piece) ? piece : null;

        public static KwRigParts Of(KwBlueprint blueprint)
        {
            KwRigParts parts = new KwRigParts();
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwRigPiece? piece = KwRigPieceRead.Of(blueprint, i);
                if (piece == null)
                    continue;
                parts.Pieces.Add(piece);
                parts.byIndex[i] = piece;
            }
            return parts;
        }
    }
}
