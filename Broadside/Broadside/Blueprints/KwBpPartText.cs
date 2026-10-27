using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A blueprint piece's saved part text made again to new sizes for <see cref="KwBpReshape"/>, each the way its tool
    /// writes it, and refused (null, and the reason) when the new size is past what that tool allows: a keel's length,
    /// rise and post (<see cref="KwKeelSpec"/>), a rib's frame (<see cref="KwFrame"/>), a mast's height and its sails'
    /// sizes (<see cref="KwMastSpec"/>, <see cref="KwSailSpec"/>, its platforms' heights with it), a bowsprit's length
    /// (<see cref="KwBowsprit"/>), a ladder's length (<see cref="KwLadder"/>), a Post cap tool cap's base (<see cref="KwCapSpec"/>); planking, decks and railings in
    /// <see cref="KwBpSpanText"/>, cabins (old castles too) in <see cref="KwBpHouseText"/>. A keel's and the planking's
    /// thickness are timber sizes and stay. A text that does not read is left as it is.
    /// </summary>
    internal static class KwBpPartText
    {
        public static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();
        private static readonly int LayHash = KwRudderLay.Key.GetStableHashCode();

        /// <summary>A generated part's tool ("planking", "keel", ...); empty for any other piece.</summary>
        public static string Tool(KwBpPiece piece) =>
            piece.Prefab.StartsWith(KwToolPieces.PartPrefix, StringComparison.Ordinal) ? piece.Prefab.Substring(KwToolPieces.PartPrefix.Length) : "";

        public static bool IsSpan(string tool) => tool == "planking" || tool == "deck" || tool == "rail";

        /// <summary>
        /// The piece at a new place, with a new saved text (null keeps its own); a rudder's measured lay and steering gear
        /// dropped when asked (its owner measures them again).
        /// </summary>
        public static KwBpPiece Moved(KwBpPiece piece, Vector3 at, string? text, bool dropLay = false)
        {
            List<KwBpValue> data = new List<KwBpValue>(piece.Data.Count);
            foreach (KwBpValue value in piece.Data)
            {
                if (dropLay && (value.Hash == LayHash || value.Hash == KwRudderGear.Hash))
                    continue;
                data.Add(text != null && value.Hash == SpanHash && value.Value is string ? new KwBpValue(SpanHash, text) : value);
            }
            return new KwBpPiece(piece.Prefab, at, piece.Rotation, data);
        }

        /// <summary>A straight part made to a new length: a span run or taper, or a straight keel.</summary>
        public static string? Lengthened(string tool, string saved, float length, out string? error)
        {
            error = null;
            if (IsSpan(tool))
                return KwBpSpanText.Lengthened(saved, length, out error);
            if (tool != "keel" || !KwKeelSpec.TryParse(saved, out KwKeelSpec keel))
                return saved;
            return Keel(new KwKeelSpec(length, keel.Rise, keel.Curve, keel.Post, keel.PostAbove, keel.Body), out error);
        }

        /// <summary>A part with its beam times w: span frames, rib frames, cabins; any other part as it is.</summary>
        public static string? Widened(string tool, string saved, float w, out string? error)
        {
            error = null;
            if (IsSpan(tool))
                return KwBpSpanText.Widened(saved, w, out error);
            if (KwBpHouseText.Is(tool))
                return KwBpHouseText.Widened(tool, saved, w, out error);
            return tool == "rib" ? Rib(saved, w, 1f, out error) : saved;
        }

        /// <summary>A part with every size times s (timber thicknesses kept).</summary>
        public static string? Scaled(string tool, string saved, float s, out string? error)
        {
            error = null;
            if (IsSpan(tool))
                return KwBpSpanText.Scaled(saved, s, out error);
            if (tool == "keel" && KwKeelSpec.TryParse(saved, out KwKeelSpec keel))
                return Keel(new KwKeelSpec(keel.Length * s, keel.Rise * s, keel.Curve, keel.Post, Post(keel, s), keel.Body), out error);
            if (tool == "rib")
                return Rib(saved, s, s, out error);
            if (tool == KwSailTool.Tool)
                return Mast(saved, s, out error);
            if (KwBpHouseText.Is(tool))
                return KwBpHouseText.Scaled(tool, saved, s, out error);
            if (tool == KwBowsprit.Tool && KwBowsprit.TryParse(saved, out KwBowsprit sprit))
                return Within(sprit.Length * s, KwBowsprit.MinLength, KwBowsprit.MaxLength, "a bowsprit would be {0} m long; bowsprits are {1} to {2} m",
                    out error) ? sprit.With(sprit.Length * s, sprit.Rise, sprit.Jibs, sprit.Cloth).ToString() : null;
            if (tool == KwLadderTool.Tool && KwLadder.TryLength(saved, out float length))
                return Within(length * s, KwLadder.MinLength, KwLadder.MaxLength, "a ladder would be {0} m long; ladders are {1} to {2} m", out error)
                    ? KwLadder.Save(length * s) : null;
            if (tool == KwCapSpec.Tool && KwCapSpec.TryParse(saved, out KwCapSpec cap))
                return cap.Scaled(s, out error);
            return saved;
        }

        /// <summary>A frame of that beam and height factor on the same keel; null and the reason past a rib's limits.</summary>
        public static KwFrame? Frame(KwFrame frame, float beam, float height, out string? error)
        {
            float b = Round(frame.Beam * beam), h = Round(frame.Height * height);
            if (!Within(b, KwFrame.MinBeam, KwFrame.MaxBeam, "the hull would be {0} m wide; ribs and planking are {1} to {2} m", out error)
                || !Within(h, KwFrame.MinHeight, KwFrame.MaxHeight, "the hull would be {0} m tall; ribs and planking are {1} to {2} m", out error))
                return null;
            return beam == 1f && height == 1f ? frame : new KwFrame(b, h, 0, frame.KeelHalf);
        }

        /// <summary>Within a tool's limits (a hair of rounding allowed); else the reason, the value and limits put in.</summary>
        public static bool Within(float value, float least, float most, string reason, out string? error)
        {
            bool within = value >= least - 0.005f && value <= most + 0.005f;
            error = within ? null : string.Format(reason, Num(value), Num(least), Num(most));
            return within;
        }

        public static string Num(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        private static float Round(float value) => Mathf.Round(value * 100f) / 100f;

        // A post's height scaled; a keel with no post keeps a height a post could have, unused.
        private static float Post(KwKeelSpec keel, float s) =>
            keel.Post ? keel.PostAbove * s : Mathf.Clamp(keel.PostAbove * s, KwKeelSpec.MinPost, KwKeelSpec.MaxPost);

        private static string? Keel(KwKeelSpec keel, out string? error)
        {
            if (!Within(keel.Length, KwKeelSpec.MinLength, KwKeelSpec.MaxLength, "a keel would be {0} m long; keels are {1} to {2} m", out error)
                || !Within(keel.Rise, 0f, KwKeelSpec.MaxRise, "a keel end would climb {0} m; at most {2} m", out error)
                || !Within(keel.PostAbove, KwKeelSpec.MinPost, KwKeelSpec.MaxPost, "a keel's post would stand {0} m; posts are {1} to {2} m", out error))
                return null;
            return keel.ToString();
        }

        private static string? Rib(string saved, float beam, float height, out string? error)
        {
            error = null;
            if (!KwRibs.TryRead(saved, out string family, out KwFrame frame))
                return saved;
            KwFrame? made = Frame(frame, beam, height, out error);
            return made != null ? KwRibs.Save(family, made.Value) : null;
        }

        // A mast and every one of its sails times s, the sails placed on it again (as the sail tool places them).
        private static string? Mast(string saved, float s, out string? error)
        {
            error = null;
            if (!KwMastSpec.TryParse(saved, out KwMastSpec mast))
                return saved;
            if (!Within(mast.Mast * s, KwSailSpec.MinMast, KwSailSpec.MaxMast, "a mast would be {0} m tall; masts are {1} to {2} m", out error))
                return null;
            List<KwSailSpec> sails = new List<KwSailSpec>();
            foreach (KwSailSpec sail in mast.Sails)
            {
                if (!Within(sail.Width * s, KwSailSpec.MinSize, KwSailSpec.MaxSize, "a sail would be {0} m wide; sails are {1} to {2} m", out error)
                    || !Within(sail.Height * s, KwSailSpec.Least, KwSailSpec.MaxSize, "a sail would be {0} m tall; sails are {1} to {2} m", out error))
                    return null;
                sails.Add(sail.With(sail.Shape, sail.Width * s, sail.Height * s));
            }
            KwMastTops tops = mast.Tops.With(nestAt: mast.Tops.NestAt * s, topAt: mast.Tops.TopAt * s);
            return KwMastSpec.Fit(mast.Mast * s, sails).With(tops).ToString();
        }

        /// <summary>
        /// The last check: a part text the reshape wrote that its tool would not read (null when there is none). A text the
        /// saved ship already had is never counted, read or not.
        /// </summary>
        public static string? Unreadable(KwBlueprint saved, KwBlueprint shaped)
        {
            HashSet<string> had = new HashSet<string>();
            foreach (KwBpPiece piece in saved.Pieces)
                had.Add(KwShipParts.Saved(piece));
            foreach (KwBpPiece piece in shaped.Pieces)
            {
                string tool = Tool(piece), text = KwShipParts.Saved(piece);
                if (tool.Length == 0 || had.Contains(text) || Reads(tool, text))
                    continue;
                Log.Warn($"Broadside reshape made a {tool} text that does not read: {text}");
                return $"a {tool} part could not be made to that size";
            }
            return null;
        }

        private static bool Reads(string tool, string text)
        {
            if (IsSpan(tool))
                return KwSpan.TryParse(text, out _) || KwRoofRail.TryParse(text, out _);
            return tool switch
            {
                "keel" => KwKeelSpec.TryParse(text, out _),
                "rib" => KwRibs.TryRead(text, out _, out _),
                KwSailTool.Tool => KwMastSpec.TryParse(text, out _),
                KwLadderTool.Tool => KwLadder.TryLength(text, out _),
                KwBowsprit.Tool => KwBowsprit.TryParse(text, out _),
                KwCapSpec.Tool => KwCapSpec.TryParse(text, out _),
                KwHouses.Castle or KwHouses.Cabin => KwCabinSpec.TryParse(text, out _),
                _ => true,
            };
        }
    }
}
