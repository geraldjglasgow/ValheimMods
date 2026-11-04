using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Dock tool in the hammer's Misc tab after the dockside decorations (<see cref="KwHammer"/> adds it, so the saved
    /// ships still come last), and in its build menu's Broadside entry (<see cref="KwBuildMenu"/>). The dock sections it
    /// builds are never in the hammer. Idempotent.
    /// </summary>
    public static class KwPierHammer
    {
        public static void Add(List<GameObject> pieces)
        {
            GameObject? tool = KwPierPrefabs.ToolPrefab;
            if (tool != null && !pieces.Contains(tool))
                pieces.Add(tool);
        }
    }
}
