using System;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One of the seven rudders, a biome's: its prefab and label, what a ship built from the hammer pays for it
    /// (<see cref="Recipe"/>), how well it steers a ship (<see cref="Steering"/>), its stock and blade on the set's post
    /// (the blade's after edge from its foot up, <see cref="Edge"/>), and its design: the parts that stay on the post and
    /// the parts on the turning blade, each in its own material (<see cref="KwRudderKit"/>). Every rudder
    /// grows with its post the same way: the head and tiller with <see cref="KwRudderFit.Scale"/> and
    /// <see cref="KwRudderFit.Thick"/>, the straps round the post's own width and depth, the blade with its span
    /// (<see cref="KwRudderSpan"/>: from just over the keel bottom up to the ship's design waterline, the stock alone above
    /// it). Its link to the helm
    /// (<see cref="KwRudderLay"/>) is drawn in its own materials: its head carries the tiller or the yoke, and the deck
    /// tiller, its pivot post and the lines or rods (<see cref="KwRudderLinkShapes"/>) are its wood and its fittings.
    /// </summary>
    public sealed class KwRudderTier
    {
        private float waterline = float.NaN;

        public KwRudderTier(string biome, string name, string materials, Vector2[] edge)
        {
            Biome = biome;
            Name = name;
            Label = $"{name} ({materials})";
            Edge = edge;
        }

        public string Biome { get; }
        public string Prefab => KwRudder.Prefix + Biome;

        /// <summary>Its name in the world ("Rudder, Meadows") and in the hammer, with what it is made of.</summary>
        public string Name { get; }
        public string Label { get; }

        /// <summary>A line on how it is made, for the hammer's description.</summary>
        public string Made { get; set; } = "";

        /// <summary>The blade's after edge on the standard post: (height over the post's foot, distance aft of the stock).</summary>
        public Vector2[] Edge { get; }

        /// <summary>
        /// The height on its outline where the ship's design waterline crosses the blade: all but <see cref="KwRudderSpan.Dry"/>
        /// of the outline's area under it (<see cref="KwRudderSpan"/>).
        /// </summary>
        public float Waterline => float.IsNaN(waterline) ? waterline = KwRudderSpan.Wet(Edge, 1f - KwRudderSpan.Dry) : waterline;

        /// <summary>How well it steers a launched ship (<see cref="KwShipStats"/>): 1 for the plain Meadows blade, more for finer ones.</summary>
        public float Steering { get; set; } = 1f;

        /// <summary>The stock's radius and half the blade's thickness on the standard post.</summary>
        public float StockRadius { get; set; } = 0.065f;
        public float BladeHalf { get; set; } = 0.045f;

        /// <summary>The game's item prefab names and amounts a ship built from the hammer pays for it (<see cref="KwBpCost"/>).</summary>
        public (string Item, int Amount)[] Recipe { get; set; } = new (string Item, int Amount)[0];

        /// <summary>What its deck tiller and pivot post are made of: its wood, and its bands and pins (metal, or leather or eitr where it has none).</summary>
        public KwLook Wood { get; set; } = KwLook.Planks;
        public KwLook Fitting { get; set; } = KwLook.Iron;

        /// <summary>Its steering links: rods of this metal, or the game's rope when none.</summary>
        public KwLook? Rod { get; set; }

        /// <summary>The design: the parts that stay on the post, and the parts on the blade (stock, blade, head, tiller or yoke).</summary>
        public Action<KwRudderKit> Fixed { get; set; } = _ => { };
        public Action<KwRudderKit> Blade { get; set; } = _ => { };

        /// <summary>The stock's radius on a post: as thick as the post or the blade asks, whichever is more.</summary>
        public float Stock(KwRudderFit fit) => StockRadius * Mathf.Max(fit.Thick, Span(fit).Thick);

        /// <summary>The stock's axis on the piece, just aft of the post: the blade turns about it (kit axes).</summary>
        public Vector3 Pivot(KwRudderFit fit) => new Vector3(-(Stock(fit) + 0.025f), 0f, 0f);

        /// <summary>The design built to a post and linked to its helm: every part's mesh in its material (<see cref="KwRudderKit.Combined"/>).</summary>
        public KwRudderKit Build(KwRudderFit fit, KwRudderLay? lay = null)
        {
            KwRudderKit kit = new KwRudderKit(this, fit, null, lay);
            Fixed(kit);
            Blade(kit);
            KwRudderLinkShapes.Deck(kit);
            return kit;
        }

        /// <summary>Where the blade's foot and tip are on a post, and how its outline is stretched to them; its tip no higher than a ceiling.</summary>
        public KwRudderSpan Span(KwRudderFit fit, float ceiling = float.MaxValue) => new KwRudderSpan(this, fit, ceiling);

        /// <summary>The blade's longest reach aft on the standard post.</summary>
        public float Longest()
        {
            float most = 0f;
            foreach (Vector2 corner in Edge)
                most = Mathf.Max(most, corner.y);
            return most;
        }
    }
}
