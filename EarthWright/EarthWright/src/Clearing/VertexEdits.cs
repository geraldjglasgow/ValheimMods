using EarthWright.Brush;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The <c>ew terrain</c> operations computed here as explicit vertex targets (whole world metres, sent privileged):
    /// <list type="bullet">
    /// <item>band: every vertex of the footprint outside the band is raised or cut into it.</item>
    /// <item>slope: a straight ramp from the ground at the player's feet to the ground at the aimed point, full weight
    /// across its width and fading out over <see cref="Falloff"/> metres beside it.</item>
    /// <item>paint with an object filter: every vertex of the footprint gets the paint and keeps its height.</item>
    /// </list>
    /// The random share (the Engine's own <see cref="VertexHash"/>, so it picks what a stroke with the same seed would),
    /// the height band (not for band, where it is the operation) and the object filters are applied to the vertex list
    /// before it is sent.
    /// </summary>
    public static class VertexEdits
    {
        private const float Falloff = 1f;
        private const float DefaultWidth = 4f;
        private const float MaxWidth = 50f;
        private const float AimRange = 250f;

        public static TerrainEdit Band(TerrainRequest request, BrushStroke footprint, out string error)
        {
            error = null;
            if (!request.HasBand)
            {
                error = "band needs the band as min:max, for example 'ew terrain band 15 20:35'";
                return null;
            }
            VertexSet set = new VertexSet { Mode = VertexMode.Targets };
            float min = request.BandMin.Value, max = request.BandMax.Value;
            foreach (Vector2Int v in AreaShape.Vertices(footprint))
            {
                float height = TerrainRead.GroundHeight(new Vector3(v.x, 0f, v.y), float.NaN);
                float target = Mathf.Clamp(height, min, max);
                if (!float.IsNaN(height) && Mathf.Abs(target - height) > 0.005f)
                    set.Targets.Add(Target(v.x, v.y, target, 1f, request.Paint));
            }
            Filter(set, request, bandToo: false);
            return Finish(set, TerrainEdits.Flags(request), out error);
        }

        public static TerrainEdit Slope(TerrainRequest request, Player player, out string error)
        {
            error = null;
            if (!TryAim(out Vector3 end))
            {
                error = "aim at the ground where the slope should end";
                return null;
            }
            float width = Mathf.Clamp(request.Width ?? request.Radius ?? DefaultWidth, 1f, MaxWidth);
            VertexSet set = SlopeBuilder.Build(player.transform.position, end, width * 0.5f, Falloff, request.Paint);
            Filter(set, request, bandToo: true);
            return Finish(set, TerrainEdits.Flags(request), out error);
        }

        /// <summary>Paint only, with an object filter: the footprint's vertices with weight 0 (height kept) and the paint.</summary>
        public static TerrainEdit PaintOnly(TerrainRequest request, BrushStroke footprint, out string error)
        {
            VertexSet set = new VertexSet { Mode = VertexMode.Targets };
            foreach (Vector2Int v in AreaShape.Vertices(footprint))
                set.Targets.Add(Target(v.x, v.y, 0f, 0f, request.Paint));
            Filter(set, request, bandToo: true);
            return Finish(set, TerrainEdits.Flags(request), out error);
        }

        public static TargetVertex Target(int x, int z, float height, float weight, PaintOp paint)
        {
            return new TargetVertex { X = x, Z = z, Height = height, Weight = weight, Paint = paint, PaintStrength = paint != PaintOp.None ? 1f : 0f };
        }

        /// <summary>The vertex edit with the request's flags, or null when no vertex is left.</summary>
        public static TerrainEdit Finish(VertexSet set, EditFlags flags, out string error)
        {
            error = null;
            if (set.Targets.Count == 0)
            {
                error = "nothing to change there (no vertex left after the filters)";
                return null;
            }
            TerrainEdit edit = TerrainEdit.ForVertices(set, TerrainEdits.Source);
            edit.Flags = flags;
            return edit;
        }

        private static void Filter(VertexSet set, TerrainRequest request, bool bandToo)
        {
            if (request.Share < 1f)
                set.Targets.RemoveAll(t => VertexHash.Unit(t.X, t.Z, request.Seed) >= request.Share);
            if (bandToo && request.HasBand)
                set.Targets.RemoveAll(t => !InBand(t.X, t.Z, request.BandMin.Value, request.BandMax.Value));
            if (!request.HasObjectFilter || set.Targets.Count == 0)
                return;
            Bounds area = set.Area();
            ObjectFilter filter = ObjectFilter.For(request, area.center, Mathf.Max(area.extents.x, area.extents.z));
            set.Targets.RemoveAll(t => !filter.Keeps(t.X, t.Z));
        }

        private static bool InBand(int x, int z, float min, float max)
        {
            float height = TerrainRead.GroundHeight(new Vector3(x, 0f, z), float.NaN);
            return !float.IsNaN(height) && height >= min && height <= max;
        }

        /// <summary>The ground under the crosshair, or the brush centre while the brush tracks the aim.</summary>
        private static bool TryAim(out Vector3 point)
        {
            point = Vector3.zero;
            GameCamera camera = GameCamera.instance;
            if (camera != null && Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, AimRange, LayerMask.GetMask("terrain")))
            {
                point = hit.point;
                return true;
            }
            point = BrushState.Center;
            return BrushAim.Tracking;
        }
    }
}
