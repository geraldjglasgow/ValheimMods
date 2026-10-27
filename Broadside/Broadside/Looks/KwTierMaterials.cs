using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>What a rudder's or a ship cannon's part is made of: each a look of the game's own (<see cref="KwTierMaterials"/>).</summary>
    public enum KwLook
    {
        Planks, Leather, FineWood, Resin, Bronze, DarkWood, Iron, Bark, Silver, CarvedWood, BlackMetal, Linen, Yggdrasil, Eitr, Ashwood, Flametal, Ember, Coal,
    }

    /// <summary>
    /// The biome materials the rudders wear: the game's own, each found by name on a game prefab that wears it, shared and never changed. The plank wood is the
    /// kit's (the wood floor's); leather the leather scraps' (Custom/Creature); fine wood the table's (piece_table, made of
    /// fine wood); dark wood the darkwood beam's; iron the iron wall's; the ancient bark the bark item's own (ElderBark,
    /// "wood_pile"); carved fine wood the raven throne's; bronze, silver and black metal the bar stacks' (Standard,
    /// metallic); linen the linen thread's; Yggdrasil wood the item's own; ashwood the ashwood wall's; flametal the flametal
    /// pillar's; eitr the refined eitr's glowing core (Standard with emission, opaque); coal the coal item's (a cannon's bore
    /// and black powder). Two are copies: the resin seam (the
    /// table's fine wood darkened to resin's amber) and flametal's ember glow (the eitr's material, dim and warm). A look
    /// the game lacks is the plank wood,
    /// logged once. Found once the game's prefabs are loaded, then kept.
    /// </summary>
    public static class KwTierMaterials
    {
        private static readonly Dictionary<KwLook, (string Prefab, string Material)> Sources = new Dictionary<KwLook, (string Prefab, string Material)>
        {
            [KwLook.Leather] = ("LeatherScraps", "leatherscraps"),
            [KwLook.FineWood] = ("piece_table", "Table_Mat"),
            [KwLook.Bronze] = ("bar_bronze_stack", "bronze"),
            [KwLook.DarkWood] = ("darkwood_beam", "DarkWood_mat"),
            [KwLook.Iron] = ("iron_wall_2x2", "metalwall"),
            [KwLook.Bark] = ("ElderBark", "wood_pile"),
            [KwLook.Silver] = ("bar_silver_stack", "silverbar"),
            [KwLook.CarvedWood] = ("piece_throne01", "RavenThrone_Mat"),
            [KwLook.BlackMetal] = ("bar_blackmetal_stack", "blackmetal"),
            [KwLook.Linen] = ("LinenThread", "thread"),
            [KwLook.Yggdrasil] = ("YggdrasilWood", "Shoot_Stack_mat"),
            [KwLook.Eitr] = ("Eitr", "eitr"),
            [KwLook.Ashwood] = ("ashwood_wall_2x2", "AshWood_mat"),
            [KwLook.Flametal] = ("Piece_flametal_pillar", "flametalbeam_mat"),
            [KwLook.Coal] = ("Coal", "coal"),
        };

        private const string Emission = "_EmissionColor";
        private static readonly Color ResinColour = new Color(0.42f, 0.24f, 0.08f);
        private static readonly Color EmberColour = new Color(0.32f, 0.12f, 0.05f);
        private static readonly Color EmberGlow = new Color(1f, 0.36f, 0.08f) * 0.7f;
        private static readonly Dictionary<KwLook, Material?> found = new Dictionary<KwLook, Material?>();

        /// <summary>The look's material; null only when the kit has no plank wood either.</summary>
        public static Material? Of(KwLook look)
        {
            if (found.TryGetValue(look, out Material? material))
                return material;
            material = Make(look);
            if (ZNetScene.instance != null)
                found[look] = material;
            return material;
        }

        private static Material? Make(KwLook look) => look switch
        {
            KwLook.Planks => KwPrefabs.Planks,
            KwLook.Resin => Tinted(Of(KwLook.FineWood), "kw_resin", ResinColour),
            KwLook.Ember => Ember(),
            _ => Borrowed(look) ?? KwPrefabs.Planks,
        };

        private static Material? Borrowed(KwLook look)
        {
            (string prefab, string name) = Sources[look];
            GameObject? source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            Material? material = source != null ? Named(source, name) : null;
            if (material == null && ZNetScene.instance != null)
                Log.Warn($"Broadside materials: the game's {prefab} has no {name}; its parts wear the plank wood.");
            return material;
        }

        // The material of that name on any of the prefab's renderers (an item's sparkle and a piece's snow skipped).
        private static Material? Named(GameObject source, string name)
        {
            foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer)
                    continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.name == name)
                        return material;
                }
            }
            return null;
        }

        private static Material? Tinted(Material? source, string name, Color colour) =>
            source == null ? null : new Material(source) { name = name, color = colour };

        // The eitr's glowing material in a dull warm colour, its emission keyword kept on, so it draws as the original does;
        // the eitr's look as it is when it has no glow to tint.
        private static Material? Ember()
        {
            Material? eitr = Of(KwLook.Eitr);
            if (eitr == null || eitr == KwPrefabs.Planks || !eitr.HasProperty(Emission))
                return eitr;
            Material ember = new Material(eitr) { name = "kw_ember", color = EmberColour };
            ember.EnableKeyword("_EMISSION");
            ember.SetColor(Emission, EmberGlow);
            return ember;
        }
    }
}
