using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Util;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Builds Hel's hall (features/arena.md section 1) under its host, as plain local scene objects with no ZNetView or
    /// ZDO: the floor, walls and ceiling at the hall's origin, the throne at (0, 0, -14.731), Hel on its seat. Every
    /// piece and every collider on <c>static_solid</c> (players stand on the floor, the walls and dome stop them, the
    /// game's pathfinding takes the floor: its tiles reach from -500 m to 5,500 m). On every peer, the dedicated server
    /// included (the bundles' Linux build), with the doors (<see cref="HallDoors"/>) everywhere because their colliders
    /// count; the look (materials, runes, fire and light, the environment, Hel's animation and hover) only where there is
    /// a screen. A missing piece builds nothing and says so.
    /// </summary>
    internal static class HallBuilder
    {
        private const string Layer = "static_solid";

        /// <summary>The throne's place in the hall: its dais's back edge 0.4 m inside the wall (its README).</summary>
        private static readonly Vector3 ThroneAt = new Vector3(0f, 0f, -14.731f);

        /// <summary>True on a machine with no screen: the dedicated server.</summary>
        public static bool NoScreen => Machine.Headless || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        public static GameObject? Build(Transform host)
        {
            int layer = LayerMask.NameToLayer(Layer);
            bool visuals = !NoScreen;
            var hall = new GameObject("ecr_helhall");
            hall.transform.SetParent(host, false);
            GameObject[]? pieces = Pieces(hall.transform, layer, visuals);
            if (pieces == null)
            {
                Object.Destroy(hall);
                Log.Error("Hel's hall was not built: a piece of it did not load (above).");
                return null;
            }
            HallPoints.Index(pieces);
            pieces[1].AddComponent<HallDoors>();
            HelSeat.Seat(hall.transform, layer, visuals);
            if (visuals)
            {
                Look(hall.transform, pieces[0]);
            }
            return hall;
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

        // Floor, walls, throne, ceiling, in that order; null when any is missing.
        private static GameObject[]? Pieces(Transform hall, int layer, bool visuals)
        {
            GameObject? floor = Piece(hall, HallBundles.Floor, Vector3.zero, layer, visuals);
            GameObject? walls = Piece(hall, HallBundles.Walls, Vector3.zero, layer, visuals);
            GameObject? throne = Piece(hall, HallBundles.Throne, ThroneAt, layer, visuals);
            GameObject? ceiling = Piece(hall, HallBundles.Ceiling, Vector3.zero, layer, visuals);
            if (floor == null || walls == null || throne == null || ceiling == null)
            {
                return null;
            }
            return new[] { floor, walls, throne, ceiling };
        }

        private static GameObject? Piece(Transform hall, string name, Vector3 at, int layer, bool visuals)
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
        private static void Look(Transform hall, GameObject floor)
        {
            floor.AddComponent<HallRunes>();
            SafeCall.Run("Hel's hall fires and lights", static () => HallFires.Kindle());
            SafeCall.Run("Hel's hall environment", static h => HallEnvironment.Add(h), hall);
            SafeCall.Run("Helgrind's way out of the hall", static h => Gate.GateWayOut.Build(h), hall);
        }
    }
}
