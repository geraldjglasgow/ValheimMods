using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the Doorway and Window tools read from the world while their ghost follows the aim (<see cref="KwOpeningHover"/>),
    /// kept so hovering stays cheap: each part's walls read once for its saved text (<see cref="KwOpeningAim.Walls"/>); the
    /// ship on the dry dock round the part aimed at, looked up at most once a second (twice when the part aimed at is not on it),
    /// with its parts that have openings and its ladders; and, while the ship stays the same pieces, each side's line, a
    /// doorway's partners and the decks inside found under places. The ship's waterline for the note is worked out again
    /// only when its pieces change. Everything is read again after the tools build (<see cref="Forget"/>). The tools'
    /// context for a placing (<see cref="Context"/>) asks these. Local to this player.
    /// </summary>
    public static class KwOpeningReads
    {
        private const float Every = 1f, Missed = 0.5f;
        private static readonly string LadderPrefab = KwToolPieces.PartPrefix + KwLadderTool.Tool;

        private sealed class Read
        {
            public Read(string saved, List<KwOpeningSite> walls, List<KwOpeningWall> cut) => (Saved, Walls, Cut) = (saved, walls, cut);

            public string Saved { get; }
            public List<KwOpeningSite> Walls { get; }
            public List<KwOpeningWall> Cut { get; }
        }

        private static readonly Dictionary<int, Read> reads = new Dictionary<int, Read>();
        private static readonly List<Piece> ship = new List<Piece>(), opened = new List<Piece>(), ladders = new List<Piece>();
        private static readonly HashSet<int> members = new HashSet<int>();
        private static readonly Dictionary<(int, string, bool), KwOpeningLine> lines = new Dictionary<(int, string, bool), KwOpeningLine>();
        private static readonly Dictionary<(int, string), List<KwPartMap>> partners = new Dictionary<(int, string), List<KwPartMap>>();
        private static readonly Dictionary<(int, string, int, int), float?> floors = new Dictionary<(int, string, int, int), float?>();
        private static readonly KwOpeningContext[] contexts = { Made(KwWindowTool.Tool), Made(KwDoorwayTool.Tool) };
        private static float next, retry;
        private static int key;
        private static bool docked, watered;

        /// <summary>A part's walls, read once for its saved text; none for a part openings are not cut in.</summary>
        public static List<KwOpeningSite> Walls(Piece piece) => ReadOf(piece)?.Walls ?? new List<KwOpeningSite>();

        /// <summary>A part's walls with the openings cut in them (none: empty).</summary>
        public static List<KwOpeningWall> Cut(Piece piece) => ReadOf(piece)?.Cut ?? new List<KwOpeningWall>();

        private static Read? ReadOf(Piece piece)
        {
            string? tool = KwOpeningHull.ToolOf(piece);
            if (tool == null)
                return null;
            ZDO zdo = piece.m_nview.GetZDO();
            string saved = zdo.GetString(KwGenerated.Key);
            int id = piece.GetInstanceID();
            if (reads.TryGetValue(id, out Read read) && read.Saved == saved)
                return read;
            read = new Read(saved, KwOpeningAim.Walls(tool, saved, KwBiomeTiers.Of(zdo), piece), KwOpeningSurfaces.Of(tool, saved, KwSeatAt.World(piece.transform)));
            reads[id] = read;
            return read;
        }

        /// <summary>The ship on the dry dock a part stands on (looked up again at most once a second); null when it stands on none.</summary>
        public static List<Piece>? Ship(Piece part, Vector3 at)
        {
            bool on = docked && members.Contains(part.GetInstanceID());
            if (Time.time >= next || (!on && Time.time >= retry))
            {
                Refresh(at);
                on = docked && members.Contains(part.GetInstanceID());
            }
            return on ? ship : null;
        }

        /// <summary>The parts of the ship last looked up that have openings (for the aim through an opening).</summary>
        public static List<Piece> Opened => opened;

        /// <summary>The launched ship the tools last aimed at (its context answers); null on the dock.</summary>
        public static KwSeaOpeningShip? Sea { get; private set; }

        /// <summary>The tools aim at a launched ship (its reads) or, null, at the dock: the context they place with.</summary>
        public static void Use(KwSeaOpeningShip? sea) => Sea = sea;

        /// <summary>The tool's context for placing on the ship last looked up (a launched ship's, while the tools aim at one).</summary>
        public static KwOpeningContext Context(string tool)
        {
            if (Sea != null)
                return Sea.Context(tool);
            KwOpeningContext context = contexts[tool == KwDoorwayTool.Tool ? 1 : 0];
            if (!watered && ship.Count > 0)
                Water(context);
            return context;
        }

        /// <summary>Everything read again on the next look (the tools have just built).</summary>
        public static void Forget()
        {
            (next, retry, key) = (0f, 0f, 0);
            Clear();
        }

        // The dock round a point, its ship, and when its pieces changed everything kept of them read again.
        private static void Refresh(Vector3 at)
        {
            (next, retry) = (Time.time + Every, Time.time + Missed);
            KwBpDockInfo? dock = KwBpDock.Find(at);
            ship.Clear();
            if (dock != null)
                ship.AddRange(KwBpDock.Ship(dock));
            docked = dock != null;
            int now = Signature();
            if (now == key)
                return;
            key = now;
            Clear();
            members.Clear();
            foreach (Piece piece in ship)
                Sort(piece);
        }

        private static void Sort(Piece piece)
        {
            members.Add(piece.GetInstanceID());
            if (Utils.GetPrefabName(piece.gameObject) == LadderPrefab)
                ladders.Add(piece);
            else if (Cut(piece).Count > 0)
                opened.Add(piece);
        }

        // Which pieces the ship is (each one's ZDO id; a part changed is a new part).
        private static int Signature()
        {
            int sum = ship.Count;
            foreach (Piece piece in ship)
            {
                if (piece != null && piece.m_nview != null && piece.m_nview.GetZDO() != null)
                    sum += piece.m_nview.GetZDO().m_uid.GetHashCode() * 16777619;
            }
            return sum;
        }

        private static void Clear()
        {
            opened.Clear();
            ladders.Clear();
            lines.Clear();
            partners.Clear();
            floors.Clear();
            watered = false;
            foreach (KwOpeningContext context in contexts)
                context.World = new KwOpeningWorld { Crosses = KwOpeningRays.Crosses };
            if (reads.Count > 400)
                reads.Clear();
        }

        // The ship's waterline for the note, worked out once for these pieces.
        private static void Water(KwOpeningContext context)
        {
            watered = true;
            KwOpeningWater? water = ship.Count > 0 && ship[0] != null ? KwOpeningWater.Of(ship[0]) : null;
            foreach (KwOpeningContext each in contexts)
                each.World = new KwOpeningWorld { Crosses = KwOpeningRays.Crosses, Below = water != null ? water.Below : null };
        }

        private static KwOpeningContext Made(string tool) => new KwOpeningContext
        {
            Tool = tool, World = new KwOpeningWorld { Crosses = KwOpeningRays.Crosses },
            Line = site => LineOf(site, tool == KwDoorwayTool.Tool), Floor = FloorOf, Partners = PartnersOf, Ladders = KwOpeningLadders.Along,
            Middle = (site, o) => site.Piece != null ? KwOpeningHull.World(site.Piece, site.Surface.Point(o.U, o.Sill + o.Height / 2f, 0f)) : Vector3.zero,
        };

        // A side's line, once for the part's wall and the tool's kind.
        private static KwOpeningLine LineOf(KwOpeningSite site, bool doorways)
        {
            (int, string, bool) at = (site.Piece != null ? site.Piece.GetInstanceID() : 0, site.Wall, doorways);
            if (!lines.TryGetValue(at, out KwOpeningLine line))
            {
                List<KwPartMap> maps = site.Planking && site.Piece != null ? KwOpeningHull.Line(site, ship) : new List<KwPartMap> { KwPartMap.Same(site) };
                lines[at] = line = new KwOpeningLine(maps, doorways);
            }
            return line;
        }

        // A doorway's partners, once for the part's wall.
        private static List<KwPartMap> PartnersOf(KwOpeningSite site)
        {
            (int, string) at = (site.Piece != null ? site.Piece.GetInstanceID() : 0, site.Wall);
            if (!partners.TryGetValue(at, out List<KwPartMap> found))
                partners[at] = found = site.Piece != null ? KwOpeningHull.Partners(site, ship) : new List<KwPartMap>();
            return found;
        }

        // The deck inside under a place, once for every 5 cm along and 25 cm up of a part's wall.
        private static float? FloorOf(KwOpeningSite site, float u, float z)
        {
            if (site.Spec != null || site.Piece == null)
                return KwOpeningRays.Floor(site, u, z);
            (int, string, int, int) at = (site.Piece.GetInstanceID(), site.Wall, Mathf.RoundToInt(u / 0.05f), Mathf.RoundToInt(z / 0.25f));
            if (!floors.TryGetValue(at, out float? floor))
                floors[at] = floor = KwOpeningRays.Floor(site, u, z);
            return floor;
        }

        /// <summary>The ladders of the ship last looked up.</summary>
        public static List<Piece> Ladders => ladders;
    }
}
