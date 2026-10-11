using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Api
{
    /// <summary>
    /// The problems one registration call found, each one sentence naming the endpoint and the prefab: what a
    /// registration endpoint answers, never null and empty when all went in. It also finds the prefab's creature for the
    /// checks that need one, once the game's prefab table has it; before that those checks are left to the moment the
    /// registration is used.
    /// </summary>
    internal sealed class ApiProblems
    {
        private readonly string _prefix;
        private readonly List<string> _lines = new List<string>();

        public ApiProblems(string endpoint, string prefab)
        {
            _prefix = endpoint + " " + prefab + ": ";
        }

        public void Add(string text) => _lines.Add(_prefix + text);

        public string[] ToArray() => _lines.ToArray();

        /// <summary>The answer to a call naming no prefab: nothing is registered.</summary>
        public static string[] NoPrefab(string endpoint) => new[] { endpoint + ": no prefab name, nothing registered" };

        /// <summary>The creature prefab of this name, once the game's prefab table has it; null before, or for anything else.</summary>
        public static Character? Creature(string prefab)
        {
            GameObject? go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            return go != null ? go.GetComponent<Character>() : null;
        }
    }
}
