using System;
using PackPanel.Look;
using UnityEngine;

internal static class Program
{
    private static void Near(float actual, float expected, string message)
    {
        if (Math.Abs(actual - expected) > 0.00001f)
            throw new Exception(message + ": expected " + expected + ", got " + actual);
    }

    private static void Main()
    {
        var texture = new Vector2(1254, 1254);
        var wood = new Rect(164, 164, 926, 926);
        int[,] cases = { {1280,720}, {1920,1080}, {2560,1440}, {3440,1440}, {3840,2160}, {1024,768}, {720,1280} };
        for (int i = 0; i < cases.GetLength(0); i++)
        {
            float width = cases[i, 0], height = cases[i, 1];
            var canvas = new Rect(-width / 2, -height / 2, width, height);
            var point = new Vector2(canvas.xMin + 200, canvas.yMax - 150);
            Vector2 uv = TimberCoordinates.Uv(point, canvas, wood, texture);
            Vector2 right = TimberCoordinates.Uv(new Vector2(point.x + 64, point.y), canvas, wood, texture);
            Vector2 down = TimberCoordinates.Uv(new Vector2(point.x, point.y - 64), canvas, wood, texture);
            Near(right.x - uv.x, uv.y - down.y, "Wood must retain its aspect ratio");
            Vector2 further = TimberCoordinates.Uv(new Vector2(point.x + 128, point.y), canvas, wood, texture);
            Near(further.x - right.x, right.x - uv.x, "New cells must retain the existing texture density");
            foreach (float x in new[] { canvas.xMin, 0, canvas.xMax })
            foreach (float y in new[] { canvas.yMin, 0, canvas.yMax })
            {
                Vector2 sample = TimberCoordinates.Uv(new Vector2(x, y), canvas, wood, texture);
                if (sample.x < wood.xMin / texture.x - 0.00001f || sample.x > wood.xMax / texture.x + 0.00001f
                    || sample.y < wood.yMin / texture.y - 0.00001f || sample.y > wood.yMax / texture.y + 0.00001f)
                    throw new Exception("Screen sample entered frame artwork at " + width + "x" + height);
            }
            var doubled = new Rect(canvas.x * 2, canvas.y * 2, width * 2, height * 2);
            Vector2 scaled = TimberCoordinates.Uv(new Vector2(point.x * 2, point.y * 2), doubled, wood, texture);
            Near(scaled.x, uv.x, "Corresponding samples must survive resolution changes");
            Near(scaled.y, uv.y, "Corresponding samples must survive resolution changes");
            Vector2 topLeft = TimberCoordinates.Uv(new Vector2(canvas.xMin, canvas.yMax), canvas, wood, texture);
            Near(topLeft.x, wood.xMin / texture.x, "Wallpaper must stay anchored at the canvas top-left");
            Near(topLeft.y, wood.yMax / texture.y, "Wallpaper must stay anchored at the canvas top-left");
        }
        Vector2 empty = TimberCoordinates.Uv(Vector2.zero, new Rect(), wood, texture);
        Near(empty.x, 0, "Empty canvas must be safe");
        Near(empty.y, 0, "Empty canvas must be safe");
        Console.WriteLine("PASS: aspect ratio, growth, frame exclusion and screen alignment across seven aspect/resolution cases and doubled resolutions.");
    }
}
