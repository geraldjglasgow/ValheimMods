using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A piece's snap points, from its definition (keel joints, feet and tops; rib feet; guide ends, block seats and keel
    /// rests; rail ends). Like the game's, snap points are inactive children tagged <c>snappoint</c>; their names show on
    /// the build hud.
    /// </summary>
    public static class KwSnaps
    {
        public static void Add(Transform root, KwDef def)
        {
            Vector3 pivot = new Vector3(def.Pivot.x, 0f, def.Pivot.y);
            foreach ((string name, Vector3 kit) in def.Snaps)
            {
                GameObject point = new GameObject(name) { tag = "snappoint", layer = root.gameObject.layer };
                point.transform.SetParent(root, false);
                point.transform.localPosition = KwMesh.ToUnity(kit - pivot);
                point.SetActive(false);
            }
        }
    }
}
