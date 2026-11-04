using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A wood tier of the dockside decorations made of a dock's wood (CLAUDE.md, "Dockside decorations"): core wood or fine
    /// wood, going with the docks of that material (<see cref="KwPierKind"/>, <see cref="Pier"/>). Its pieces are copies of
    /// the plain one (<see cref="KwDockDef.In"/>) wearing the workshop's tier albedo (<see cref="Albedo"/>: the same mesh,
    /// UVs, normal map and metal mask, only the wood repainted), named "(core wood)" or "(fine wood)", costing the tier's
    /// wood (<see cref="Item"/>) in place of the plain Wood. Plain data.
    /// </summary>
    public sealed class KwDockTier
    {
        public KwDockTier(string id, string pier, string words, string item)
        {
            (Id, Pier, Words, Item) = (id, pier, words, item);
        }

        /// <summary>"core" or "fine": the end of its prefabs' names and of its albedos'.</summary>
        public string Id { get; }

        /// <summary>The dock material it goes with: "corewood" or "finewood" (<see cref="KwPierDeck.Material"/>).</summary>
        public string Pier { get; }

        /// <summary>"core wood", "fine wood".</summary>
        public string Words { get; }

        /// <summary>The wood it costs in place of the plain Wood, by the game's prefab name.</summary>
        public string Item { get; }

        /// <summary>A plain decoration's prefab in this tier: <c>BS_dock_&lt;id&gt;_core</c>.</summary>
        public string Prefab(string plain) => plain + "_" + Id;

        /// <summary>The embedded albedo of a decoration's bundle in this tier: <c>&lt;bundle&gt;_albedo_core.png</c>.</summary>
        public string Albedo(string bundle) => $"{bundle}_albedo_{Id}.png";
    }

    /// <summary>
    /// The decorations that come in the docks' wood tiers (2026-11-03, the user's words: "We also need fine wood, core wood
    /// and regular wood mooring posts, those types of versions for things that do have wood, so the decorations that go on
    /// the docks match the dock"): the mooring post, the stone anchor, the net drying rack, the lobster pots, the harbour
    /// beacon, the fenders and the signal bell, each followed in the hammer by its core wood and fine wood copies
    /// (<see cref="Expand"/>). Their albedos are embedded from the workshop (<c>Broadside/assets/bs_dock_&lt;id&gt;_albedo_core.png</c>
    /// and <c>_fine.png</c>, 256 px), loaded once and point filtered as the bundles' own, only where there is a screen; their
    /// hammer icons are rendered from the piece (<see cref="KwIcons"/>), the plain one keeping the workshop's painted icon
    /// (<see cref="Icons"/>, which renders the dock lantern's too).
    /// </summary>
    public static class KwDockTiers
    {
        public static readonly KwDockTier Core = new KwDockTier("core", "corewood", "core wood", "RoundLog");
        public static readonly KwDockTier Fine = new KwDockTier("fine", "finewood", "fine wood", "FineWood");
        public static readonly KwDockTier[] All = { Core, Fine };

        private static readonly string[] Tiered = { "mooringpost", "anchor", "netrack", "creels", "beacon", "fenders", "bell" };
        private static readonly Dictionary<string, Texture2D?> albedos = new Dictionary<string, Texture2D?>();

        /// <summary>The definitions in the hammer's order, each tiered one followed by its core wood and fine wood copies.</summary>
        public static KwDockDef[] Expand(IEnumerable<KwDockDef> defs)
        {
            List<KwDockDef> all = new List<KwDockDef>();
            foreach (KwDockDef def in defs)
            {
                all.Add(def);
                if (Has(def.Id))
                    all.AddRange(Array.ConvertAll(All, def.In));
            }
            return all.ToArray();
        }

        /// <summary>Whether a decoration (by its id, the prefab without <c>BS_dock_</c>) comes in the wood tiers.</summary>
        public static bool Has(string id) => Array.IndexOf(Tiered, id) >= 0;

        /// <summary>The tier going with a dock material ("corewood", "finewood"); null for plain wood or anything else.</summary>
        public static KwDockTier? OfPier(string? material) => Array.Find(All, t => t.Pier == material);

        /// <summary>
        /// A tier's albedo for a decoration's bundle, loaded once (point filtered, as the bundles' textures); null for a plain
        /// decoration, or one not embedded (logged once: that piece wears the plain look).
        /// </summary>
        public static Texture2D? Albedo(KwDockDef def)
        {
            if (def.Tier == null)
                return null;
            string file = def.Tier.Albedo(def.Bundle);
            if (albedos.TryGetValue(file, out Texture2D? known))
                return known;
            Texture2D? texture = KwTextures.Load(file, false, TextureWrapMode.Repeat, TextureWrapMode.Repeat);
            if (texture != null)
                texture.filterMode = FilterMode.Point;
            else
                Log.Warn($"Broadside: {file} is not embedded; {def.Prefab} wears the plain wood.");
            return albedos[file] = texture;
        }

        /// <summary>
        /// The tier pieces' hammer icons rendered from each piece as it is dressed, and the dock lantern's (its head on a short
        /// post; the bundle's icon is the head alone): a screen only, kept between launches.
        /// </summary>
        public static void Icons(IEnumerable<GameObject> pieces)
        {
            if (!KwIcons.CanRender)
                return;
            KwIconRig? rig = null;
            try
            {
                foreach (GameObject piece in pieces)
                    Icon(ref rig, piece);
            }
            finally
            {
                rig?.Dispose();
            }
        }

        private static void Icon(ref KwIconRig? rig, GameObject piece)
        {
            KwDockDef? def = Array.Find(KwDockside.Defs, d => d.Prefab == piece.name);
            bool rendered = def != null && (def.Tier != null || def.Prefab == KwDockLanternPiece.Prefab);
            Sprite? icon = rendered ? KwIcons.Icon(ref rig, piece.transform, piece.name, null) : null;
            if (icon != null)
                piece.GetComponent<Piece>().m_icon = icon;
        }
    }
}
