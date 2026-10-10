using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// The look a workshop model is drawn in. A bundle keeps plain Standard placeholder materials that a mod dresses in a
    /// game material at runtime (GameMaterials: Plain(Dress(game, placeholder), 0.1)); look=creature or look=piece does
    /// the same with the game's creature and item shader or its building shader (<see cref="Dress"/>'s shorthands), so
    /// the picture shows what the game will draw. look=workshop keeps the placeholders. Particles and the workshop's own
    /// glow parts (materials named *_glow, Standard with emission, which mods keep) are never dressed.
    /// </summary>
    internal static class BoothLook
    {
        internal const string Workshop = "workshop";
        private const float Gloss = 0.1f;
        private static readonly string[] Looks = { "creature", "piece", Workshop };

        /// <summary>The look as the booth keys it: null for the placeholders.</summary>
        internal static string Check(string look)
        {
            if (string.IsNullOrEmpty(look)) return null;
            string known = Looks.FirstOrDefault(l => string.Equals(l, look, StringComparison.OrdinalIgnoreCase))
                ?? throw new BridgeException($"look= is {string.Join(", ", Looks)}");
            return known == Workshop ? null : known;
        }

        /// <summary>
        /// The look a model gets unless asked: none for an effect, the building shader for a piece or a model from the
        /// workshop's Props, else the creature and item shader.
        /// </summary>
        internal static string For(string kind, string category)
        {
            if (kind == "Effects") return Workshop;
            bool prop = string.Equals(category, "Props", StringComparison.OrdinalIgnoreCase) && kind != "Creatures" && kind != "Items";
            return kind == "Pieces" || prop ? "piece" : "creature";
        }

        /// <summary>Dresses the copy's placeholders; returns the materials made, for the caller to destroy with the copy.</summary>
        internal static List<Material> Apply(GameObject copy, string look)
        {
            var made = new Dictionary<Material, Material>();
            if (look == null) return made.Values.ToList();
            Material game = Dress.Source(look, null);
            foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(true).Where(r => r is MeshRenderer || r is SkinnedMeshRenderer))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => Dressed(game, m, made)).ToArray();
            return made.Values.ToList();
        }

        private static Material Dressed(Material game, Material placeholder, Dictionary<Material, Material> made)
        {
            if (!placeholder || placeholder.name.EndsWith("_glow", StringComparison.OrdinalIgnoreCase)) return placeholder;
            if (!made.TryGetValue(placeholder, out Material dressed)) made[placeholder] = dressed = Dress.Dressed(game, placeholder, Gloss);
            return dressed;
        }
    }
}
