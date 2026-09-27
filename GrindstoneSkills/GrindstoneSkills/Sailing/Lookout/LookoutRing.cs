using PatchGuard;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrindstoneSkills
{
    /// <summary>
    /// The lookout pulse as everyone near the ship sees it: a pale ring laid on the sea at the ship's position that
    /// races out to the pulse's radius and fades, drawn locally on each client. It stays where the pulse started while
    /// the ship sails on, and removes itself when it is done.
    /// </summary>
    internal sealed class LookoutRing : MonoBehaviour
    {
        private const int Segments = 96;
        private const float Seconds = 2f;
        private const float StartRadius = 3f;
        private const float Width = 0.5f;
        private const float AboveSea = 0.6f;

        private static readonly Color Colour = new Color(0.62f, 0.88f, 1f, 0.95f);
        private static Material material;

        private LineRenderer line;
        private Vector3 center;
        private float radius;
        private float born;

        public static void Draw(Vector3 shipPosition, float radius)
        {
            Material lineMaterial = LineMaterial();
            if (lineMaterial == null)
                return;
            GameObject holder = new GameObject("grindstone_lookout_ring");
            LookoutRing ring = holder.AddComponent<LookoutRing>();
            ring.center = new Vector3(shipPosition.x, SeaLevel(shipPosition) + AboveSea, shipPosition.z);
            ring.radius = Mathf.Max(StartRadius, radius);
            ring.born = Time.time;
            ring.line = Line(holder, lineMaterial);
            ring.Spread(0f);
        }

        private void Update() => Guard.Run("lookout ring", Step);

        private void Step()
        {
            float t = (Time.time - born) / Seconds;
            if (t >= 1f)
                Destroy(gameObject);
            else
                Spread(t);
        }

        /// <summary>Fast at first, easing out at the edge; wider and fainter as it goes, so it reads at a distance.</summary>
        private void Spread(float t)
        {
            float eased = 1f - (1f - t) * (1f - t);
            float current = Mathf.Lerp(StartRadius, radius, eased);
            for (int i = 0; i < Segments; i++)
            {
                float angle = Mathf.PI * 2f * i / Segments;
                line.SetPosition(i, center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * current);
            }
            Color colour = Colour;
            colour.a *= 1f - t * t;
            line.startColor = colour;
            line.endColor = colour;
            line.widthMultiplier = Width * (1f + 2f * eased);
        }

        private static float SeaLevel(Vector3 position) =>
            ZoneSystem.instance != null ? Mathf.Max(ZoneSystem.instance.m_waterLevel, position.y - 1f) : position.y;

        private static LineRenderer Line(GameObject holder, Material lineMaterial)
        {
            LineRenderer line = holder.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Segments;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static Material LineMaterial()
        {
            if (material != null)
                return material;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            return material = shader != null ? new Material(shader) : null;
        }
    }
}
