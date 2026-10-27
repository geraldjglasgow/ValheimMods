using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's patches drawn (<see cref="KwHullPatches"/>), on the screens that draw the ship, under its visual
    /// holder: one board patch over each patched hole on the hull's outside (<see cref="KwHullPatchBoard"/>). When the
    /// patches change: a new one is hammered on with the game's hammering sound, and one that is off (its hole mended with the
    /// hammer, or widened past it by a new hit) breaks away with the game's wood-breaking sound, falls off the ship and is
    /// gone (<see cref="KwHullPatchFall"/>); those already on or off when the ship loads just show or not. Only drawn; nothing
    /// is sent.
    /// </summary>
    public sealed class KwHullPatchLooks : MonoBehaviour
    {
        private readonly Dictionary<int, GameObject> boards = new Dictionary<int, GameObject>();
        private KwHullPatches patches = null!;
        private int drawn = -1;
        private bool loaded, broken;

        public static void Add(Transform ship, KwHullPatches patches)
        {
            Transform? visual = ship.Find(KwShipPrefab.Visual);
            if (visual != null)
                visual.gameObject.AddComponent<KwHullPatchLooks>().patches = patches;
        }

        // A throw is reported once and stops this ship's patches being drawn, rather than once a frame.
        private void Update()
        {
            if (broken || patches == null || patches.Version == drawn)
                return;
            (drawn, broken) = (patches.Version, true);
            Guard.Run("Broadside hull patches look", Show);
            (loaded, broken) = (true, false);
        }

        private void Show()
        {
            HashSet<int> on = patches.Shown;
            foreach (int id in new List<int>(boards.Keys))
            {
                if (!on.Contains(id))
                    Off(id);
            }
            foreach (KwHullHole hole in patches.Holes.Holes)
            {
                if (on.Contains(hole.Id) && !boards.ContainsKey(hole.Id))
                    On(hole);
            }
        }

        private void On(KwHullHole hole)
        {
            GameObject board = KwHullPatchBoard.Make(transform, hole, patches.Covering(hole));
            boards[hole.Id] = board;
            if (loaded)
                KwHullPatchFall.Knock(board);
        }

        private void Off(int id)
        {
            GameObject board = boards[id];
            boards.Remove(id);
            if (board == null)
                return;
            if (loaded)
                KwHullPatchFall.Drop(board);
            else
                Destroy(board);
        }

        // The patches go with the ship's look; one falling away is its own and goes by itself.
        private void OnDestroy()
        {
            foreach (GameObject board in boards.Values)
            {
                if (board != null)
                    Destroy(board);
            }
        }
    }
}
