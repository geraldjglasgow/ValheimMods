using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Which way along a keel line the ship's bow is, from the ship's own pieces round it as a blueprint reads it
    /// (<see cref="KwBpBow"/>): a rudder on the line marks the stern; with none, the way the masts on the line face. 0 when
    /// neither tells (no rudder or mast yet), and the Cabin tool's front is then the way the player looks.
    /// </summary>
    public static class KwCabinBow
    {
        // How far off the keel line a rudder or a mast may stand to count as this ship's.
        private const float Aside = 1.5f;

        /// <summary>1 when the bow is along the line's way, -1 when it is back, 0 when the ship does not tell.</summary>
        public static float Way(List<Piece> near, Vector3 origin, Vector3 forward, float middle)
        {
            float pull = 0f;
            foreach (Piece piece in near)
            {
                string name = piece != null ? Utils.GetPrefabName(piece.gameObject) : "";
                if (name.Length == 0 || !Near(origin, forward, piece!.transform.position))
                    continue;
                float along = Vector3.Dot(piece.transform.position - origin, forward);
                if (KwRudder.Is(name))
                    return along > middle ? -1f : 1f;
                float facing = Vector3.Dot(piece.transform.forward, forward);
                if (name == KwSailTool.Made && Mathf.Abs(facing) > 0.1f)
                    pull += Mathf.Sign(facing);
            }
            return Mathf.Abs(pull) > 0.5f ? Mathf.Sign(pull) : 0f;
        }

        private static bool Near(Vector3 origin, Vector3 forward, Vector3 at)
        {
            Vector3 off = at - origin, aside = off - forward * Vector3.Dot(off, forward);
            return new Vector2(aside.x, aside.z).magnitude < Aside;
        }
    }
}
