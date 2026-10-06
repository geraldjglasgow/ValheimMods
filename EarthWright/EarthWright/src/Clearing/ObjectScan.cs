using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Finds the networked objects this machine has loaded near a point, from the game's own instance list (a walk over
    /// every loaded object, so a clearing walks it once per pass and filters what it found). Objects in a dungeon
    /// (placed high above the world) never count for a point on the surface, and the other way round.
    /// </summary>
    public static class ObjectScan
    {
        /// <summary>Every loaded object whose position lies inside the area.</summary>
        public static List<ZNetView> Inside(ClearArea area) => Within(area.Center, area.Reach, area.Contains);

        /// <summary>The objects of <paramref name="views"/> whose position lies inside the area.</summary>
        public static List<ZNetView> Inside(ClearArea area, List<ZNetView> views)
        {
            List<ZNetView> inside = new List<ZNetView>();
            foreach (ZNetView view in views)
            {
                if (view != null && area.Contains(view.transform.position))
                    inside.Add(view);
            }
            return inside;
        }

        /// <summary>Every loaded object within the flat radius of the centre that passes the optional test.</summary>
        public static List<ZNetView> Within(Vector3 center, float radius, Func<Vector3, bool> test)
        {
            List<ZNetView> found = new List<ZNetView>();
            Collect(center, radius, test, found);
            return found;
        }

        /// <summary>As <see cref="Within"/>, into a list the caller keeps (cleared first).</summary>
        public static void Collect(Vector3 center, float radius, Func<Vector3, bool> test, List<ZNetView> found)
        {
            found.Clear();
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
                return;
            float r2 = radius * radius;
            bool interior = Character.InInterior(center);
            foreach (ZNetView view in scene.m_instances.Values)
            {
                if (view == null || !view.IsValid())
                    continue;
                Vector3 p = view.transform.position;
                float dx = p.x - center.x;
                float dz = p.z - center.z;
                if (dx * dx + dz * dz <= r2 && Character.InInterior(p) == interior && (test == null || test(p)))
                    found.Add(view);
            }
        }
    }
}
