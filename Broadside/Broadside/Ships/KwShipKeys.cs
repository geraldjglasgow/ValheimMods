using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Whose a launched ship is, kept in its ZDO for its map pin (<see cref="KwShipPins"/>): who launched it
    /// (<see cref="ByKey"/>, the building player's id, and its name, <see cref="NameKey"/>, the blueprint's file name,
    /// written at launch on the building player's game, the server told of the new ship), and who steered it last
    /// (<see cref="HelmKey"/>: the ship's owner copies the game's helm user, <c>ZDOVars.s_user</c>, every two seconds while
    /// someone holds the helm). Ids are the players' character ids (<c>Player.GetPlayerID</c>), as the helm's.
    /// </summary>
    public sealed class KwShipKeys : MonoBehaviour
    {
        public const string ByKey = "kw_ship_by", HelmKey = "kw_ship_helm", NameKey = "kw_ship_name";
        public static readonly int ByHash = ByKey.GetStableHashCode(), HelmHash = HelmKey.GetStableHashCode(), NameHash = NameKey.GetStableHashCode();
        private const float Every = 2f;

        private ZNetView? view;
        private float next;

        /// <summary>A new ship's keys, on the building player's game: who launched it and its name; the server is told of it.</summary>
        public static void Launched(ZDO zdo, string name)
        {
            Player? me = Player.m_localPlayer;
            if (me != null)
                zdo.Set(ByHash, me.GetPlayerID());
            zdo.Set(NameHash, Short(name));
            KwShipFinder.Tell(zdo.m_uid);
        }

        /// <summary>A blueprint's name as a pin shows it: its file name without its folders.</summary>
        public static string Short(string name)
        {
            string last = name.Substring(name.LastIndexOf('/') + 1).Trim();
            return last.Length > 0 ? last : "Ship";
        }

        private void Awake() => view = GetComponent<ZNetView>();

        // On the owner: the player at the helm is the last helmsman.
        private void Update()
        {
            if (Time.time < next || view == null || !view.IsValid() || !view.IsOwner())
                return;
            next = Time.time + Every;
            ZDO zdo = view.GetZDO();
            long user = zdo.GetLong(ZDOVars.s_user);
            if (user != 0L && user != zdo.GetLong(HelmHash))
                zdo.Set(HelmHash, user);
        }
    }
}
