using EliteCreaturesReborn.Visuals;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// What a machine holding a Cloning creature or its decoy sees and hears. The reveal is the `reveal effect` at the
    /// creature as it shows again, with the `reveal sound` played there too, so the moment reads even with effects turned
    /// down to nothing. The decoy goes in a puff, the `vanish effect`, where it stood. Each is a local cosmetic clone
    /// (<see cref="CosmeticClone"/>), so the player's effect density applies, sized to the body as Blinking's are; a
    /// configured name the game does not have falls back to the nearest effect by keyword.
    /// </summary>
    internal static class CloneEffects
    {
        private static readonly string[] Burst = { "spawn", "flash", "ghost", "smoke" };
        private static readonly string[] Cue = { "spawn", "ghost", "wraith" };
        private static readonly string[] Puff = { "despawn", "poof", "puff", "smoke", "ghost" };

        public static void Reveal(string effect, string sound, Vector3 at, float radius)
        {
            CosmeticClone.Flash(EffectResolver.Resolve(effect, Burst, "Cloning reveal effect"), at, radius);
            CosmeticClone.Sound(EffectResolver.ResolveSound(sound, Cue, "Cloning reveal sound"), at);
        }

        public static void Vanish(string effect, Vector3 at, float radius) =>
            CosmeticClone.Flash(EffectResolver.Resolve(effect, Puff, "Cloning vanish effect"), at, radius);
    }
}
