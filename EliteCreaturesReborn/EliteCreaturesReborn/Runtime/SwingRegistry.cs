using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The creatures loaded on this machine that do not animate at the game's own speed: a swing speed from stars or Mad
    /// other than 1, or a Tethered boss, whose speed moves with its pair's health and so is listed even while it is 1.
    /// <c>AnimSpeedPatch</c> runs for every character's animator each fixed step, players included, so everyone else
    /// costs it this one lookup. Filled on every machine by <see cref="EliteController"/> as it applies the traits it
    /// loaded from the ZDO, which it does once and never redoes (a hand-over or a rule reload changes nothing here), and
    /// emptied as the controller is destroyed with its creature.
    /// </summary>
    public static class SwingRegistry
    {
        private static readonly Dictionary<Character, EliteController> Swinging = new Dictionary<Character, EliteController>();

        /// <summary>The controller of a creature listed here, or null for a player and for every creature at its own speed.</summary>
        public static EliteController? For(Character character) =>
            character is not null && Swinging.TryGetValue(character, out EliteController controller) ? controller : null;

        /// <summary>Lists the creature when its swing speed is not 1 or can change; called once its traits are applied.</summary>
        public static void Track(EliteController controller)
        {
            if (!Mathf.Approximately(controller.SwingSpeedFactor, 1f) || controller.Traits.HasAspect(Aspect.Tethered))
            {
                Swinging[controller.Creature] = controller;
            }
        }

        // `is not null` rather than Unity's ==: the creature may already read as destroyed here, and its entry must go.
        public static void Forget(Character character)
        {
            if (character is not null)
            {
                Swinging.Remove(character);
            }
        }
    }
}
