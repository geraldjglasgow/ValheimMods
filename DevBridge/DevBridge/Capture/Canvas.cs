using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Capture
{
    /// <summary>RGBA pixels addressed from the top-left corner, stored bottom row first as Texture2D keeps them.</summary>
    internal sealed class Canvas
    {
        internal readonly int Width, Height;
        internal readonly Color32[] Pixels;

        internal Canvas(Color32[] pixels, int width, int height)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
        }

        internal Canvas(int width, int height, Color32 fill) : this(new Color32[width * height], width, height)
        {
            for (int i = 0; i < Pixels.Length; i++) Pixels[i] = fill;
        }

        private int Index(int x, int top) => (Height - 1 - top) * Width + x;

        /// <summary>Full alpha everywhere, so no file comes out see-through where the frame kept some alpha.</summary>
        internal Canvas Opaque()
        {
            for (int i = 0; i < Pixels.Length; i++) Pixels[i].a = 255;
            return this;
        }

        internal void Fill(int x, int top, int width, int height, Color32 colour)
        {
            foreach (int i in Area(x, top, width, height)) Pixels[i] = colour;
        }

        /// <summary>Darkens a rectangle to a quarter: a backing that keeps white text readable on any picture.</summary>
        internal void Shade(int x, int top, int width, int height)
        {
            foreach (int i in Area(x, top, width, height))
            {
                Color32 c = Pixels[i];
                Pixels[i] = new Color32((byte)(c.r / 4), (byte)(c.g / 4), (byte)(c.b / 4), 255);
            }
        }

        /// <summary>Copies another canvas in with its top-left corner at (x, top), at most width by height of it.</summary>
        internal void Paste(Canvas source, int x, int top, int width, int height)
        {
            width = Mathf.Min(width, source.Width, Width - x);
            height = Mathf.Min(height, source.Height, Height - top);
            if (width <= 0) return;
            for (int row = 0; row < height; row++)
                Array.Copy(source.Pixels, source.Index(0, row), Pixels, Index(x, top + row), width);
        }

        /// <summary>The indices of a rectangle's pixels, clipped to the canvas.</summary>
        private IEnumerable<int> Area(int x, int top, int width, int height)
        {
            for (int row = Mathf.Max(top, 0); row < Mathf.Min(top + height, Height); row++)
                for (int column = Mathf.Max(x, 0); column < Mathf.Min(x + width, Width); column++)
                    yield return Index(column, row);
        }
    }
}
