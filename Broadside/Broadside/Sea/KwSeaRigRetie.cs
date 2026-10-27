using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's masts' ropes tied on again after a sea edit changed what they tie on to or must keep clear of (a
    /// mast added, changed or removed; a railing), where the ship is drawn: each mast's sails and ropes made again from the
    /// blueprint as it is now, with its look and hit colliders (<see cref="KwSeaRigLayers.Retie"/>,
    /// <see cref="KwSeaRigRedraw.Rig"/>; its marks kept), one mast at a time among the ship's later work
    /// (<see cref="KwSeaShip.Later"/>, one step a frame), since tying a mast's ropes on takes about 5 ms. A peer that loads
    /// the ship later ties them the same way from the same blueprint. A fallen mast is tied again too, and shown down at
    /// once (<see cref="KwMastFall.Gone"/>), so it stands right when it is put back.
    /// </summary>
    public sealed class KwSeaRigRetie : MonoBehaviour
    {
        private readonly Queue<int> waiting = new Queue<int>();
        private KwSeaShip ship = null!;
        private System.Action? step;

        /// <summary>Every mast but those just remade queued to be tied on again (nothing on a dedicated server).</summary>
        public static void Queue(KwSeaShip ship, ICollection<int> remade)
        {
            if (!KwShipBody.Drawn)
                return;
            KwSeaRigRetie queue = ship.GetComponent<KwSeaRigRetie>() ?? ship.gameObject.AddComponent<KwSeaRigRetie>();
            queue.ship = ship;
            for (int i = 0; i < ship.Blueprint.Pieces.Count; i++)
            {
                if (ship.Blueprint.Pieces[i].Prefab == KwSailTool.Made && !remade.Contains(i) && !queue.waiting.Contains(i))
                    queue.waiting.Enqueue(i);
            }
            if (queue.waiting.Count > 0)
                ship.Later(queue.step ??= queue.Next);
        }

        // One mast tied on again, the next queued behind the ship's other later work.
        private void Next()
        {
            if (waiting.Count == 0 || ship == null)
                return;
            int index = waiting.Dequeue();
            if (waiting.Count > 0)
                ship.Later(step!);
            KwBpPiece? mast = index < ship.Blueprint.Pieces.Count ? ship.Blueprint.Pieces[index] : null;
            if (mast == null || mast.Prefab != KwSailTool.Made)
                return;
            KwSeaRigLayers.Retie(ship, mast);
            KwSeaRigRedraw.Rig(ship, new[] { index });
        }
    }
}
