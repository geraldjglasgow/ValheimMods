using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock lantern's post as its ZDO saves it (<see cref="Key"/>), so every peer and every reload builds the same:
    /// <c>lantern1|length|material|seed|dock</c> (or <c>floor</c>). The piece stands where the post's axis meets the deck's
    /// top, just outside the deck's edge, its +Z in over the deck; the post rises <see cref="Rise"/> over that to the lantern
    /// head and runs <see cref="Length"/> under it to its foot, sunk into the sea floor or the ground (found by a cast once,
    /// when it was placed). Its material is the dock's ("wood", "corewood", "finewood", <see cref="KwPierKind"/>); its seed
    /// bends a rough wood pole; and what it stands beside: a dock section (held up by it, <see cref="KwPierLadderSupport"/>)
    /// or a game floor (the game's own support). Its cost (<see cref="Requirements"/>): one of the material's item per 2 m of
    /// post, at least 2, and the lantern's own 2 Bronze and 2 Resin. Plain data.
    /// </summary>
    public sealed class KwDockLanternSpec
    {
        /// <summary>The ZDO string a dock lantern saves its post in.</summary>
        public const string Key = "bs_dock_lantern";

        /// <summary>How high the post's top (the lantern head's foot) stands over the deck's top.</summary>
        public const float Rise = 1.8f;

        public const float MinLength = 0.25f, MaxLength = 20.3f;
        private const string Head = "lantern1", Dock = "dock", Floor = "floor";
        private const int PerItem = 2, LeastItems = 2;
        private static readonly (string Item, int Amount)[] Lamp = { ("Bronze", 2), ("Resin", 2) };
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public KwDockLanternSpec(float length, KwPierKind kind, int seed, bool onDock)
        {
            (Length, Kind, Seed, OnDock) = (length, kind, seed, onDock);
        }

        /// <summary>Metres of post under the deck's top, down to its foot.</summary>
        public float Length { get; }

        public KwPierKind Kind { get; }
        public int Seed { get; }

        /// <summary>Beside a dock section (true) or a game floor.</summary>
        public bool OnDock { get; }

        /// <summary>The post's wood: one of its item per 2 m of the whole post, rounded up, at least 2.</summary>
        public int Wood => Mathf.Max(LeastItems, Mathf.CeilToInt((Length + Rise) / PerItem - 0.001f));

        /// <summary>The whole cost by item: the post's wood, then the lantern's own.</summary>
        public List<(string Item, int Amount)> Items()
        {
            List<(string Item, int Amount)> items = new List<(string Item, int Amount)> { (Kind.Item, Wood) };
            items.AddRange(Lamp);
            return items;
        }

        /// <summary>The cost as the game's requirements (recovered: a built lantern's, given back when it is removed); an item the game lacks left out.</summary>
        public Piece.Requirement[] Requirements(bool recover)
        {
            List<Piece.Requirement> made = new List<Piece.Requirement>();
            foreach ((string item, int amount) in Items())
            {
                ItemDrop? drop = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(item)?.GetComponent<ItemDrop>() : null;
                if (drop != null)
                    made.Add(new Piece.Requirement { m_resItem = drop, m_amount = amount, m_recover = recover });
            }
            return made.ToArray();
        }

        public override string ToString() => string.Join("|", Head, Length.ToString("F2", Inv), Kind.Id, Seed.ToString(Inv), OnDock ? Dock : Floor);

        /// <summary>A lantern's post from its saved text; false (and nothing built) for anything else.</summary>
        public static bool TryParse(string? text, out KwDockLanternSpec spec)
        {
            spec = null!;
            string[] parts = (text ?? "").Split('|');
            if (parts.Length != 5 || parts[0] != Head || !float.TryParse(parts[1], NumberStyles.Float, Inv, out float length)
                || !int.TryParse(parts[3], NumberStyles.Integer, Inv, out int seed) || (parts[4] != Dock && parts[4] != Floor)
                || float.IsNaN(length) || length < MinLength - 0.01f || length > MaxLength + 0.01f)
                return false;
            spec = new KwDockLanternSpec(length, KwPierKinds.Of(parts[2]), seed, parts[4] == Dock);
            return true;
        }
    }
}
