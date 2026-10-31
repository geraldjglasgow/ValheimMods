using System;
using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// Which sectors (zones) around a player's own the game sends them, for one simulation distance, worked out once the
    /// way ZDOMan.FindSectorObjects walks them on every send: the player's own sector and the near ones give every object
    /// in them, the distant ring only the objects flagged Distant. Near is a circle of the near distance plus half a zone,
    /// the distant ring a circle of the near and far distances together plus 0.8 of a zone (ZoneSystem.ZonesWithinRadius,
    /// called here exactly as the game calls it); the classic setting makes both squares and starts the distant ring past
    /// the near square. The result depends only on how far a sector is from the player's, so one table serves every
    /// player with that setting. Each player's own setting counts (ZNetPeer.m_simulationDistance), as in the game.
    /// </summary>
    internal sealed class AreaShape
    {
        public const byte Out = 0;
        public const byte Distant = 1;
        public const byte Near = 2;

        private static readonly List<AreaShape> Known = new List<AreaShape>();

        public readonly SimulationDistance Sim;

        /// <summary>How many sectors out from the player's the table reaches, every way.</summary>
        public readonly int Radius;

        private readonly int _width;
        private readonly byte[] _levels;

        private AreaShape(SimulationDistance sim)
        {
            Sim = sim;
            Radius = Math.Max(0, Math.Max(sim.NearSimulationDistance, sim.TotalSimulationDistance));
            _width = 2 * Radius + 1;
            _levels = new byte[_width * _width];
            for (int dy = -Radius; dy <= Radius; dy++)
            {
                for (int dx = -Radius; dx <= Radius; dx++)
                {
                    _levels[(dy + Radius) * _width + dx + Radius] = Decide(sim, dx, dy);
                }
            }
        }

        /// <summary>The table for a simulation distance, made the first time it is asked for (there are only a few).</summary>
        public static AreaShape For(SimulationDistance sim)
        {
            foreach (AreaShape shape in Known)
            {
                if (shape.Sim.Equals(sim))
                {
                    return shape;
                }
            }
            AreaShape made = new AreaShape(sim);
            Known.Add(made);
            return made;
        }

        /// <summary><see cref="Near"/>, <see cref="Distant"/> or <see cref="Out"/> for the sector dx, dy from the player's.</summary>
        public byte At(int dx, int dy)
        {
            if (dx < -Radius || dx > Radius || dy < -Radius || dy > Radius)
            {
                return Out;
            }
            return _levels[(dy + Radius) * _width + dx + Radius];
        }

        // FindSectorObjects: the own sector, then the near rings (FindObjects), then the distant rings (FindDistantObjects),
        // which skip a sector already taken as near.
        private static byte Decide(SimulationDistance sim, int dx, int dy)
        {
            int ring = Math.Max(Math.Abs(dx), Math.Abs(dy));
            int near = sim.NearSimulationDistance;
            int total = sim.TotalSimulationDistance;
            if (ring == 0 || (ring <= near && (sim.IsClassic || Within(dx, dy, near, false))))
            {
                return Near;
            }
            int firstDistant = sim.IsClassic ? near + 1 : 1;
            if (ring >= firstDistant && ring <= total && (sim.IsClassic || Within(dx, dy, total, true)))
            {
                return Distant;
            }
            return Out;
        }

        private static bool Within(int dx, int dy, int radius, bool ghostZone)
        {
            return ZoneSystem.instance.ZonesWithinRadius(new Vector2s(0, 0), new Vector2s(dx, dy), radius, ghostZone);
        }
    }
}
