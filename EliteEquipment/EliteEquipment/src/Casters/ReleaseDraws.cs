using LocalEffects;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The releases of Ward, Sanctuary, War Chant and Feather Fall drawn on this machine (from <see cref="SpellFx.Draw"/>,
    /// for <see cref="CasterFx"/>'s message): Ward's bubble on its target and off again (<see cref="WardBubble"/>),
    /// Sanctuary's dome where it stands (<see cref="Sanctuary"/>), War Chant's ring rolling out to the chant's reach as
    /// Hearthsong's does, and Feather Fall's feathers round every friend in reach (<see cref="FeatherFall"/>). A release
    /// missing from the bundle falls back to the shared chant ring (Ward's to the game shield's own bubble).
    /// </summary>
    public static class ReleaseDraws
    {
        /// <summary>True when the effect is one of these; <paramref name="shared"/> is then the effect to draw the shared way, or null.</summary>
        public static bool Draw(string effect, Vector3 at, Character character, out string shared)
        {
            shared = null;
            if (effect == SpellFx.CastWard || effect == WardBubble.Ended)
                WardBubble.Show(character, effect == SpellFx.CastWard);
            else if (effect == SpellFx.CastSanctuary)
                shared = Sanctuary.Show(at);
            else if (effect == SpellFx.CastWarchant)
                shared = Ring(effect, at, character);
            else if (effect == SpellFx.CastFeatherfall)
                shared = FeatherFall.Show(at, character);
            else
                return false;
            return true;
        }

        /// <summary>A chant's ground ring at the caster, turned to the caster's facing; the shared ring when the bundle lacks it.</summary>
        private static string Ring(string effect, Vector3 at, Character caster)
        {
            GameObject prefab = CasterFx.Prepared(effect);
            if (prefab == null)
                return CasterFx.Chant;
            SpellFx.Ground(prefab, at, caster);
            Sound(effect, at);
            return null;
        }

        /// <summary>A release's game sound at a spot, on this machine only.</summary>
        public static void Sound(string effect, Vector3 at) =>
            LocalEffect.Sound(ZNetScene.instance?.GetPrefab(CasterSounds.For(effect)), at);
    }
}
