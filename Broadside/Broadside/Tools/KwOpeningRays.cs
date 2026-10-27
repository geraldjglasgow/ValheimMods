using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the Doorway and Window tools find with rays in the world (what the crosshair is on is <see cref="KwOpeningAimed"/>'s):
    /// the deck inside under a place on planking (a ray down from just inboard of it onto a deck part or a cabin's roof),
    /// whether an opening on planking would cut through a deck inside the hull (rays inboard of it at its ends and middle,
    /// from its frame's top to its foot and back). The game's placement ray layers. Runs on the clicking player's game.
    /// </summary>
    public static class KwOpeningRays
    {
        // How far inboard of planking the rays look for decks.
        private const float Inboard = 0.25f, Beside = 0.12f;
        private static readonly string DeckPrefab = KwToolPieces.PartPrefix + "deck";

        private static int Mask => Player.m_localPlayer != null ? Player.m_localPlayer.m_placeRayMask : Physics.DefaultRaycastLayers;

        /// <summary>The deck inside under a place on a site's wall (kit z): a cabin's floor; on planking a ray down from inboard of it; null for none.</summary>
        public static float? Floor(KwOpeningSite site, float u, float z)
        {
            if (site.Spec != null)
                return site.Spec.Floor;
            if (KwSeaOpeningPart.Of(site) is KwSeaOpeningPart part && KwOpeningReads.Sea != null)
                return KwSeaOpeningFloor.Floor(KwOpeningReads.Sea, part, site, u, z);
            if (site.Piece == null)
                return null;
            Vector3 from = KwOpeningHull.World(site.Piece, site.Surface.Point(u, z + 0.05f, -site.Surface.Thick - Inboard));
            RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, 8f, Mask);
            Array.Sort(hits, (a, c) => a.distance.CompareTo(c.distance));
            foreach (RaycastHit hit in hits)
            {
                Piece? piece = hit.collider.GetComponentInParent<Piece>();
                if (piece != null && hit.normal.y > 0.7f && Floors(piece, hit.point))
                    return KwOpeningHull.Kit(site.Piece, hit.point).z;
            }
            return null;
        }

        // A deck part, or a cabin's roof at its top.
        private static bool Floors(Piece piece, Vector3 point)
        {
            string name = Utils.GetPrefabName(piece.gameObject);
            return name == DeckPrefab || (KwHouses.IsPart(name) && KwCabinRoofs.OnRoof(piece, point));
        }

        /// <summary>Whether an opening on planking would cut through a deck inside the hull (a doorway's own floor left out).</summary>
        public static bool Crosses(KwOpeningSite site, KwOpening o)
        {
            if (site.Piece == null || !site.Planking)
                return false;
            float wide = KwOpeningFrames.Width(o.Frame).Wide, low = o.IsDoorway ? o.Sill + 0.06f : o.Sill - wide, high = o.Top + wide;
            foreach (float u in new[] { o.U0 + 0.02f, o.U, o.U1 - 0.02f })
            {
                Vector3 a = KwOpeningHull.World(site.Piece, site.Surface.Point(u, high, -site.Surface.Thick - Beside));
                Vector3 c = KwOpeningHull.World(site.Piece, site.Surface.Point(u, low, -site.Surface.Thick - Beside));
                if (Deck(a, c) || Deck(c, a))
                    return true;
            }
            return false;
        }

        // A deck part or a cabin's roof or floor met between two points.
        private static bool Deck(Vector3 a, Vector3 c)
        {
            Vector3 d = c - a;
            float length = d.magnitude;
            if (length < 0.001f)
                return false;
            foreach (RaycastHit hit in Physics.RaycastAll(a, d / length, length, Mask))
            {
                Piece? piece = hit.collider.GetComponentInParent<Piece>();
                string name = piece != null ? Utils.GetPrefabName(piece.gameObject) : "";
                if (name == DeckPrefab || (KwHouses.IsPart(name) && Mathf.Abs(hit.normal.y) > 0.7f))
                    return true;
            }
            return false;
        }
    }
}
