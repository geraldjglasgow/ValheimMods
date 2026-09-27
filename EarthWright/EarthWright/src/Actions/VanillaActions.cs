using EarthWright.Terrain;

namespace EarthWright.Actions
{
    /// <summary>
    /// The game's own terrain entries, read from its prefabs (TerrainOp settings, game build of 2026-09):
    /// <list type="bullet">
    /// <item>mud_road_v2 "Level ground": smooth r3 power 1 toward the ghost height, paint dirt r3.</item>
    /// <item>raise_v2 "Raise ground": raise r2 by 1 m, power 0.1, paint dirt r2.5.</item>
    /// <item>path_v2 "Pathen": paint dirt r2, no height change.</item>
    /// <item>paved_road_v2 "Paved road": smooth r3 power 1, paint paved r2.2.</item>
    /// <item>cultivate_v2 "Cultivate": smooth r3 power 3, paint cultivated r3.</item>
    /// <item>replant_v2 "Replant grass": paint grass (the game's Reset paint) r2.2.</item>
    /// </list>
    /// </summary>
    public static class VanillaActions
    {
        public static void Register()
        {
            ActionCatalog.Register(Level("mud_road_v2", ToolFamily.Hoe, PaintOp.Dirt, 3f, 1f));
            ActionCatalog.Register(Level("paved_road_v2", ToolFamily.Hoe, PaintOp.Paved, 3f, 2.2f / 3f));
            ActionCatalog.Register(Level("cultivate_v2", ToolFamily.Cultivator, PaintOp.Cultivated, 3f, 1f));
            ActionCatalog.Register(new ToolAction
            {
                Id = "raise_v2", Family = ToolFamily.Hoe, Height = HeightOp.Raise, Paint = PaintOp.Dirt,
                BaseRadius = 2f, PaintRatio = 1.25f, Amount = 1f, UsesAmount = true, Hardness = 0.8f,
            });
            ActionCatalog.Register(PaintOnly("path_v2", ToolFamily.Hoe, PaintOp.Dirt, 2f));
            ActionCatalog.Register(PaintOnly("replant_v2", ToolFamily.Cultivator, PaintOp.Grass, 2.2f));
        }

        private static ToolAction Level(string id, ToolFamily family, PaintOp paint, float radius, float paintRatio)
        {
            return new ToolAction
            {
                Id = id, Family = family, Height = HeightOp.Level, Style = LevelStyle.Ease, Paint = paint,
                BaseRadius = radius, PaintRatio = paintRatio, UsesTarget = true, MaxStep = 1f, Hardness = 0.3f,
            };
        }

        private static ToolAction PaintOnly(string id, ToolFamily family, PaintOp paint, float radius)
        {
            return new ToolAction { Id = id, Family = family, Paint = paint, BaseRadius = radius, Hardness = 1f };
        }

        /// <summary>The game's paint type as an EarthWright paint.</summary>
        public static PaintOp PaintOf(TerrainModifier.PaintType type)
        {
            switch (type)
            {
                case TerrainModifier.PaintType.Dirt: return PaintOp.Dirt;
                case TerrainModifier.PaintType.Cultivate: return PaintOp.Cultivated;
                case TerrainModifier.PaintType.Paved: return PaintOp.Paved;
                case TerrainModifier.PaintType.Reset: return PaintOp.Grass;
                case TerrainModifier.PaintType.ClearVegetation: return PaintOp.ClearVegetation;
                case TerrainModifier.PaintType.DeepSnow: return PaintOp.DeepSnow;
                default: return PaintOp.None;
            }
        }
    }
}
