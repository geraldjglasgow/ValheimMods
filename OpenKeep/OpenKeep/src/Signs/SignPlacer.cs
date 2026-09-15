using UnityEngine;

namespace OpenKeep.Signs
{
    /// <summary>
    /// Creates the vanilla <c>sign</c> piece for a container the local game owns, the way the game creates a placed
    /// piece: <c>Object.Instantiate</c> of the scene prefab, whose <c>ZNetView.Awake</c> gives it a persistent ZDO.
    /// No build cost, no placement effect, no station. The instance's wear is switched off (<see cref="SignWear"/>)
    /// before anything else runs on it. The creator is the local player when there is one (the
    /// <c>creator</c> ZDO long and the piece's field, as <c>Piece.SetCreator</c> writes them; the platform user index
    /// stays unset because that type lives in an assembly the mod does not reference); on a dedicated server the
    /// sign has no creator.
    /// </summary>
    public static class SignPlacer
    {
        private const string PrefabName = "sign";
        private static bool warned;

        /// <summary>Places the container's sign, links both ZDOs and writes the first text. Null when the game has no sign prefab.</summary>
        public static ZDO Place(Container container, string text)
        {
            GameObject prefab = Prefab();
            if (prefab == null)
                return null;
            SignPose pose = SignPlacement.Compute(container);
            GameObject sign = UnityEngine.Object.Instantiate(prefab, SignPlacement.PivotFor(pose), pose.Rotation);
            ZNetView view = sign.GetComponent<ZNetView>();
            if (view == null || !view.IsValid())
            {
                UnityEngine.Object.Destroy(sign);
                Plugin.Log.LogWarning("OpenKeep: the sign got no ZDO; no sign placed");
                return null;
            }
            ZDO zdo = view.GetZDO();
            SignWear.Protect(sign);
            SignPlacement.AlignBottom(sign, zdo, pose);
            SetCreator(sign);
            SignLinks.Link(container.m_nview.GetZDO(), zdo);
            SignWriter.Write(zdo, text, force: true);
            return zdo;
        }

        /// <summary>Places the sign anew where it belongs now and carries its words over; the old one is destroyed.</summary>
        public static ZDO Replace(Container container, ZDO old)
        {
            ZDO fresh = Place(container, old.GetString(SignLinks.AutoTextKey, ""));
            if (fresh == null)
                return old;
            SignWriter.Copy(old, fresh);
            SignRemover.Destroy(old);
            return fresh;
        }

        private static void SetCreator(GameObject sign)
        {
            WearNTear wear = sign.GetComponent<WearNTear>();
            if (wear != null)
                wear.OnPlaced();
            Player player = Player.m_localPlayer;
            Piece piece = sign.GetComponent<Piece>();
            if (player == null || piece == null || piece.m_nview == null || !piece.m_nview.IsOwner() || piece.GetCreator() != 0L)
                return;
            long creator = player.GetPlayerID();
            piece.m_creator = creator;
            piece.m_nview.GetZDO().Set(ZDOVars.s_creator, creator);
        }

        private static GameObject Prefab()
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(PrefabName) : null;
            if (prefab == null && !warned)
            {
                warned = true;
                Plugin.Log.LogWarning("OpenKeep: the game has no 'sign' prefab; no contents signs are placed");
            }
            return prefab;
        }
    }
}
