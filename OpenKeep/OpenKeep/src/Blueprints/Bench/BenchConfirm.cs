using System;
using System.Collections.Generic;
using System.Linq;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>The game's own yes / no box (UnifiedPopup) before a delete or a remove, and the picked names it lists.</summary>
    public static class BenchConfirm
    {
        public static void Ask(string headerWord, string text, Action yes)
        {
            if (!UnifiedPopup.IsAvailable())
                return;
            UnifiedPopup.Push(new YesNoPopup(Language.Localize(headerWord), text, () =>
            {
                UnifiedPopup.Pop();
                BlueprintSafe.Run("OpenKeep blueprint bench", yes);
            }, UnifiedPopup.Pop, localizeText: false));
        }

        /// <summary>"Barn, Houses, Hut" or "Barn, Houses, Hut (+4)".</summary>
        public static string Names(List<BenchEntry> picked)
        {
            string first = string.Join(", ", picked.Take(3).Select(e => e.Name));
            return picked.Count > 3 ? first + " (+" + (picked.Count - 3) + ")" : first;
        }
    }
}
