using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A sail's cloth alive in the wind, as the game's own sails are: a full, smooth belly blown out by the wind, deeper
    /// the harder it blows and the more it comes from behind; long, slow billows running over the cloth from its leading
    /// edge to its after one and from its head to its foot; luffing (the cloth flapping loose in bigger, quicker waves)
    /// with the wind from ahead or along it, or the sail reefed to half; easing and filling again as the game's wind drops
    /// and rises (<see cref="KwSailWind"/>); and a little swing after the ship as it rolls and pitches
    /// (<see cref="KwSailSwing"/>). Its cloth at a hoist is built twice, slack (hanging, hardly bellied) and full
    /// (<see cref="KwSailLayout.Fill"/>), and drawn between them thirty times a second, each point as free as it is
    /// (<see cref="KwSailPoints"/>: what holds the cloth, the yard, mast, boom and clews, stays put), shaded smooth
    /// (<see cref="KwSailWeld"/>), its bolt rope with it. Every motion is broad and slow: nothing jitters point by point.
    /// However it moves, a square sail's cloth and bolt rope never come into its mast (<see cref="KwSailClear"/>).
    /// Local to every client (nothing is sent: the wind is the same everywhere), only for sails in view within
    /// <see cref="Range"/> (half as often beyond <see cref="Near"/>), never on a dedicated server.
    /// </summary>
    public sealed class KwSailMotion : MonoBehaviour
    {
        private const float Every = 1f / 30f, Near = 35f, Range = 80f;

        private KwSailLayout? s;
        private float hoist, wait, period = Every;
        private Mesh? mesh;
        private LineRenderer? bolt;
        private Renderer? look;
        private KwSailWeld? weld;
        private KwSailClear? clear;
        private KwSailWind? wind;
        private readonly KwSailSwing swing = new KwSailSwing();
        private KwSailPoint[] points = new KwSailPoint[0], edge = new KwSailPoint[0];
        private Vector3[] placed = new Vector3[0], vertices = new Vector3[0], normals = new Vector3[0], edgeWork = new Vector3[0];
        private Vector3 lean;
        private Rigidbody? ship;
        private bool looked;

        /// <summary>What bends the cloth after the wind has placed it, if anything: a launched ship's sail drooping where it is no longer fastened (<see cref="KwSailDamageDroop"/>).</summary>
        public IKwClothBend? Bend { get; set; }

        /// <summary>The cloth at hoist f, alive from now on (the same component kept for the next hoist).</summary>
        public static void Pose(Transform cloth, KwSailLayout s, float f)
        {
            KwSailMotion motion = cloth.GetComponent<KwSailMotion>() ?? cloth.gameObject.AddComponent<KwSailMotion>();
            motion.Shape(s, f);
        }

        private void Shape(KwSailLayout layout, float f)
        {
            (s, hoist) = (layout, f);
            Mesh still = Built(layout, f, 0f), full = Built(layout, f, 1f);
            Vector3[] slack = still.vertices, filled = full.vertices;
            int[] triangles = full.triangles;
            weld = new KwSailWeld(slack, triangles);
            clear = KwSailClear.Of(layout, slack, filled, triangles);
            float most = KwSailPoints.Most(slack, filled);
            points = KwSailPoints.Of(layout, f, weld.Pick(slack), weld.Pick(filled), most);
            (placed, vertices, normals) = (new Vector3[points.Length], new Vector3[slack.Length], new Vector3[slack.Length]);
            Destroy(still);
            Swap(full);
            Edges(layout, f, most);
            look = GetComponent<Renderer>();
            if (wind == null)
                Begin();
            Draw();
        }

        // The cloth at a hoist and a fill (the layout set back to full after).
        private static Mesh Built(KwSailLayout layout, float f, float fill)
        {
            layout.Fill = fill;
            Mesh made = KwSailCanvas.Cloth(layout, f);
            layout.Fill = 1f;
            return made;
        }

        // The cloth's new mesh in place of the last one, which goes; written often from now on.
        private void Swap(Mesh full)
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            Mesh old = filter.sharedMesh;
            full.MarkDynamic();
            filter.sharedMesh = mesh = full;
            if (old != null && old != full)
                Destroy(old);
        }

        // The bolt rope's points, moved as the cloth's edge they lie on.
        private void Edges(KwSailLayout layout, float f, float most)
        {
            Transform? line = transform.Find(KwSailRigging.BoltName);
            bolt = line != null ? line.GetComponent<LineRenderer>() : null;
            edge = KwSailPoints.Of(layout, f, EdgeAt(layout, f, 0f), EdgeAt(layout, f, 1f), most);
            edgeWork = new Vector3[edge.Length];
        }

        private static Vector3[] EdgeAt(KwSailLayout layout, float f, float fill)
        {
            layout.Fill = fill;
            Vector3[] made = KwSailCanvas.Edge(layout, f).ConvertAll(KwMesh.ToUnity).ToArray();
            layout.Fill = 1f;
            return made;
        }

        // The wind as it is when the sail first comes, taken at once rather than eased into.
        private void Begin()
        {
            wind = new KwSailWind(Random.value);
            wind.Feel(KwSailAir.Here(transform, Vector3.zero), s!.Square, hoist, 0f);
        }

        private void Update()
        {
            if (s == null || mesh == null || wind == null || !KwIcons.CanRender)
                return;
            wait -= Time.deltaTime;
            if (wait > 0f)
                return;
            float dt = Mathf.Min(period - wait, 0.25f);
            Camera? eye = Utils.GetMainCamera();
            float far = eye != null ? (eye.transform.position - transform.position).sqrMagnitude : float.MaxValue;
            wait = period = far > Near * Near ? 2f * Every : Every;
            if ((look != null && !look.isVisible) || far > Range * Range)
                return;
            Feel(dt);
            Draw();
        }

        // The wind on the sail and the ship's pull on it. The sail's speed is its ship's body's (its physics, not where it
        // is drawn between frames); a sail ashore has none.
        private void Feel(float dt)
        {
            if (!looked)
                (ship, looked) = (GetComponentInParent<Rigidbody>(), true);
            Vector3 speed = ship != null ? ship.GetPointVelocity(transform.position) : Vector3.zero;
            wind!.Feel(KwSailAir.Here(transform, speed), s!.Square, hoist, dt);
            float slack = 1f - (s.Square ? 0.75f : 0.5f) * Mathf.Min(Mathf.Abs(wind.Fill), 1f);
            float length = s.Square ? hoist * s.Height : 0.5f * Mathf.Min(s.Width, hoist * s.Height);
            lean = swing.Feel(transform.rotation, speed, Time.time, length, slack, s.Square, dt);
        }

        private void Draw()
        {
            KwSailNow now = wind!.Now(s!, hoist, lean);
            for (int k = 0; k < points.Length; k++)
                placed[k] = now.At(points[k]);
            Bend?.Bend(points, placed);
            clear?.Keep(placed);
            weld!.Spread(placed, vertices);
            weld.Spread(weld.Normals(placed, now.Normal), normals);
            mesh!.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            if (bolt == null || bolt.positionCount != edge.Length)
                return;
            for (int i = 0; i < edge.Length; i++)
                edgeWork[i] = now.At(edge[i]);
            Bend?.Bend(edge, edgeWork);
            clear?.Keep(edgeWork);
            bolt.SetPositions(edgeWork);
        }
    }
}
