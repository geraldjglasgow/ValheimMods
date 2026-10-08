using EliteCrafting.Items;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// The Rune Table's offering bowl fills with essence as the pool grows (user request 2026-10-07: "have the bowl fill
    /// with essence the more you have in the table. maybe 500+ makes it look full. it fills more and more in increments
    /// of 50"): a pale glowing surface inside the bowl, one step higher for every 50 essence in the pool, full from 500,
    /// none under 50. The bowl is found by the model's <c>col_box_bowl</c> collider, at the bowl's middle; its inside is
    /// the workshop model's (floor 1.09 m, rim 1.195 m above the table's foot). Its material is a whitened copy of the
    /// table's own glyph glow (the <c>glow</c> part), whose shader the game is known to draw: <c>Shader.Find("Standard")</c>
    /// returns null in the game, which left the first disc unshaded and magenta. Every client draws it from the table's
    /// replicated ZDO, looked at when its data changes, at most twice a second; nothing is sent. Switched off on a
    /// dedicated server and on a model without the bowl.
    /// </summary>
    internal sealed class TableBowl : MonoBehaviour
    {
        private const float Every = 0.5f;
        private const int Step = 50;
        private const int Steps = 10;
        private const float BowlMiddle = 1.105f;
        private const float Lowest = 1.10f;
        private const float Highest = 1.185f;
        private const float Margin = 0.94f;

        // The bowl's inside as (height, radius), from its floor up: the workshop's lathe profile.
        private static readonly Vector2[] Inside = { new Vector2(1.095f, 0.13f), new Vector2(1.13f, 0.21f), new Vector2(1.195f, 0.235f) };

        private ZNetView? _view;
        private TableStore? _store;
        private Transform? _bowl;
        private Material? _glow;
        private GameObject? _surface;
        private uint _seen = uint.MaxValue;
        private int _level = -1;
        private float _next;

        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            _store = _view != null ? new TableStore(_view) : null;
            _bowl = TableShelf.Find(transform, "col_box_bowl");
            _glow = TableShelf.Find(transform, "glow")?.GetComponent<MeshRenderer>()?.sharedMaterial;
            enabled = _bowl != null && _glow != null && !StoneVisuals.Headless;
        }

        private void Update()
        {
            if (Time.time < _next || _store == null || !_store.Valid)
            {
                return;
            }
            _next = Time.time + Every;
            uint revision = _view!.GetZDO().DataRevision;
            if (revision == _seen)
            {
                return;
            }
            _seen = revision;
            Show(Mathf.Min(_store.Essence / Step, Steps));
        }

        private void Show(int level)
        {
            if (level == _level)
            {
                return;
            }
            _level = level;
            if (level > 0 && _surface == null)
            {
                _surface = BowlSurface.Make(_bowl!, _glow!);
            }
            if (_surface == null)
            {
                return;
            }
            _surface.SetActive(level > 0);
            float height = Mathf.Lerp(Lowest, Highest, (Mathf.Max(level, 1) - 1f) / (Steps - 1f));
            float across = 2f * RadiusAt(height) * Margin;
            _surface.transform.localPosition = new Vector3(0f, height - BowlMiddle, 0f);
            _surface.transform.localScale = new Vector3(across, 1f, across);
        }

        private static float RadiusAt(float height)
        {
            for (int i = 1; i < Inside.Length; i++)
            {
                if (height <= Inside[i].x)
                {
                    float t = Mathf.InverseLerp(Inside[i - 1].x, Inside[i].x, height);
                    return Mathf.Lerp(Inside[i - 1].y, Inside[i].y, t);
                }
            }
            return Inside[Inside.Length - 1].y;
        }
    }

    /// <summary>The essence's surface in the bowl: a flat disc of radius 0.5 facing up, pale and glowing, no collider.</summary>
    internal static class BowlSurface
    {
        private const int Sides = 24;

        private static Mesh? _disc;
        private static Material? _material;

        public static GameObject Make(Transform bowl, Material glow)
        {
            var surface = new GameObject("ecf_bowl_essence");
            surface.layer = bowl.gameObject.layer;
            surface.transform.SetParent(bowl, false);
            surface.AddComponent<MeshFilter>().sharedMesh = _disc ??= Disc();
            MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material ??= Whiten(glow);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return surface;
        }

        private static Mesh Disc()
        {
            var vertices = new Vector3[Sides + 1];
            var triangles = new int[Sides * 3];
            for (int i = 0; i < Sides; i++)
            {
                float angle = 2f * Mathf.PI * i / Sides;
                vertices[i + 1] = new Vector3(0.5f * Mathf.Cos(angle), 0f, 0.5f * Mathf.Sin(angle));
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % Sides + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { name = "ecf_bowl_essence", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // The essence item's whitish glow: the glyphs' emissive material, recoloured.
        private static Material Whiten(Material glow)
        {
            var material = new Material(glow) { name = "ecf_bowl_essence" };
            material.color = new Color(0.78f, 0.81f, 0.86f);
            material.SetFloat("_Glossiness", 0.45f);
            material.EnableKeyword("_EMISSION");
            // A soft glow, not a lamp (user 2026-10-07: "not as bright").
            material.SetColor("_EmissionColor", new Color(0.2f, 0.23f, 0.27f));
            return material;
        }
    }
}
