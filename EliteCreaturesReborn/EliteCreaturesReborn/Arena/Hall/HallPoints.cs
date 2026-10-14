using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The built arena's named children by name, gathered once as the arena is built and emptied as it is removed: the
    /// points the workshop put in the pieces (spawn_0..7, portal_0..7, arrive, gate_inside, flame_0..15 in the walls;
    /// seat_root, seat, spear_rest, spear_top, boss_stand, fire_0..1 in the throne), and every other part of the floor,
    /// walls and throne (door_left, glow_rune_00, ...), so nothing is ever retyped from the briefs.
    /// The first of a name wins. <see cref="ArenaHall.TryPoint"/> is the public face; the builder reads it before the
    /// hall is marked built.
    /// </summary>
    internal static class HallPoints
    {
        private static readonly Dictionary<string, Transform> Points = new Dictionary<string, Transform>();

        public static void Index(IEnumerable<GameObject> pieces)
        {
            Points.Clear();
            foreach (GameObject piece in pieces)
            {
                foreach (Transform part in piece.GetComponentsInChildren<Transform>(true))
                {
                    if (!Points.ContainsKey(part.name))
                    {
                        Points.Add(part.name, part);
                    }
                }
            }
        }

        public static void Clear() => Points.Clear();

        /// <summary>The named part of the built hall; false when there is none or it went with the scene.</summary>
        public static bool TryGet(string name, out Transform point) =>
            Points.TryGetValue(name, out point) && point != null;
    }
}
