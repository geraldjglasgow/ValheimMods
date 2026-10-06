using System.Collections.Generic;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// Every creature loaded on this machine whose traits have resolved, by creature, for the per-frame paths that start
    /// from a Character (the nameplate asks for its hover name every frame it shows): one lookup instead of a component
    /// search. Filled by <see cref="EliteController"/> as it applies the traits it loaded, and emptied as the controller
    /// is destroyed with its creature.
    /// </summary>
    public static class ReadyElites
    {
        private static readonly Dictionary<Character, EliteController> ByCreature = new Dictionary<Character, EliteController>();

        /// <summary>The resolved controller of a creature, or null for a player and for one not resolved yet.</summary>
        public static EliteController? Of(Character character) =>
            character is not null && ByCreature.TryGetValue(character, out EliteController controller) ? controller : null;

        public static void Track(EliteController controller) => ByCreature[controller.Creature] = controller;

        // `is not null` rather than Unity's ==: the creature may already read as destroyed here, and its entry must go.
        public static void Forget(Character character)
        {
            if (character is not null)
            {
                ByCreature.Remove(character);
            }
        }
    }
}
