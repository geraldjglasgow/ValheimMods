using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The server's side of the ship pins (<see cref="KwShipPins"/>): it knows every launched ship's ZDO, loaded or not,
    /// and answers a player's game with the place and name of each ship that player launched or steered last
    /// (<see cref="KwShipKeys"/>). Which ZDOs are ships is found once a session by one pass over the server's ZDOs (the
    /// <c>KW_ship</c> prefab's hash), at the first question; a ship launched later is told to it by the launching game
    /// (<see cref="TellCall"/>). Each answer looks every known ship up by id; one that is gone (destroyed, or never
    /// arrived a minute after it was told) is forgotten.
    /// </summary>
    public static class KwShipFinder
    {
        public const string AskCall = "KW_ShipWhere", AnswerCall = "KW_ShipsAre", TellCall = "KW_ShipLaunched";
        private const float Grace = 60f;
        private static readonly int Hash = KwShipPrefab.Name.GetStableHashCode();
        private static readonly Dictionary<ZDOID, float> ships = new Dictionary<ZDOID, float>();
        private static ZDOMan? scanned;

        public static void Register(ZRoutedRpc rpc)
        {
            rpc.Register<long>(AskCall, (sender, player) => Guard.Run("Broadside ship pins", () => Answer(sender, player)));
            rpc.Register<ZDOID>(TellCall, (_, id) => Guard.Run("Broadside ship pins", () => Known(id)));
        }

        /// <summary>Tells the server of a ship just launched.</summary>
        public static void Tell(ZDOID id) => ZRoutedRpc.instance?.InvokeRoutedRPC(TellCall, id);

        private static bool Serving => ZNet.instance != null && ZNet.instance.IsServer() && ZDOMan.instance != null;

        private static void Known(ZDOID id)
        {
            if (Serving)
                ships[id] = Time.time;
        }

        // A player's ships, each its id, its place and its name, back to the asking game.
        private static void Answer(long sender, long player)
        {
            if (!Serving || player == 0L)
                return;
            List<ZDO> mine = Mine(player);
            ZPackage package = new ZPackage();
            package.Write(mine.Count);
            foreach (ZDO zdo in mine)
            {
                package.Write(zdo.m_uid);
                package.Write(zdo.GetPosition());
                package.Write(zdo.GetString(KwShipKeys.NameHash, "Ship"));
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, AnswerCall, package);
        }

        private static List<ZDO> Mine(long player)
        {
            ZDOMan man = ZDOMan.instance;
            if (scanned != man)
                Scan(man);
            List<ZDO> mine = new List<ZDO>();
            List<ZDOID>? gone = null;
            foreach (KeyValuePair<ZDOID, float> ship in ships)
            {
                ZDO? zdo = man.GetZDO(ship.Key);
                if (zdo == null || zdo.GetPrefab() != Hash)
                {
                    if (Time.time - ship.Value > Grace)
                        (gone ??= new List<ZDOID>()).Add(ship.Key);
                }
                else if (zdo.GetLong(KwShipKeys.ByHash) == player || zdo.GetLong(KwShipKeys.HelmHash) == player)
                    mine.Add(zdo);
            }
            gone?.ForEach(id => ships.Remove(id));
            return mine;
        }

        // Every ship among the server's ZDOs, once a session.
        private static void Scan(ZDOMan man)
        {
            scanned = man;
            foreach (ZDO zdo in man.m_objectsByID.Values)
            {
                if (zdo.GetPrefab() == Hash)
                    ships[zdo.m_uid] = Time.time;
            }
        }
    }
}
