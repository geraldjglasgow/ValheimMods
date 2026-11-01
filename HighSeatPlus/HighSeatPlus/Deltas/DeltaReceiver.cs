using System;

namespace HighSeatPlus
{
    /// <summary>
    /// This game's side of the delta protocol (<see cref="DeltaFormat"/>), on a player's game connected to a server.
    /// On connecting it hears the server's delta packages and says hello with its version; a server without HighSeat
    /// never answers, and its "ZDOData" goes to the game as always. Each delta package is turned into the game's own
    /// package (<see cref="PackageConvert"/>) and handed to the game's ZDOMan.RPC_ZDOData, as if the server had sent it
    /// whole; then the revisions the game took are noted (<see cref="HeldCopies"/>) and the objects whose delta did not
    /// fit this game's copy (missing, or at other revisions than the delta starts from) are asked for whole in one
    /// call, which the server answers by sending them whole next. Nothing is ever thrown into the game's networking: a
    /// package that cannot be read, or anything failing on the way, makes this game give up on deltas for the rest of
    /// the connection (hello with 0): the server goes back to the game's own format and sends everything again, so no
    /// object is left missing or stale. That includes the game's own RPC_ZDOData throwing on a converted package
    /// (another mod's patch on it): the objects after the failure would be lost, so the package's objects leave the
    /// book, the failure is logged once in full, and this game gives up the same way.
    /// </summary>
    internal static class DeltaReceiver
    {
        private static bool _gaveUp;
        private static bool _gameFailed;

        /// <summary>A new connection to a server: hears its delta packages and says hello.</summary>
        public static void Listen(ZNetPeer server)
        {
            _gaveUp = false;
            server.m_rpc.Register<ZPackage>(DeltaFormat.DataRpc, Received);
            server.m_rpc.Invoke(DeltaFormat.HelloRpc, DeltaFormat.Version);
        }

        private static void Received(ZRpc rpc, ZPackage pkg)
        {
            try
            {
                ZDOMan man = ZDOMan.instance;
                if (man != null && ZNet.instance != null && !ZNet.instance.IsServer())
                {
                    Take(man, rpc, pkg);
                }
            }
            catch (Exception e)
            {
                GiveUp(rpc, e.Message);
            }
            finally
            {
                PackageConvert.Clear();
            }
        }

        private static void Take(ZDOMan man, ZRpc rpc, ZPackage pkg)
        {
            if (!PackageConvert.Convert(man, pkg))
            {
                GiveUp(rpc, "a delta package could not be read");
                return;
            }
            Apply(man, rpc);
            AskAgain(rpc);
        }

        // The game's own RPC_ZDOData on the converted package, then what it took is noted.
        private static void Apply(ZDOMan man, ZRpc rpc)
        {
            PackageConvert.Game.SetPos(0);
            try
            {
                man.RPC_ZDOData(rpc, PackageConvert.Game);
            }
            catch (Exception e)
            {
                GameFailed(rpc, e);
                return;
            }
            foreach (HeldCopies.Expect expect in PackageConvert.Expected)
            {
                HeldCopies.Settle(expect);
            }
            DeltaClientStats.Received(PackageConvert.Deltas, PackageConvert.Expected.Count - PackageConvert.Deltas);
        }

        private static void GameFailed(ZRpc rpc, Exception e)
        {
            foreach (HeldCopies.Expect expect in PackageConvert.Expected)
            {
                HeldCopies.Forget(expect.Id);
            }
            if (!_gameFailed)
            {
                _gameFailed = true;
                HighSeatPlus.Log.LogWarning($"the game's own handling of world data failed on a delta package (logged once): {e}");
            }
            GiveUp(rpc, "the game's own handling of a package failed");
        }

        private static void AskAgain(ZRpc rpc)
        {
            if (PackageConvert.Asks.Count == 0)
            {
                return;
            }
            ZPackage ids = new ZPackage();
            ids.Write(PackageConvert.Asks.Count);
            foreach (ZDOID id in PackageConvert.Asks)
            {
                ids.Write(id);
            }
            rpc.Invoke(DeltaFormat.FullCopyRpc, ids);
            DeltaClientStats.CountAsked(PackageConvert.Asks.Count);
        }

        // Back to the game's own format for the rest of this connection; the server then sends everything again.
        private static void GiveUp(ZRpc rpc, string why)
        {
            if (_gaveUp)
            {
                return;
            }
            _gaveUp = true;
            DeltaClientStats.GaveUp = true;
            HighSeatPlus.Log.LogWarning($"smaller world updates are off for this connection ({why}); the server sends whole objects");
            try
            {
                rpc.Invoke(DeltaFormat.HelloRpc, 0);
            }
            catch (Exception)
            {
                // The connection is going; the next one starts afresh.
            }
        }
    }
}
