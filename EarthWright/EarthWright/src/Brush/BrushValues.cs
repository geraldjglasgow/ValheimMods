using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>What the paint key picked: the entry's own paint, a paint that replaces it, or keeping the ground's paint.</summary>
    public enum PaintChoice
    {
        Own = 0,
        Dirt,
        Paved,
        Cultivated,
        Grass,
        Original,
        Vegetation,
        ClearVegetation,
        Keep,
    }

    /// <summary>
    /// The values the player set for one entry. Remembered per entry for the game session (see
    /// <see cref="BrushMemory"/>), so switching between level and raise keeps each one's size. Local player state only.
    /// </summary>
    public sealed class BrushValues
    {
        public EntryDefaults Defaults;
        public float Radius;
        public float Radius2;
        public float Hardness;
        public float Amount;
        public float MaxStep;
        public float Strength;
        public LevelStyle Style;
        public BrushShape Shape;
        public PaintChoice Paint;

        public static BrushValues From(EntryDefaults d)
        {
            return new BrushValues
            {
                Defaults = d,
                Radius = d.Radius,
                Radius2 = Mathf.Max(0.5f, Mathf.Round(d.Radius) * 0.5f),
                Hardness = d.Hardness,
                Amount = d.Amount,
                MaxStep = d.MaxStep,
                Strength = d.Strength,
                Style = d.Style,
                Shape = d.Shape,
                Paint = PaintChoice.Own,
            };
        }
    }

    /// <summary>The remembered values of every entry used this session, by piece prefab name.</summary>
    public static class BrushMemory
    {
        private static readonly Dictionary<string, BrushValues> remembered = new Dictionary<string, BrushValues>();

        /// <summary>The remembered values of the entry, or fresh ones from its defaults.</summary>
        public static BrushValues For(ToolAction action)
        {
            if (remembered.TryGetValue(action.Id, out BrushValues values))
                return values;
            values = BrushValues.From(EntryDefaults.Resolve(action));
            remembered[action.Id] = values;
            return values;
        }

        /// <summary>Forgets everything (the YAML rules changed): each entry starts from its new defaults.</summary>
        public static void Forget()
        {
            remembered.Clear();
            BrushTick.ForceReload();
        }
    }
}
