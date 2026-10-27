using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the crosshair is on for the Doorway and Window tools: the wall as a site (none: why), the opening there, the ship
    /// on the dry dock, or the launched ship (<see cref="KwSeaOpeningShip"/>) whose part it is.
    /// </summary>
    public sealed class KwOpeningSight
    {
        public KwOpeningSight(KwOpeningSite? site, KwOpening? opening, List<Piece>? ship, string why, KwSeaOpeningShip? sea = null)
        {
            (Site, Opening, Ship, Why, Sea) = (site, opening, ship, why, sea);
        }

        public KwOpeningSite? Site { get; }
        public KwOpening? Opening { get; }
        public List<Piece>? Ship { get; }
        public string Why { get; }

        /// <summary>The launched ship aimed at; null on the dock.</summary>
        public KwSeaOpeningShip? Sea { get; }

        /// <summary>Whether the part aimed at may be cut: on a dry dock, or on a launched ship.</summary>
        public bool Cuttable => Ship != null || Sea != null;
    }

    /// <summary>
    /// Where the Doorway and Window tools are aimed, every frame while one is in hand and at a click, from what the tools
    /// keep read (<see cref="KwOpeningReads"/>): an opening the line of sight passes through (its cut leaves nothing there
    /// to hit) on the ship's parts with openings, else the wall the game's placement ray hits (<see cref="KwOpeningAim.Pick"/>)
    /// and the opening on it there, if any. One ray and plain arithmetic a frame. Runs on the player's game.
    /// </summary>
    public static class KwOpeningAimed
    {
        // How far the tools reach, and the slack past the hit an opening still counts in (a shut lid stands proud of its wall).
        private const float Reach = 50f, Slack = 0.15f;
        private const string Off = "Click the hull's planking or a cabin's wall";

        public static KwOpeningSight Of(Player player)
        {
            Transform eye = GameCamera.instance.transform;
            bool hit = Physics.Raycast(eye.position, eye.forward, out RaycastHit h, Reach, player.m_placeRayMask);
            Piece? piece = hit ? h.collider.GetComponentInParent<Piece>() : null;
            List<Piece>? ship = piece != null ? KwOpeningReads.Ship(piece, h.point) : null;
            (KwOpeningSite Site, KwOpening Opening)? through = Through(eye, hit ? h.distance + Slack : Reach);
            if (through != null)
                return new KwOpeningSight(through.Value.Site, through.Value.Opening, ship ?? Ship(through.Value.Site), "");
            string? tool = KwOpeningHull.ToolOf(piece);
            if (piece == null || tool == null)
                return new KwOpeningSight(null, null, ship, Off);
            KwOpeningSite? site = KwOpeningAim.Pick(KwOpeningReads.Walls(piece), tool, KwOpeningHull.Kit(piece, h.point), out string why);
            KwOpening? at = site != null ? KwOpeningAim.At(site.Openings, site.Wall, site.U, site.Z) : null;
            return new KwOpeningSight(site, at, ship, site != null ? "" : why);
        }

        // The ship of a site found through an opening (none was hit: the ship last looked up).
        private static List<Piece>? Ship(KwOpeningSite site) =>
            site.Piece != null ? KwOpeningReads.Ship(site.Piece, site.Piece.transform.position) : null;

        // The nearest opening on the ship's parts with openings that the line of sight passes through before it hits anything.
        private static (KwOpeningSite, KwOpening)? Through(Transform eye, float most)
        {
            (Piece? piece, KwOpening? opening, string wall, float t) = (null, null, "", most);
            foreach (Piece part in KwOpeningReads.Opened)
            {
                if (part == null)
                    continue;
                Vector3 from = KwOpeningHull.Kit(part, eye.position), way = KwMesh.ToKit(part.transform.InverseTransformDirection(eye.forward));
                var crossed = KwOpeningAim.Through(KwOpeningReads.Cut(part), from, way, t);
                if (crossed != null)
                    (piece, opening, wall, t) = (part, crossed.Value.Opening, crossed.Value.Wall.Surface.Wall, crossed.Value.T);
            }
            if (piece == null || opening == null)
                return null;
            KwOpeningSite? site = KwOpeningReads.Walls(piece).Find(w => w.Wall == wall)?.At(opening.U, opening.Sill + opening.Height / 2f);
            KwOpening? saved = site?.Openings.Find(o => o.Id == opening.Id);
            return site != null && saved != null ? (site, saved) : ((KwOpeningSite, KwOpening)?)null;
        }
    }
}
