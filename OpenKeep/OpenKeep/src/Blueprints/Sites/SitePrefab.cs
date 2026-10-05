using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The construction site's post, prefab <see cref="PrefabName"/>: a copy of the game's 2 m wood pole made once under
    /// an inactive holder (so the copy's Awake never runs there), registered in every network scene like the OpenKeep
    /// hammer so every machine can load a site from its ZDO. It never breaks (no WearNTear, its fresh look kept), the
    /// hammer cannot remove it, it gives nothing back, monsters do not go for it, and it carries <see cref="SiteMarker"/>.
    /// </summary>
    public static class SitePrefab
    {
        public const string PrefabName = "OpenKeep_Site";
        private const string Source = "wood_pole2";

        private static GameObject prefab;

        /// <summary>The post to place, or null before a network scene made it (or when the game has no wood pole).</summary>
        public static GameObject Prefab => prefab;

        /// <summary>ZNetScene.Awake postfix: the post as a networked prefab.</summary>
        public static void Register(ZNetScene scene)
        {
            GameObject made = Make(scene.GetPrefab(Source));
            int hash = PrefabName.GetStableHashCode();
            if (made == null || scene.m_namedPrefabs.ContainsKey(hash))
                return;
            scene.m_prefabs.Add(made);
            scene.m_namedPrefabs.Add(hash, made);
        }

        private static GameObject Make(GameObject pole)
        {
            if (prefab != null || pole == null)
                return prefab;
            GameObject holder = new GameObject("OpenKeep Site Prefab");
            holder.SetActive(false);
            Object.DontDestroyOnLoad(holder);
            GameObject made = Object.Instantiate(pole, holder.transform, false);
            made.name = PrefabName;
            Unbreakable(made);
            SetUpPiece(made.GetComponent<Piece>());
            made.AddComponent<SiteMarker>();
            prefab = made;
            return prefab;
        }

        /// <summary>No wear and tear: the post keeps its fresh look and never takes damage or falls.</summary>
        private static void Unbreakable(GameObject made)
        {
            WearNTear wear = made.GetComponent<WearNTear>();
            if (wear == null)
                return;
            SetActive(wear.m_new, true);
            SetActive(wear.m_worn, false);
            SetActive(wear.m_broken, false);
            SetActive(wear.m_wet, false);
            Object.DestroyImmediate(wear);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }

        private static void SetUpPiece(Piece piece)
        {
            if (piece == null)
                return;
            piece.m_name = SiteWords.MarkerName;
            piece.m_description = "";
            piece.m_canBeRemoved = false;
            piece.m_resources = new Piece.Requirement[0];
            piece.m_primaryTarget = false;
            piece.m_randomTarget = false;
        }
    }

    /// <summary>
    /// ZNetScene.Awake postfix: the construction site's post as a networked prefab. Guarded: a throw here would leave the
    /// network scene without its prefabs and the world loading for ever.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class SiteScenePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ZNetScene __instance)
        {
            BlueprintSafe.Run("OpenKeep site prefab", () => SitePrefab.Register(__instance));
        }
    }
}
