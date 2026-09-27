using System;
using System.Collections.Generic;
using EarthWright.Core;

namespace EarthWright.History
{
    /// <summary>
    /// Handlers that take the undo key before the terrain history does: the ramp and road tools remove their last
    /// point first, and only when no tool has a point to remove is the last terrain change undone. (How far an edit may
    /// reach beyond its own area, e.g. gentle slopes, comes from <c>Engine.ExtraReach</c>, not from here.)
    /// </summary>
    public static class UndoHooks
    {
        private static readonly List<Func<bool>> first = new List<Func<bool>>();

        /// <summary>Adds a handler; it returns true when it consumed the undo press.</summary>
        public static void AddFirst(Func<bool> tryConsume) => first.Add(tryConsume);

        /// <summary>True when a handler consumed the press. A handler that throws is logged and counts as not consuming.</summary>
        public static bool TryFirst()
        {
            foreach (Func<bool> handler in first)
            {
                if (Safe.Call("EarthWright undo hook", handler, false))
                    return true;
            }
            return false;
        }
    }
}
