using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Sends a blueprint's ground work the way the game sends its own terrain operations: one package per terrain
    /// compiler the work touches (made where missing, as a hoe does), over the compiler's own network view to its owner
    /// (RPC <see cref="RpcName"/>). The owner writes each height as the game's level operation does (the level change
    /// added to what is there, within 8 m of the generated ground) and the paint, saves the compiler to its ZDO, which
    /// every client draws from, and redraws the heightmap. A vertex on a zone edge goes to both compilers.
    /// </summary>
    public static class GroundWriter
    {
        public const string RpcName = "OpenKeep_BlueprintGround";

        /// <summary>More points than a heightmap holds are never read from one package.</summary>
        private const int MaxPoints = 65 * 65;

        /// <summary>Sends the work; returns how many compilers were sent a part.</summary>
        public static int Send(GroundWork work)
        {
            int sent = 0;
            foreach (KeyValuePair<Heightmap, List<GroundPoint>> part in Parts(work))
            {
                TerrainComp comp = part.Key.GetAndCreateTerrainCompiler();
                if (comp == null || comp.m_nview == null || !comp.m_nview.IsValid())
                    continue;
                comp.m_nview.InvokeRPC(RpcName, Write(part.Value));
                sent++;
            }
            return sent;
        }

        /// <summary>The points by every loaded heightmap holding them.</summary>
        private static Dictionary<Heightmap, List<GroundPoint>> Parts(GroundWork work)
        {
            Dictionary<Heightmap, List<GroundPoint>> parts = new Dictionary<Heightmap, List<GroundPoint>>();
            List<Heightmap> found = new List<Heightmap>();
            foreach (GroundPoint p in work.Points)
            {
                found.Clear();
                Heightmap.FindHeightmap(new Vector3(p.X, 0f, p.Z), 0f, found);
                foreach (Heightmap map in found)
                {
                    if (!parts.TryGetValue(map, out List<GroundPoint> list))
                        parts[map] = list = new List<GroundPoint>();
                    list.Add(p);
                }
            }
            return parts;
        }

        private static ZPackage Write(List<GroundPoint> points)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(points.Count);
            foreach (GroundPoint p in points)
            {
                pkg.Write(p.X);
                pkg.Write(p.Z);
                pkg.Write(p.Height);
                pkg.Write(p.Moves);
                pkg.Write((byte)p.Paint);
            }
            return pkg;
        }

        /// <summary>TerrainComp.Awake postfix (installed only while blueprints are on): the compiler answers the RPC.</summary>
        public static void Register(TerrainComp comp)
        {
            if (comp.m_nview != null && comp.m_nview.IsValid())
                comp.m_nview.Register<ZPackage>(RpcName, (sender, pkg) => BlueprintSafe.Run("OpenKeep blueprint ground", () => Apply(comp, pkg)));
        }

        /// <summary>Owner side: writes every point inside this compiler's heightmap, then saves and redraws once.</summary>
        private static void Apply(TerrainComp comp, ZPackage pkg)
        {
            if (comp == null || !comp.m_initialized || !comp.m_nview.IsOwner() || comp.m_hmap == null)
                return;
            int count = Mathf.Min(pkg.ReadInt(), MaxPoints);
            for (int n = 0; n < count; n++)
                ApplyPoint(comp, Read(pkg));
            comp.Save();
            comp.m_hmap.Poke(1);
            if (ClutterSystem.instance != null)
                ClutterSystem.instance.ResetGrass(comp.m_hmap.transform.position, 46f);
        }

        private static GroundPoint Read(ZPackage pkg)
        {
            return new GroundPoint { X = pkg.ReadInt(), Z = pkg.ReadInt(), Height = pkg.ReadSingle(), Moves = pkg.ReadBool(), Paint = (GroundPaint)pkg.ReadByte() };
        }

        private static void ApplyPoint(TerrainComp comp, GroundPoint p)
        {
            Heightmap map = comp.m_hmap;
            map.WorldToVertex(new Vector3(p.X, 0f, p.Z), out int x, out int y);
            int pitch = comp.m_width + 1;
            if (x < 0 || y < 0 || x >= pitch || y >= pitch || float.IsNaN(p.Height) || float.IsInfinity(p.Height))
                return;
            int i = y * pitch + x;
            if (p.Moves)
            {
                // As the game's LevelTerrain: the change from the height shown now is added to the level delta.
                float change = p.Height - map.transform.position.y - map.GetHeight(x, y) + comp.m_smoothDelta[i];
                comp.m_smoothDelta[i] = 0f;
                comp.m_levelDelta[i] = Mathf.Clamp(comp.m_levelDelta[i] + change, -BlueprintRules.GameLimit, BlueprintRules.GameLimit);
                comp.m_modifiedHeight[i] = true;
            }
            if (p.Paint != GroundPaint.None)
            {
                comp.m_paintMask[i] = GroundPaints.Apply(comp.m_paintMask[i], p.Paint);
                comp.m_modifiedPaint[i] = true;
            }
        }
    }
}
