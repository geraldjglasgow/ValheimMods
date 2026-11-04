using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One piece near the area: the zone it keeps clear (<see cref="KwDecorKeepClear"/> has the list), the path ends it adds
    /// (a door's two sides, a ladder's or stairs' foot, a station's front, a portal's two sides), a yard (a ship, a
    /// workbench), or, for a dockside decoration already standing, its footprint and spacing.
    /// </summary>
    public static class KwDecorClearRules
    {
        private const string Workbench = "$piece_workbench";

        public static void Add(KwDecorArea area, Piece piece)
        {
            if (KwDecorSurface.Decoration(piece))
                Standing(area, piece);
            else if (!Moving(area, piece) && !Passage(area, piece) && !Station(area, piece))
                Used(piece, area.Kept);
        }

        private static bool Moving(KwDecorArea area, Piece piece)
        {
            if (piece.GetComponent<Ship>() != null)
            {
                area.Kept.Add(KwDecorBox.Around(BoundsOf(piece), 1.5f));
                area.Yards.Add(piece.transform.position);
                return true;
            }
            if (piece.GetComponent<Vagon>() == null)
                return false;
            area.Kept.Add(Square(piece.transform, 1.6f));
            return true;
        }

        private static bool Passage(KwDecorArea area, Piece piece)
        {
            Transform at = piece.transform;
            if (piece.GetComponent<Door>() != null)
                area.Kept.Add(new KwDecorBox(at.position, at.right, Mathf.Max(0.9f, Across(BoundsOf(piece), at.right) + 0.3f), 2f));
            else if (piece.GetComponent<TeleportWorld>() != null)
                area.Kept.Add(new KwDecorBox(at.position, at.right, 1.8f, 3f));
            else if (Climbs(piece))
            {
                area.Kept.Add(KwDecorBox.Around(BoundsOf(piece), 1.5f));
                area.Ways.Add(at.position);
                return true;
            }
            else
                return false;
            area.Ways.Add(at.position + at.forward * 1.5f);
            area.Ways.Add(at.position - at.forward * 1.5f);
            return true;
        }

        private static bool Climbs(Piece piece) =>
            piece.name.IndexOf("stair", StringComparison.OrdinalIgnoreCase) >= 0 || piece.name.IndexOf("ladder", StringComparison.OrdinalIgnoreCase) >= 0
            || piece.GetComponentInChildren<Ladder>() != null;

        private static bool Station(KwDecorArea area, Piece piece)
        {
            Transform at = piece.transform;
            CraftingStation station = piece.GetComponent<CraftingStation>();
            List<KwDecorBox> boxes = area.Kept;
            if (station != null)
            {
                boxes.Add(Square(at, 2f));
                boxes.Add(Front(at, 2f, 1.2f, 1.5f));
                area.Ways.Add(at.position + at.forward * 2f);
                if (station.m_name == Workbench)
                    area.Yards.Add(at.position);
                return true;
            }
            bool works = piece.GetComponent<Fireplace>() != null || piece.GetComponent<CookingStation>() != null || piece.GetComponent<Smelter>() != null
                || piece.GetComponent<Fermenter>() != null || piece.GetComponent<Beehive>() != null;
            if (works || piece.GetComponent<StationExtension>() != null)
                boxes.Add(Square(at, works ? 1.6f : 1f));
            return works || piece.GetComponent<StationExtension>() != null;
        }

        private static void Used(Piece piece, List<KwDecorBox> boxes)
        {
            Transform at = piece.transform;
            if (piece.GetComponent<Container>() != null)
            {
                boxes.Add(Square(at, 0.5f));
                boxes.Add(Front(at, 0.9f, 0.8f, 0.7f));
            }
            else if (piece.GetComponent<Bed>() != null)
                boxes.Add(KwDecorBox.Around(BoundsOf(piece), 1f));
            else if (piece.GetComponent<Sign>() != null)
                boxes.Add(Front(at, 0.7f, 0.7f, 0.7f));
            else if (piece.GetComponent<ItemStand>() != null)
                boxes.Add(Square(at, 0.6f));
        }

        // A decoration already standing: its footprint kept its spacing.
        private static void Standing(KwDecorArea area, Piece piece)
        {
            string prefab = Utils.GetPrefabName(piece.gameObject);
            KwDockDef? def = Array.Find(KwDockside.Defs, d => d.Prefab == prefab);
            if (def == null || def.Decor == null)
                return;
            Transform at = piece.transform;
            area.Existing.Add((KwDecorFoot.Box(def.Decor, at.position, at.rotation), def.Decor.Spacing));
        }

        private static KwDecorBox Square(Transform at, float half) => new KwDecorBox(at.position, at.right, half, half);

        private static KwDecorBox Front(Transform at, float ahead, float halfX, float halfZ) =>
            new KwDecorBox(at.position + at.forward * ahead, at.right, halfX, halfZ);

        // Half a world box's length along a way across the ground.
        private static float Across(Bounds bounds, Vector3 way) => Mathf.Abs(way.x) * bounds.extents.x + Mathf.Abs(way.z) * bounds.extents.z;

        /// <summary>A piece's world box: its colliders', else its renderers', else a metre round it.</summary>
        public static Bounds BoundsOf(Piece piece)
        {
            Bounds? all = null;
            foreach (Collider collider in piece.GetComponentsInChildren<Collider>())
                all = Join(all, collider.bounds);
            if (all == null)
            {
                foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>())
                    all = Join(all, renderer.bounds);
            }
            return all ?? new Bounds(piece.transform.position, Vector3.one);
        }

        private static Bounds Join(Bounds? all, Bounds more)
        {
            if (all == null)
                return more;
            Bounds joined = all.Value;
            joined.Encapsulate(more);
            return joined;
        }
    }
}
