using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The world seen from under water, on the local player's client: while the player dives (<see cref="Diving.Under"/>)
    /// and the camera is below the water's surface, the fog (and the game's fog toward the sun) turns sea-green and
    /// thick, so the sea floor fades out a few metres away. Darker at night (it follows the brightness of the weather's own fog) and deeper down. Called by
    /// <see cref="EnvFogPass"/> right after the game set the fog for the frame, last, so it wins over Fog Lift; the next
    /// frame starts again from the weather's fog, so surfacing puts the game's fog straight back.
    /// </summary>
    public static class UnderwaterView
    {
        private static readonly Color Water = new Color(0.07f, 0.24f, 0.27f);

        /// <summary>The fog colour toward the sun, which the game's shaders blend in; set to the water's too.</summary>
        private static readonly int SunFogColor = Shader.PropertyToID("_SunFogColor");

        /// <summary>The fog's density under water: things fade out about 15 m away.</summary>
        private const float Density = 0.07f;

        /// <summary>Depth, in metres under the surface, at which the water is darkest.</summary>
        private const float DarkestDepth = 40f;

        public static void Apply()
        {
            if (!Diving.Under || !GameSkillSettings.Swimming.Value)
                return;
            Vector3 camera = Utils.GetMainCamera().transform.position;
            float depth = Floating.GetLiquidLevel(camera, 1f, LiquidType.Water) - camera.y;
            if (depth <= 0f)
                return;
            float light = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(RenderSettings.fogColor.grayscale * 2f));
            float deep = Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(depth / DarkestDepth));
            Color water = Water * (light * deep);
            RenderSettings.fogColor = water;
            RenderSettings.fogDensity = Density;
            Shader.SetGlobalColor(SunFogColor, water);
        }
    }
}
