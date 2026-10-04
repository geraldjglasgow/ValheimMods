using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// terrain: edited ground from the terrain compilers (TerrainEdits), and each terrain modifier (TerrainModifier, the
    /// older per-object edits some locations and pieces still carry) as rings of what it does round its position, in the
    /// game's own gizmo colours: level green (a square when it levels square), smooth blue, paint yellow.
    /// </summary>
    internal static class TerrainDrawer
    {
        private static readonly Color Level = new Color(0.3f, 1f, 0.3f, 0.9f);
        private static readonly Color Smooth = new Color(0.3f, 0.55f, 1f, 0.9f);
        private static readonly Color Paint = new Color(1f, 0.9f, 0.3f, 0.9f);

        internal static void Draw(OverlayArea area, Category into)
        {
            TerrainEdits.Draw(area, into);
            foreach (TerrainModifier modifier in TerrainModifier.GetAllInstances())
            {
                if (!modifier || !modifier.enabled || !area.Holds(modifier.transform.position, modifier.GetRadius())) continue;
                into.Count("modifiers");
                Vector3 at = modifier.transform.position + Vector3.up * (modifier.m_levelOffset + 0.1f);
                float level = modifier.m_levelRadius;
                if (modifier.m_level) into.Lines.Add(modifier.m_square ? Shapes.Square(at, level) : Shapes.Ring(at, level), Level);
                if (modifier.m_smooth) into.Lines.Add(Shapes.Ring(at, modifier.m_smoothRadius), Smooth);
                if (modifier.m_paintCleared) into.Lines.Add(Shapes.Ring(at, modifier.m_paintRadius), Paint);
            }
        }
    }
}
