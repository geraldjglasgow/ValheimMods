using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock ladder's shape as its ZDO saves it (<see cref="Key"/>), so every peer and every reload builds the same:
    /// <c>ladder1|length|seed|dock</c> (or <c>floor</c>). Its length in metres from the deck's top, where the piece stands
    /// (on the edge, facing out over the water), down to its foot; a seed for its look; and what it hangs from: a dock
    /// section (held up by it and gone with it, <see cref="KwPierLadderSupport"/>, and its curb cut there,
    /// <see cref="KwPierGaps"/>) or a game floor (the game's own support). Its material is its prefab's. Its cost is one of
    /// the material's item a metre, rounded up (<see cref="Cost"/>). Plain data.
    /// </summary>
    public sealed class KwPierLadderSpec
    {
        /// <summary>The ZDO string a dock ladder saves its shape in.</summary>
        public const string Key = "bs_pier_ladder";

        public const float MinLength = 0.6f, MaxLength = 10f;

        private const string Head = "ladder1", Dock = "dock", Floor = "floor";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public KwPierLadderSpec(float length, int seed, bool onDock)
        {
            (Length, Seed, OnDock) = (length, seed, onDock);
        }

        public float Length { get; }
        public int Seed { get; }

        /// <summary>Hung from a dock section (true) or a game floor.</summary>
        public bool OnDock { get; }

        /// <summary>What it costs in its material's item: one a metre, rounded up, at least one.</summary>
        public int Cost => Mathf.Max(1, Mathf.CeilToInt(Length - 0.001f));

        public override string ToString() => string.Join("|", Head, Length.ToString("F2", Inv), Seed.ToString(Inv), OnDock ? Dock : Floor);

        /// <summary>A ladder's shape from its saved text; false (and nothing built) for anything else.</summary>
        public static bool TryParse(string? text, out KwPierLadderSpec spec)
        {
            spec = null!;
            string[] parts = (text ?? "").Split('|');
            if (parts.Length != 4 || parts[0] != Head || !float.TryParse(parts[1], NumberStyles.Float, Inv, out float length)
                || !int.TryParse(parts[2], NumberStyles.Integer, Inv, out int seed) || (parts[3] != Dock && parts[3] != Floor)
                || float.IsNaN(length) || length < MinLength - 0.01f || length > MaxLength + 0.01f)
                return false;
            spec = new KwPierLadderSpec(length, seed, parts[3] == Dock);
            return true;
        }
    }
}
