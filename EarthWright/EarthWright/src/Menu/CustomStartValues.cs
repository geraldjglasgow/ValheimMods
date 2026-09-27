using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;

namespace EarthWright.Menu
{
    /// <summary>
    /// A custom entry's starting brush. Its radius and height reach the brush through the entry's action
    /// (<see cref="CustomEntry.CreateAction"/>: BaseRadius and Amount, which the Brush module's EntryDefaults read);
    /// the shape, which an action cannot carry, is written here. The Brush module remembers each entry's values for the
    /// session (<see cref="BrushMemory"/>) and publishes them into BrushState every frame, so the values are set on
    /// that remembered object - once per object and entry file: the first time the entry is selected, again after the
    /// file changed (fresh starting values replace the remembered ones, which were made from the old file) and after
    /// the brush forgot its memory (its own YAML file changed). In between the player's adjustments are kept.
    /// </summary>
    public static class CustomStartValues
    {
        private static readonly Dictionary<string, BrushValues> applied = new Dictionary<string, BrushValues>();
        private static int appliedVersion = -1;

        public static void Initialize() => Ticker.OnUpdate("EarthWright custom entry start values", Tick);

        private static void Tick()
        {
            ToolAction action = BrushState.Active ? BrushState.Action : null;
            CustomEntry entry = CustomEntries.ForAction(action);
            if (entry == null)
                return;
            if (appliedVersion != CustomEntries.Version)
            {
                appliedVersion = CustomEntries.Version;
                applied.Clear();
            }
            BrushValues values = BrushMemory.For(action);
            if (applied.TryGetValue(action.Id, out BrushValues done) && ReferenceEquals(done, values))
                return;
            Reset(values, action, entry);
            applied[action.Id] = values;
        }

        /// <summary>The entry's starting values from the current file, in place of whatever was remembered.</summary>
        private static void Reset(BrushValues values, ToolAction action, CustomEntry entry)
        {
            BrushValues start = BrushValues.From(EntryDefaults.Resolve(action));
            values.Defaults = start.Defaults;
            values.Radius = start.Radius;
            values.Radius2 = start.Radius2;
            values.Amount = start.Amount;
            values.Hardness = start.Hardness;
            values.Shape = entry.Shape ?? start.Shape;
        }
    }
}
