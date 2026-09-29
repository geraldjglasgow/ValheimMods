using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// The greataxe's swing sounds, one per step of its combo, played as the swing's trail starts (as the game plays a
    /// weapon's): the Battleaxe's own swing for the slash, the Executioner's flat sweep for the spin, the sledge's swing
    /// for the overhead, their sub-bass off (the preview's g_swing, g_spin, g_overhead). Networked cues
    /// (<see cref="HeadsmanSoundCue"/>), so every peer near hears them.
    /// </summary>
    public static class GreataxeSwings
    {
        private static readonly (string name, string cue)[] Steps =
        {
            ("ECP_Greataxe_sfx_slash", "g_swing"), ("ECP_Greataxe_sfx_spin", "g_spin"), ("ECP_Greataxe_sfx_overhead", "g_overhead"),
        };

        private static readonly List<GameObject> prefabs = new List<GameObject>();
        private static readonly List<EffectList> lists = new List<EffectList>();

        public static IEnumerable<GameObject> Prefabs => prefabs;

        public static void Build()
        {
            if (prefabs.Count > 0)
            {
                return;
            }
            foreach (var (name, cue) in Steps)
            {
                GameObject prefab = HeadsmanSoundCue.Build(name, cue);
                prefabs.Add(prefab);
                lists.Add(new EffectList { m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = prefab } } });
            }
        }

        /// <summary>The swing sound of a combo step, as an effect list; none before the prefabs are built.</summary>
        public static EffectList For(int step) => lists.Count == 0 ? new EffectList() : lists[Mathf.Clamp(step, 0, lists.Count - 1)];
    }
}
