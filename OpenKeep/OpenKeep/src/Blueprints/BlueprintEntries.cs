using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The Blueprints tab's entries: empty pieces of OpenKeep's own, made once per tool or blueprint and kept under an
    /// inactive holder (so an entry's own Awake never runs), each with its own icon. They are never placed
    /// (<see cref="BlueprintTool"/> takes their click), never rotate (so the wheel zooms the camera), cost nothing and
    /// cannot be removed. Prefab names hold no spaces or brackets, since the game cuts a prefab name at the first of
    /// those; a blueprint's name word is its file name made readable. Folders have no entries: they live on the folder
    /// panel and the breadcrumb (<see cref="Tab.FolderPanel"/>, <see cref="Tab.Breadcrumb"/>).
    /// </summary>
    public static class BlueprintEntries
    {
        public const string BlueprintPrefix = "OpenKeep_Blueprint_";
        public const string FixName = "OpenKeep_FixGround";
        public const string PlannerName = "OpenKeep_SitePlanner";
        public const string CopyName = "OpenKeep_Copy";
        public const string GhostsName = "OpenKeep_GhostSwitch";

        private static readonly Dictionary<string, GameObject> blueprints = new Dictionary<string, GameObject>();
        private static readonly Dictionary<GameObject, string> paths = new Dictionary<GameObject, string>();
        private static readonly Dictionary<string, GameObject> tools = new Dictionary<string, GameObject>();
        private static GameObject holder;

        /// <summary>The blueprint path of an entry, or null for a tool or any other piece.</summary>
        public static string PathOf(Piece piece) => piece != null && paths.TryGetValue(piece.gameObject, out string path) ? path : null;

        /// <summary>A blueprint's entry, its description read again from the file (pieces, size, keys).</summary>
        public static GameObject Blueprint(string path)
        {
            GameObject entry = Known(path);
            entry.GetComponent<Piece>().m_description = BlueprintDescription(BlueprintLibrary.Load(path));
            return entry;
        }

        public static GameObject Fix() => Tool(FixName, BlueprintWords.FixName, BlueprintWords.FixDescription, BlueprintIcons.FixGround);

        public static GameObject Planner() => Tool(PlannerName, BlueprintWords.PlannerName, BlueprintWords.PlannerDescription, BlueprintIcons.Planner);

        public static GameObject Copy() => Tool(CopyName, BlueprintWords.CopyName, BlueprintWords.CopyDescription, BlueprintIcons.Copy);

        /// <summary>The Construction ghosts switch, its icon and description showing the switch as it stands now.</summary>
        public static GameObject Ghosts()
        {
            GameObject entry = Tool(GhostsName, BlueprintWords.GhostsName, BlueprintWords.GhostsShownDescription, BlueprintIcons.GhostsShown);
            Piece piece = entry.GetComponent<Piece>();
            bool shown = GhostSwitch.Shown;
            piece.m_icon = BlueprintIcons.Get(shown ? BlueprintIcons.GhostsShown : BlueprintIcons.GhostsHidden);
            piece.m_description = Language.Localize(shown ? BlueprintWords.GhostsShownDescription : BlueprintWords.GhostsHiddenDescription);
            return entry;
        }

        /// <summary>A folder as players read it: its path, or "the top folder".</summary>
        public static string Shown(string folder) => string.IsNullOrEmpty(folder) ? Language.Localize(BlueprintWords.TopFolder) : folder;

        private static GameObject Tool(string prefabName, string name, string description, string icon)
        {
            if (!tools.TryGetValue(prefabName, out GameObject entry) || entry == null)
                tools[prefabName] = entry = Make(prefabName, name, Language.Localize(description), BlueprintIcons.Get(icon));
            return entry;
        }

        /// <summary>The entry of a blueprint path, made the first time.</summary>
        private static GameObject Known(string path)
        {
            if (blueprints.TryGetValue(path, out GameObject entry) && entry != null)
                return entry;
            string word = Language.Add(WordKey(path), Title(BlueprintLibrary.Leaf(path)));
            entry = Make(BlueprintPrefix + Plain(path) + "_" + Hash(path), word, "", BlueprintIcons.Get(BlueprintIcons.Blueprint));
            blueprints[path] = entry;
            paths[entry] = path;
            return entry;
        }

        /// <summary>An empty piece that never rotates (the wheel zooms), costs nothing and cannot be removed.</summary>
        private static GameObject Make(string prefabName, string name, string description, Sprite sprite)
        {
            GameObject go = new GameObject(prefabName);
            go.transform.SetParent(Holder(), false);
            Piece piece = go.AddComponent<Piece>();
            piece.m_name = name;
            piece.m_description = description;
            piece.m_icon = sprite;
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_resources = new Piece.Requirement[0];
            piece.m_canBeRemoved = false;
            piece.m_canRotate = false;
            return go;
        }

        /// <summary>"ok_bp_entry_" and the path in lower-case letters, digits and underscores, with a hash against clashes.</summary>
        private static string WordKey(string path) => "ok_bp_entry_" + Plain(path).ToLowerInvariant() + "_" + Hash(path);

        private static string Plain(string path) => Regex.Replace(path, "[^A-Za-z0-9_]", "_");

        private static string Hash(string path) => (path.GetStableHashCode() & 0xffff).ToString("x4");

        /// <summary>"plain_wood_house" as "Plain wood house".</summary>
        public static string Title(string name)
        {
            string spaced = name.Replace('_', ' ').Replace('-', ' ').Trim();
            return spaced.Length == 0 ? name : char.ToUpperInvariant(spaced[0]) + spaced.Substring(1);
        }

        private static string BlueprintDescription(Blueprint bp)
        {
            if (bp == null)
                return BlueprintWords.Format(BlueprintWords.Unreadable, BlueprintLibrary.Error ?? "");
            string size = BlueprintWords.Format(BlueprintWords.EntrySize, bp.Pieces.Count,
                BlueprintWords.Metres(bp.PieceBounds.width), BlueprintWords.Metres(bp.PieceBounds.height));
            string water = bp.HasWater ? "\n" + Language.Localize(BlueprintWords.EntryWater) : "";
            return bp.Description + "\n\n" + size + water + "\n\n" + Language.Localize(BlueprintWords.EntryKeys);
        }

        /// <summary>The inactive parent of every entry, kept across scenes.</summary>
        private static Transform Holder()
        {
            if (holder == null)
            {
                holder = new GameObject("OpenKeep Blueprint Entries");
                holder.SetActive(false);
                Object.DontDestroyOnLoad(holder);
            }
            return holder.transform;
        }
    }
}
