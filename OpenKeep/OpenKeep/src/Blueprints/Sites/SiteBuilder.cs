using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Builds the construction sites this machine owns (the ZDO's owner is the one machine that writes a site), looking
    /// at each every eighth of a second while blueprints are on and the local player is alive (a site whose looks change
    /// nothing, one waiting for materials, less often, up to every <see cref="MaxIdle"/> s, until its ZDO changes); a site the server owns is
    /// taken over first, since a dedicated server has no player to build with. Piece by piece ("Build As
    /// Resources Come In", the default): the ground first once its stone is there, then one piece per look in
    /// <see cref="SiteOrder"/> while the store holds that piece's materials, never skipping ahead. All at once (that
    /// setting off): nothing until the store holds everything still needed, then the ground and the rest through
    /// <see cref="BuildJob"/>. Free ("Build Without Materials" or the builder in no-cost mode): the ground and the rest at
    /// once. A finished site hands back what is left in its store, tells the crew nearby and removes its post.
    /// </summary>
    public static class SiteBuilder
    {
        private const float StepInterval = 0.125f;

        /// <summary>A machine that just became the owner waits this long, so the last owner's final writes are in first, seconds.</summary>
        private const float Settle = 1f;

        /// <summary>A site whose steps change nothing is looked at less often, down to once in this many seconds (a delivery wakes it).</summary>
        private const float MaxIdle = 2f;

        /// <summary>Per frame (SiteHooks): a step for every owned site whose time has come.</summary>
        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead() || !BlueprintSettings.Enabled)
                return;
            IReadOnlyList<SiteMarker> sites = SiteMarker.Loaded;
            for (int n = sites.Count - 1; n >= 0; n--)
            {
                SiteMarker site = sites[n];
                if (site == null || site.State.Blueprint == null)
                    continue;
                TakeFromServer(site);
                if (Due(site))
                    BlueprintSafe.Run("OpenKeep site builder", () => StepAndRest(site, player));
            }
        }

        /// <summary>
        /// A dedicated server owns what lies near its own reference point (the world's centre) and has no player to build
        /// with, so a player's machine that has such a site loaded asks the server for it (<see cref="SiteTakeOver"/>: the
        /// server hands it to the first that asks); the server's own ownership rules then leave it with that player while
        /// they stay near. A hosting player's machine builds its own sites.
        /// </summary>
        private static void TakeFromServer(SiteMarker site)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() || ZRoutedRpc.instance == null)
                return;
            long server = ZRoutedRpc.instance.GetServerPeerID();
            if (site.View.GetZDO().GetOwner() == server && !HasPlayer(server))
                SiteTakeOver.Ask(site);
        }

        /// <summary>The machine plays a character (the server's player list says so; a dedicated server has none).</summary>
        private static bool HasPlayer(long peer)
        {
            foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList())
            {
                if (!info.m_characterID.IsNone() && info.m_characterID.UserID == peer)
                    return true;
            }
            return false;
        }

        /// <summary>The site is this machine's, has been for a moment, and its next step has come (which it then sets).</summary>
        private static bool Due(SiteMarker site)
        {
            SiteRun run = site.Run;
            if (!site.View.IsOwner())
            {
                run.OwnedSince = -1f;
                return false;
            }
            if (run.OwnedSince < 0f)
                run.OwnedSince = Time.time;
            bool woke = run.IdleDelay > 0f && site.State.Zdo.DataRevision != run.IdleRevision;
            if (Time.time < run.OwnedSince + Settle || (Time.time < run.NextStep && !woke))
                return false;
            run.NextStep = Time.time + StepInterval;
            return true;
        }

        /// <summary>A step; when it wrote nothing to the site the next one waits longer (<see cref="MaxIdle"/> at most), else the wait resets.</summary>
        private static void StepAndRest(SiteMarker site, Player player)
        {
            uint before = site.State.Zdo.DataRevision;
            Step(site, player);
            SiteRun run = site.Run;
            uint after = site.State.Zdo.DataRevision;
            if (after != before || BuildJob.IsBuilding(site))
            {
                run.IdleDelay = 0f;
                return;
            }
            run.IdleDelay = Mathf.Min(Mathf.Max(run.IdleDelay * 2f, StepInterval * 2f), MaxIdle);
            run.IdleRevision = after;
            run.NextStep = Time.time + run.IdleDelay;
        }

        private static void Step(SiteMarker site, Player player)
        {
            if (BuildJob.IsBuilding(site))
                return;
            SiteState s = site.State;
            bool cheated = player.NoCostCheat();
            bool free = cheated || BlueprintSettings.FreeMaterials;
            int next = SiteOrder.Next(s);
            if (s.GroundDone && next < 0)
                Finish(site);
            else if (free || !SiteSettings.PieceByPiece)
                TryAllAtOnce(site, player, free, cheated);
            else if (!s.GroundDone)
                SiteGround.TryShape(site, free: false);
            else
                SitePieces.Place(site, new[] { next }, player, free: false, cheated);
        }

        /// <summary>When the store has everything (or the build is free): the ground now, the pieces through the fast build.</summary>
        private static void TryAllAtOnce(SiteMarker site, Player player, bool free, bool cheated)
        {
            if (BuildJob.Busy || (!free && SiteNeeds.Missing(site).Count > 0))
                return;
            if (SiteGround.TryShape(site, free))
                BuildJob.Start(site, player, free, cheated);
        }

        /// <summary>Every piece stands: the store's rest dropped at the post, the crew told, the post removed.</summary>
        private static void Finish(SiteMarker site)
        {
            SiteState s = site.State;
            int left = SiteStore.Drop(site.transform.position, s.Store);
            SiteNetwork.AnnounceBuilt(site.transform.position, s.Name, s.Blueprint.Pieces.Count);
            Plugin.Log.LogInfo($"OpenKeep: site {s.Name} at {s.Frame.Origin} finished ({s.Blueprint.Pieces.Count} pieces, {left} materials handed back)");
            ZNetScene.instance.Destroy(site.gameObject);
        }
    }
}
