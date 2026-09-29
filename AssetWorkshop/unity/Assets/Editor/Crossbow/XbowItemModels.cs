using UnityEditor;
using UnityEngine;

namespace Workshop.Crossbow
{
    /// <summary>
    /// The players' Bone Crossbow (EliteCreaturesPack Crossbow/XbowItem) as the game holds its crossbows: two models the
    /// mod puts where the game's Arbalest has its "Unloaded" and "Loaded" children, which the game's WeaponLoadState
    /// swaps as the player reloads and shoots.
    ///
    ///   ecp_xbow_item_unloaded   the crossbow, its string let go (straight between the prod tips)
    ///   ecp_xbow_item_loaded     the crossbow spanned (the string in the nut) with a blunt bolt in the groove
    ///
    /// Each model's origin is the fore-stock's middle (<see cref="XbowParts.Support"/>), front +Z and groove up +Y: the
    /// Arbalest's attach frame puts the player's left hand at its origin with the crossbow pointing +Z and its prod up,
    /// so the left hand holds our fore-stock, our prod lands where the Arbalest's is and our grip near its trigger.
    /// </summary>
    public static class XbowItemModels
    {
        public const string Unloaded = "ecp_xbow_item_unloaded";
        public const string Loaded = "ecp_xbow_item_loaded";

        public static string[] Build(string folder) => new[] { Model(folder, Unloaded, false), Model(folder, Loaded, true) };

        private static string Model(string folder, string name, bool loaded)
        {
            var root = new GameObject(name);
            GameObject crossbow = Part(folder, "ecp_xbow_crossbow", root.transform);
            crossbow.transform.localPosition = -XbowParts.Support;
            Vector3 middle = loaded ? XbowParts.Nut : XbowParts.Rest;
            String(folder, crossbow.transform, "string_a", XbowParts.TipA, middle);
            String(folder, crossbow.transform, "string_b", XbowParts.TipB, middle);
            if (loaded)
                XbowBolt.In(crossbow.transform, "bolt", 1f).transform.localPosition = XbowParts.Groove;
            string path = $"{folder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Log.Info("item model " + path);
            return path;
        }

        /// <summary>One half of the string, the one-metre cord stretched from its prod tip to the middle.</summary>
        private static void String(string folder, Transform crossbow, string name, Vector3 from, Vector3 to)
        {
            GameObject half = Part(folder, "ecp_xbow_string", crossbow);
            half.name = name;
            half.transform.localPosition = from;
            half.transform.localRotation = Quaternion.LookRotation(to - from, Vector3.up);
            half.transform.localScale = new Vector3(1f, 1f, (to - from).magnitude);
        }

        private static GameObject Part(string folder, string asset, Transform parent)
        {
            var part = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{asset}/{asset}.prefab"), parent, false);
            part.name = asset;
            return part;
        }
    }
}
