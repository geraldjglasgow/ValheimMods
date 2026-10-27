using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's materials set as the game's own ships' are. The game's piece shader (<c>Custom/Piece</c>)
    /// shades building pieces with a value noise laid out in the world, per vertex, and a vertex ripple: right for a
    /// house, but on a ship the hull moves through the noise, and light and dark streaks run along its planks as it bobs
    /// and rolls (measured 2026-10-24: 21% of a hull's pixels flickering frame to frame, 1.3% with the game ships'
    /// settings, a vanilla longship 1.5%). So every piece-shader material on a ship is swapped for a ship copy with the
    /// vanilla ship's settings: no value noise, no ripple, the moveable-object switch on. Copies, one per material and
    /// kept: the same materials on docks and buildings stay as they are.
    /// </summary>
    public static class KwShipMaterials
    {
        private static readonly Dictionary<Material, Material> copies = new Dictionary<Material, Material>();

        public static void Apply(Transform ship)
        {
            foreach (Renderer renderer in ship.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material? copy = materials[i] != null ? Ship(materials[i]) : null;
                    if (copy != null && copy != materials[i])
                        (materials[i], changed) = (copy, true);
                }
                if (changed)
                    renderer.sharedMaterials = materials;
            }
        }

        // The ship copy of a piece-shader material (made once); any other material as it is.
        private static Material Ship(Material source)
        {
            if (!source.HasProperty("_ValueNoise") || !source.HasProperty("_MoveableObject"))
                return source;
            if (copies.TryGetValue(source, out Material made) && made != null)
                return made;
            Material ship = new Material(source) { name = source.name + " (ship)" };
            ship.SetFloat("_ValueNoise", 0f);
            ship.SetFloat("_ValueNoiseVertex", 0f);
            ship.DisableKeyword("_VALUENOISEVERTEX_ON");
            ship.SetFloat("_RippleDistance", 0f);
            ship.SetFloat("_MoveableObject", 1f);
            ship.EnableKeyword("_MOVEABLEOBJECT_ON");
            return copies[source] = ship;
        }
    }
}
