using System.Collections.Generic;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// Where new fake players go. Spread: each on its own spot of land across the map, its loop on land too (the world
    /// generator's height above the water at the centre and four points round it). Spots are taken first from the zones
    /// the world has already generated whose eight neighbours are generated too (land players have been through, so the
    /// fakes' areas add little new world), in random order, at least <see cref="Gaps"/>[0] zones apart (320 m, so the
    /// areas the server keeps round each do not overlap), then closer when they run out; when the explored land runs out
    /// too, any land within 9.5 km of the centre of the world: the server then generates the zones round those, as for a
    /// real player who goes there, and the world save keeps them. Crowd: one centre, from x= and z=, else the hosting
    /// player, else the first real player connected; each fake's loop then lies inside the radius round it.
    /// </summary>
    internal static class Spots
    {
        private const float Zone = 64f;
        private const float WorldRadius = 9500f;
        private const int TriesPerSpot = 40;
        private static readonly int[] Gaps = { 5, 3, 1 };

        /// <summary>Up to count spots, away from the taken ones; how many lie outside explored land comes back too.</summary>
        internal static List<Vector3> Spread(int count, float loop, List<Vector3> taken, System.Random random, out int unexplored)
        {
            var picked = new List<Vector3>(count);
            List<Vector2s> inner = InnerZones(random);
            var used = new bool[inner.Count];
            foreach (int gap in Gaps)
            {
                if (picked.Count >= count) break;
                FromZones(inner, used, gap * Zone, loop, count, taken, picked);
            }
            int explored = picked.Count;
            Anywhere(count, loop, taken, picked, random);
            unexplored = picked.Count - explored;
            return picked;
        }

        /// <summary>The crowd's centre: x= and z=, else the hosting player, else the first real player connected.</summary>
        internal static Vector3 CrowdCentre(LoadArgs args)
        {
            if (args.HasCentre) return new Vector3(args.X, 0f, args.Z);
            if (Player.m_localPlayer) return Player.m_localPlayer.transform.position;
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady() && !(peer.m_socket is FakeSocket)) return peer.m_refPos;
            }
            throw new BridgeException("mode=crowd needs x= and z= here: this game has no player of its own and no real player " +
                                      "is connected to crowd round");
        }

        private static void FromZones(List<Vector2s> zones, bool[] used, float gap, float loop, int count, List<Vector3> taken, List<Vector3> picked)
        {
            for (int i = 0; i < zones.Count && picked.Count < count; i++)
            {
                if (used[i]) continue;
                Vector3 spot = ZoneSystem.GetZonePos(zones[i]);
                if (!Land(spot, loop) || Near(spot, gap, taken, picked)) continue;
                used[i] = true;
                picked.Add(spot);
            }
        }

        private static void Anywhere(int count, float loop, List<Vector3> taken, List<Vector3> picked, System.Random random)
        {
            for (int tries = TriesPerSpot * count; tries > 0 && picked.Count < count; tries--)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float distance = WorldRadius * Mathf.Sqrt((float)random.NextDouble());
                var spot = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                if (Land(spot, loop) && !Near(spot, Gaps[1] * Zone, taken, picked)) picked.Add(spot);
            }
        }

        // Generated zones with all eight neighbours generated, in random order.
        private static List<Vector2s> InnerZones(System.Random random)
        {
            HashSet<Vector2s> generated = ZoneSystem.instance.m_generatedZones;
            var inner = new List<Vector2s>();
            foreach (Vector2s zone in generated)
            {
                if (Surrounded(generated, zone)) inner.Add(zone);
            }
            for (int i = inner.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (inner[i], inner[j]) = (inner[j], inner[i]);
            }
            return inner;
        }

        private static bool Surrounded(HashSet<Vector2s> generated, Vector2s zone)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if ((dx != 0 || dy != 0) && !generated.Contains(new Vector2s(zone.x + dx, zone.y + dy))) return false;
                }
            }
            return true;
        }

        // Land at the spot and at four points of a loop round it, by the world generator's height.
        private static bool Land(Vector3 spot, float loop)
        {
            float above = ZoneSystem.instance.m_waterLevel + 1f;
            if (WorldGenerator.instance.GetHeight(spot.x, spot.z) < above) return false;
            for (int k = 0; k < 4; k++)
            {
                float angle = k * Mathf.PI * 0.5f;
                if (WorldGenerator.instance.GetHeight(spot.x + Mathf.Cos(angle) * loop, spot.z + Mathf.Sin(angle) * loop) < above) return false;
            }
            return true;
        }

        private static bool Near(Vector3 spot, float gap, List<Vector3> taken, List<Vector3> picked) =>
            Within(spot, gap, taken) || Within(spot, gap, picked);

        private static bool Within(Vector3 spot, float gap, List<Vector3> others)
        {
            float limit = gap * gap;
            foreach (Vector3 other in others)
            {
                float dx = other.x - spot.x, dz = other.z - spot.z;
                if (dx * dx + dz * dz < limit) return true;
            }
            return false;
        }
    }
}
