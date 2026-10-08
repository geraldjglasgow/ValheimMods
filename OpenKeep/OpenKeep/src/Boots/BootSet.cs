using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// One of the game's 20 leggings and the boots split from it: the set key (<c>Iron</c>, also the workshop split's,
    /// <see cref="NativeSplit"/>), the game's leggings prefab, the boots item prefab (<c>OpenKeep_Boots_Iron</c>,
    /// PackPanel's contract is the prefix) and its name token. The prefabs and the cut trousers are filled in when the
    /// scene or the item database first wakes (<see cref="BootsItems"/>).
    /// </summary>
    public sealed class BootSet
    {
        public const string Prefix = "OpenKeep_Boots_";

        public BootSet(string key, string legs, string token)
        {
            Key = key;
            Legs = legs;
            Prefab = Prefix + key;
            Token = token;
            Hash = Prefab.GetStableHashCode();
            LegsHash = legs.GetStableHashCode();
        }

        public string Key { get; }

        /// <summary>The game's leggings prefab.</summary>
        public string Legs { get; }

        /// <summary>The leggings prefab's hash, what VisEquipment names them by.</summary>
        public int LegsHash { get; }

        /// <summary>The boots item prefab's name, what the game hashes.</summary>
        public string Prefab { get; }

        public int Hash { get; }

        /// <summary>The boots' name token, <c>$ok_boots_iron</c>.</summary>
        public string Token { get; }

        /// <summary>The boots item; null until built, or when the game's leggings were not found.</summary>
        public GameObject Item { get; set; }

        /// <summary>The game's leggings prefab once found.</summary>
        public GameObject LegsPrefab { get; set; }

        /// <summary>The trousers part of each of the leggings' skinned meshes (<see cref="LegsLook"/>); empty when uncut.</summary>
        public Dictionary<Mesh, Mesh> Trousers { get; } = new Dictionary<Mesh, Mesh>();
    }
}
