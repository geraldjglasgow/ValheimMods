using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a cabin goes (<see cref="KwCabinTool"/>): the deck sections and the hull along a keel line, read from the pieces
    /// loaded round the deck clicked first (<see cref="Site"/>), and the range two clicks choose on it (<see cref="Range"/>):
    /// both sections and every one between them on the same deck, which must run on without a gap and lie level; the hull's
    /// sections over it in the cabin's frame, blended at its ends; and each end at the hull's own end (pointed or a flat
    /// stern) or open deck. A cabin on a cabin stands on that cabin's roof (<see cref="OnRoof"/>). Pure but for reading the
    /// loaded pieces.
    /// </summary>
    public static class KwCabinHull
    {
        // How far round the deck pieces are read; a deck section's height within which two are the same deck; the most a
        // range's deck may rise; a gap the sections may leave; how near the hull's end a range's end is at it.
        private const float Reach = 60f, Level = 0.3f, Slope = 0.15f, Gap = 0.05f, AtEnd = 0.3f;
        private static readonly string Deck = KwToolPieces.PartPrefix + "deck", Planking = KwToolPieces.PartPrefix + "planking";

        /// <summary>The keel line through a deck part: its deck sections, the planking's sections along it, the bow's way; null and why without planking.</summary>
        public static KwCabinSite? Site(Piece deck, out string why)
        {
            Vector3 origin = deck.transform.position, forward = Level0(deck.transform.forward);
            List<Piece> near = new List<Piece>();
            Piece.GetAllPiecesInRadius(origin, Reach, near);
            List<KwDeckRun> decks = new List<KwDeckRun>();
            List<(float Along, Vector2[] Half, bool Flat)> sections = new List<(float, Vector2[], bool)>();
            foreach (Piece piece in near)
                Read(piece, origin, forward, decks, sections);
            sections.Sort((a, b) => a.Along.CompareTo(b.Along));
            why = "No planking along this deck's keel line to build a cabin in";
            if (sections.Count < 2 || !decks.Exists(d => d.Piece == deck))
                return null;
            float middle = (sections[0].Along + sections[sections.Count - 1].Along) / 2f;
            return new KwCabinSite(origin, forward, decks, sections, KwCabinBow.Way(near, origin, forward, middle));
        }

        private static Vector3 Level0(Vector3 way) => new Vector3(way.x, 0f, way.z).normalized;

        // One loaded piece read into the line's deck sections or the hull's sections, when it is a deck or planking on the line.
        private static void Read(Piece piece, Vector3 origin, Vector3 forward, List<KwDeckRun> decks, List<(float Along, Vector2[] Half, bool Flat)> sections)
        {
            string name = piece != null ? Utils.GetPrefabName(piece.gameObject) : "";
            ZNetView? view = name == Deck || name == Planking ? piece!.m_nview : null;
            ZDO? zdo = view != null && view.IsValid() && InLine(origin, forward, piece!.transform.position, piece.transform.forward) ? view.GetZDO() : null;
            if (zdo == null || !KwSpan.TryParse(zdo.GetString(KwGenerated.Key), out KwSpan span))
                return;
            Transform at = piece!.transform;
            span = KwSeatAt.World(at).Seated(span);
            float start = Vector3.Dot(at.position - origin, forward), sign = Mathf.Sign(Vector3.Dot(forward, at.forward)), lift = at.position.y - origin.y;
            if (name == Deck)
                decks.Add(DeckRun(piece, span, start, sign, lift));
            else
                Sections(span, start, sign, lift, sections);
        }

        /// <summary>Whether a part (its origin and way) stands on a keel line: running along it either way, its origin on it.</summary>
        public static bool InLine(Vector3 origin, Vector3 forward, Vector3 position, Vector3 way)
        {
            Vector3 off = position - origin, aside = off - forward * Vector3.Dot(off, forward);
            return Mathf.Abs(Vector3.Dot(forward, way)) > 0.98f && new Vector2(aside.x, aside.z).magnitude < 0.3f && Mathf.Abs(off.y) < 1f;
        }

        // A deck part's run along the line and its heights at its two ends over the line's keel (seated as it is built).
        private static KwDeckRun DeckRun(Piece piece, KwSpan span, float start, float sign, float lift)
        {
            float level = span.Seat?.Level ?? 0f, end = start + sign * span.Length;
            float a = KwSpan.Deck(span.DeckA, span.FrameA, level) + lift, b = KwSpan.Deck(span.DeckB, span.FrameB, level) + lift;
            return new KwDeckRun(piece, Mathf.Min(start, end), Mathf.Max(start, end), Mathf.Min(a, b), Mathf.Max(a, b));
        }

        // A planking part's sections along the line, lifted to the line's keel, the hull's flat sterns marked.
        private static void Sections(KwSpan span, float start, float sign, float lift, List<(float Along, Vector2[] Half, bool Flat)> sections)
        {
            KwDef def = span.Def("planking");
            for (int i = 0; i < def.Stations.Count; i++)
            {
                Station s = def.Stations[i];
                bool flat = (i == def.Stations.Count - 1 && def.FlatFore) || (i == 0 && def.FlatAft);
                sections.Add((start + sign * s.X, System.Array.ConvertAll(s.Half, p => p + new Vector2(0f, lift)), flat));
            }
        }

        /// <summary>
        /// The range from one deck section to another (or the same) on the site's line, its frame facing the way given
        /// (1 along the line, -1 back); null and why when they are not on one level deck that runs on between them.
        /// </summary>
        public static KwCabinRange? Range(KwCabinSite site, Piece first, Piece last, float way, out string why)
        {
            KwDeckRun? a = site.Find(first), c = site.Find(last);
            why = "That deck section is not on the first one's keel line";
            return a != null && c != null ? Between(site, a.Value, c.Value, way, out why) : null;
        }

        /// <summary>The range between two of the site's deck sections (the same one twice for one section); null and why as <see cref="Range"/>.</summary>
        public static KwCabinRange? Between(KwCabinSite site, KwDeckRun a, KwDeckRun c, float way, out string why)
        {
            why = "Those deck sections are on different decks; a cabin stands on one deck";
            if (Mathf.Abs(a.Middle - c.Middle) > Level)
                return null;
            float from = Mathf.Min(a.From, c.From), to = Mathf.Max(a.To, c.To), level = a.Middle;
            List<KwDeckRun> under = site.Decks.FindAll(d => Mathf.Abs(d.Middle - level) <= Level && d.To > from + Gap && d.From < to - Gap);
            why = Covered(under, from, to) ?? "";
            return why.Length > 0 ? null : Make(site, from, to, under.Min(d => d.Low), way);
        }

        // Why the deck sections under a range are not one level deck: a gap among them, or a slope.
        private static string? Covered(List<KwDeckRun> under, float from, float to)
        {
            under.Sort((a, b) => a.From.CompareTo(b.From));
            float reach = from;
            foreach (KwDeckRun run in under)
            {
                if (run.From > reach + Gap)
                    break;
                reach = Mathf.Max(reach, run.To);
            }
            if (reach < to - Gap)
                return "The deck sections between them do not make one deck: there is a gap";
            return under.Max(d => d.High) - under.Min(d => d.Low) > Slope ? "Those deck sections are not level; a cabin needs a level deck" : null;
        }

        // The range's frame (its back at from facing along the line, or at to facing back) and the hull over it.
        private static KwCabinRange Make(KwCabinSite site, float from, float to, float floor, float way)
        {
            List<Station> stations = Stations(site.Sections, from, to, way);
            int atFrom = EndAt(site.Sections, from), atTo = EndAt(site.Sections, to);
            Vector3 at = site.Origin + site.Forward * (way > 0f ? from : to);
            return new KwCabinRange(new Vector3(at.x, site.Origin.y, at.z), site.Forward * way, stations, floor, floor,
                way > 0f ? atFrom : atTo, way > 0f ? atTo : atFrom, 0f, 0f);
        }

        // What a range's end is at: the hull's first or last section (a flat stern or a pointed end), or open deck.
        private static int EndAt(List<(float Along, Vector2[] Half, bool Flat)> sections, float along)
        {
            var (first, last) = (sections[0], sections[sections.Count - 1]);
            if (Mathf.Abs(along - first.Along) < AtEnd)
                return first.Flat ? KwCabinEnd.Flat : KwCabinEnd.Pointed;
            if (Mathf.Abs(along - last.Along) < AtEnd)
                return last.Flat ? KwCabinEnd.Flat : KwCabinEnd.Pointed;
            return KwCabinEnd.Open;
        }

        // The hull's sections over the range in the cabin's frame (x from its back), blended at both ends, a centimetre apart at least.
        private static List<Station> Stations(List<(float Along, Vector2[] Half, bool Flat)> sections, float from, float to, float way)
        {
            float length = to - from;
            List<Station> stations = new List<Station> { new Station(0f, At(sections, way > 0f ? from : to)) };
            List<(float X, Vector2[] Half)> placed = sections.ConvertAll(s => (way > 0f ? s.Along - from : to - s.Along, s.Half));
            placed.Sort((a, b) => a.X.CompareTo(b.X));
            foreach ((float x, Vector2[] half) in placed)
            {
                if (x > 0.01f && x < length - 0.01f && x - stations[stations.Count - 1].X > 0.01f)
                    stations.Add(new Station(x, half));
            }
            stations.Add(new Station(length, At(sections, way > 0f ? to : from)));
            return stations;
        }

        // The hull's section at a place along the line: blended between the sections either side of it (when they have as
        // many points), else the nearer.
        private static Vector2[] At(List<(float Along, Vector2[] Half, bool Flat)> sections, float along)
        {
            int after = sections.FindIndex(s => s.Along >= along);
            if (after <= 0)
                return sections[after == 0 ? 0 : sections.Count - 1].Half;
            var (a, c) = (sections[after - 1], sections[after]);
            float span = c.Along - a.Along;
            if (span < 1e-4f || a.Half.Length != c.Half.Length)
                return along - a.Along < c.Along - along ? a.Half : c.Half;
            return KwJoints.Blend(a.Half, c.Half, (along - a.Along) / span);
        }

        /// <summary>The range over a built cabin's roof: the cabin under it's own length and hull, its frame the same way, its sides standing on that cabin's.</summary>
        public static KwCabinRange? OnRoof(Piece roof, out string why)
        {
            why = "This cabin cannot be read to build on";
            return KwCabinRoofs.Spec(roof, out KwCabinSpec under) ? Over(under, roof.transform.position, roof.transform.forward) : null;
        }

        /// <summary>The range over a cabin standing at a place (its frame's origin) and facing a level way.</summary>
        public static KwCabinRange Over(KwCabinSpec under, Vector3 position, Vector3 forward)
        {
            List<Station> stations = KwCabinOutline.Inside(under).ConvertAll(s => new Station(s.X - under.X0, s.Half));
            int back = KwCabinOutline.Closed(under, under.Back) ? under.Back.Hull : KwCabinEnd.Open;
            int front = KwCabinOutline.Closed(under, under.Front) ? under.Front.Hull : KwCabinEnd.Open;
            return new KwCabinRange(position + forward * under.X0, forward, stations, under.Top, under.Ref, back, front, under.Left.Total, under.Right.Total);
        }
    }
}
