using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// A stack spilled from a broken crate shown as a stack (<see cref="KwCrateSpill"/>): the drop is the game's one pickup
    /// holding the whole stack, and on every screen it wears a small heap of copies of its own model beside and on it, one
    /// more for each doubling of the stack (2 or 3 items: one copy, 4 to 7: two, 8 to 15: three, 16 or more: four), drawn
    /// from the drop's own synced stack size, so a stack partly picked up shrinks its heap. Cosmetic only: the copies are
    /// meshes and materials under the drop, no colliders, nothing networked, nothing sent (the drop's ZDO carries one flag,
    /// <see cref="HeapHash"/>, set by the crate's owner as it drops the stack); laid side by side across the model's narrow
    /// way, then on top between them, each turned a few degrees by the drop's id, the same on every screen; none on a
    /// dedicated server. Only drops so marked: every other drop in the world is left as the game draws it.
    /// </summary>
    public sealed class KwCrateHeap : MonoBehaviour
    {
        /// <summary>On a drop: it came out of a broken crate and shows its stack as a heap.</summary>
        public static readonly int HeapHash = "kwc_heap".GetStableHashCode();

        private const int Most = 4;
        private const float Look = 1f, Rise = 0.8f, Jitter = 12f;
        private readonly List<GameObject> copies = new List<GameObject>();
        private List<(Mesh Mesh, Material[] Looks, Matrix4x4 Pose, int Layer, ShadowCastingMode Shadow)>? parts;
        private ItemDrop? drop;
        private ZNetView? view;
        private uint seen = uint.MaxValue;
        private int shown = -1;
        private float next;

        /// <summary>The heap on a marked drop, on a screen (the owner's own drop, marked after the game woke it).</summary>
        public static void Add(ItemDrop drop)
        {
            if (KwCargoModel.Screens && drop.GetComponent<KwCrateHeap>() == null)
                drop.gameObject.AddComponent<KwCrateHeap>();
        }

        /// <summary>How many copies a stack shows besides the drop's own model.</summary>
        public static int Extra(int stack) => stack <= 1 ? 0 : Math.Min(Most, (int)Math.Floor(Math.Log(stack, 2)));

        private void Start()
        {
            (drop, view) = (GetComponent<ItemDrop>(), GetComponent<ZNetView>());
            if (drop == null || view == null || !KwCargoModel.Screens)
                enabled = false;
        }

        private void Update()
        {
            if (Time.time < next)
                return;
            next = Time.time + Look;
            Guard.Run("Broadside crate heap", static h => h.Check(), this);
        }

        // The stack read again when the drop's ZDO changed; the heap made again when its count changes.
        private void Check()
        {
            ZDO? zdo = view!.GetZDO();
            if (zdo == null || zdo.DataRevision == seen)
                return;
            seen = zdo.DataRevision;
            drop!.Load();
            int count = Extra(drop.m_itemData.m_stack);
            if (count != shown)
                Build(count);
        }

        private void Build(int count)
        {
            foreach (GameObject copy in copies)
                Destroy(copy);
            copies.Clear();
            shown = count;
            parts ??= KwCrateHeapParts.Read(drop!);
            if (count == 0 || parts.Count == 0)
                return;
            (Vector3 across, float wide, float high) = KwCrateHeapParts.Measure(drop!.gameObject);
            System.Random random = new System.Random((int)view!.GetZDO().m_uid.ID);
            for (int k = 0; k < count; k++)
                Place(Offset(k, across, wide, high), (float)(random.NextDouble() * 2.0 - 1.0) * Jitter);
        }

        // Beside it on either side across its narrow way, then on top between them.
        private static Vector3 Offset(int k, Vector3 across, float wide, float high)
        {
            float side = k % 2 == 0 ? 1f : -1f;
            return k < 2 ? across * (side * wide) : across * (side * wide * 0.5f) + Vector3.up * (high * Rise);
        }

        // One copy of the model at an offset in the world, turned about the drop's middle, kept under the drop.
        private void Place(Vector3 offset, float yaw)
        {
            Transform root = drop!.transform;
            Matrix4x4 heap = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one) * Matrix4x4.Translate(root.position)
                * Matrix4x4.Rotate(Quaternion.Euler(0f, yaw, 0f)) * Matrix4x4.Translate(-root.position) * root.localToWorldMatrix;
            foreach ((Mesh mesh, Material[] looks, Matrix4x4 pose, int layer, ShadowCastingMode shadow) in parts!)
            {
                GameObject copy = new GameObject("kwc_heap") { layer = layer };
                Matrix4x4 world = heap * pose;
                copy.transform.SetPositionAndRotation(world.GetColumn(3), world.rotation);
                copy.transform.localScale = world.lossyScale;
                copy.transform.SetParent(root, true);
                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = copy.AddComponent<MeshRenderer>();
                (renderer.sharedMaterials, renderer.shadowCastingMode) = (looks, shadow);
                copies.Add(copy);
            }
        }
    }

    /// <summary>
    /// A drop's model as the heap copies it, read once a heap: its drawn meshes (the nearest level of detail only), each with
    /// its materials and its pose under the drop; and its size in the world, across its narrow way and up.
    /// </summary>
    public static class KwCrateHeapParts
    {
        public static List<(Mesh, Material[], Matrix4x4, int, ShadowCastingMode)> Read(ItemDrop drop)
        {
            List<(Mesh, Material[], Matrix4x4, int, ShadowCastingMode)> found = new List<(Mesh, Material[], Matrix4x4, int, ShadowCastingMode)>();
            HashSet<Renderer>? nearest = Nearest(drop.GetComponentInChildren<LODGroup>());
            Matrix4x4 back = drop.transform.worldToLocalMatrix;
            foreach (MeshRenderer renderer in drop.GetComponentsInChildren<MeshRenderer>())
            {
                MeshFilter? filter = renderer.GetComponent<MeshFilter>();
                bool drawn = renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.name != "kwc_heap";
                if (drawn && filter != null && filter.sharedMesh != null && (nearest == null || nearest.Contains(renderer)))
                    found.Add((filter.sharedMesh, renderer.sharedMaterials, back * renderer.transform.localToWorldMatrix, renderer.gameObject.layer, renderer.shadowCastingMode));
            }
            return found;
        }

        private static HashSet<Renderer>? Nearest(LODGroup? group)
        {
            LOD[]? levels = group != null ? group.GetLODs() : null;
            return levels != null && levels.Length > 0 ? new HashSet<Renderer>(levels[0].renderers) : null;
        }

        /// <summary>The way across the model's narrow side (flat in the world), how wide it is that way and how high.</summary>
        public static (Vector3 Across, float Wide, float High) Measure(GameObject drop)
        {
            Bounds? all = null;
            foreach (MeshRenderer renderer in drop.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.name == "kwc_heap" || !renderer.enabled)
                    continue;
                Bounds box = renderer.bounds;
                if (all.HasValue)
                    box.Encapsulate(all.Value);
                all = box;
            }
            Vector3 size = all?.size ?? Vector3.one * 0.3f;
            Vector3 across = size.x <= size.z ? Vector3.right : Vector3.forward;
            return (across, Mathf.Max(0.05f, Mathf.Min(size.x, size.z)), Mathf.Max(0.03f, size.y));
        }
    }
}
