using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A fish out of the trawl is dead (the user, 2026-11-04: "the fish need to be dead coming out of the net"), on every
    /// peer: its ZDO marked (<see cref="DeadHash"/>, by the game that landed it), the game's fish behaviour off (no swimming,
    /// jumping, hooking or escaping) and its animation stopped, so it lies still as a plain item; out of the water whatever
    /// the sea round it says (<see cref="KwTrawlFishDry"/>), so it can always be picked up. Picked up, it is the game's own
    /// fish item; dropped again later from a pack it is the game's own fish again.
    /// </summary>
    public sealed class KwTrawlDead : MonoBehaviour
    {
        public const string DeadKey = "bs_catch_dead";
        public static readonly int DeadHash = DeadKey.GetStableHashCode();

        private static readonly HashSet<Fish> dead = new HashSet<Fish>();
        private Fish? fish;

        /// <summary>On the game that landed it: marked dead in its ZDO, and dead here at once.</summary>
        public static void Mark(GameObject made, ZDO? zdo)
        {
            zdo?.Set(DeadHash, true);
            Make(made);
        }

        /// <summary>Dead here (once), from the mark in its ZDO or as it is landed.</summary>
        public static void Make(GameObject item)
        {
            if (item.GetComponent<KwTrawlDead>() == null)
                item.AddComponent<KwTrawlDead>();
        }

        /// <summary>Whether a fish is a dead catch here.</summary>
        public static bool Is(Fish fish) => dead.Contains(fish);

        private void Awake()
        {
            fish = GetComponent<Fish>();
            if (fish != null)
            {
                fish.enabled = false;
                dead.Add(fish);
            }
            foreach (Animator animator in GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
        }

        private void OnDestroy()
        {
            if (fish != null)
                dead.Remove(fish);
        }
    }
}
