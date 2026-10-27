using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The shields hung on a launched ship's railings through a sea edit of its railings, on the owner: the hooks are
    /// numbered together in the blueprint's order (<see cref="KwShipShields"/>), so a railing changed or removed renumbers
    /// the hooks after it. Each hung shield keeps its hook where the railing it hangs on still has one within
    /// <see cref="Keep"/> of where it hung (every hook of a railing not edited); any other drops where it hung, never lost
    /// (a railing's shields drop as when it is destroyed). The renumbering is plain arithmetic.
    /// </summary>
    public static class KwSeaShields
    {
        public const float Keep = 0.05f;

        /// <summary>On the owner, its railings changed: the hung shields renumbered in the ZDO, the ones left without a hook dropped.</summary>
        public static void Owner(KwSeaShip ship, KwSeaChanges mine, ZDO zdo)
        {
            Dictionary<int, string> hung = KwShieldRecord.Read(zdo.GetString(KwShieldRecord.Hash));
            if (hung.Count == 0)
                return;
            List<(int Index, Vector3 At)> before = Hooks(Before(ship.Blueprint, mine)), after = Hooks(Pieces(ship.Blueprint));
            (Dictionary<int, string> kept, List<(string Saved, Vector3 At)> dropped) = Remap(hung, before, after);
            zdo.Set(KwShieldRecord.Hash, KwShieldRecord.Write(kept));
            foreach ((string saved, Vector3 at) in dropped)
                KwShieldRecord.Drop(saved, ship.World(at) + Vector3.up * 0.3f, Quaternion.identity);
        }

        /// <summary>The hung shields from the hooks before to the hooks after; those with no hook left, with where they hung.</summary>
        public static (Dictionary<int, string> Kept, List<(string Saved, Vector3 At)> Dropped) Remap(Dictionary<int, string> hung,
            List<(int Index, Vector3 At)> before, List<(int Index, Vector3 At)> after)
        {
            Dictionary<int, string> kept = new Dictionary<int, string>();
            List<(string, Vector3)> dropped = new List<(string, Vector3)>();
            foreach (KeyValuePair<int, string> entry in hung)
            {
                if (entry.Key < 0 || entry.Key >= before.Count)
                    continue;
                (int index, Vector3 at) = before[entry.Key];
                int slot = after.FindIndex(h => h.Index == index && (h.At - at).magnitude <= Keep);
                if (slot >= 0 && !kept.ContainsKey(slot))
                    kept[slot] = entry.Value;
                else
                    dropped.Add((entry.Value, at));
            }
            return (kept, dropped);
        }

        /// <summary>Every hook of the pieces in order (null entries skipped): its railing's index and its place in the ship's frame.</summary>
        public static List<(int Index, Vector3 At)> Hooks(List<KwBpPiece?> pieces)
        {
            List<(int, Vector3)> hooks = new List<(int, Vector3)>();
            for (int i = 0; i < pieces.Count; i++)
            {
                KwBpPiece? piece = pieces[i];
                if (piece == null || !KwRailStretches.IsRail(piece.Prefab))
                    continue;
                foreach (Vector3 at in KwShieldSlots.Places(KwSeaPiece.Text(piece)))
                    hooks.Add((i, piece.Position + piece.Rotation * at));
            }
            return hooks;
        }

        /// <summary>The blueprint's pieces as they are.</summary>
        public static List<KwBpPiece?> Pieces(KwBlueprint blueprint) => blueprint.Pieces.ConvertAll(p => KwSeaApply.IsGone(p) ? null : p);

        /// <summary>The blueprint's pieces as they were before these changes (an added one not there yet).</summary>
        public static List<KwBpPiece?> Before(KwBlueprint blueprint, KwSeaChanges changes)
        {
            List<KwBpPiece?> pieces = Pieces(blueprint);
            foreach (KwSeaChange change in changes.All)
            {
                if (change.Index < pieces.Count)
                    pieces[change.Index] = change.Old;
            }
            return pieces;
        }
    }
}
