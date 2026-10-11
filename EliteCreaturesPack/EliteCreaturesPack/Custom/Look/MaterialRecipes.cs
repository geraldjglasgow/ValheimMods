using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// Copies of game materials with a colour or the main texture changed, made once and shared by everything that draws
    /// them: every creature of a prefab, its corpse, every item a tinted creature carries, an overlay's particles. Keyed by
    /// what went into them (the source material, the new texture, the colour, the kind of change), so the next world built
    /// from the same definitions reuses them and nothing grows with each load. The game's own materials are never changed.
    /// Only made on a machine that draws.
    /// </summary>
    internal static class MaterialRecipes
    {
        /// <summary>A body or item: tint and texture. An overlay: colourized (the hue carrier) or grey (a multiplier).</summary>
        public enum Change
        {
            Dress,
            Colourize,
            Grey,
        }

        private const string MainTexture = "_MainTex";
        private static readonly string[] TintColours = { "_Color", "_TintColor" };
        private static readonly string[] EffectColours = { "_Color", "_TintColor", "_EmissionColor" };
        private static readonly Dictionary<(int, int, Color, Change), Material> made = new Dictionary<(int, int, Color, Change), Material>();

        /// <summary>Whether a tint can colour the material: it has the game's tint colour (the player's body has none).</summary>
        public static bool CanTint(Material? material) => material != null && TintColour(material) != null;

        /// <summary>
        /// The material with its tint colour set to <paramref name="tint"/> and, where its main texture is
        /// <paramref name="from"/>, that texture swapped for <paramref name="to"/>; the material itself when neither applies.
        /// </summary>
        public static Material Dressed(Material source, Color? tint, Texture? from, Texture? to)
        {
            bool swap = to != null && from != null && source.HasProperty(MainTexture) && source.GetTexture(MainTexture) == from;
            bool tints = tint != null && CanTint(source);
            if (!swap && !tints)
            {
                return source;
            }
            Color colour = tints ? new Color(tint!.Value.r, tint.Value.g, tint.Value.b, 1f) : Color.clear; // a tint's alpha is not used
            Texture? texture = swap ? to : null;
            var key = (source.GetInstanceID(), texture != null ? texture.GetInstanceID() : 0, colour, Change.Dress);
            return Made(key, source, copy => Dress(copy, tints ? tint : null, texture));
        }

        /// <summary>An overlay material: every colour it has colourized to <paramref name="colour"/>, or made grey.</summary>
        public static Material Recoloured(Material source, Color colour, Change change)
        {
            Color key = change == Change.Grey ? Color.clear : colour;
            return Made((source.GetInstanceID(), 0, key, change), source, copy => Recolour(copy, colour, change));
        }

        /// <summary>A texture is being replaced (its file changed): the materials wearing it go with it.</summary>
        public static void Forget(Texture texture)
        {
            int id = texture.GetInstanceID();
            List<(int, int, Color, Change)> stale = new List<(int, int, Color, Change)>();
            foreach (KeyValuePair<(int, int, Color, Change), Material> entry in made)
            {
                if (entry.Key.Item2 == id)
                {
                    stale.Add(entry.Key);
                }
            }
            foreach ((int, int, Color, Change) key in stale)
            {
                Object.Destroy(made[key]);
                made.Remove(key);
            }
        }

        private static Material Made((int, int, Color, Change) key, Material source, Action<Material> change)
        {
            if (made.TryGetValue(key, out Material found) && found != null)
            {
                return found;
            }
            Material copy = new Material(source) { name = source.name + "_ecp" };
            change(copy);
            made[key] = copy;
            return copy;
        }

        private static void Dress(Material copy, Color? tint, Texture? texture)
        {
            if (texture != null)
            {
                copy.SetTexture(MainTexture, texture);
            }
            string? property = tint != null ? TintColour(copy) : null;
            if (property != null)
            {
                copy.SetColor(property, ColourMaths.Tinted(copy.GetColor(property), tint!.Value));
            }
        }

        private static void Recolour(Material copy, Color colour, Change change)
        {
            foreach (string property in EffectColours)
            {
                if (copy.HasProperty(property))
                {
                    Color old = copy.GetColor(property);
                    copy.SetColor(property, change == Change.Grey ? ColourMaths.Grey(old) : ColourMaths.Colourize(old, colour, false));
                }
            }
        }

        private static string? TintColour(Material material)
        {
            foreach (string property in TintColours)
            {
                if (material.HasProperty(property))
                {
                    return property;
                }
            }
            return null;
        }
    }
}
