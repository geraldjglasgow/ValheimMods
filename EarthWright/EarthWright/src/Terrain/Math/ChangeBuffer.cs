using System;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>One planned height change: the index, the heights before and after (local) and the new raw compiler values.</summary>
    public struct HeightChange
    {
        public int Index;
        public float Before;
        public float After;
        public bool Limited;
        public float NewLevel;
        public float NewSmooth;
        public bool NewModified;
    }

    /// <summary>One planned paint change: the index and the new raw compiler values.</summary>
    public struct PaintChange
    {
        public int Index;
        public Color Before;
        public Color NewColor;
        public bool NewModified;
    }

    /// <summary>
    /// What a planner decided for one heightmap: the height and paint changes in the order they were found. Reused
    /// between plans (no allocation once grown), so a caller must consume it before the next plan into the same buffer.
    /// </summary>
    public sealed class ChangeBuffer
    {
        public HeightChange[] Heights = new HeightChange[4225];
        public int HeightCount;
        public PaintChange[] Paints = new PaintChange[4225];
        public int PaintCount;

        /// <summary>At least one vertex was held back by a height limit (changed or not).</summary>
        public bool HitLimit;

        public void Clear()
        {
            HeightCount = 0;
            PaintCount = 0;
            HitLimit = false;
        }

        /// <summary>A normal height edit: the vertex takes the new height as a level delta from its base.</summary>
        public void AddHeight(int index, float before, float after, float baseHeight, bool limited, bool forget)
        {
            AddRawHeight(index, before, after, forget ? 0f : after - baseHeight, 0f, !forget);
            Heights[HeightCount - 1].Limited = limited;
        }

        public void AddRawHeight(int index, float before, float after, float level, float smooth, bool modified)
        {
            if (HeightCount == Heights.Length)
                Array.Resize(ref Heights, Heights.Length * 2);
            Heights[HeightCount++] = new HeightChange
            {
                Index = index, Before = before, After = after, NewLevel = level, NewSmooth = smooth, NewModified = modified,
            };
        }

        public void AddPaint(int index, Color before, Color color, bool modified)
        {
            if (PaintCount == Paints.Length)
                Array.Resize(ref Paints, Paints.Length * 2);
            Paints[PaintCount++] = new PaintChange { Index = index, Before = before, NewColor = color, NewModified = modified };
        }
    }
}
