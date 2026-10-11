using System;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// Recolours an overlay (a copy of the game's burning or smoke look) to one colour, keeping its light and dark: what
    /// the player sees of a particle is several colours multiplied (the material's, the particle's start colour, its colour
    /// over life, and for the game's gradient-mapped fire the two custom-data colours that carry the flame's hues). If
    /// every one of them took the overlay colour, the colour would be multiplied by itself and drift. So in each particle
    /// system the one that carries the hue is colourized (<see cref="ColourMaths.Colourize"/>: the fire's custom colours
    /// when it has them, otherwise its start colour) and every other multiplier is made grey at its own brightness; lights
    /// and the materials of plain meshes take the colour, particle materials go grey. Done once on the overlay's template,
    /// at build, on a machine that draws.
    /// </summary>
    internal static class OverlayColours
    {
        public static void Paint(GameObject template, Color colour)
        {
            Func<Color, Color> carry = c => ColourMaths.Colourize(c, colour, true);
            Func<Color, Color> grey = ColourMaths.Grey;
            foreach (ParticleSystem system in template.GetComponentsInChildren<ParticleSystem>(true))
            {
                Particles(system, carry, grey);
            }
            foreach (Light light in template.GetComponentsInChildren<Light>(true))
            {
                light.color = ColourMaths.Colourize(light.color, colour, false);
            }
            foreach (Renderer renderer in template.GetComponentsInChildren<Renderer>(true))
            {
                MaterialRecipes.Change change = renderer is ParticleSystemRenderer || renderer is TrailRenderer
                    ? MaterialRecipes.Change.Grey : MaterialRecipes.Change.Colourize;
                renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials,
                    m => m != null ? MaterialRecipes.Recoloured(m, colour, change) : m!);
            }
        }

        private static void Particles(ParticleSystem system, Func<Color, Color> carry, Func<Color, Color> grey)
        {
            bool fire = CustomColours(system, carry);
            ParticleSystem.MainModule main = system.main;
            main.startColor = Map(main.startColor, fire ? grey : carry);
            ParticleSystem.ColorOverLifetimeModule overLife = system.colorOverLifetime;
            overLife.color = Map(overLife.color, grey);
            ParticleSystem.TrailModule trails = system.trails;
            trails.colorOverLifetime = Map(trails.colorOverLifetime, grey);
            trails.colorOverTrail = Map(trails.colorOverTrail, grey);
        }

        /// <summary>The gradient-mapped fire's custom-data colours, colourized; whether the system has any in use.</summary>
        private static bool CustomColours(ParticleSystem system, Func<Color, Color> carry)
        {
            ParticleSystem.CustomDataModule custom = system.customData;
            bool any = false;
            foreach (ParticleSystemCustomData stream in new[] { ParticleSystemCustomData.Custom1, ParticleSystemCustomData.Custom2 })
            {
                if (custom.GetMode(stream) == ParticleSystemCustomDataMode.Color)
                {
                    custom.SetColor(stream, Map(custom.GetColor(stream), carry));
                    any |= custom.enabled;
                }
            }
            return any;
        }

        /// <summary>
        /// Every colour of a gradient setting mapped once: its two colours and two gradients (the single colour and
        /// gradient are the second of each, so they are not mapped twice); its mode kept.
        /// </summary>
        private static ParticleSystem.MinMaxGradient Map(ParticleSystem.MinMaxGradient gradient, Func<Color, Color> map)
        {
            gradient.colorMin = map(gradient.colorMin);
            gradient.colorMax = map(gradient.colorMax);
            Keys(gradient.gradientMin, map);
            Keys(gradient.gradientMax, map);
            return gradient;
        }

        private static void Keys(Gradient? gradient, Func<Color, Color> map)
        {
            if (gradient != null)
            {
                gradient.SetKeys(Array.ConvertAll(gradient.colorKeys, k => new GradientColorKey(map(k.color), k.time)), gradient.alphaKeys);
            }
        }
    }
}
