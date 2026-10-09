using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// The fitted looks, by the game's leggings that wear them. Atlas regions are measured on the game's textures
    /// (2026-10-08): the iron chest's 128 px mesh atlas (mail block, leather, iron disc and strips), the bronze chest's
    /// 128 px mesh atlas (stitched leather panel, plain below its stitches; bronze discs; straight bronze strip) and its
    /// 256 px body paint (the bronze plates: 4 x 4 texels with dark rims in offset rows, repeating every 13 x 12 texels
    /// from the first plate at 32, 19), the wolf set's 256 px mesh atlas shared by its chest and trousers (its silver mail
    /// patches, metal, the largest clean block at 108-129, 211-232; its grey pelt; the leather straps; the silver discs,
    /// metal 1), and the carapace set's 128 px mesh atlas shared by its chest and trousers (the chest's round scales,
    /// repeating every 6 x 8 texels in offset rows; mottled leather; the blue chitin plates, opaque at 88-125, 96-125; a
    /// steel strip, metal 1), and the flametal set's 128 px mesh atlas shared by its chest and trousers (its iridescent
    /// mail, metal 1, repeating every 4 x 7 texels, clean at 104-127, 0-34; the heat-tinted plate; steel; mauve leather),
    /// and the Protector set's 256 px mesh atlas shared by its breastplate and trousers (the trousers' charcoal leather;
    /// the breastplate's steel with its orange flame motifs, metal 1; its ember orange; its pale pelt; brown leather), and the
    /// Vanguard set's 256 px mesh atlas (the trousers' teal panel with its gold vine at 102-191, 2-51, the vine's stem at
    /// x 148; the chest's gold braid, flat at 140-196, 236-240; its gold brooch at 200-216, 163-184; brown leather).
    /// </summary>
    internal static class LegStyles
    {
        private static readonly Dictionary<int, LegStyle> byLegs = new Dictionary<int, LegStyle>();

        static LegStyles()
        {
            Add(Iron());
            Add(Bronze());
            Add(Wolf());
            Add(Carapace());
            Add(Flametal());
            Add(Protector());
            Add(Vanguard());
        }

        public static LegStyle ByLegsHash(int hash) => byLegs.TryGetValue(hash, out LegStyle style) ? style : null;

        private static void Add(LegStyle style) => byLegs[style.LegsHash] = style;

        /// <summary>
        /// Iron mail: the chest's mail rings at its own 110 texels a metre, a leather belt with an iron disc buckle,
        /// straps above and below each knee (tilted like the chest's arm wraps), an ankle cuff without boots.
        /// </summary>
        private static LegStyle Iron()
        {
            var style = new LegStyle("Iron", "ArmorIronLegs", "ArmorIronChest")
            {
                Density = 110f,
                FieldOrigin = new Vector2(6f, 4f),
                Leather = new RectInt(0, 192, 76, 58),
            };
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(6, 36, 99, 27), new RectInt(0, 0, 198, 189), AtlasFill.Tile));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(0, 70, 105, 58), new RectInt(0, 192, 105, 58), AtlasFill.Copy));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(80, 73, 20, 20), new RectInt(118, 196, 20, 20), AtlasFill.Copy));
            style.Bands.Add(new BandSpec(Vector3.up, Belt.Bottom, Belt.Top, 0.006f, true, new RectInt(0, 200, 76, 28)));
            style.Bands.Add(new BandSpec(new Vector3(0f, 1f, 0.25f), 0.63f, 0.66f, 0.005f, false, new RectInt(0, 232, 76, 7)));
            style.Bands.Add(new BandSpec(new Vector3(0f, 1f, -0.2f), 0.43f, 0.46f, 0.005f, false, new RectInt(0, 241, 76, 7)));
            style.Bands.Add(new BandSpec(Vector3.up, 0.03f, 0.065f, 0.005f, false, new RectInt(0, 241, 76, 7), ankle: true));
            style.Buckle = new DiscSpec(0.028f, new Vector2(128f, 206f), 8f);
            return style;
        }

        /// <summary>
        /// Bronze plates: the chest's plates on its dark leather from the hips to the ankles (at the body paint's 63
        /// texels a metre, so the plates are the chest's size), a bronze trim round each thigh above the knee, a bronze
        /// disc as the belt's buckle, a leather ankle cuff without boots.
        /// </summary>
        private static LegStyle Bronze()
        {
            var style = new LegStyle("Bronze", "ArmorBronzeLegs", "ArmorBronzeChest")
            {
                Density = 63f,
                FieldOrigin = new Vector2(4f, 4f),
                Leather = new RectInt(0, 160, 64, 50),
            };
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(8, 106, 44, 15), new RectInt(0, 0, 100, 80), AtlasFill.Mirror));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestPaint, 256, new RectInt(32, 18, 13, 12), new RectInt(0, 8, 100, 72), AtlasFill.Tile));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(6, 70, 64, 50), new RectInt(0, 160, 64, 50), AtlasFill.Copy));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(58, 33, 8, 30), new RectInt(80, 160, 8, 30), AtlasFill.Copy));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(97, 5, 16, 17), new RectInt(100, 160, 16, 17), AtlasFill.Copy));
            style.Bands.Add(new BandSpec(Vector3.up, Belt.Bottom, Belt.Top, 0.006f, true, new RectInt(0, 168, 64, 28)));
            style.Bands.Add(new BandSpec(Vector3.up, 0.56f, 0.595f, 0.005f, false, new RectInt(80, 160, 8, 30), rotated: true));
            style.Bands.Add(new BandSpec(Vector3.up, 0.03f, 0.065f, 0.005f, false, new RectInt(0, 200, 64, 7), ankle: true));
            style.Buckle = new DiscSpec(0.028f, new Vector2(108f, 168.5f), 6.5f);
            return style;
        }

        /// <summary>
        /// Wolf silver: the chest's silver mail (white rings, metal, a 15 x 21 block that repeats every 15 x 3 texels)
        /// from the hips to the ankles at 100 texels a metre (rings about 3 cm, as on the iron mail), standing 1.2 cm off
        /// the skin rather than 1 (the user: "the silver material and a little thicker"; then 2 cm and 1.4 cm were "a bit
        /// thinner, so the boots can just fit over top"), and full length under the boots ("don't remove any of the pants
        /// under the legs"). The chest's pale fur (its grey
        /// pelt's light middle, 88-115, 16-35, what shows white on its shoulders) behind the belt, as a ruff below it
        /// and as a cuff round each knee (the user: "white fir on the straps of the pants? and under the belt"); a belt,
        /// a strap over each knee cuff and an ankle cuff without boots from the leather strap the boots' wraps wear; the
        /// chest's silver disc as the buckle.
        /// </summary>
        private static LegStyle Wolf()
        {
            var style = new LegStyle("Wolf", "ArmorWolfLegs", "ArmorWolfChest")
            {
                Density = 100f,
                Thickness = 0.012f,
                UnderBoots = true,
                FieldOrigin = new Vector2(4f, 4f),
                Leather = new RectInt(0, 160, 64, 50),
            };
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(109, 211, 15, 21), new RectInt(0, 0, 256, 120), AtlasFill.Tile));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(88, 16, 28, 20), new RectInt(0, 160, 64, 50), AtlasFill.Mirror));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(88, 16, 28, 20), new RectInt(136, 160, 56, 20), AtlasFill.Mirror));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(216, 66, 8, 72), new RectInt(80, 160, 8, 72), AtlasFill.Copy));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(140, 18, 20, 20), new RectInt(112, 160, 20, 20), AtlasFill.Copy));
            style.Bands.Add(new BandSpec(Vector3.up, Belt.Bottom, Belt.Top, 0.006f, true, new RectInt(80, 160, 8, 72), rotated: true));
            style.Bands.Add(new BandSpec(Vector3.up, Belt.Bottom - 0.05f, Belt.Bottom + 0.005f, 0.004f, true, new RectInt(136, 160, 56, 20)));
            style.Bands.Add(new BandSpec(new Vector3(0f, 1f, 0.25f), 0.585f, 0.65f, 0.006f, false, new RectInt(136, 160, 56, 20)));
            style.Bands.Add(new BandSpec(new Vector3(0f, 1f, 0.25f), 0.605f, 0.63f, 0.01f, false, new RectInt(80, 160, 8, 72), rotated: true));
            style.Bands.Add(new BandSpec(Vector3.up, 0.03f, 0.065f, 0.005f, false, new RectInt(80, 160, 8, 72), rotated: true, ankle: true));
            style.Buckle = new DiscSpec(0.028f, new Vector2(122f, 170f), 7f);
            return style;
        }

        /// <summary>
        /// Carapace scales: the chest's round scales (a 48 x 16 block, eight by two of its repeats) from the hips to the
        /// ankles at 110 texels a metre (the iron mail's, both chests on 128 px atlases), 1.2 cm off the skin and full
        /// length under the boots, as the wolf silver. A blue chitin cuff round each knee from the plates the boots wear,
        /// the chest's mottled leather under and as the belt and as the ankle cuff without boots, its steel as the buckle.
        /// </summary>
        private static LegStyle Carapace()
        {
            var style = new LegStyle("Carapace", "ArmorCarapaceLegs", "ArmorCarapaceChest")
            {
                Density = 110f,
                Thickness = 0.012f,
                UnderBoots = true,
                FieldOrigin = new Vector2(4f, 4f),
                Leather = new RectInt(0, 160, 64, 50),
            };
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(38, 26, 48, 16), new RectInt(0, 0, 240, 128), AtlasFill.Tile));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(2, 2, 18, 40), new RectInt(0, 160, 64, 50), AtlasFill.Mirror));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(88, 96, 38, 30), new RectInt(100, 160, 38, 30), AtlasFill.Copy));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(34, 84, 12, 40), new RectInt(144, 160, 12, 40), AtlasFill.Copy));
            style.Bands.Add(new BandSpec(Vector3.up, Belt.Bottom, Belt.Top, 0.006f, true, new RectInt(0, 168, 64, 28)));
            style.Bands.Add(new BandSpec(new Vector3(0f, 1f, 0.25f), 0.57f, 0.64f, 0.006f, false, new RectInt(100, 160, 38, 30)));
            style.Bands.Add(new BandSpec(Vector3.up, 0.03f, 0.065f, 0.005f, false, new RectInt(0, 200, 64, 7), ankle: true));
            style.Buckle = new DiscSpec(0.028f, new Vector2(150f, 180f), 5f);
            return style;
        }

        /// <summary>
        /// Flametal mail: the chest's iridescent mail (a 24 x 35 block, six by five of its repeats) from the hips to the
        /// ankles at 110 texels a metre (both on 128 px atlases, as the iron mail), 1.2 cm off the skin and full length under
        /// the boots, as the wolf silver and carapace scales. A cuff of the chest's heat-tinted flametal plate round each
        /// knee, its mauve leather under and as the belt and as the ankle cuff without boots, its steel as the buckle.
        /// </summary>
        private static LegStyle Flametal()
        {
            var style = new LegStyle("Flametal", "ArmorFlametalLegs", "ArmorFlametalChest")
            {
                Density = 110f,
                Thickness = 0.012f,
                UnderBoots = true,
                FieldOrigin = new Vector2(4f, 4f),
                Leather = new RectInt(0, 160, 64, 50),
            };
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(104, 0, 24, 35), new RectInt(0, 0, 240, 128), AtlasFill.Tile));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(72, 72, 40, 30), new RectInt(0, 160, 64, 50), AtlasFill.Mirror));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(2, 74, 26, 30), new RectInt(100, 160, 26, 30), AtlasFill.Copy));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 128, new RectInt(40, 62, 14, 30), new RectInt(144, 160, 14, 30), AtlasFill.Copy));
            style.Bands.Add(new BandSpec(Vector3.up, Belt.Bottom, Belt.Top, 0.006f, true, new RectInt(0, 168, 64, 28)));
            style.Bands.Add(new BandSpec(new Vector3(0f, 1f, 0.25f), 0.57f, 0.64f, 0.006f, false, new RectInt(100, 160, 26, 30)));
            style.Bands.Add(new BandSpec(Vector3.up, 0.03f, 0.065f, 0.005f, false, new RectInt(0, 200, 64, 7), ankle: true));
            style.Buckle = new DiscSpec(0.028f, new Vector2(151f, 175f), 5f);
            return style;
        }

        /// <summary>
        /// Protector: the game's balloon trousers' own charcoal leather snug from the hips to the ankles at their 82 texels
        /// a metre, 1.2 cm off the skin and full length under the boots. In the breastplate's look: a band of its steel with
        /// the orange flame motifs round each leg above the knee, edged above and below with its ember orange as its plates
        /// are; a brown leather belt with a steel buckle; its pale pelt as the ankle cuff without boots. The breastplate's
        /// plates and tabard, shaped for the balloon trousers, are drawn in over these (<see cref="ChestFit"/>).
        /// </summary>
        private static LegStyle Protector()
        {
            var style = new LegStyle("Protector", "ArmorDeepNorthHeavylegs", "ArmorDeepNorthHeavyChest")
            {
                Density = 82f,
                Thickness = 0.012f,
                UnderBoots = true,
                FieldOrigin = new Vector2(4f, 4f),
                Leather = new RectInt(0, 160, 64, 50),
            };
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(46, 136, 29, 79), new RectInt(0, 0, 128, 100), AtlasFill.Mirror));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(0, 206, 36, 50), new RectInt(0, 160, 64, 50), AtlasFill.Mirror));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(130, 230, 126, 24), new RectInt(0, 104, 126, 24), AtlasFill.Copy));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(202, 99, 24, 56), new RectInt(130, 104, 24, 56), AtlasFill.Copy));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(180, 58, 72, 28), new RectInt(160, 104, 72, 28), AtlasFill.Copy));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(162, 176, 30, 20), new RectInt(160, 140, 30, 20), AtlasFill.Copy));
            style.Bands.Add(new BandSpec(Vector3.up, Belt.Bottom, Belt.Top, 0.006f, true, new RectInt(0, 168, 64, 28)));
            style.Bands.Add(new BandSpec(Vector3.up, 0.565f, 0.635f, 0.006f, false, new RectInt(0, 104, 126, 24)));
            style.Bands.Add(new BandSpec(Vector3.up, 0.553f, 0.567f, 0.008f, false, new RectInt(130, 104, 24, 56), rotated: true));
            style.Bands.Add(new BandSpec(Vector3.up, 0.633f, 0.647f, 0.008f, false, new RectInt(130, 104, 24, 56), rotated: true));
            style.Bands.Add(new BandSpec(Vector3.up, 0.03f, 0.09f, 0.006f, false, new RectInt(160, 104, 72, 28), ankle: true));
            style.Buckle = new DiscSpec(0.028f, new Vector2(175f, 150f), 6f);
            return style;
        }

        /// <summary>
        /// Vanguard: the game's balloon trousers' own teal panel with its gold vine, snug and tapering to the ankles at their
        /// 91 texels a metre, 1.2 cm off the skin and full length under the boots. A strip of it half a leg's round wide
        /// (25 texels, the male body's mean leg round being 49.7 at this density), centred on the vine's stem and mirrored
        /// both ways, so the stem runs down the front and back of each leg and the vine's arms meet their mirror images in
        /// diamonds down to the ankles (the user: "taper down to the ankles, extend that gold looking pattern down"). The
        /// vine starts 12 cm below the waist, on the thighs below the belt and the chest's skirt (from the waist it fell
        /// under them: "where is the gold that was on the top of the pants?"), plain teal from the same panel above it. The
        /// chest's gold braid on brown leather as the belt and as the ankle cuff without boots, its gold brooch the buckle.
        /// </summary>
        private static LegStyle Vanguard()
        {
            var style = new LegStyle("Vanguard", "ArmorDeepNorthMediumlegs", "ArmorDeepNorthMediumChest")
            {
                Density = 91f,
                Thickness = 0.012f,
                UnderBoots = true,
                FieldOrigin = new Vector2(0f, 6f),
                Leather = new RectInt(0, 160, 64, 50),
            };
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(136, 27, 25, 10), new RectInt(0, 0, 200, 16), AtlasFill.Mirror));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(136, 2, 25, 37), new RectInt(0, 16, 200, 112), AtlasFill.Mirror));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(140, 200, 56, 28), new RectInt(0, 160, 64, 50), AtlasFill.Mirror));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(140, 233, 56, 10), new RectInt(100, 160, 56, 10), AtlasFill.Copy));
            style.Recipe.Add(new AtlasCopy(AtlasSource.ChestMesh, 256, new RectInt(198, 160, 22, 28), new RectInt(170, 160, 22, 28), AtlasFill.Copy));
            style.Bands.Add(new BandSpec(Vector3.up, Belt.Bottom, Belt.Top, 0.006f, true, new RectInt(100, 160, 56, 10)));
            style.Bands.Add(new BandSpec(Vector3.up, 0.03f, 0.06f, 0.005f, false, new RectInt(100, 160, 56, 10), ankle: true));
            style.Buckle = new DiscSpec(0.028f, new Vector2(180f, 173.5f), 7f);
            return style;
        }
    }

    /// <summary>Where the belt lies, metres: every look's belt, and the leather under it.</summary>
    internal static class Belt
    {
        public const float Bottom = LegRegion.Waist - 0.085f;
        public const float Top = LegRegion.Waist - 0.02f;
    }
}
