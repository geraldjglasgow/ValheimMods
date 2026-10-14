using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Util;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Builds Hel's arena (features/arena.md section 1; ValheimAssets <c>Assets/Locations/HelArena</c>, its
    /// CONTRACT.md) under its host, as plain local scene objects with no ZNetView or ZDO: the floor, the walls (16 m
    /// behind the throne; open above since 2026-10-13, their invisible sky boxes stripped of their colliders), the cover
    /// (ten broken pillars and ten low walls) and the land at the arena's origin, the indoor hall's throne at
    /// (0, 0, -55.6), Hel on its seat, Hel's two anvils beside the dais (<see cref="HallAnvils"/>). Every piece and every collider on <c>static_solid</c> (players stand on the floor,
    /// the walls and cover stop players and creatures, the game's pathfinding takes them: its tiles reach from -500 m to
    /// 5,500 m). On every peer, the dedicated server included (the bundles' Linux build), with the doors
    /// (<see cref="HallDoors"/>), the land (its plateau boxes) and Hel's river's kill (<see cref="HallLake"/>) everywhere;
    /// the look (materials, the cave's roof, the river with its bubbles and mist, runes, fire and light, the cave's light
    /// and the view, Hel's animation and hover) only where there is a screen. A missing floor, wall or throne builds
    /// nothing and says so; missing cover or land is logged and the arena stands without it.
    /// </summary>
    internal static class HallBuilder
    {
        private const string Layer = "static_solid";
        private const string SkyBoxes = "col_box_sky_";

        /// <summary>The throne's place: its dais's back edge at r 65.5, against the wall's backdrop (CONTRACT.md).</summary>
        private static readonly Vector3 ThroneAt = new Vector3(0f, 0f, -55.6f);

        /// <summary>True on a machine with no screen: the dedicated server.</summary>
        public static bool NoScreen => Machine.Headless || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        public static GameObject? Build(Transform host)
        {
            int layer = LayerMask.NameToLayer(Layer);
            bool visuals = !NoScreen;
            var hall = new GameObject("ecr_helarena");
            hall.transform.SetParent(host, false);
            GameObject[]? pieces = Pieces(hall.transform, layer, visuals);
            if (pieces == null)
            {
                Object.Destroy(hall);
                Log.Error("Hel's arena was not built: a piece of it did not load (above).");
                return null;
            }
            GameObject? land = Land(hall.transform, layer, visuals);
            HallPoints.Index(land == null ? pieces : new[] { pieces[0], pieces[1], pieces[2], land });
            Furnish(hall.transform, pieces[1], layer, visuals);
            if (visuals)
            {
                Look(hall.transform, pieces[0], layer);
            }
            return hall;
        }

        // What stands in the arena besides its pieces, on every peer: the cover, the gate's doors, Hel, her anvils, and
        // the river's kill.
        private static void Furnish(Transform hall, GameObject walls, int layer, bool visuals)
        {
            Piece(hall, HallBundles.Cover, Vector3.zero, layer, visuals);
            walls.AddComponent<HallDoors>();
            HelSeat.Seat(hall, layer, visuals);
            HallAnvils.Place(hall, layer, visuals);
            hall.gameObject.AddComponent<HallLake>();
        }

        /// <summary>The part and everything under it on the layer.</summary>
        public static void SetLayer(Transform part, int layer)
        {
            part.gameObject.layer = layer;
            foreach (Transform child in part)
            {
                SetLayer(child, layer);
            }
        }

        // Floor, walls, throne, in that order; null when any is missing.
        private static GameObject[]? Pieces(Transform hall, int layer, bool visuals)
        {
            GameObject? floor = Piece(hall, HallBundles.Floor, Vector3.zero, layer, visuals);
            GameObject? walls = Piece(hall, HallBundles.Walls, Vector3.zero, layer, visuals);
            GameObject? throne = Piece(hall, HallBundles.Throne, ThroneAt, layer, visuals);
            if (floor == null || walls == null || throne == null)
            {
                return null;
            }
            OpenSky(walls);
            return new[] { floor, walls, throne };
        }

        // No invisible boundary (the user, 2026-10-13): the walls' sky boxes (col_box_sky_00..59 and col_box_sky_cap, no
        // renderer), which kept fliers in up to a cap at 40 m, lose their colliders on every peer. The asset keeps them.
        private static void OpenSky(GameObject walls)
        {
            foreach (Collider box in walls.GetComponentsInChildren<Collider>(true))
            {
                if (box.name.StartsWith(SkyBoxes, System.StringComparison.Ordinal))
                {
                    Object.DestroyImmediate(box);
                }
            }
        }

        // A piece at its place, on the layer, in the game's stone where there is a screen. Only renderers are dressed:
        // collider-only children are left as they are.
        internal static GameObject? Piece(Transform hall, string name, Vector3 at, int layer, bool visuals)
        {
            GameObject? prefab = HallBundles.Prefab(name, name);
            if (prefab == null)
            {
                return null;
            }
            GameObject piece = Object.Instantiate(prefab, hall, false);
            piece.name = name;
            piece.transform.localPosition = at;
            piece.transform.localRotation = Quaternion.identity;
            SetLayer(piece.transform, layer);
            if (visuals)
            {
                HallMaterials.DressStone(piece);
            }
            return piece;
        }

        // The runes after the floor's materials are dressed: each copies its dressed glow material.
        private static void Look(Transform hall, GameObject floor, int layer)
        {
            floor.AddComponent<HallRunes>();
            hall.gameObject.AddComponent<HallView>();
            hall.gameObject.AddComponent<HallBubbles>();
            SafeCall.Run("Hel's arena cave", static (h, l) => HallOutside.Cave(h, l), hall, layer);
            SafeCall.Run("Hel's arena river", static h => HallOutside.River(h), hall);
            SafeCall.Run("Hel's arena fires and lights", static () => HallFires.Kindle());
            SafeCall.Run("Hel's arena mist", static () => HallMist.Lay());
            SafeCall.Run("Hel's arena sky", static h => HallEnvironment.Add(h), hall);
            SafeCall.Run("Helgrind's way out of the arena", static h => Gate.GateWayOut.Build(h), hall);
        }

        // The land and mountains round the arena, on every peer: its plateau boxes (static_solid) are a net under anything
        // that gets over the wall; its look, glow and mist only where there is a screen. Not shipped: a warning only.
        private static GameObject? Land(Transform hall, int layer, bool visuals)
        {
            if (!HallBundles.Embedded(HallBundles.Land))
            {
                Log.Warn($"Hel's arena: land missing ({HallBundles.Land} is not in this build); the arena stands in open sky.");
                return null;
            }
            return Piece(hall, HallBundles.Land, Vector3.zero, layer, visuals);
        }
    }
}
