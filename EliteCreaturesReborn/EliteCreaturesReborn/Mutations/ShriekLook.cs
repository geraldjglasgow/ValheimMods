using EliteCreaturesReborn.Visuals;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// What every machine holding a Screecher sees and hears when it shrieks, each a local cosmetic clone
    /// (<see cref="CosmeticClone"/>, so the player's effect density applies to what is drawn): the `shriek sound` played
    /// at it, a ripple of distorted air bursting from its body (the Fader's roar, a sound made visible), and a camera
    /// shake that fades with distance across the shriek's reach. So a player it deafens knows at once what did it and
    /// from where, and one just outside the reach sees what they escaped. A dedicated server draws nothing.
    /// </summary>
    internal static class ShriekLook
    {
        private const string RippleEffect = "fx_Fader_Roar";

        /// <summary>The shake at the creature itself; it falls to nothing at the edge of the reach.</summary>
        private const float Shake = 0.8f;

        private static readonly string[] Ripple = { "roar", "shockwave", "nova" };
        private static readonly string[] Screech = { "screech", "scream", "shriek", "howl" };

        public static void Play(Character creature, string sound, Vector3 at, float radius)
        {
            if (BlinkEffects.Headless())
            {
                return;
            }
            CosmeticClone.Sound(EffectResolver.ResolveSound(sound, Screech, "Screecher shriek sound"), at);
            float body = creature != null ? BlinkEffects.Radius(creature) : 4f;
            CosmeticClone.Flash(EffectResolver.Resolve(RippleEffect, Ripple, "Screecher shriek ripple"), at, body);
            if (GameCamera.instance != null && radius > 0f)
            {
                GameCamera.instance.AddShake(at, radius, Shake, continous: false);
            }
        }
    }
}
