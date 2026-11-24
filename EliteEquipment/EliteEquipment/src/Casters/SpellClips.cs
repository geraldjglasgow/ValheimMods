using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The spells' own motions (features/casters.md, "Spell animations"): one humanoid upper-body clip a spell in the
    /// <c>ee_casteranim</c> bundle, played in the charged staff's cast slot (<see cref="SpellMotion"/>), each with the game's
    /// <c>Hit</c> event where the spell is cast (Elder's Volley six, one a seed). The times are the workshop's
    /// (<c>Assets/Animations/ee_casteranim/out/spells.json</c>, 2026-11-02), clip seconds: where the motion ends and the
    /// still idle it holds after begins (<c>motionEnd</c>), and its first <c>Hit</c>. Loaded once; none where nothing is
    /// drawn or the bundle lacks the clip.
    /// </summary>
    public static class SpellClips
    {
        public const string Reach = "ee_spell_reach";
        public const string Raise = "ee_spell_raise";
        public const string Sweep = "ee_spell_sweep";
        public const string Thrust = "ee_spell_thrust";

        private const string Bundle = "ee_casteranim";

        private sealed class Timing
        {
            public float End;
            public float Hit;
        }

        private static readonly Dictionary<string, Timing> timings = new Dictionary<string, Timing>
        {
            { Reach, new Timing { End = 0.90f, Hit = 0.40f } },
            { Raise, new Timing { End = 1.30f, Hit = 0.92f } },
            { Sweep, new Timing { End = 0.80f, Hit = 0.42f } },
            { Thrust, new Timing { End = 1.30f, Hit = 0.30f } },
        };

        private static readonly Dictionary<string, AnimationClip> loaded = new Dictionary<string, AnimationClip>();

        /// <summary>The clip, or null (nothing drawn here, no bundle, or the bundle lacks it: the game's cast plays instead).</summary>
        public static AnimationClip Get(string name)
        {
            if (name == null)
                return null;
            if (loaded.TryGetValue(name, out AnimationClip clip))
                return clip;
            AssetBundle bundle = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ? null : CasterBundles.Get(Bundle);
            clip = bundle != null ? bundle.LoadAsset<AnimationClip>(name) : null;
            if (bundle != null && clip == null)
                Plugin.Log.LogWarning($"EliteEquipment: {Bundle} has no {name}, that spell keeps the game's cast");
            loaded[name] = clip;
            return clip;
        }

        /// <summary>Clip seconds where the motion ends (its own exit follows on the still idle); the clip's end if unknown.</summary>
        public static float End(string name, AnimationClip clip) => timings.TryGetValue(name, out Timing t) ? t.End : clip.length;

        /// <summary>Clip seconds of the first <c>Hit</c>, where the spell is cast and its charge ends.</summary>
        public static float Hit(string name, AnimationClip clip) => timings.TryGetValue(name, out Timing t) ? t.Hit : clip.length * 0.5f;
    }
}
