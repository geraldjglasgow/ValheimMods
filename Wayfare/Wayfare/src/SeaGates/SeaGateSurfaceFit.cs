using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>How a system made for the small wooden portal (a ring of about 1.3 m radius) is stretched over a gate
    /// 10 to 30 m wide. Its circle or cone becomes the gate's rectangle: the stretched flames along the rectangle's
    /// edges, everything else (the dark smoke, the sparks, the trails) over its whole area. Particles grow a little
    /// and emission grows with the area or the edge length, capped, so a wide gate stays affordable. Orbital speeds
    /// shrink so the edge keeps the small portal's linear speed instead of whipping round at ten times it.</summary>
    internal static class SeaGateSurfaceFit
    {
        private const float SmallPortalRadius = 1.3f;
        private const float MinRadius = 0.3f;
        private const float MaxSizeScale = 2.5f;
        private const float Coverage = 0.6f;         // of the small portal's particle density, over the area
        private const float MaxRateScale = 10f;
        private const int MaxParticles = 2500;
        private const float SortingFudge = -3f;      // draw over the sheet, which sits in the same place

        /// <summary>Refits the system; returns the factor its emission rate must grow by.</summary>
        internal static float Apply(ParticleSystem system, float width, float height)
        {
            float radius = Radius(system);
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            bool edge = renderer != null && renderer.renderMode == ParticleSystemRenderMode.Stretch;
            float size = Mathf.Clamp(Mathf.Sqrt(Mathf.Min(width, height) * 0.5f / radius), 1f, MaxSizeScale);
            float rateScale = Mathf.Clamp(edge
                ? (width + height) / (Mathf.PI * radius) / size
                : width * height / (Mathf.PI * radius * radius) * Coverage / (size * size), 1f, MaxRateScale);
            Shape(system, edge, width, height);
            Orbit(system, radius / (0.5f * Mathf.Sqrt(width * width + height * height)));
            Main(system, size, rateScale);
            if (renderer != null)
                renderer.sortingFudge += SortingFudge;
            return rateScale;
        }

        internal static ParticleSystem.MinMaxCurve Scaled(ParticleSystem.MinMaxCurve curve, float factor)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    curve.constant *= factor;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    curve.constantMin *= factor;
                    curve.constantMax *= factor;
                    break;
                default:
                    curve.curveMultiplier *= factor;
                    break;
            }
            return curve;
        }

        private static float Radius(ParticleSystem system)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            return shape.enabled && shape.radius >= MinRadius ? shape.radius : SmallPortalRadius;
        }

        private static void Shape(ParticleSystem system, bool edge, float width, float height)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = edge ? ParticleSystemShapeType.BoxEdge : ParticleSystemShapeType.Box;
            shape.scale = new Vector3(width, height, 0f);
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;
        }

        private static void Orbit(ParticleSystem system, float factor)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            if (!velocity.enabled)
                return;
            velocity.orbitalX = Scaled(velocity.orbitalX, factor);
            velocity.orbitalY = Scaled(velocity.orbitalY, factor);
            velocity.orbitalZ = Scaled(velocity.orbitalZ, factor);
        }

        private static void Main(ParticleSystem system, float size, float rateScale)
        {
            ParticleSystem.MainModule main = system.main;
            if (main.startSize3D)
            {
                main.startSizeX = Scaled(main.startSizeX, size);
                main.startSizeY = Scaled(main.startSizeY, size);
                main.startSizeZ = Scaled(main.startSizeZ, size);
            }
            else
            {
                main.startSize = Scaled(main.startSize, size);
            }
            main.maxParticles = Mathf.Min(MaxParticles, Mathf.CeilToInt(main.maxParticles * rateScale));
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.cullingMode = ParticleSystemCullingMode.Pause;  // off screen costs nothing
        }
    }
}
