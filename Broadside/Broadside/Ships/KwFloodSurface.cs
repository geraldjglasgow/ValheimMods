using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The water inside a launched ship's hold, drawn compartment by compartment (<see cref="KwFloodHolds"/>): each wet
    /// compartment's flat surface at its own level, rising as it floods, from each of the hull's cross-sections in it
    /// (<see cref="KwHullSlices.Rings"/>) to the next, across the hull where the surface meets its sides
    /// (<see cref="KwFloodFaces.Chord"/>); and at a bulkhead where the water stands higher on one side, the water's end
    /// against it, down to the other side's surface (<see cref="KwFloodFaces.Face"/>). Its top in the game's own sea
    /// material, still (<see cref="KwFloodWater.Sea"/>), its underside and ends opaque, seen from under the water or from
    /// the side (<see cref="KwFloodWater.Under"/>). Where the sea has taken over a compartment (its water up to the sea's,
    /// or the ship sinking: <see cref="KwShipSeaInside"/>) the top is not drawn, the sea's own surface being the water
    /// there, at the same height in the same water, so the one hands over to the other with no second surface; its
    /// underside stays, at the sea's line, so from under the water the surface is still overhead. Never the sea itself:
    /// the sea outside and the mask that keeps it out of the dry hold are not touched. Made again a few times a second
    /// while a level, the sea's line or the ship's tilt moves it; hidden while the hold is dry. Only where there is a screen.
    /// </summary>
    public sealed class KwFloodSurface : MonoBehaviour
    {
        private const float Every = 0.2f, Moved = 0.004f, Turned = 0.3f, Step = 0.02f, Near = 0.02f, Tile = 64.2f;
        private static readonly Vector4 Tangent = new Vector4(1f, 0f, 0f, -1f);
        private static readonly int[] Corners = { 0, 2, 3, 0, 3, 1 };

        private KwShipFlood? flood;
        private KwShipSeaInside? sea;
        private List<(float Z, Vector2[] Shape)> rings = new List<(float, Vector2[])>();
        private Mesh? mesh;
        private MeshRenderer? shown;
        private float at = -99f;
        private float[] drawn = new float[0];
        private Vector3 drawnUp;
        private bool dressed;
        private readonly List<Vector3> points = new List<Vector3>(), normals = new List<Vector3>(), sheet = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<Vector4> tangents = new List<Vector4>();
        private readonly List<int> tops = new List<int>(), unders = new List<int>(), fan = new List<int>();

        public static void Add(Transform ship, KwShipFlood flood, List<(float Z, Vector2[] Shape)> rings)
        {
            Transform visual = ship.Find(KwShipPrefab.Visual);
            GameObject holder = new GameObject("flood") { layer = visual.gameObject.layer };
            holder.transform.SetParent(visual, false);
            KwFloodSurface surface = holder.AddComponent<KwFloodSurface>();
            surface.mesh = new Mesh { name = "KW_ship_flood", subMeshCount = 2 };
            holder.AddComponent<MeshFilter>().sharedMesh = surface.mesh;
            MeshRenderer renderer = holder.AddComponent<MeshRenderer>();
            (renderer.shadowCastingMode, renderer.enabled) = (UnityEngine.Rendering.ShadowCastingMode.Off, false);
            holder.AddComponent<KwMeshOwner>();
            (surface.flood, surface.sea, surface.rings, surface.shown) = (flood, ship.GetComponent<KwShipSeaInside>(), rings, renderer);
            surface.Dress();
        }

        private void Update()
        {
            KwFloodHolds? holds = flood != null ? flood.Holds : null;
            if (holds == null || shown == null)
                return;
            if (shown.enabled)
                KwFloodWater.Tick();
            if (Time.time - at < Every)
                return;
            at = Time.time;
            if (!dressed && KwFloodWater.Sea() != null)
                Dress();
            shown.enabled = flood!.Solved || (sea != null && sea.Sinking);
            if (shown.enabled && Changed(holds))
                Build(holds);
        }

        // The top in the sea's water once a sea has loaded (until then the underside's), the underside opaque.
        private void Dress()
        {
            Material? top = KwFloodWater.Sea();
            dressed = top != null;
            shown!.sharedMaterials = new[] { top != null ? top : KwFloodWater.Under(), KwFloodWater.Under() };
            KwShipMaterials.Apply(transform);
        }

        // Whether a compartment's level, its being wet or the sea's, or the surfaces' tilt moved since it was last drawn;
        // always while the sea is the water somewhere (its line moves with the waves).
        private bool Changed(KwFloodHolds holds)
        {
            bool changed = drawn.Length != holds.Holds || Vector3.Angle(holds.Up, drawnUp) >= Turned;
            if (drawn.Length != holds.Holds)
                drawn = new float[holds.Holds];
            for (int k = 0; k < holds.Holds; k++)
            {
                float level = Seas(k) ? float.PositiveInfinity : holds.Wet(k) ? holds.LevelOf(k) : float.NaN;
                changed |= float.IsInfinity(level) || float.IsNaN(level) != float.IsNaN(drawn[k]) || Mathf.Abs(level - drawn[k]) >= Moved;
                drawn[k] = level;
            }
            drawnUp = holds.Up;
            return changed;
        }

        private bool Seas(int k) => sea != null && sea.Sea(k);

        // Every compartment's surface (the sea's line where the sea has it) and every bulkhead's water end, into the mesh.
        private void Build(KwFloodHolds holds)
        {
            points.Clear();
            normals.Clear();
            uvs.Clear();
            tangents.Clear();
            tops.Clear();
            unders.Clear();
            for (int k = 0; k < holds.Holds; k++)
            {
                if (Seas(k) || holds.Wet(k))
                    Surface(holds, k, Seas(k));
            }
            for (int b = 0; b + 1 < holds.Holds; b++)
                End(holds, b);
            mesh!.Clear();
            mesh.subMeshCount = 2;
            mesh.SetVertices(points);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTangents(tangents);
            mesh.SetTriangles(tops, 0);
            mesh.SetTriangles(unders, 1);
        }

        // A strip from each section's chord at the compartment's surface to the next, within the compartment: its top
        // (not where the sea has it) and its underside.
        private void Surface(KwFloodHolds holds, int k, bool seas)
        {
            float aft = holds.Aft(k) - Near, fore = holds.Fore(k) + Near;
            sheet.Clear();
            fan.Clear();
            int previous = -1;
            for (int r = 0; r < rings.Count; r++)
            {
                (float z, Vector2[] shape) = rings[r];
                if (z < aft || z > fore)
                    continue;
                if (!Plane(holds, k, r, seas, out Vector3 up, out float level) || !KwFloodFaces.Chord(shape, z, up, level, out Vector3 port, out Vector3 starboard))
                {
                    previous = -1;
                    continue;
                }
                sheet.Add(port);
                sheet.Add(starboard);
                if (previous >= 0)
                    Quad(previous);
                previous = sheet.Count - 2;
            }
            Sheet(seas ? Vector3.up : holds.Up, !seas, true);
        }

        // Two triangles between the chord from previous (port, starboard) and the next one after it.
        private void Quad(int previous)
        {
            foreach (int corner in Corners)
                fan.Add(previous + corner);
        }

        // The plane a compartment's surface lies in at section r: its own water's, or where the sea has it the sea's line there.
        private bool Plane(KwFloodHolds holds, int k, int r, bool seas, out Vector3 up, out float level)
        {
            (up, level) = (holds.Up, holds.LevelOf(k));
            if (!seas)
                return true;
            if (sea == null || r >= sea.Found.Length || !sea.Found[r])
                return false;
            (up, level) = (new Vector3(sea.Normals[r].x, sea.Normals[r].y, 0f), sea.Levels[r]);
            return true;
        }

        // At bulkhead b, where the water stands higher on one side than the other (neither side the sea's): its end
        // against the bulkhead, the section there from the higher surface down to the lower one (to the bottom, with the
        // lower side dry).
        private void End(KwFloodHolds holds, int b)
        {
            bool aftWet = holds.Wet(b), foreWet = holds.Wet(b + 1);
            if ((!aftWet && !foreWet) || Seas(b) || Seas(b + 1))
                return;
            float aft = aftWet ? holds.LevelOf(b) : float.NegativeInfinity, fore = foreWet ? holds.LevelOf(b + 1) : float.NegativeInfinity;
            float z = holds.Fore(b), high = Mathf.Max(aft, fore), low = Mathf.Min(aft, fore);
            int ring = Ring(z);
            if (ring < 0 || high - low < Step)
                return;
            KwFloodFaces.Face(rings[ring].Shape, z, holds.Up, high, low, aftWet && foreWet, sheet);
            fan.Clear();
            for (int i = 1; i + 1 < sheet.Count; i++)
            {
                fan.Add(0);
                fan.Add(i);
                fan.Add(i + 1);
            }
            Sheet(Vector3.forward, false, true, true); // the section's corners run round it so that its front faces forward
        }

        // The cross-section at a place along the ship (-1 where there is none).
        private int Ring(float z)
        {
            for (int i = 0; i < rings.Count; i++)
            {
                if (Mathf.Abs(rings[i].Z - z) <= Near)
                    return i;
            }
            return -1;
        }

        // The sheet's points and triangles into the mesh: as they are, facing the way given, into the top (when top), and
        // turned over into the underside (when under); both into the underside, for an end seen from either side.
        private void Sheet(Vector3 facing, bool top, bool under, bool both = false)
        {
            if (fan.Count == 0)
                return;
            if (top || both)
                Side(facing, false, both ? unders : tops);
            if (under)
                Side(-facing, true, unders);
        }

        // One side of the sheet: its points, facing the way given, and its triangles (turned over when flipped).
        private void Side(Vector3 facing, bool flipped, List<int> into)
        {
            int start = points.Count;
            foreach (Vector3 p in sheet)
            {
                points.Add(p);
                normals.Add(facing);
                uvs.Add(new Vector2(p.x / Tile, p.z / Tile));
                tangents.Add(Tangent);
            }
            for (int i = 0; i < fan.Count; i += 3)
            {
                into.Add(start + fan[i]);
                into.Add(start + fan[flipped ? i + 2 : i + 1]);
                into.Add(start + fan[flipped ? i + 1 : i + 2]);
            }
        }
    }
}
