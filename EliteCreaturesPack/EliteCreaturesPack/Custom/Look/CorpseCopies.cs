using System;
using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// The corpse of a creature with a size, a tint, a texture or tinted items. The game makes a corpse by spawning its
    /// death effects' ragdoll at the ragdoll prefab's own size and colours, so a big red troll would fall as a plain one.
    /// Each ragdoll of the creature's death effects that is its base's own corpse (in the base's death effects) is replaced
    /// by a copy, <c>&lt;creature&gt;_ragdoll</c>: a networked part registered beside the creature on every peer, scaled by
    /// the creature's size and given its look (<see cref="LookFinish"/> dresses it where something draws). The death list
    /// plays on the owner only, so the owner spawns the copy once and every peer makes the same prefab from its ZDO; the
    /// game's star colours (<c>Ragdoll.Setup</c>) and Elite Creatures Reborn's corpse looks still go on top. Whether a copy is
    /// made depends on the definitions alone, never on files or graphics, so every peer registers the same parts. A
    /// ragdoll the definition named from another creature keeps its own look.
    /// </summary>
    internal static class CorpseCopies
    {
        private static readonly Dictionary<string, string> originals = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// The ragdoll prefab a corpse copy was made from (by name), or the name itself: for code that knows a ragdoll by
        /// its prefab name (the human's corpse timing).
        /// </summary>
        public static string OriginalOf(string ragdollName) => originals.TryGetValue(ragdollName, out string original) ? original : ragdollName;

        /// <summary>The creature's own corpse copies, made and put in its death list; empty when it needs none.</summary>
        public static List<GameObject> Make(CreatureBuild build, LookPlan plan)
        {
            List<GameObject> corpses = new List<GameObject>();
            Character? character = build.Shell.GetComponent<Character>();
            if (!plan.OwnCorpse || character == null)
            {
                return corpses;
            }
            HashSet<GameObject> own = OwnRagdolls(build.Base);
            Dictionary<GameObject, GameObject> copies = new Dictionary<GameObject, GameObject>();
            List<EffectList.EffectData> replaced = new List<EffectList.EffectData>();
            foreach (EffectList.EffectData entry in character.m_deathEffects?.m_effectPrefabs ?? new EffectList.EffectData[0])
            {
                EffectList.EffectData copy = EffectEntries.Copy(entry);
                if (copy.m_prefab != null && own.Contains(copy.m_prefab))
                {
                    copy.m_prefab = CopyOf(build, plan, copy.m_prefab, copies, corpses);
                }
                replaced.Add(copy);
            }
            character.m_deathEffects = new EffectList { m_effectPrefabs = replaced.ToArray() };
            return corpses;
        }

        private static GameObject CopyOf(CreatureBuild build, LookPlan plan, GameObject ragdoll, Dictionary<GameObject, GameObject> copies,
            List<GameObject> corpses)
        {
            if (copies.TryGetValue(ragdoll, out GameObject made))
            {
                return made;
            }
            made = build.CopyPart(ragdoll, "ragdoll", networked: true);
            originals[made.name] = ragdoll.name;
            BodySize.Apply(made, plan.Size);
            copies[ragdoll] = made;
            corpses.Add(made);
            return made;
        }

        /// <summary>The ragdolls of the base's own death effects: its corpse.</summary>
        private static HashSet<GameObject> OwnRagdolls(GameObject? basePrefab)
        {
            HashSet<GameObject> own = new HashSet<GameObject>();
            Character? character = basePrefab != null ? basePrefab.GetComponent<Character>() : null;
            foreach (EffectList.EffectData entry in character?.m_deathEffects?.m_effectPrefabs ?? new EffectList.EffectData[0])
            {
                if (entry != null && entry.m_prefab != null && entry.m_prefab.GetComponent<Ragdoll>() != null)
                {
                    own.Add(entry.m_prefab);
                }
            }
            return own;
        }
    }
}
