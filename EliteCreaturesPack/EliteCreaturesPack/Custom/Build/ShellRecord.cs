using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// One creature while it is being built: its chain, its shell, the parts its steps made, the custom creatures it
    /// cannot do without, and whether a step failed it. Shared by every <see cref="CreatureBuild"/> of its chain.
    /// </summary>
    internal sealed class ShellRecord
    {
        public ShellRecord(CreatureChain chain, GameObject shell, GameObject? basePrefab)
        {
            Chain = chain;
            Shell = shell;
            BasePrefab = basePrefab;
        }

        public CreatureChain Chain { get; }

        public GameObject Shell { get; }

        /// <summary>The game or mod prefab at the root of the chain (the player's prefab for a human); null if missing.</summary>
        public GameObject? BasePrefab { get; }

        /// <summary>Extra prefabs its steps made, and whether each is registered with ZNetScene beside it.</summary>
        public List<KeyValuePair<GameObject, bool>> Parts { get; } = new List<KeyValuePair<GameObject, bool>>();

        /// <summary>Each part's original by the part's name: the prefab it was first copied from (see <see cref="CreatureBuild.OriginOf"/>).</summary>
        public Dictionary<string, string> PartOrigins { get; } = new Dictionary<string, string>(System.StringComparer.Ordinal);

        /// <summary>Custom creatures it names (an effect that spawns one): if one of them is left out, so is this one.</summary>
        public HashSet<string> Needs { get; } = new HashSet<string>();

        public bool Failed { get; set; }

        /// <summary>A counter for unique part names.</summary>
        public int PartCount { get; set; }

        /// <summary>Destroys the shell and its parts: the creature was left out.</summary>
        public void Discard()
        {
            foreach (KeyValuePair<GameObject, bool> part in Parts)
            {
                if (part.Key != null)
                {
                    Object.Destroy(part.Key);
                }
            }
            if (Shell != null)
            {
                Object.Destroy(Shell);
            }
        }
    }
}
