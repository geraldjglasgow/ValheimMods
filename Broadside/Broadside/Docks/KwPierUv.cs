using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a dock section's timbers take their texture from: its material's atlas (<see cref="KwPierAtlas"/>), long strips
    /// with the grain along u. Every deck board, square timber, log length, pole and rope is given a strip of its family
    /// (<see cref="Boards"/>, <see cref="Timbers"/> (core wood's bark), and wood's <see cref="Narrow"/> boards,
    /// <see cref="Poles"/> and <see cref="Ropes"/>) chosen from the section's seed in the order the parts are made, so the
    /// same on every peer and every load; never one of the last few chosen of its family (eight boards, four timbers: the
    /// boards beside and before it in the deck differ); and a random place along it that leaves the part room, mapped at the
    /// atlas's density along the grain and the strip's height across (a board's width, a timber's 0.2 m, once round a log,
    /// pole or rope; a rough board its own width, <see cref="Board(float, float)"/>). A part's ends take an end-grain tile
    /// of their own, from the place chosen last (the part being made). One for each section built, its places handed to
    /// <see cref="KwPierMesh"/> as a <see cref="Vector3"/>: the strip (its family times 1000 and its number), the metres
    /// along it, and the metres its height stands for across (0: the strips' own).
    /// </summary>
    public sealed class KwPierUv
    {
        /// <summary>The strip families, in a place's code (family times 1000).</summary>
        public const int Boards = 0, Timbers = 1, Narrow = 2, Poles = 3, Ropes = 4;

        private const int Code = 1000;
        private const float NarrowBelow = 0.21f;
        private static readonly int[] Apart = { 8, 4, 8, 3, 1 };

        // Texels kept clear inside a strip's edges, and at its far end, so point sampling never takes a neighbour's.
        private const float Inset = 0.1f, Margin = 2f;

        private readonly KwPierAtlas atlas;
        private readonly System.Random random;
        private readonly List<int>[] recent = { new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>() };
        private Vector3 current;

        private KwPierUv(KwPierAtlas atlas, int seed)
        {
            this.atlas = atlas;
            random = new System.Random(unchecked(seed * 7919 + 1013));
        }

        /// <summary>The longest a timber or log runs along its grain before it is cut and starts on a strip of its own.</summary>
        public float Run => 4f;

        /// <summary>A section's texture places, from its seed (<see cref="KwPierSpec.Seed"/>).</summary>
        public static KwPierUv For(KwPierKind kind, int seed) => new KwPierUv(KwPierAtlas.Of(kind), seed);

        /// <summary>A deck board's place: a board strip unlike the last eight, with room along it for the board's length.</summary>
        public Vector3 Board(float length) => Pick(Boards, length, 0f);

        /// <summary>A board of its own width across the whole strip (both its edges show): a narrow strip under 0.21 m where the atlas has them.</summary>
        public Vector3 Board(float length, float width) => Pick(width < NarrowBelow && atlas.Has(Narrow) ? Narrow : Boards, length, width);

        /// <summary>A square timber's or a log length's place (core wood's bark), with room for its length.</summary>
        public Vector3 Timber(float length) => Pick(Timbers, length, 0f);

        /// <summary>A round pole's place (wood's bark-patched poles; a timber where the atlas has none).</summary>
        public Vector3 Pole(float length) => Pick(atlas.Has(Poles) ? Poles : Timbers, length, 0f);

        /// <summary>A rope's place (wood's twisted rope; a timber where the atlas has none).</summary>
        public Vector3 Rope(float length) => Pick(atlas.Has(Ropes) ? Ropes : Timbers, length, 0f);

        private Vector3 Pick(int family, float length, float across)
        {
            KwPierStrips strips = atlas.Family(family);
            List<int> chosen = recent[family];
            int i = random.Next(strips.Count);
            while (chosen.Contains(i))
                i = random.Next(strips.Count);
            chosen.Add(i);
            if (chosen.Count > Mathf.Min(Apart[family], strips.Count - 1))
                chosen.RemoveAt(0);
            float room = Mathf.Max(0f, (KwPierAtlas.Length - Margin) / KwPierAtlas.Density - length);
            float along = Mathf.Floor((float)random.NextDouble() * room * KwPierAtlas.Density) / KwPierAtlas.Density;
            return current = new Vector3(family * Code + i, along, across);
        }

        /// <summary>A timber's side: metres along its grain and across the face from its corner, on the part's place.</summary>
        public Vector2 Side(float along, float across, Vector3 place)
        {
            (KwPierStrips strips, Vector2 corner) = Strip(place);
            float span = place.z > 0f ? place.z : strips.Across;
            float row = Inset + Mathf.Clamp01(across / span) * (strips.Height - 2f * Inset);
            return KwPierAtlas.At(corner.x + (place.y + along) * KwPierAtlas.Density, corner.y + row);
        }

        /// <summary>A round log, pole or rope: how far round it (0 to 1, the strip's height) and metres along it, on the length's place.</summary>
        public Vector2 Round(float around, float along, Vector3 place)
        {
            (KwPierStrips strips, Vector2 corner) = Strip(place);
            float row = Inset + Mathf.Clamp01(around) * (strips.Height - 2f * Inset);
            return KwPierAtlas.At(corner.x + (place.y + along) * KwPierAtlas.Density, corner.y + row);
        }

        /// <summary>A timber's end: metres from the end's middle, on the end-grain tile of the part made last.</summary>
        public Vector2 End(float x, float y)
        {
            int code = Mathf.RoundToInt(current.x), family = code / Code;
            int shift = family == Boards ? 31 : family == Timbers ? 0 : family * 13;
            int tile = ((code % Code) * 7 + Mathf.FloorToInt(current.y * KwPierAtlas.Density) + shift) % KwPierAtlas.Ends;
            Vector2 middle = KwPierAtlas.TileMiddle(tile);
            return KwPierAtlas.At(middle.x + InTile(x), middle.y - InTile(y));
        }

        private static float InTile(float metres)
        {
            const float half = KwPierAtlas.Tile * 0.5f - Inset;
            return Mathf.Clamp(metres * KwPierAtlas.Density, -half, half);
        }

        // The strips a place is on and its strip's top left corner.
        private (KwPierStrips Strips, Vector2 Corner) Strip(Vector3 place)
        {
            int code = Mathf.RoundToInt(place.x);
            KwPierStrips strips = atlas.Family(code / Code);
            return (strips, strips.Corner(Mathf.Clamp(code % Code, 0, strips.Count - 1)));
        }
    }
}
