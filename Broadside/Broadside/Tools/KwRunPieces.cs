using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a run between two keel ends (<see cref="KwKeelRun"/>) finds along its keel line: the ribs standing on it between
    /// the ends, in order from the first (only the lowest standing ones: a rib up a stem stands higher, and planking joins
    /// ribs at one height, so it is passed over as the part from that end follows the curve past it), and the parts of the
    /// tool already lying along it. Looked up at once for a click; the preview, which asks every frame, reuses the last
    /// look for the same two ends and tool for half a second. Local to the clicking player.
    /// </summary>
    public sealed class KwRunPieces
    {
        // beside the line; one rib height; parallel; clear of an end; a deck's height apart; how long a look is reused;
        // how high over the keel line a rib up a stem may stand
        private const float Beside = 0.3f, Level = 0.05f, Square = 0.98f, Touch = 0.05f, Headroom = 0.5f, Fresh = 0.5f, Climb = 9f;
        private static readonly List<Piece> Found = new List<Piece>();
        private static KwRunPieces? last;

        private readonly Piece pieceA, pieceB;
        private readonly string tool;
        private readonly float time;

        private KwRunPieces(KwRunLine line, string tool) => (pieceA, pieceB, this.tool, time) = (line.PieceA, line.PieceB, tool, Time.time);

        /// <summary>The ribs standing on the line between its ends, the lowest standing only, in order from the first end.</summary>
        public List<Piece> Ribs { get; } = new List<Piece>();

        /// <summary>
        /// The tool's parts already lying along the line (either way): where each starts, the way it runs, its length and,
        /// for a deck, its height over where it starts at any x along it (null for any other tool's part).
        /// </summary>
        public List<(Vector3 At, Vector3 Way, float Length, Func<float, float>? Deck)> Parts { get; } = new List<(Vector3, Vector3, float, Func<float, float>?)>();

        /// <summary>What is along a line for a tool; with reuse, the last look when it was for the same ends and tool, and fresh.</summary>
        public static KwRunPieces Of(KwRunLine line, string tool, bool reuse)
        {
            KwRunPieces? seen = last;
            if (reuse && seen != null && seen.pieceA == line.PieceA && seen.pieceB == line.PieceB && seen.tool == tool && Time.time - seen.time < Fresh)
                return seen;
            KwRunPieces look = new KwRunPieces(line, tool);
            look.Look(line);
            last = look;
            return look;
        }

        private void Look(KwRunLine line)
        {
            string prefab = KwToolPieces.PartPrefix + tool;
            Found.Clear();
            Piece.GetAllPiecesInRadius(line.Start + line.Axis * (line.Length / 2f), line.Length / 2f + Climb, Found);
            foreach (Piece piece in Found)
            {
                if (piece == null || line.Aside(piece.transform.position) > Beside)
                    continue;
                float along = line.Along(piece.transform.position);
                if (Utils.GetPrefabName(piece.gameObject) == prefab)
                    Part(line, piece);
                else if (along > Touch && along < line.Length - Touch && KwTools.IsRib(piece))
                    Ribs.Add(piece);
            }
            Found.Clear();
            Lowest(line);
        }

        // A part of the tool lying along the line, either way, with its span.
        private void Part(KwRunLine line, Piece piece)
        {
            ZDO? zdo = piece.GetComponent<ZNetView>()?.GetZDO();
            Vector3 way = piece.transform.forward;
            if (zdo != null && Mathf.Abs(Vector3.Dot(way, line.Axis)) >= Square && KwSpan.TryParse(zdo.GetString(KwGenerated.Key), out KwSpan span))
                Parts.Add((piece.transform.position, way, span.Length, tool == "deck" ? KwMesh.DeckHeight(span.Def("deck")) : null));
        }

        // Only the ribs standing lowest (within 5 cm of the lowest), in order along the line.
        private void Lowest(KwRunLine line)
        {
            float lowest = float.MaxValue;
            Ribs.ForEach(rib => lowest = Mathf.Min(lowest, rib.transform.position.y));
            Ribs.RemoveAll(rib => rib.transform.position.y > lowest + Level);
            Ribs.Sort((p, q) => line.Along(p.transform.position).CompareTo(line.Along(q.transform.position)));
        }

        /// <summary>
        /// Whether a part of the tool already lies where a planned one would: along its line, either way, over its middle;
        /// for a deck, only one at about the planned deck's height there (a deck lower in the hold leaves room for another).
        /// </summary>
        public bool Has(KwPlan plan)
        {
            Vector3 mid = plan.Position + plan.Direction * (plan.Span.Length / 2f);
            float height = tool == "deck" ? plan.Position.y + KwMesh.DeckHeight(plan.Span.Def("deck"))(plan.Span.Length / 2f) : 0f;
            foreach ((Vector3 at, Vector3 way, float length, Func<float, float>? deck) in Parts)
            {
                Vector3 off = mid - at;
                float along = Vector3.Dot(off, way);
                Vector3 aside = off - way * along;
                bool over = Mathf.Abs(Vector3.Dot(way, plan.Direction)) >= Square && new Vector2(aside.x, aside.z).magnitude < Beside
                    && along > Touch && along < length - Touch;
                if (over && (deck == null || Mathf.Abs(at.y + deck(along) - height) < Headroom))
                    return true;
            }
            return false;
        }
    }
}
