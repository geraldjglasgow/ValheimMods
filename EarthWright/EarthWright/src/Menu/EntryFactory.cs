using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// Makes the entry prefabs: a copy of a game hoe or cultivator piece under an inactive holder that survives scene
    /// loads, renamed to the entry's id, with EarthWright's words and icon, no resources and no crafting station.
    /// Because the holder is inactive, the copy's <c>TerrainOp.Awake</c> (which would edit the terrain at once) and
    /// <c>Piece.Awake</c> never run on the template; the game's placement and ghost instantiate it as a root object.
    /// The templates are local prefabs with no ZNetView, made the same way on every machine; EarthWright's own entries
    /// are made once per game run and remade only if something destroyed them.
    /// </summary>
    public static class EntryFactory
    {
        /// <summary>Used when an entry's own template piece is not in the game's tables (another mod removed it).</summary>
        private const string FallbackBase = "path_v2";

        private static GameObject holder;
        private static readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();

        /// <summary>The prefab of one of EarthWright's own entries, or null.</summary>
        public static GameObject Get(string id)
        {
            return id != null && templates.TryGetValue(id, out GameObject prefab) && prefab != null ? prefab : null;
        }

        /// <summary>Makes every entry that does not exist yet. Needs <see cref="GamePieces"/> captured.</summary>
        public static void EnsureAll()
        {
            foreach (EntryDef def in EntryDefs.All)
            {
                if (Get(def.Id) != null)
                    continue;
                GameObject prefab = Clone(def.BasePrefab, def.Id);
                if (prefab == null)
                    continue;
                Setup(prefab.GetComponent<Piece>(), "$" + def.NameKey, "$" + def.DescriptionKey, EntryIcons.Own(def.Icon), def.Action.IsSpecial);
                templates[def.Id] = prefab;
            }
        }

        /// <summary>A copy of a game piece named <paramref name="name"/> under the inactive holder, or null (logged).</summary>
        public static GameObject Clone(string basePrefab, string name)
        {
            GameObject source = GamePieces.Get(basePrefab) ?? GamePieces.Get(FallbackBase);
            if (source == null)
            {
                Plugin.Log.LogWarning($"EarthWright: the game piece {basePrefab} was not found, the menu entry {name} is not made.");
                return null;
            }
            GameObject copy = Object.Instantiate(source, Holder.transform, false);
            copy.name = name;
            return copy;
        }

        /// <summary>
        /// The piece's words, icon, and no cost of its own (the Costs module charges terrain work). A special entry works
        /// on objects or runs a command, so it drops the cultivator pieces' "vegetation ground only" rule, which would
        /// show its ghost as invalid on bare or paved ground.
        /// </summary>
        public static void Setup(Piece piece, string name, string description, Sprite icon, bool special)
        {
            piece.m_name = name;
            piece.m_description = description;
            if (icon != null)
                piece.m_icon = icon;
            piece.m_resources = new Piece.Requirement[0];
            piece.m_craftingStation = null;
            piece.m_enabled = true;
            if (special)
                piece.m_vegetationGroundOnly = false;
        }

        private static GameObject Holder
        {
            get
            {
                if (holder != null)
                    return holder;
                holder = new GameObject("EarthWright_MenuEntries");
                holder.SetActive(false);
                Object.DontDestroyOnLoad(holder);
                return holder;
            }
        }
    }
}
