using System.Collections.Generic;
using System.Linq;
using OpenKeep.Blueprints.Sites;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// 'openkeep blueprint undo': takes down this player's last construction site of the session - the site itself when
    /// it still stands (the same request as Shift + E, so its store is dropped at its post), every loaded piece standing
    /// where the site's blueprint put one (found by prefab and position, so it works whichever machine built them; taken
    /// down through the game's own removal, their materials dropped as the hammer does unless building is free), and its
    /// ground, when this machine shaped it and no ground fix came after (<see cref="GroundUndo"/>).
    /// </summary>
    public static class BuildUndo
    {
        /// <summary>Positions are matched on a 5 cm grid.</summary>
        private const float Grid = 20f;

        private static ZDOID site = ZDOID.None;
        private static Blueprint blueprint;
        private static BuildFrame frame;
        private static bool groundRecorded;

        /// <summary>The local player placed a site: it is the one undo takes down.</summary>
        public static void Begin(SiteMarker marker, Blueprint bp, BuildFrame at)
        {
            site = marker.View.GetZDO().m_uid;
            blueprint = bp;
            frame = at;
            groundRecorded = false;
        }

        /// <summary>This machine shaped a site's ground: kept for undo when it is the local player's last site.</summary>
        public static void GroundShaped(SiteMarker marker, GroundWork work)
        {
            if (site.IsNone() || marker.View.GetZDO() == null || marker.View.GetZDO().m_uid != site)
                return;
            GroundUndo.Record(work, fix: false);
            groundRecorded = true;
        }

        public static void Run(Terminal.ConsoleEventArgs args)
        {
            if (site.IsNone() || blueprint == null)
            {
                args.Context.AddString("OpenKeep: no construction site was placed in this session.");
                return;
            }
            // The server checks the request and ignores a site that is gone (finished or taken down already).
            SiteTakeDown.Request(site);
            int removed = RemovePieces();
            int ground = groundRecorded && GroundUndo.Has && !GroundUndo.LastWasFix ? GroundUndo.Restore() : 0;
            args.Context.AddString($"OpenKeep: {blueprint.Name}: the site is taken down if it still stood, {removed} loaded pieces taken down, " +
                $"{ground} ground points put back.");
            site = ZDOID.None;
            blueprint = null;
        }

        /// <summary>Takes down every loaded player-built piece standing where the blueprint put a piece of the same prefab.</summary>
        private static int RemovePieces()
        {
            HashSet<(Vector3Int, string)> wanted = new HashSet<(Vector3Int, string)>(
                blueprint.Pieces.Select(p => (Cell(frame.World(p.X, p.Y, p.Z)), p.Prefab)));
            int removed = 0;
            foreach (Piece piece in Piece.s_allPieces.ToList())
            {
                if (piece != null && piece.IsPlacedByPlayer() && wanted.Contains((Cell(piece.transform.position), Utils.GetPrefabName(piece.gameObject))))
                    removed += BlueprintSafe.Call("OpenKeep blueprint undo", () => Remove(piece), false) ? 1 : 0;
            }
            return removed;
        }

        private static Vector3Int Cell(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * Grid), Mathf.RoundToInt(p.y * Grid), Mathf.RoundToInt(p.z * Grid));

        private static bool Remove(Piece piece)
        {
            ZNetView view = piece.GetComponent<ZNetView>();
            if (view == null || !view.IsValid())
                return false;
            bool free = BlueprintSettings.FreeMaterials;
            WearNTear wear = piece.GetComponent<WearNTear>();
            if (wear != null)
            {
                wear.Remove(blockDrop: free);
                return true;
            }
            view.ClaimOwnership();
            if (!free)
                piece.DropResources();
            ZNetScene.instance.Destroy(piece.gameObject);
            return true;
        }
    }
}
