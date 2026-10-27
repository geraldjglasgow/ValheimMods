using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The two small post caps: the finial, a turned knob about 0.5 m tall (a collar on its foot, a short neck, a bulb, a
    /// bead and a point), and the plain cap, a board overhanging the post with a gentle slope fore and aft up to a small
    /// ridge, about 0.15 m. Kit axes: x outward, y across, z up from the middle of the post's top.
    /// </summary>
    public static class KwCapSmall
    {
        private const int TurnedSides = 10;

        // The finial's turned profile from the foot's top up: (radius, height).
        private static readonly Vector2[] Profile =
        {
            new Vector2(0.13f, FinialFoot), new Vector2(0.13f, 0.1f), new Vector2(0.075f, 0.12f), new Vector2(0.06f, 0.18f),
            new Vector2(0.105f, 0.22f), new Vector2(0.13f, 0.28f), new Vector2(0.105f, 0.34f), new Vector2(0.055f, 0.37f),
            new Vector2(0.07f, 0.395f), new Vector2(0.04f, 0.42f), new Vector2(0.006f, 0.5f),
        };

        /// <summary>The finial's foot's height (where its turned work starts) and the plain cap's board's thickness.</summary>
        public const float FinialFoot = 0.07f, Board = 0.04f;

        // The plain cap: the board's half size, the slope's top, the ridge's half depth and top.
        private const float BoardX = 0.19f, BoardY = 0.25f, SlopeTop = 0.115f, RidgeX = 0.035f, RidgeTop = 0.15f;

        /// <summary>The finial: its foot, then the profile turned round the post's middle.</summary>
        public static void Finial(KwCapPen b)
        {
            KwCaps.Foot(b, Profile[0].y, 0.14f, 0.19f);
            List<Vector3> line = new List<Vector3>();
            List<Vector2> sizes = new List<Vector2>();
            foreach (Vector2 p in Profile)
            {
                line.Add(new Vector3(0f, 0f, p.y));
                sizes.Add(new Vector2(p.x, p.x));
            }
            KwCapSweep.Tube(b, line, sizes, KwCapSweep.Round(TurnedSides));
        }

        /// <summary>The plain cap: the board, a slope narrowing fore and aft to a flat top, and the ridge along it across the ship.</summary>
        public static void Plain(KwCapPen b)
        {
            b.Box(new Vector3(-BoardX, -BoardY, 0f), new Vector3(BoardX, BoardY, Board));
            float x0 = BoardX - 0.01f, y0 = BoardY - 0.01f, x1 = 0.025f, y1 = BoardY - 0.025f;
            b.Hexa(new[]
            {
                new Vector3(-x0, -y0, Board), new Vector3(x0, -y0, Board), new Vector3(x0, y0, Board), new Vector3(-x0, y0, Board),
                new Vector3(-x1, -y1, SlopeTop), new Vector3(x1, -y1, SlopeTop), new Vector3(x1, y1, SlopeTop), new Vector3(-x1, y1, SlopeTop),
            });
            b.Box(new Vector3(-RidgeX, -y0, SlopeTop - 0.005f), new Vector3(RidgeX, y0, RidgeTop));
        }
    }
}
