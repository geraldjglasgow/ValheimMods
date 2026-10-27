using System;
using System.Collections;
using System.Collections.Generic;
using Splatform;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// Building a blueprint and removing a ship, a few pieces a frame (<see cref="KwBpRunner"/>). Building runs on the
    /// acting player's game: each piece is the kit's normal networked prefab, instantiated where the frame puts it, given
    /// its saved ZDO values (<see cref="KwBpData"/>), the build's ship id and the player as creator, so every peer and the
    /// server see an ordinary piece. Only the kit's ship pieces are built (<see cref="KwBlueprintStation.IsShipPiece"/>), so
    /// a hand-edited file cannot spawn anything else. Removing goes through the game's own path: a piece's
    /// <see cref="WearNTear.Remove"/> asks its owner to destroy it (nothing comes back: every piece is free); pieces under a
    /// ward the player has no access to are kept.
    /// </summary>
    public static class KwBpJobs
    {
        // At most this many pieces a frame: a generated part builds its mesh as it appears, and each removal plays the
        // piece's own break effect on its owner.
        private const int BuildsPerFrame = 12, RemovalsPerFrame = 20;

        /// <summary>A ship is being built (one at a time).</summary>
        public static bool Busy { get; private set; }

        public static void Build(Player player, KwBlueprint blueprint, Vector3 origin, Quaternion turn)
        {
            Busy = true;
            string ship = Guid.NewGuid().ToString("N");
            KwBpRunner.Run("Broadside blueprint build", Building(player, player.GetPlayerID(), blueprint, origin, turn, ship), () => Busy = false);
        }

        public static void Remove(Player player, List<Piece> pieces, string what) =>
            KwBpRunner.Run("Broadside ship removal", Removing(player, pieces, what), () => { });

        private static IEnumerator Building(Player player, long creator, KwBlueprint blueprint, Vector3 origin, Quaternion turn, string ship)
        {
            int made = 0, left = 0;
            KwBpPace pace = new KwBpPace(BuildsPerFrame);
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                if (ZNetScene.instance == null)
                    yield break;
                if (Spawn(piece, origin, turn, ship, creator))
                    made++;
                else
                    left++;
                if (pace.Spent())
                {
                    yield return null;
                    pace.Restart();
                }
            }
            Say(player, left == 0 ? $"Built {blueprint.Name}: {made} pieces" : $"Built {blueprint.Name}: {made} pieces, {left} unknown pieces left out");
        }

        private static bool Spawn(KwBpPiece piece, Vector3 origin, Quaternion turn, string ship, long creator)
        {
            GameObject? prefab = KwBlueprintStation.IsShipPiece(piece.Prefab) ? ZNetScene.instance.GetPrefab(piece.Prefab) : null;
            if (prefab == null)
                return false;
            GameObject built = Object.Instantiate(prefab, origin + turn * piece.Position, turn * piece.Rotation);
            ZNetView? view = built.GetComponent<ZNetView>();
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo != null)
            {
                KwBpData.Apply(zdo, piece.Data);
                zdo.Set(KwBlueprintStation.ShipHash, ship);
            }
            built.GetComponent<Piece>()?.SetCreator(creator, PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            return true;
        }

        private static IEnumerator Removing(Player player, List<Piece> pieces, string what)
        {
            int removed = 0, kept = 0;
            KwBpPace pace = new KwBpPace(RemovalsPerFrame);
            foreach (Piece piece in pieces)
            {
                int result = RemoveOne(piece);
                removed += result > 0 ? 1 : 0;
                kept += result < 0 ? 1 : 0;
                if (pace.Spent())
                {
                    yield return null;
                    pace.Restart();
                }
            }
            Say(player, kept == 0 ? $"{what}: {removed} pieces removed" : $"{what}: {removed} pieces removed, {kept} kept (a ward you have no access to)");
        }

        // 1 removed, 0 already gone, -1 kept (warded).
        private static int RemoveOne(Piece piece)
        {
            ZNetView? view = piece != null ? piece.m_nview : null;
            if (view == null || !view.IsValid() || ZNetScene.instance == null)
                return 0;
            if (!PrivateArea.CheckAccess(piece!.transform.position, 0f, false, false))
                return -1;
            WearNTear? wear = piece.GetComponent<WearNTear>();
            if (wear != null)
            {
                wear.Remove(true);
                return 1;
            }
            view.ClaimOwnership();
            ZNetScene.instance.Destroy(piece.gameObject);
            return 1;
        }

        private static void Say(Player player, string text)
        {
            if (player != null)
                player.Message(MessageHud.MessageType.Center, text);
        }
    }
}
