using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>How a door's leaf is dressed by its part's tier (<see cref="KwDoorMesh"/>, <see cref="KwDoorIron"/>).</summary>
    public enum KwDoorStyle
    {
        /// <summary>Meadows: plain boards, two short strap hinges.</summary>
        Plain,

        /// <summary>Black Forest and Swamp: darker boards, three long iron straps nailed across.</summary>
        Strapped,

        /// <summary>Mountains and up: iron bands right across, studded, and studs over the boards between them.</summary>
        Banded,
    }

    /// <summary>
    /// What a door wears, so it reads as a door and not as a piece of the ship's wall (the user's words, 2026-10-25: "The
    /// door should not just look like the texture of the ship."): the game's own wooden door's wood (<c>door_wood</c> on
    /// <c>wood_door</c>, copied once the game's prefabs are loaded, never shipped, its tiling put back to the mesh's own
    /// UVs), darkened for the later tiers as their decks are, and the game's iron for its straps
    /// (<see cref="KwOpeningLooks.Iron"/>). A game without that door's wood dresses doors in the plank wood. Never
    /// made on a dedicated server, which draws no leaves.
    /// </summary>
    public static class KwDoorLooks
    {
        private const string Prefab = "wood_door", Source = "door_wood";

        // Each tier's wood over the game's door wood: Meadows as the game has it, Black Forest's fine wood warmer and darker,
        // the Swamp's ancient bark greyer and darker still, the Mountains weathered grey, the Plains tarred brown,
        // Yggdrasil's red-brown, Ashlands' blackwood nearly black.
        private static readonly Dictionary<string, Color> Tints = new Dictionary<string, Color>
        {
            ["meadows"] = Color.white,
            ["blackforest"] = new Color(0.78f, 0.64f, 0.5f),
            ["swamp"] = new Color(0.55f, 0.52f, 0.47f),
            ["mountains"] = new Color(0.68f, 0.66f, 0.64f),
            ["plains"] = new Color(0.62f, 0.49f, 0.37f),
            ["mistlands"] = new Color(0.62f, 0.47f, 0.44f),
            ["ashlands"] = new Color(0.34f, 0.3f, 0.29f),
        };

        private static readonly Dictionary<string, Material> made = new Dictionary<string, Material>();
        private static Material? source;

        /// <summary>How a tier's doors are dressed.</summary>
        public static KwDoorStyle Style(KwBiomeTier? tier) => tier?.Id switch
        {
            null or "meadows" => KwDoorStyle.Plain,
            "blackforest" or "swamp" => KwDoorStyle.Strapped,
            _ => KwDoorStyle.Banded,
        };

        /// <summary>The wood a tier's doors wear: the game's door wood in the tier's tint; the plank wood without it.</summary>
        public static Material? Wood(KwBiomeTier tier)
        {
            if (made.TryGetValue(tier.Id, out Material wood) && wood != null)
                return wood;
            Material? door = Door();
            if (door == null)
                return KwOpeningLooks.Wood(tier);
            wood = new Material(door) { name = "kw_door_" + tier.Id, mainTextureScale = Vector2.one, mainTextureOffset = Vector2.zero };
            wood.color = Tints.TryGetValue(tier.Id, out Color tint) ? tint : Color.white;
            made[tier.Id] = wood;
            return wood;
        }

        // The game's door wood, found once its prefabs are loaded (logged once when it is not there).
        private static Material? Door()
        {
            if (source != null || ZNetScene.instance == null)
                return source;
            GameObject? prefab = ZNetScene.instance.GetPrefab(Prefab);
            foreach (Renderer renderer in prefab != null ? prefab.GetComponentsInChildren<Renderer>(true) : new Renderer[0])
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.name == Source)
                        return source = material;
                }
            }
            if (!warned)
                Log.Warn($"Broadside doors: the game's {Prefab} has no {Source}; doors wear the plank wood.");
            warned = true;
            return null;
        }

        private static bool warned;
    }
}
