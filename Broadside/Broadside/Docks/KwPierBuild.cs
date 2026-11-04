using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A planned dock's sections placed through the game's own placement (<c>Player.PlacePiece</c>: the networked piece
    /// made on this player's game, its creator, its placing and wear set up, the game's place effect and building
    /// statistics, the hammer's swing for the first), each then given its shape in its new ZDO and built at once
    /// (<see cref="KwPier.Take"/>), all in the click's frame. Every peer then builds the same from the ZDO; nothing else is
    /// sent.
    /// </summary>
    public static class KwPierBuild
    {
        /// <summary>The sections placed; how many were.</summary>
        public static int Spawn(Player player, KwPierPlan plan)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(plan.Kind.Prefab) : null;
            Piece? piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece == null)
            {
                Log.Warn($"Broadside docks: no {plan.Kind.Prefab} in the network scene; nothing built.");
                return 0;
            }
            int made = 0;
            for (int i = 0; i < plan.Sections.Count; i++)
            {
                player.PlacePiece(piece, plan.SectionAt(i), plan.Turn, i == 0, player.NoCostCheat());
                Piece? placed = Newest(prefab!.name, player.GetPlayerID());
                if (placed == null)
                {
                    Log.Warn($"Broadside docks: section {i + 1} of a dock was not found after placing it.");
                    continue;
                }
                KwPier.Take(placed.gameObject, plan.Sections[i]);
                made++;
            }
            return made;
        }

        /// <summary>
        /// The piece the game just placed: the newest in its list of every piece (each adds itself there as it wakes),
        /// checked to be of that prefab and this player's (a dock ladder's too, <see cref="KwPierLadderTool"/>).
        /// </summary>
        public static Piece? Newest(string prefab, long creator)
        {
            List<Piece> all = Piece.s_allPieces;
            Piece? last = all.Count > 0 ? all[all.Count - 1] : null;
            return last != null && Utils.GetPrefabName(last.gameObject) == prefab && last.GetCreator() == creator ? last : null;
        }
    }
}
