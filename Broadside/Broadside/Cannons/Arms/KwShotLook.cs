using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// A shot's projectile wearing a cannon item's model in flight (<see cref="KwShot.Wears"/>: a ballista bolt, a
    /// cannonball): the game projectile's own meshes taken off (its trail, particles and hit effects kept), a copy of the
    /// item's model put on along its flight, its nose at the projectile's origin (where the game tests for hits), with no
    /// colliders of its own.
    /// </summary>
    public static class KwShotLook
    {
        private const string Trail = "trail";

        public static void Wear(GameObject shot, string item)
        {
            GameObject? prefab = KwItems.Prefab(item);
            Transform? model = prefab != null ? prefab.transform.Find(KwItemModel.AttachName) : null;
            if (model == null)
            {
                Log.Warn($"Broadside cannons: {shot.name} keeps the game's look: there is no cannon item {item}.");
                return;
            }
            List<Component> drawn = shot.GetComponentsInChildren<Component>(true).Where(c => c is MeshRenderer || c is SkinnedMeshRenderer || c is MeshFilter).ToList();
            foreach (Component part in drawn)
                Object.DestroyImmediate(part);
            GameObject look = Object.Instantiate(model.gameObject, shot.transform, false);
            look.name = "kw_look";
            foreach (Collider collider in look.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (Transform part in look.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = shot.layer;
            BoxCollider? box = model.GetComponent<BoxCollider>();
            look.transform.localPosition = new Vector3(0f, 0f, box != null ? -(box.center.z + box.size.z / 2f) : 0f);
            Projectile projectile = shot.GetComponent<Projectile>();
            if (projectile.m_visual == null || projectile.m_visual.name != Trail)
                projectile.m_visual = look;
        }
    }
}
