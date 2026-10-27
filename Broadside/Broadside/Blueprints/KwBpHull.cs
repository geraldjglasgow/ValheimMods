using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship's hull measured from its blueprint, in the blueprint's frame (Unity axes, z the way the bow points): the
    /// keel line's height (the ribs' feet, where every hull part starts), how far it reaches fore and aft, its widest
    /// beam and its depth from the keel line to the highest sheer. From the generated hull parts' spans; a blueprint with
    /// none (a bare keel) is measured from its pieces' places, a small boat's size assumed. It tells the ship's pieces from
    /// the ones left lying on the dock (<see cref="KwBpStrays"/>), and sizes a ship built from the hammer: its ghost on the
    /// water (<see cref="KwBpWater"/>), its float (<see cref="KwShipHydro"/>) and its fittings.
    /// </summary>
    public sealed class KwBpHull
    {
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();

        public float Keel { get; private set; } = float.MaxValue;
        public float Fore { get; private set; } = float.MinValue;
        public float Aft { get; private set; } = float.MaxValue;
        public float Beam { get; private set; }
        public float Depth { get; private set; }

        /// <summary>The highest deck's height over the keel line (a third of the depth with no deck: the bilge's floor).</summary>
        public float DeckTop { get; private set; }

        /// <summary>The lowest deck's height over the keel line (very high with no deck).</summary>
        public float DeckLow { get; private set; } = float.MaxValue;

        public float Length => Mathf.Max(Fore - Aft, 1f);

        /// <summary>The widest section's half outline (out from the centre line, up from the keel line), keel to sheer.</summary>
        public Vector2[] Section { get; private set; } = { new Vector2(0.5f, 0f), new Vector2(0.75f, 0.8f) };

        /// <summary>Where the widest section stands along the ship, in the blueprint's frame.</summary>
        public float SectionZ { get; private set; }

        /// <summary>The hull's depth at its widest section, keel line to sheer: its depth amidships, without the raised ends that <see cref="Depth"/> takes in.</summary>
        public float Midship => Mathf.Max(Section[Section.Length - 1].y, 0.6f);

        /// <summary>How far out from the centre line the widest section's side is at a height over the keel line.</summary>
        public float SideAt(float height)
        {
            for (int i = 0; i + 1 < Section.Length; i++)
            {
                if (height <= Section[i + 1].y)
                    return Mathf.Lerp(Section[i].x, Section[i + 1].x, Mathf.InverseLerp(Section[i].y, Section[i + 1].y, height));
            }
            return Section[Section.Length - 1].x;
        }

        /// <summary>The ship's own origin in the blueprint's frame: on the keel line, on the centre line, halfway along.</summary>
        public Vector3 Origin => new Vector3(0f, Keel, (Fore + Aft) / 2f);

        public static KwBpHull Measure(KwBlueprint blueprint)
        {
            KwBpHull hull = new KwBpHull();
            foreach ((KwBpPiece piece, _, KwSpan span) in Parts(blueprint))
                hull.Take(piece, span);
            if (hull.Keel == float.MaxValue)
                hull.Guess(blueprint);
            hull.Beam = Mathf.Max(hull.Beam, 1f);
            hull.Depth = Mathf.Max(hull.Depth, 0.6f);
            if (hull.DeckTop <= 0f)
                hull.DeckTop = hull.Depth / 3f;
            return hull;
        }

        // A hull part (seated on the blueprint's keels): its keel line, its ends along the ship, and its sections' widths and sheers.
        private void Take(KwBpPiece piece, KwSpan span)
        {
            Keel = Mathf.Min(Keel, piece.Position.y + span.Level());
            if (piece.Prefab == KwToolPieces.PartPrefix + "deck")
                (DeckTop, DeckLow) = (Mathf.Max(DeckTop, Top(span.Def("deck"))), Mathf.Min(DeckLow, Low(span.Def("deck"))));
            Vector3 end = piece.Position + piece.Rotation * Vector3.forward * span.Length;
            Fore = Mathf.Max(Fore, Mathf.Max(piece.Position.z, end.z));
            Aft = Mathf.Min(Aft, Mathf.Min(piece.Position.z, end.z));
            foreach (Station station in span.Stations())
            {
                float wide = 2f * MaxWidth(station.Half);
                if (wide > Beam)
                    (Section, SectionZ) = (station.Half, piece.Position.z + (piece.Rotation * Vector3.forward).z * station.X);
                Beam = Mathf.Max(Beam, wide);
                Depth = Mathf.Max(Depth, station.Half[station.Half.Length - 1].y);
            }
        }

        private static float Top(KwDef deck) => float.IsNaN(deck.DeckZEnd) ? deck.DeckZ : Mathf.Max(deck.DeckZ, deck.DeckZEnd);

        private static float Low(KwDef deck) => float.IsNaN(deck.DeckZEnd) ? deck.DeckZ : Mathf.Min(deck.DeckZ, deck.DeckZEnd);

        private static float MaxWidth(Vector2[] half)
        {
            float widest = 0f;
            foreach (Vector2 p in half)
                widest = Mathf.Max(widest, p.x);
            return widest;
        }

        // No hull parts: the pieces' own places, a small boat's beam and depth.
        private void Guess(KwBlueprint blueprint)
        {
            Keel = 0f;
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                Fore = Mathf.Max(Fore, piece.Position.z);
                Aft = Mathf.Min(Aft, piece.Position.z);
                Keel = Mathf.Min(Keel, piece.Position.y);
            }
            if (Fore < Aft)
                Fore = Aft = 0f;
            Beam = 1.5f;
            SectionZ = (Fore + Aft) / 2f;
            Depth = 0.8f;
        }

        /// <summary>
        /// The generated hull parts of a blueprint, each with its tool and its span, seated on the blueprint's keels as on
        /// the dock (<see cref="KwSeat"/>).
        /// </summary>
        public static List<(KwBpPiece Piece, string Tool, KwSpan Span)> Parts(KwBlueprint blueprint)
        {
            List<(KwBpPiece, string, KwSpan)> parts = new List<(KwBpPiece, string, KwSpan)>();
            KwKeelLines keels = KwKeelLines.Of(blueprint);
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                string text = KwBpData.Text(piece.Data, SpanHash) ?? "";
                if (piece.Prefab.StartsWith(KwToolPieces.PartPrefix) && KwSpan.TryParse(text, out KwSpan span))
                    parts.Add((piece, piece.Prefab.Substring(KwToolPieces.PartPrefix.Length), new KwSeatAt(keels, piece.Position, piece.Rotation * Vector3.forward).Seated(span)));
            }
            return parts;
        }
    }
}
