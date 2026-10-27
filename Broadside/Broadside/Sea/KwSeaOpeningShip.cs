using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship as the Doorway and Window tools read it at sea, from its blueprint in its own frame (on the dock they
    /// read the world's pieces, <see cref="KwOpeningReads"/>): its planking parts and cabins (<see cref="KwSeaOpeningPart"/>),
    /// its decks (<see cref="KwSeaOpeningDeck"/>), its ladders, its design waterline; and the tools' context for placing on
    /// it (<see cref="Context"/>): the planking in line on a side and a doorway's partners (<see cref="KwSeaOpeningMaps"/>),
    /// the deck inside under a place (<see cref="KwSeaOpeningFloor"/>), the ladders against a wall, each worked out once for
    /// these reads. Read again whenever the ship's blueprint changed (<see cref="KwSeaShip.Version"/>). Pure but for the
    /// ship it names.
    /// </summary>
    public sealed class KwSeaOpeningShip
    {
        private static KwSeaOpeningShip? last;
        private readonly KwOpeningContext[] contexts;
        private readonly Dictionary<(int, string, bool), KwOpeningLine> lines = new Dictionary<(int, string, bool), KwOpeningLine>();
        private readonly Dictionary<(int, string), List<KwPartMap>> partners = new Dictionary<(int, string), List<KwPartMap>>();
        private readonly Dictionary<(int, string, int, int), float?> floors = new Dictionary<(int, string, int, int), float?>();

        public KwSeaOpeningShip(KwBlueprint blueprint, KwKeelLines keels, float waterline, KwSeaShip? ship = null)
        {
            (Blueprint, Waterline, Ship, Version, Revision) = (blueprint, waterline, ship, ship != null ? ship.Version : 0, ship != null ? ship.Revision : 0);
            for (int i = 0; i < blueprint.Pieces.Count; i++)
                Sort(i, blueprint.Pieces[i], keels);
            Decks = KwSeaOpeningDeck.Of(blueprint);
            contexts = new[] { Made(KwWindowTool.Tool), Made(KwDoorwayTool.Tool) };
        }

        public KwSeaShip? Ship { get; }
        public int Version { get; }

        /// <summary>The ship's revision these reads are of: what an edit planned on them is asked with.</summary>
        public int Revision { get; }
        public KwBlueprint Blueprint { get; }
        public float Waterline { get; }
        public List<KwSeaOpeningPart> Parts { get; } = new List<KwSeaOpeningPart>();
        public List<KwSeaOpeningDeck> Decks { get; }
        public List<KwLadderPose> Ladders { get; } = new List<KwLadderPose>();

        /// <summary>The reads of a launched ship as it is now (made again after an edit was drawn).</summary>
        public static KwSeaOpeningShip Of(KwSeaShip ship)
        {
            if (last == null || last.Ship != ship || last.Version != ship.Version)
                last = new KwSeaOpeningShip(ship.Blueprint, ship.Keels, KwShipTuning.Draught(ship.Hull), ship);
            return last;
        }

        /// <summary>A part by its blueprint index; null when it is not one openings are cut in.</summary>
        public KwSeaOpeningPart? Part(int index) => Parts.Find(p => p.Index == index);

        /// <summary>The tool's context for placing on this ship.</summary>
        public KwOpeningContext Context(string tool) => contexts[tool == KwDoorwayTool.Tool ? 1 : 0];

        /// <summary>What a plan asks of the world on this ship: decks inside an opening, the sill under the waterline.</summary>
        public KwOpeningWorld World() => new KwOpeningWorld { Crosses = (site, o) => KwSeaOpeningFloor.Crosses(this, site, o), Below = (site, o) => KwSeaOpeningFloor.Below(this, site, o) };

        // A piece kept by what it is: a part openings are cut in, or a ladder.
        private void Sort(int index, KwBpPiece piece, KwKeelLines keels)
        {
            string? tool = KwSeaOpeningPart.ToolOf(piece);
            if (tool != null)
                Parts.Add(new KwSeaOpeningPart(index, piece, tool, keels, Revision));
            else if (piece.Prefab == KwToolPieces.PartPrefix + KwLadderTool.Tool && KwLadder.TryLength(KwShipParts.Saved(piece), out float length))
                Ladders.Add(new KwLadderPose(piece.Position, piece.Rotation * Vector3.up, piece.Rotation * Vector3.forward, piece.Rotation * Vector3.right, length));
        }

        private KwOpeningContext Made(string tool) => new KwOpeningContext
        {
            Tool = tool, World = World(), Line = site => LineOf(site, tool == KwDoorwayTool.Tool), Floor = FloorOf, Partners = PartnersOf, Ladders = LaddersOf,
            Middle = (site, o) => KwSeaOpeningPart.Of(site) is KwSeaOpeningPart part ? part.Ship(site.Surface.Point(o.U, o.Sill + o.Height / 2f, 0f)) : Vector3.zero,
        };

        private static int Key(KwOpeningSite site) => KwSeaOpeningPart.Of(site)?.Index ?? -1;

        // A side's line, once for the part's wall and the tool's kind.
        private KwOpeningLine LineOf(KwOpeningSite site, bool doorways)
        {
            (int, string, bool) at = (Key(site), site.Wall, doorways);
            if (!lines.TryGetValue(at, out KwOpeningLine line))
                lines[at] = line = new KwOpeningLine(KwSeaOpeningMaps.Line(this, site), doorways);
            return line;
        }

        // A doorway's partners, once for the part's wall.
        private List<KwPartMap> PartnersOf(KwOpeningSite site)
        {
            (int, string) at = (Key(site), site.Wall);
            if (!partners.TryGetValue(at, out List<KwPartMap> found))
                partners[at] = found = KwSeaOpeningMaps.Partners(this, site);
            return found;
        }

        // The deck inside under a place, once for every 5 cm along and 25 cm up of a part's wall.
        private float? FloorOf(KwOpeningSite site, float u, float z)
        {
            KwSeaOpeningPart? part = KwSeaOpeningPart.Of(site);
            if (site.Spec != null || part == null)
                return site.Spec?.Floor;
            (int, string, int, int) at = (part.Index, site.Wall, Mathf.RoundToInt(u / 0.05f), Mathf.RoundToInt(z / 0.25f));
            if (!floors.TryGetValue(at, out float? floor))
                floors[at] = floor = KwSeaOpeningFloor.Floor(this, part, site, u, z);
            return floor;
        }

        // The ship's ladders against a site's wall within two heights.
        private List<(float Lo, float Hi)> LaddersOf(KwOpeningSite site, float z0, float z1)
        {
            List<(float Lo, float Hi)> found = new List<(float, float)>();
            KwSeaOpeningPart? part = KwSeaOpeningPart.Of(site);
            if (part == null)
                return found;
            foreach (KwLadderPose ladder in Ladders)
            {
                (float Lo, float Hi)? stretch = KwOpeningLadders.Against(site, part.Kit, part.KitWay, ladder, z0, z1);
                if (stretch != null)
                    found.Add(stretch.Value);
            }
            return found;
        }
    }
}
