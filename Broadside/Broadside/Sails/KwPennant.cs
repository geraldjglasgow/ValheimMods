using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A long, thin pennant flown from the staff over a masthead (<see cref="KwMastTops.Pennant"/>, over a crow's nest
    /// there, <see cref="KwMastTops.StaffTop"/>): its hoist bound to the staff's face by rope lashings, its head just under
    /// the staff's truck (<see cref="KwPennantHoist"/>), its first part (a tenth of its length, at least the hoist's width)
    /// running straight out from the staff square to it, as a flag on a staff leaves it (the user, 2026-11-03: it was "not
    /// really attached to anything", a point in the air beside the staff). Past that it streams the way the wind blows
    /// over the ship as it sails (the apparent wind the sails swing to, <see cref="KwApparentWind"/>: the game's wind less
    /// the ship's own speed; the game's wind alone on a dock), near level in a strong wind and drooping as the wind falls,
    /// square to the world's up however the ship heels or the mast rakes, and flutters in slow, even waves running out to
    /// its narrow tip, a little quicker and wider the harder it blows, the droop and the waves building up from nothing
    /// where the straight part ends (the user, 2026-10-26: they moved "too fast for what the wind is" and were "off axis
    /// and uneven": the true wind against sails trimmed to the apparent one, the mast's own down, 1 to 3.5 waves a second,
    /// a 30° twist). Its cloth carries what the sails' cloth does for the cloth shader (white vertex colours, tangents for
    /// its relief, both sides lit by the shader's two-sided normals), so it reads as the same cloth, not black against the
    /// sky. Its shape comes from the game's wind, the ship's speed as this peer sees it and the network's clock alone
    /// (<see cref="Shape"/>), so every player sees the same pennant without anything sent. Drawn every frame within
    /// <see cref="Near"/>, twenty times a second beyond, not at all beyond <see cref="Range"/> or out of view; never made
    /// on a dedicated server (<see cref="KwShipBody.Drawn"/>).
    /// </summary>
    public sealed class KwPennant : MonoBehaviour
    {
        public const string Name = "pennant";

        // The faces past the straight part, and the points along it: the hoist, the straight part's end, then one a face.
        private const int Segments = 12, Stations = Segments + 2;
        private const float Far = 1f / 20f, Near = 35f, Range = 80f;

        // How much of the free part, past the straight part, the droop, sway and twist take to build up to their full.
        private const float Ramp = 0.3f;

        // The staff's axis in the pennant's own axes: the mast's up.
        private static readonly Vector3 Staff = Vector3.up;

        private KwPennantHoist hoist;
        private float wait;
        private Mesh? mesh;
        private Renderer? look;
        private KwShipWind? ship;
        private Vector3[] vertices = new Vector3[2 * Stations];
        private bool broken;

        /// <summary>The mast's pennant under it (or none, the old one gone), its hoist on its staff under the truck, lashed to it.</summary>
        public static void Add(Transform mast, KwMastSpec spec)
        {
            Transform? old = mast.Find(Name);
            if (old != null)
            {
                old.name = Name + "_gone";
                Destroy(old.gameObject);
            }
            if (!spec.Tops.HasPennant || !KwShipBody.Drawn)
                return;
            KwPennantHoist hoist = new KwPennantHoist(spec.Mast, spec.Tops.StaffTop(spec.Mast));
            GameObject made = new GameObject(Name) { layer = mast.gameObject.layer };
            made.transform.SetParent(mast, false);
            made.transform.localPosition = KwMesh.ToUnity(new Vector3(0f, 0f, hoist.Top));
            made.AddComponent<MeshFilter>();
            MeshRenderer renderer = made.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = KwPennantLook.Of(spec.Tops.Pennant, spec);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            made.AddComponent<KwMeshOwner>();
            made.AddComponent<KwPennant>().hoist = hoist;
            hoist.Lash(made.transform);
        }

        private void Start()
        {
            mesh = new Mesh { name = "KW_pennant" };
            mesh.MarkDynamic();
            Shape(vertices, hoist, Vector3.back, Vector3.up, 0f, 0f);
            mesh.vertices = vertices;
            mesh.uv = Uvs(hoist.Root / hoist.Length);
            mesh.colors = White(vertices.Length);
            mesh.triangles = Triangles();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            look = GetComponent<Renderer>();
            ship = GetComponentInParent<KwShipWind>();
            Draw();
        }

        // A throw is reported once and stops this pennant, rather than once a frame.
        private void Update()
        {
            if (broken || mesh == null || !KwIcons.CanRender)
                return;
            wait -= Time.deltaTime;
            if (wait > 0f)
                return;
            Camera? eye = Utils.GetMainCamera();
            float far = eye != null ? (eye.transform.position - transform.position).sqrMagnitude : float.MaxValue;
            wait = far > Near * Near ? Far : 0f;
            if ((look != null && !look.isVisible) || far > Range * Range)
                return;
            broken = true;
            Guard.Run("Broadside pennant", static p => p.Draw(), this);
            broken = false;
        }

        // The pennant as the wind over the ship has it now, in its own axes, shaded smooth with its relief's tangents.
        private void Draw()
        {
            (Vector3 wind, float strength) = Air();
            float now = ZNet.instance != null ? (float)(ZNet.instance.GetTimeSeconds() % 3600.0) : Time.time;
            Vector3 up = transform.InverseTransformDirection(Vector3.up);
            Shape(vertices, hoist, transform.InverseTransformDirection(wind), up, strength, now);
            mesh!.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// The wind over the pennant (world axes, level) and how hard it blows for it, 0 to 1: the game's wind at its speed
        /// (<see cref="KwApparentWind.Strongest"/> in the strongest, as the sails take it) less the ship's own speed, its
        /// strength the game's scaled by how much faster or slower the wind over the deck blows than the wind itself.
        /// </summary>
        private (Vector3 Wind, float Strength) Air()
        {
            EnvMan? env = EnvMan.instance;
            if (env == null)
                return (Vector3.back, 0.3f);
            float intensity = Mathf.Clamp01(env.GetWindIntensity());
            float speed = KwApparentWind.Strongest * Mathf.Sqrt(Mathf.Lerp(0.25f, 1f, intensity));
            Vector3 to = env.GetWindDir(), velocity = ship != null ? ship.Velocity : Vector3.zero;
            Vector3 wind = new Vector3(to.x, 0f, to.z).normalized * speed - new Vector3(velocity.x, 0f, velocity.z);
            return (wind, Mathf.Clamp01(intensity * Mathf.Min(1.5f, wind.magnitude / speed)));
        }

        /// <summary>
        /// The pennant's edges (in its own axes: its hoist's head on the staff's axis at the origin, the staff up its y,
        /// <paramref name="up"/> the world's up there), pairs of points from the hoist to the tip: its top edge's point, then
        /// its foot's. The hoist lies on the staff's face down the staff, on the side the wind blows to; the cloth runs
        /// straight out from there square to the staff for its <see cref="KwPennantHoist.Root"/>, then streams down the wind
        /// (made level to the world), drooping by the wind's weakness, its waves running out along it with the clock: about
        /// half a wave a second in a calm, a wave and a half in the strongest wind, a little sway and twist.
        /// </summary>
        public static void Shape(Vector3[] into, KwPennantHoist hoist, Vector3 wind, Vector3 up, float strength, float now)
        {
            up = up.sqrMagnitude > 1e-6f ? up.normalized : Vector3.up;
            Vector3 way = Square(wind, up, Vector3.Cross(up, Vector3.right)), off = Square(way, Staff, Vector3.back);
            (into[0], into[1]) = (off * hoist.AtHead, off * hoist.AtFoot - Staff * hoist.Width);
            float root = hoist.Root / hoist.Length, half = Half(hoist, root);
            Vector3 centre = (into[0] + into[1]) / 2f + off * hoist.Root;
            (into[2], into[3]) = (centre + Staff * half, centre - Staff * half);
            Fly(into, hoist, (way, off, up), strength, now, centre);
        }

        // The cloth past its straight part, from the centre of the straight part's end: each face's way down the wind turns
        // from square to the staff to level with the world and droops as it goes, the sway and twist growing from nothing.
        private static void Fly(Vector3[] into, KwPennantHoist hoist, (Vector3 Way, Vector3 Off, Vector3 Up) air, float strength, float now, Vector3 centre)
        {
            Vector3 side = Vector3.Cross(air.Up, air.Way).normalized;
            float droop = Mathf.Lerp(1.15f, 0.12f, strength), amp = (0.03f + 0.05f * strength) * hoist.Length;
            float beat = 0.45f + 1.05f * strength, root = hoist.Root / hoist.Length, step = (hoist.Length - hoist.Root) / Segments;
            for (int i = 1; i <= Segments; i++)
            {
                float v = (float)i / Segments, mid = Build((i - 0.5f) / Segments), bend = droop * (0.7f + 0.6f * (i - 0.5f) / Segments) * mid;
                Vector3 dir = Vector3.Lerp(air.Off, air.Way, mid).normalized * Mathf.Cos(bend) - Vector3.Lerp(Staff, air.Up, mid).normalized * Mathf.Sin(bend);
                centre += dir * step;
                float u = Along(i + 1, root), phase = 2f * Mathf.PI * (1.2f * u - beat * now), sway = amp * v * v;
                float twist = 0.18f * Mathf.Cos(phase) * Mathf.Min(1f, 8f * sway), half = Half(hoist, u);
                Vector3 at = centre + side * (sway * Mathf.Sin(phase)), edge = Edge(dir, Vector3.Lerp(Staff, air.Up, Build(v)).normalized, twist);
                (into[2 * i + 2], into[2 * i + 3]) = (at + edge * half, at - edge * half);
            }
        }

        // The way across the cloth, edge to edge, at a point: the lift (the staff, or the world's up further out) made
        // square to the cloth's way along there, turned about that way by the twist.
        private static Vector3 Edge(Vector3 dir, Vector3 lift, float twist)
        {
            Vector3 edge = (lift - dir * Vector3.Dot(lift, dir)).normalized;
            return edge * Mathf.Cos(twist) + Vector3.Cross(dir, edge) * Mathf.Sin(twist);
        }

        // A way made square to an axis, or the fallback (square to it too) where it lies along it.
        private static Vector3 Square(Vector3 way, Vector3 axis, Vector3 fallback)
        {
            way -= axis * Vector3.Dot(way, axis);
            return way.sqrMagnitude > 1e-6f ? way.normalized : fallback.normalized;
        }

        // How far the droop, sway and twist have built up a share v along the free part: none where the straight part
        // ends, all of them from Ramp on, smoothly between.
        private static float Build(float v) => Mathf.SmoothStep(0f, 1f, v / Ramp);

        // Half the cloth's width a share u of its length from the hoist: it narrows to a point near its tip.
        private static float Half(KwPennantHoist hoist, float u) => hoist.Width * (1f - 0.92f * u) / 2f;

        // A point's share of the length from the hoist: 0 at the hoist, the straight part's share at its end, then even
        // steps to 1 at the tip.
        private static float Along(int k, float root) => k == 0 ? 0f : root + (1f - root) * (k - 1) / Segments;

        // The middle of the cloth's texture along it, clear of the sail's edges.
        private static Vector2[] Uvs(float root)
        {
            Vector2[] uvs = new Vector2[2 * Stations];
            for (int k = 0; k < Stations; k++)
            {
                float x = 0.3f + 0.4f * Along(k, root);
                (uvs[2 * k], uvs[2 * k + 1]) = (new Vector2(x, 0.6f), new Vector2(x, 0.4f));
            }
            return uvs;
        }

        // White vertex colours, as every sail's cloth has (MeshBuilder's): the cloth shader reads them.
        private static Color[] White(int count)
        {
            Color[] colours = new Color[count];
            for (int i = 0; i < count; i++)
                colours[i] = Color.white;
            return colours;
        }

        // One face a segment (the cloth's shader draws both sides, each lit as its own), as two triangles.
        private static int[] Triangles()
        {
            int[] triangles = new int[6 * (Stations - 1)];
            for (int i = 0; i < Stations - 1; i++)
            {
                int a = 2 * i, b = a + 1, c = a + 2, d = a + 3;
                (triangles[6 * i], triangles[6 * i + 1], triangles[6 * i + 2]) = (a, c, b);
                (triangles[6 * i + 3], triangles[6 * i + 4], triangles[6 * i + 5]) = (b, c, d);
            }
            return triangles;
        }
    }
}
