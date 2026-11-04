using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// The layout's see-through previews: each decoration's drawn meshes (the renderers its piece shows, none of its
    /// colliders, lights, fire or sound) copied once into a hidden template in Broadside's see-through look
    /// (<see cref="KwPreview.Glass"/>), and copies of it stood where the layout puts each one, kept in a pool. When what
    /// shows changes only the difference moves: previews no longer shown go back to the pool at once, new ones are stood
    /// a few a frame (6 made or 24 taken from the pool), so a 64 m yard's hundred never stalls a frame. Only drawn, on this
    /// player's screen; on the ghost layer, so no ray of the reading or the game meets them.
    /// </summary>
    public static class KwDecorGhosts
    {
        private const int MakeAFrame = 6, TakeAFrame = 24;
        private static readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, List<GameObject>> pool = new Dictionary<string, List<GameObject>>();
        private static readonly Dictionary<KwDecorPlan, GameObject> shown = new Dictionary<KwDecorPlan, GameObject>();
        private static readonly Queue<KwDecorPlan> waiting = new Queue<KwDecorPlan>();
        private static int layer = -2;

        private static int Layer => layer != -2 ? layer : layer = Mathf.Max(0, LayerMask.NameToLayer("ghost"));

        /// <summary>These and no others shown: the ones gone put back now, the new ones stood over the next frames.</summary>
        public static void Show(List<KwDecorPlan> plans)
        {
            HashSet<KwDecorPlan> wanted = new HashSet<KwDecorPlan>(plans);
            foreach (KwDecorPlan plan in new List<KwDecorPlan>(shown.Keys))
            {
                if (!wanted.Contains(plan))
                    Put(plan);
            }
            waiting.Clear();
            foreach (KwDecorPlan plan in plans)
            {
                if (!shown.ContainsKey(plan))
                    waiting.Enqueue(plan);
            }
        }

        /// <summary>A few more of the waiting previews stood, each frame.</summary>
        public static void Step()
        {
            (int made, int taken) = (0, 0);
            while (waiting.Count > 0 && made < MakeAFrame && taken < TakeAFrame)
            {
                KwDecorPlan plan = waiting.Dequeue();
                bool pooled = Pool(plan.Kind.Name).Count > 0;
                GameObject ghost = Take(plan.Kind);
                ghost.transform.SetPositionAndRotation(plan.Position, plan.Rotation);
                // a lantern pole over a dock's piling drawn without its cairn; a pooled one set back for any other plan
                KwDockTrim.Set(ghost, KwDecorLights.Seated(plan));
                ghost.SetActive(true);
                shown[plan] = ghost;
                (made, taken) = pooled ? (made, taken + 1) : (made + 1, taken);
            }
        }

        /// <summary>Every preview put back in the pool.</summary>
        public static void Hide()
        {
            foreach (KwDecorPlan plan in new List<KwDecorPlan>(shown.Keys))
                Put(plan);
            waiting.Clear();
        }

        private static void Put(KwDecorPlan plan)
        {
            GameObject ghost = shown[plan];
            shown.Remove(plan);
            if (ghost == null)
                return;
            ghost.SetActive(false);
            Pool(plan.Kind.Name).Add(ghost);
        }

        private static List<GameObject> Pool(string prefab)
        {
            if (!pool.TryGetValue(prefab, out List<GameObject> list))
                pool[prefab] = list = new List<GameObject>();
            return list;
        }

        // A pooled copy, or a new one from the template; a copy the scene took away is passed over.
        private static GameObject Take(KwDecorKind kind)
        {
            List<GameObject> free = Pool(kind.Name);
            while (free.Count > 0)
            {
                GameObject ghost = free[free.Count - 1];
                free.RemoveAt(free.Count - 1);
                if (ghost != null)
                    return ghost;
            }
            return Object.Instantiate(Template(kind));
        }

        private static GameObject Template(KwDecorKind kind)
        {
            if (templates.TryGetValue(kind.Name, out GameObject made) && made != null)
                return made;
            GameObject root = new GameObject("BS_decor_ghost_" + kind.Name) { layer = Layer };
            root.SetActive(false);
            Object.DontDestroyOnLoad(root);
            Matrix4x4 toRoot = kind.Prefab.transform.worldToLocalMatrix;
            foreach (MeshRenderer renderer in kind.Prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (Drawn(renderer, kind.Prefab.transform))
                    Copy(root.transform, renderer, toRoot * renderer.transform.localToWorldMatrix);
            }
            return templates[kind.Name] = root;
        }

        // A renderer the placed piece shows: enabled, under parts all switched on.
        private static bool Drawn(MeshRenderer renderer, Transform root)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (!renderer.enabled || filter == null || filter.sharedMesh == null)
                return false;
            for (Transform part = renderer.transform; part != null && part != root; part = part.parent)
            {
                if (!part.gameObject.activeSelf)
                    return false;
            }
            return true;
        }

        private static void Copy(Transform root, MeshRenderer source, Matrix4x4 pose)
        {
            GameObject part = new GameObject(source.name) { layer = Layer };
            part.transform.SetParent(root, false);
            part.transform.localPosition = pose.GetColumn(3);
            part.transform.localRotation = pose.rotation;
            part.transform.localScale = pose.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            MeshRenderer drawn = part.AddComponent<MeshRenderer>();
            drawn.sharedMaterials = System.Array.ConvertAll(source.sharedMaterials, Glass);
            (drawn.shadowCastingMode, drawn.receiveShadows) = (ShadowCastingMode.Off, false);
        }

        // The see-through look of a material that has a main texture; a plain see-through one otherwise.
        private static Material Glass(Material material)
        {
            Material? solid = material != null && material.HasProperty("_MainTex") ? material : null;
            return KwPreview.Glass(solid ?? Plain()) ?? Plain();
        }

        private static Material? plain;

        private static Material Plain()
        {
            if (plain != null)
                return plain;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            plain = new Material(shader) { color = new Color(1f, 1f, 1f, 0.4f), renderQueue = 3000 };
            return plain;
        }
    }
}
