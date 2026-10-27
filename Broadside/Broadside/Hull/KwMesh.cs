using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One piece's kind and shape: a skin through stations (generated planking; a flat stern closes its last station), a
    /// deck (the stations' section at <see cref="DeckZ"/>), or a shape of its own. The pivot (x, z, in the piece's own
    /// coordinates) is the piece's origin in the world.
    /// </summary>
    public sealed class KwDef
    {
        public KwDef(string name, string label, string kind)
        {
            Name = name;
            Label = label;
            Kind = kind;
        }

        public string Name { get; }
        public string Label { get; }
        public string Kind { get; }

        /// <summary>The hammer's description under the name (a tool's actions, one a line, <see cref="KwToolText"/>); null for the default.</summary>
        public string? Description { get; set; }
        public List<Station> Stations { get; set; } = new List<Station>();
        public bool FlatFore { get; set; }

        /// <summary>A flat stern closes its first station instead (a part laid from a flat stern to another keel end).</summary>
        public bool FlatAft { get; set; }

        /// <summary>
        /// Planking: the level keel line's height in the piece's frame, which its side strakes are laid level from
        /// (<see cref="KwGirth"/>); 0 but on a part starting partway along a stem, whose rib stands on the climbed keel.
        /// </summary>
        public float Level { get; set; }

        /// <summary>
        /// Planking: whether it meets its neighbouring part at a rib at its first station (every part but one laid from keel
        /// end to keel end) and at its last (a part between two ribs), where its strakes' texture comes out as the
        /// neighbour's does (<see cref="KwGirth"/>).
        /// </summary>
        public bool RibStart { get; set; }
        public bool RibEnd { get; set; }
        public float DeckZ { get; set; }

        /// <summary>A deck's height at its last station; NaN for a level deck at <see cref="DeckZ"/>.</summary>
        public float DeckZEnd { get; set; } = float.NaN;

        /// <summary>
        /// A deck's port edge of its own, as (x, half width) along it; null for the stations' inside at its height
        /// (<see cref="KwShapes.InsideWidth"/>). A hold's floor has its own: it stops where the hull grows too narrow (<see cref="KwSole"/>).
        /// </summary>
        public List<Vector2>? Edge { get; set; }
        public Vector2 Pivot { get; set; }

        /// <summary>Parts with a mesh of their own under the model (a keel block's cap): name, kit position, shape.</summary>
        public List<(string Name, Vector3 Kit, System.Action<MeshBuilder> Shape)> Parts { get; } = new List<(string Name, Vector3 Kit, System.Action<MeshBuilder> Shape)>();

        /// <summary>The whole piece is a keel support that fits the keel resting on it (<see cref="KwSupport"/>).</summary>
        public bool Support { get; set; }

        /// <summary>Runs on the finished prefab copy (inactive, on the bench): add or configure the feature's own components.</summary>
        public System.Action<GameObject>? Setup { get; set; }

        /// <summary>
        /// A workshop model the piece wears instead of a generated mesh (the blueprint table's, <see cref="KwTableModel"/>):
        /// given the scene and the stripped piece copy, it puts the model child in place with its colliders and returns it;
        /// null (no bundle) falls back to <see cref="Shape"/>.
        /// </summary>
        public System.Func<ZNetScene, GameObject, GameObject?>? Model { get; set; }

        /// <summary>A piece's own shape (dock, keel, rib, tool, railing), its snap points, and whether it wears the plank wood.</summary>
        public System.Action<MeshBuilder>? Shape { get; set; }
        public List<(string Name, Vector3 Kit)> Snaps { get; } = new List<(string Name, Vector3 Kit)>();

        /// <summary>A deck's hatch openings, kit x along it and y across, left out of its boards (<see cref="KwDeckHoles"/>).</summary>
        public List<Rect> Holes { get; } = new List<Rect>();

        /// <summary>How a deck's top and underside are cut into boards, its tier's (<see cref="KwDeckBoards"/>); null, one whole slab.</summary>
        public KwDeckLayout? Boards { get; set; }

        /// <summary>A deck's own seed for its boards, saved with its span (<see cref="KwSpan.Seed"/>; 0 for a deck saved before seeds).</summary>
        public int Seed { get; set; }
        public bool Wood { get; set; }

        /// <summary>Planking: the openings cut through it (<see cref="KwOpening"/>, those that fit its sides), left out of its strakes.</summary>
        public List<KwOpening> Openings { get; } = new List<KwOpening>();
    }

    /// <summary>
    /// Meshes for Broadside's pieces: every face drawn on both sides, planking and decks with real thickness (<see cref="KwPlanks"/>), mapped onto
    /// their biome tier's textures (<see cref="KwUv"/>): a skin's outside and inside on their own bands, strake by strake,
    /// the strakes level along the hull's sides and one width round its bottom (<see cref="KwStrakes"/>);
    /// decks on their boards, laid board by board when the deck has its tier's layout (<see cref="KwDeckBoards"/>), or as
    /// one whole slab for a collider. Kit axes turned into Unity's (kit x forward is Unity z, kit y port is Unity -x, kit z
    /// up is Unity y).
    /// </summary>
    public static class KwMesh
    {
        public static Vector3 ToUnity(Vector3 kit) => new Vector3(-kit.y, kit.z, kit.x);

        public static Vector3 ToKit(Vector3 unity) => new Vector3(unity.z, -unity.x, unity.y);

        /// <summary>A piece's mesh; solid, a deck as one whole slab whatever its boards (the same faces as before boards, for its collider).</summary>
        public static Mesh Build(KwDef def, bool solid = false)
        {
            MeshBuilder builder = new MeshBuilder(new Vector3(def.Pivot.x, 0f, def.Pivot.y));
            if (def.Shape != null)
                def.Shape(builder);
            else if (def.Kind == "deck")
                Deck(builder, def, solid ? null : def.Boards);
            else if (def.Stations.Count > 1)
                Skin(builder, def);
            return builder.ToMesh(def.Name);
        }

        // A skin: strakes level along the sides from the keel line and one width round the bottom, measured on its stations
        // (KwGirth), both sides, its ends closed, a flat stern's transom board in the same rows.
        private static void Skin(MeshBuilder builder, KwDef def)
        {
            List<Station> st = def.Stations;
            KwGirth girth = KwGirth.Of(def);
            builder.KeepsRows = true;
            foreach (float side in new[] { 1f, -1f })
            {
                for (int s = 0; s + 1 < st.Count; s++)
                    KwStrakes.Gap(builder, girth, s, side);
                KwStrakes.EndCap(builder, girth, 0, side);
                KwStrakes.EndCap(builder, girth, st.Count - 1, side);
            }
            if (def.FlatFore)
                KwPlanks.TransomBoard(builder, st[st.Count - 1], 1f, def.Level);
            if (def.FlatAft)
                KwPlanks.TransomBoard(builder, st[0], -1f, def.Level);
        }

        // A deck: its top and underside laid as boards when it has a layout, its edges (and without boards its top and
        // underside whole) on the tier's deck boards running along the ship, 4 m a texture (KwUv.Deck).
        private static void Deck(MeshBuilder builder, KwDef def, KwDeckLayout? boards)
        {
            System.Func<float, float> z = DeckHeight(def);
            List<Vector2> edge = def.Edge ?? KwShapes.DeckEdge(def.Stations, z, KwShapes.InsideWidth);
            float x0 = edge[0].x;
            System.Func<Vector3, Vector2> uv = p => KwUv.Deck(p, x0);
            if (boards != null)
                KwDeckBoards.Lay(builder, edge, z, def.Holes, boards, KwDeckBoards.Seed(def));
            if (def.Holes.Count > 0)
            {
                KwDeckHoles.Slab(builder, edge, z, def.Holes, uv, boards != null);
                return;
            }
            List<Vector3> outline = Outline(edge, z);
            if (boards != null)
            {
                KwPlanks.DeckEdges(builder, outline, uv);
                return;
            }
            builder.Polygon(outline, outline.ConvertAll(p => uv(p)));
            KwPlanks.DeckSlab(builder, outline, uv);
        }

        // A deck's top all round: up its port edge, back down its starboard edge.
        private static List<Vector3> Outline(List<Vector2> edge, System.Func<float, float> z)
        {
            List<Vector3> outline = new List<Vector3>();
            foreach (Vector2 p in edge)
                outline.Add(new Vector3(p.x, p.y, z(p.x)));
            for (int i = edge.Count - 1; i >= 0; i--)
            {
                if (edge[i].y > 1e-4f)
                    outline.Add(new Vector3(edge[i].x, -edge[i].y, z(edge[i].x)));
            }
            return outline;
        }

        // Level at DeckZ, or sloping evenly from DeckZ at the first station to DeckZEnd at the last.
        public static System.Func<float, float> DeckHeight(KwDef def)
        {
            float x0 = def.Stations[0].X, x1 = def.Stations[def.Stations.Count - 1].X;
            if (float.IsNaN(def.DeckZEnd) || x1 - x0 < 1e-3f)
                return _ => def.DeckZ;
            return x => Mathf.Lerp(def.DeckZ, def.DeckZEnd, (x - x0) / (x1 - x0));
        }
    }

    /// <summary>Collects double-sided fan polygons (kit coordinates, a UV per corner) into one Unity mesh around a pivot.</summary>
    public sealed class MeshBuilder
    {
        /// <summary>The UV channel (TEXCOORD2) a planking mesh carries each vertex's strake and side in, for <see cref="KwPlankShift"/>.</summary>
        public const int RowChannel = 2;

        private readonly Vector3 pivot;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<Vector2> rows = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        public MeshBuilder(Vector3 pivot) => this.pivot = pivot;

        /// <summary>
        /// The strake the next faces belong to and its side, (k, 1 port, -1 starboard, 0 across the ship), and whether the
        /// mesh keeps them in its <see cref="RowChannel"/>: a planking part's and its bands' do, so <see cref="KwPlankShift"/>
        /// can start each strake at its own place in the texture once the part's place is known.
        /// </summary>
        public Vector2 Row { get; set; }
        public bool KeepsRows { get; set; }

        public void Polygon(List<Vector3> kit, List<Vector2> uv) => Polygon(kit, uv, uv);

        /// <summary>
        /// One face only: for a material that draws both sides itself (the game's cloth: two faces there fight), or a face
        /// only ever seen from one side (a deck board's top). Seen from the side its corners' order points to (anticlockwise
        /// seen from there, kit axes), as <see cref="Polygon(List{Vector3}, List{Vector2}, List{Vector2})"/>'s front face.
        /// </summary>
        public void Face(List<Vector3> kit, List<Vector2> uv)
        {
            if (kit.Count >= 3)
                Fan(kit, uv, false);
        }

        /// <summary>A polygon's front face (its corners' order) with one set of UVs, its back face with another.</summary>
        public void Polygon(List<Vector3> kit, List<Vector2> front, List<Vector2> back)
        {
            if (kit.Count < 3)
                return;
            Fan(kit, front, false);
            Fan(kit, back, true);
        }

        private void Fan(List<Vector3> ring, List<Vector2> uv, bool back)
        {
            int start = vertices.Count;
            Vector3 centre = Vector3.zero;
            Vector2 middle = Vector2.zero;
            for (int i = 0; i < ring.Count; i++)
            {
                centre += ring[i] / ring.Count;
                middle += uv[i] / ring.Count;
            }
            Add(centre, middle);
            for (int i = 0; i < ring.Count; i++)
                Add(ring[i], uv[i]);
            // Kit axes to Unity's is a mirror (determinant -1), which turns a winding round: the front, seen from the side
            // the kit corners' order points to, takes (start, b, a) in Unity.
            for (int i = 0; i < ring.Count; i++)
            {
                int a = start + 1 + i, b = start + 1 + (i + 1) % ring.Count;
                triangles.AddRange(back ? new[] { start, a, b } : new[] { start, b, a });
            }
        }

        /// <summary>Every UV gathered so far moved by a function (a post cap's planar UVs onto its keel texture's carving swatch).</summary>
        public void MapUv(System.Func<Vector2, Vector2> map)
        {
            for (int i = 0; i < uvs.Count; i++)
                uvs[i] = map(uvs[i]);
        }

        private void Add(Vector3 kit, Vector2 uv)
        {
            vertices.Add(KwMesh.ToUnity(kit - pivot));
            uvs.Add(uv);
            rows.Add(Row);
        }

        public Mesh ToMesh(string name)
        {
            Mesh mesh = new Mesh { name = name };
            if (vertices.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            if (KeepsRows)
                mesh.SetUVs(RowChannel, rows);
            mesh.SetColors(new List<Color>(new Color[vertices.Count]).ConvertAll(_ => Color.white));
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
