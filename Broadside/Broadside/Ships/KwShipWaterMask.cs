using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Keeps the sea out of a launched ship's dry hold on screen, as the game's ships do: an unseen thin slab inside the
    /// hull in the game's own water mask material (the karve's <c>watermask</c>, <c>Custom/WaterMask</c>: drawn after the
    /// solid world, depth-tested, marking stencil 2 where it shows, so the sea drawn after it leaves those pixels alone).
    /// Wherever the mask shows, the sea is not drawn, in front of it or behind, so it must never show from outside: once
    /// it did (a slab fixed round the design waterline, lofted to a sliver fin past the raked stem and stern and through
    /// the planking near them; a sinking hull looked dry under a pale strip of sea, and the sea floor showed at the
    /// stern). The slab stands round the sea's actual surface in the hull, worked out again as the ship rides
    /// (<see cref="KwShipSeaInside"/>), however low it sits, heels or pitches. Each cross-section is drawn in past the
    /// planking's inside face, cut to the band round the sea's line there and over the hold's water, and a run of them
    /// stops at the last that reaches the band (<see cref="KwMaskShape"/>, <see cref="KwMaskSections"/>), so it hides the
    /// sea only where the sea passes through the dry hold. A compartment whose water has reached the sea, or a sinking
    /// ship, is not masked at all: the sea shows in it and is its water. Built again only when the lines or the water
    /// move it; only where there is a screen.
    /// </summary>
    public sealed class KwShipWaterMask : MonoBehaviour
    {
        private const float Near = 0.01f, Still = 0.005f;
        private KwShipSeaInside? sea;
        private List<KwMaskSection> sections = new List<KwMaskSection>();
        private Mesh? mesh;
        private MeshRenderer? shown;
        private Bounds whole;
        private int seen = -1;
        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Vector2[]> bands = new List<Vector2[]>();
        private readonly List<float> places = new List<float>();
        private readonly List<Vector2[]> spare = new List<Vector2[]>();
        private readonly List<float> now = new List<float>(), last = new List<float>();

        /// <summary>The ship's sea in its hull (<see cref="KwShipSeaInside"/>) and, with the game's mask material, the mask.</summary>
        public static void Add(Transform ship, List<(float Z, Vector2[] Shape)> sections, float waterline)
        {
            KwShipSeaInside sea = ship.gameObject.AddComponent<KwShipSeaInside>();
            sea.Set(sections, waterline);
            Material? material = ZNetScene.instance?.GetPrefab("VikingShip")?.transform.Find("ship/visual/watermask")?.GetComponent<MeshRenderer>()?.sharedMaterial;
            if (material == null || sections.Count < 2)
                return;
            GameObject holder = new GameObject("watermask") { layer = LayerMask.NameToLayer("vehicle") };
            holder.transform.SetParent(ship.Find(KwShipPrefab.Visual), false);
            Mesh mesh = new Mesh { name = "KW_ship_watermask", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.MarkDynamic();
            holder.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = holder.AddComponent<MeshRenderer>();
            (renderer.sharedMaterial, renderer.shadowCastingMode, renderer.receiveShadows, renderer.enabled) = (material, UnityEngine.Rendering.ShadowCastingMode.Off, false, false);
            holder.AddComponent<KwMeshOwner>();
            holder.AddComponent<KwShipWaterMask>().Set(sea, sections, renderer, mesh);
        }

        // The sections the slab is lofted through, drawn in once (KwMaskSections); the bounds it can ever fill.
        private void Set(KwShipSeaInside inside, List<(float Z, Vector2[] Shape)> rings, MeshRenderer renderer, Mesh made)
        {
            (sea, shown, mesh) = (inside, renderer, made);
            sections = KwMaskSections.Of(rings);
            whole = new Bounds(new Vector3(0f, rings[0].Shape[0].y, rings[0].Z), Vector3.zero);
            foreach ((float z, Vector2[] shape) in rings)
            {
                foreach (Vector2 p in shape)
                    whole.Encapsulate(new Vector3(p.x, p.y, z));
            }
        }

        // After the sea's lines are worked out (KwShipSeaInside.Update), the slab again when they changed.
        private void LateUpdate()
        {
            if (sea == null || mesh == null || shown == null || sea.Version == seen)
                return;
            seen = sea.Version;
            if (Moved(sea))
                Build(sea);
        }

        // Whether the sea's lines, the hold's water or the sea's compartments moved since the slab was built (by Still or
        // more), so a ship at rest in a calm sea is not built again ten times a second.
        private bool Moved(KwShipSeaInside inside)
        {
            now.Clear();
            for (int r = 0; r < inside.Found.Length; r++)
            {
                now.Add(inside.Found[r] ? inside.Levels[r] : float.NaN);
                now.Add(inside.Normals[r].x);
            }
            KwFloodHolds? holds = inside.Holds;
            for (int k = 0; holds != null && k < holds.Holds; k++)
                now.Add(inside.Sea(k) ? float.PositiveInfinity : holds.Wet(k) ? holds.LevelOf(k) : float.NaN);
            if (holds != null)
            {
                now.Add(holds.Up.x);
                now.Add(holds.Up.z);
            }
            bool moved = now.Count != last.Count;
            for (int i = 0; !moved && i < now.Count; i++)
                moved = Differ(now[i], last[i]);
            if (!moved)
                return false;
            last.Clear();
            last.AddRange(now);
            return true;
        }

        private static bool Differ(float a, float b) =>
            float.IsNaN(a) || float.IsNaN(b) ? float.IsNaN(a) != float.IsNaN(b) : !(Mathf.Abs(a - b) < Still) && a != b;

        // The slab compartment by compartment, none where the sea has taken over; the whole hull as one without a hold.
        private void Build(KwShipSeaInside inside)
        {
            points.Clear();
            triangles.Clear();
            KwFloodHolds? holds = inside.Holds;
            for (int k = 0; k < (holds != null ? holds.Holds : 1); k++)
            {
                if (holds == null)
                    Run(inside, null, 0, float.MinValue, float.MaxValue);
                else if (!inside.Sea(k))
                    Run(inside, holds, k, holds.Aft(k) - Near, holds.Fore(k) + Near);
            }
            mesh!.Clear();
            shown!.enabled = triangles.Count > 0;
            if (triangles.Count == 0)
                return;
            mesh.SetVertices(points);
            mesh.SetTriangles(triangles, 0, false);
            mesh.bounds = whole;
        }

        // One compartment's sections, each cut to its band, lofted in runs of neighbours that have one.
        private void Run(KwShipSeaInside inside, KwFloodHolds? holds, int k, float aft, float fore)
        {
            bool wet = holds != null && holds.Wet(k);
            Vector3 up = holds != null ? holds.Up : Vector3.up;
            float level = wet ? holds!.LevelOf(k) : 0f;
            foreach (KwMaskSection section in sections)
            {
                if (section.At < aft || section.At > fore)
                    continue;
                if (!Section(inside, section, wet, new Vector2(up.x, up.y), level - up.z * section.At))
                    Flush();
            }
            Flush();
        }

        // A section's band added to the run: false where it has none (no sea found, too small, all under the water).
        private bool Section(KwShipSeaInside inside, KwMaskSection section, bool wet, Vector2 waterUp, float water)
        {
            Vector2[] band = Next();
            if (section.Inner == null || !KwMaskSections.Line(section, inside.Normals, inside.Levels, inside.Found, out Vector2 n, out float d)
                || !KwMaskShape.Band(section.Inner, n, d, wet, waterUp, water, band))
            {
                spare.Add(band);
                return false;
            }
            bands.Add(band);
            places.Add(section.Z);
            return true;
        }

        // The run so far lofted into the slab, and a new one begun.
        private void Flush()
        {
            KwMaskShape.Loft(bands, places, points, triangles);
            spare.AddRange(bands);
            bands.Clear();
            places.Clear();
        }

        // A band's points, reused from the last build where one is spare.
        private Vector2[] Next()
        {
            if (spare.Count == 0)
                return new Vector2[KwMaskShape.Round];
            Vector2[] band = spare[spare.Count - 1];
            spare.RemoveAt(spare.Count - 1);
            return band;
        }
    }
}
