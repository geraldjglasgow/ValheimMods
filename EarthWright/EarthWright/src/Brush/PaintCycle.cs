using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Terrain;

namespace EarthWright.Brush
{
    /// <summary>
    /// The paint key: the entry's own paint, then the paints the server allows ("Allowed Paints"), per entry. Keep
    /// (height only, the ground's paint stays) is offered for entries that change the height and for ramps and roads
    /// (their "no paint"). The choice is
    /// published as <see cref="BrushState.PaintOverride"/> and <see cref="BrushState.KeepPaint"/>.
    /// </summary>
    public static class PaintCycle
    {
        private static readonly ChoiceList<PaintChoice> allowed = new ChoiceList<PaintChoice>(() => BrushSettings.AllowedPaints);

        public static void Next(ToolAction action, BrushValues values)
        {
            List<PaintChoice> cycle = Cycle(action);
            int index = cycle.IndexOf(Effective(action, values));
            values.Paint = cycle[(index + 1) % cycle.Count];
            BrushAnnounce.Toggle(BrushWords.Paint + ": " + BrushWords.PaintName(values.Paint));
        }

        /// <summary>The player's choice, or Own when the server no longer allows it for this entry.</summary>
        public static PaintChoice Effective(ToolAction action, BrushValues values)
        {
            PaintChoice choice = values.Paint;
            return choice == PaintChoice.Own || Offered(choice, action) ? choice : PaintChoice.Own;
        }

        private static bool Offered(PaintChoice choice, ToolAction action)
        {
            bool keepUseless = choice == PaintChoice.Keep && action.Height == HeightOp.None && !EntryKinds.IsPath(action);
            return choice != PaintChoice.Own && allowed.Contains(choice) && !keepUseless;
        }

        public static void Publish(ToolAction action, BrushValues values)
        {
            PaintChoice choice = Effective(action, values);
            BrushState.KeepPaint = choice == PaintChoice.Keep;
            BrushState.PaintOverride = OpOf(choice);
        }

        /// <summary>The choice a paint written into the brush state stands for (the panel's paint grid).</summary>
        public static PaintChoice ChoiceOf(PaintOp op, bool keep)
        {
            switch (op)
            {
                case PaintOp.Dirt: return PaintChoice.Dirt;
                case PaintOp.Paved: return PaintChoice.Paved;
                case PaintOp.Cultivated: return PaintChoice.Cultivated;
                case PaintOp.Grass: return PaintChoice.Grass;
                case PaintOp.Original: return PaintChoice.Original;
                case PaintOp.Vegetation: return PaintChoice.Vegetation;
                case PaintOp.ClearVegetation: return PaintChoice.ClearVegetation;
                default: return keep ? PaintChoice.Keep : PaintChoice.Own;
            }
        }

        /// <summary>The stroke would paint vegetation: the density is then the entry's amount value.</summary>
        public static bool PaintsVegetation(ToolAction action, BrushValues values)
        {
            PaintChoice choice = Effective(action, values);
            return choice == PaintChoice.Vegetation || (choice == PaintChoice.Own && action.Paint == PaintOp.Vegetation);
        }

        private static List<PaintChoice> Cycle(ToolAction action)
        {
            List<PaintChoice> cycle = new List<PaintChoice> { PaintChoice.Own };
            foreach (PaintChoice choice in allowed.Values)
            {
                if (Offered(choice, action))
                    cycle.Add(choice);
            }
            return cycle;
        }

        private static PaintOp OpOf(PaintChoice choice)
        {
            switch (choice)
            {
                case PaintChoice.Dirt: return PaintOp.Dirt;
                case PaintChoice.Paved: return PaintOp.Paved;
                case PaintChoice.Cultivated: return PaintOp.Cultivated;
                case PaintChoice.Grass: return PaintOp.Grass;
                case PaintChoice.Original: return PaintOp.Original;
                case PaintChoice.Vegetation: return PaintOp.Vegetation;
                case PaintChoice.ClearVegetation: return PaintOp.ClearVegetation;
                default: return PaintOp.None;
            }
        }
    }
}
