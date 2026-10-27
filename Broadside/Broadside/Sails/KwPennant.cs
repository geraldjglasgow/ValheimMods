using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A long, thin pennant streaming from the staff over a masthead (<see cref="KwMastTops.Pennant"/>, over a crow's nest
    /// there, <see cref="KwMastTops.StaffTop"/>): its hoist on the staff's top, it streams the way the wind blows over the
    /// ship as it sails (the apparent wind the sails swing to, <see cref="KwApparentWind"/>: the game's wind less the ship's
    /// own speed; the game's wind alone on a dock), near level in a strong wind and drooping as the wind falls, square to
    /// the world's up however the ship heels or the mast rakes, and flutters in slow, even waves running out to its narrow
    /// tip, a little quicker and wider the harder it blows (the user, 2026-10-26: they moved "too fast for what the wind
    /// is" and were "off axis and uneven": the true wind against sails trimmed to the apparent one, the mast's own down,
    /// 1 to 3.5 waves a second, a 30° twist). Its shape comes from the game's wind, the ship's speed as this peer sees it
    /// and the network's clock alone (<see cref="Shape"/>), so every player sees the same pennant without anything sent.
    /// Drawn every frame within <see cref="Near"/>, twenty times a second beyond, not at all beyond <see cref="Range"/> or
    /// out of view; never made on a dedicated server (<see cref="KwShipBody.Drawn"/>).
    /// </summary>
    public sealed class KwPennant : MonoBehaviour
    {
        public const string Name = "pennant";
        private const int Segments = 12;
        private const float Far = 1f / 20f, Near = 35f, Range = 80f;

        private float length, width, wait;
        private Mesh? mesh;
        private Renderer? look;
        private KwShipWind? ship;
        private Vector3[] vertices = new Vector3[2 * (Segments + 1)];
        private bool broken;

        /// <summary>The mast's pennant under it (or none, the old one gone), at its staff's top over the masthead.</summary>
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
            GameObject made = new GameObject(Name) { layer = mast.gameObject.layer };
            made.transform.SetParent(mast, false);
            made.transform.localPosition = KwMesh.ToUnity(new Vector3(0f, 0f, spec.Tops.StaffTop(spec.Mast) - 0.03f));
            made.AddComponent<MeshFilter>();
            MeshRenderer renderer = made.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = KwPennantLook.Of(spec.Tops.Pennant, spec);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            made.AddComponent<KwMeshOwner>();
            KwPennant pennant = made.AddComponent<KwPennant>();
            (pennant.length, pennant.width) = (Mathf.Clamp(0.3f * spec.Mast, 1.5f, 5f), Mathf.Clamp(0.03f * spec.Mast + 0.12f, 0.18f, 0.4f));
        }

        private void Start()
        {
            mesh = new Mesh { name = "KW_pennant" };
            mesh.MarkDynamic();
            Shape(vertices, length, width, Vector3.back, Vector3.up, 0f, 0f);
            mesh.vertices = vertices;
            mesh.uv = Uvs();
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

        // The pennant as the wind over the ship has it now, in its own axes.
        private void Draw()
        {
            (Vector3 wind, float strength) = Air();
            float now = ZNet.instance != null ? (float)(ZNet.instance.GetTimeSeconds() % 3600.0) : Time.time;
            Vector3 up = transform.InverseTransformDirection(Vector3.up);
            Shape(vertices, length, width, transform.InverseTransformDirection(wind), up, strength, now);
            mesh!.vertices = vertices;
            mesh.RecalculateNormals();
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
        /// The pennant's edges (its hoist's top at the origin, in its own axes, <paramref name="up"/> the world's up there),
        /// pairs of points from the hoist to the tip: its top edge's point, then its foot's. It streams down the wind (the
        /// way it blows to, made level to the world), drooping by the wind's weakness, and its waves run out along it with
        /// the clock: about half a wave a second in a calm, a wave and a half in the strongest wind, a little sway and twist.
        /// </summary>
        public static void Shape(Vector3[] into, float length, float width, Vector3 wind, Vector3 up, float strength, float now)
        {
            up = up.sqrMagnitude > 1e-6f ? up.normalized : Vector3.up;
            Vector3 way = wind - up * Vector3.Dot(wind, up);
            way = way.sqrMagnitude > 1e-6f ? way.normalized : Vector3.Cross(up, Vector3.right).normalized;
            Vector3 side = Vector3.Cross(up, way).normalized, centre = -up * (width / 2f);
            float droop = Mathf.Lerp(1.15f, 0.12f, strength), amp = (0.03f + 0.05f * strength) * length;
            float beat = 0.45f + 1.05f * strength, step = length / Segments;
            (into[0], into[1]) = (Vector3.zero, -up * width);
            for (int i = 1; i <= Segments; i++)
            {
                float u = (float)i / Segments, phase = 2f * Mathf.PI * (1.2f * u - beat * now), bend = droop * (0.7f + 0.6f * u);
                Vector3 dir = way * Mathf.Cos(bend) - up * Mathf.Sin(bend);
                centre += dir * step;
                float sway = amp * u * u, twist = 0.18f * Mathf.Cos(phase) * Mathf.Min(1f, 8f * sway);
                Vector3 at = centre + side * (sway * Mathf.Sin(phase));
                Vector3 edge = (up - dir * Vector3.Dot(up, dir)).normalized;
                edge = edge * Mathf.Cos(twist) + Vector3.Cross(dir, edge) * Mathf.Sin(twist);
                float half = width * (1f - 0.92f * u) / 2f;
                (into[2 * i], into[2 * i + 1]) = (at + edge * half, at - edge * half);
            }
        }

        // The middle of the cloth's texture along it, clear of the sail's edges.
        private static Vector2[] Uvs()
        {
            Vector2[] uvs = new Vector2[2 * (Segments + 1)];
            for (int i = 0; i <= Segments; i++)
                (uvs[2 * i], uvs[2 * i + 1]) = (new Vector2(0.3f + 0.4f * i / Segments, 0.6f), new Vector2(0.3f + 0.4f * i / Segments, 0.4f));
            return uvs;
        }

        // One face a segment (the cloth's shader draws both sides), as two triangles.
        private static int[] Triangles()
        {
            int[] triangles = new int[6 * Segments];
            for (int i = 0; i < Segments; i++)
            {
                int a = 2 * i, b = a + 1, c = a + 2, d = a + 3;
                (triangles[6 * i], triangles[6 * i + 1], triangles[6 * i + 2]) = (a, c, b);
                (triangles[6 * i + 3], triangles[6 * i + 4], triangles[6 * i + 5]) = (b, c, d);
            }
            return triangles;
        }
    }
}
