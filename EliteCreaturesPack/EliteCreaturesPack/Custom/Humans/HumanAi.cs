using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// A human's mind: a MonsterAI with the game's Draugr's values, the game's nearest thing to a fighting person (the
    /// player's build, the player's kind of weapons): 30 m sight in a 90 degree view, alert within 10 m, keeps out of
    /// water, attacks player buildings, never flees for low health, gives up a target it cannot reach. Its groans (alert
    /// and idle sounds) are left out, since a person makes none of its own here; its health is the Draugr's too, until a
    /// definition sets one. Also the empty CharacterDrop a definition's drops go into.
    /// </summary>
    internal static class HumanAi
    {
        public const string Teacher = "Draugr";

        public static void Add(GameObject shell, ZNetScene scene)
        {
            MonsterAI ai = shell.AddComponent<MonsterAI>();
            GameObject? teacher = scene.GetPrefab(Teacher);
            Learn(ai, teacher);
            LearnHealth(shell, teacher);
            ai.m_alertedEffects = new EffectList();
            ai.m_idleSound = new EffectList();
            shell.AddComponent<CharacterDrop>().m_drops = new List<CharacterDrop.Drop>();
        }

        // The player's own health is a low base that food raises; a person who eats nothing would fall to a few blows.
        // Until a definition sets `health`, a human is as hardy as the Draugr it learned to fight from.
        private static void LearnHealth(GameObject shell, GameObject? teacher)
        {
            Character? body = shell.GetComponent<Character>();
            Character? source = teacher != null ? teacher.GetComponent<Character>() : null;
            if (body != null && source != null)
            {
                body.m_health = source.m_health;
            }
        }

        /// <summary>The Draugr's values, read from a private copy of it, so no list or effect is shared with the game's Draugr.</summary>
        private static void Learn(MonsterAI ai, GameObject? teacher)
        {
            if (teacher == null || teacher.GetComponent<MonsterAI>() == null)
            {
                Log.Warn($"Humans: the game has no {Teacher} to learn from; they keep MonsterAI's own defaults.");
                return;
            }
            GameObject lesson = PrefabBench.Copy(teacher, Teacher + "_ecp_lesson");
            try
            {
                MonsterAI source = lesson.GetComponent<MonsterAI>();
                HumanFieldCopy.Copy(typeof(BaseAI), source, ai);
                HumanFieldCopy.Copy(typeof(MonsterAI), source, ai);
            }
            finally
            {
                Object.DestroyImmediate(lesson);
            }
        }
    }
}
