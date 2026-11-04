using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// The hammock's placing preview: the planned hammock (<see cref="Whole"/>: posts, hooks, bars, ropes and cloth, as it will
    /// be built) or one end alone (<see cref="End"/>), in one see-through mesh in the plank wood's texture (the kit's
    /// see-through look, <see cref="KwPreview.Glass"/>), tinted red while it is refused; where the ship is drawn aboard. Its mesh
    /// is made again only when what it shows changes; it is moved every frame. Local to the player's screen, never networked,
    /// casting no shadow.
    /// </summary>
    public static class KwHammockView
    {
        private static readonly Color Refused = new Color(1f, 0.32f, 0.25f, 0.45f);
        private static readonly Dictionary<Material, Material> red = new Dictionary<Material, Material>();
        private static readonly Vector3 Up = new Vector3(0f, 0f, 1f);
        private static GameObject? shown;
        private static Mesh? mesh;
        private static string drawn = "";

        /// <summary>The planned hammock where it would stand.</summary>
        public static void Whole(KwHammockPlan plan)
        {
            KwHammockSpec spec = plan.Spec!;
            (Vector3 at, Quaternion turn) = plan.Drawn;
            Show("whole|" + spec + "|" + (plan.Why != null), at, turn, plan.Why != null, () => WholeMesh(spec));
        }

        /// <summary>One end alone where it would go: a post standing there, or a hook on the surface.</summary>
        public static void End(KwHammockClick end)
        {
            Vector3 up = end.Up, face = end.Face, look = GameCamera.instance != null ? GameCamera.instance.transform.forward : Vector3.forward;
            Vector3 way = KwHammockRules.Flat(end.Post ? look : face, up);
            if (way.sqrMagnitude < 0.01f)
                way = KwHammockRules.Flat(look, up);
            Quaternion turn = Quaternion.LookRotation(way.sqrMagnitude > 1e-6f ? way.normalized : Vector3.forward, up);
            KwHammockMount mount = Mount(end.Post, KwHammockShape.Kit(Quaternion.Inverse(turn) * face).normalized);
            (Vector3 at, Quaternion drawnTurn) = KwHammockAim.Drawn(end.Ship, end.At, turn);
            string key = end.Post ? "post" : $"hook|{mount.Face.x:F2},{mount.Face.y:F2},{mount.Face.z:F2}";
            Show(key, at, drawnTurn, false, () => EndMesh(mount));
        }

        public static void Hide()
        {
            if (shown != null && shown.activeSelf)
                shown.SetActive(false);
        }

        // An end at the frame's origin: a post with its peg toward +x at the standard height, or a hook out of its face.
        private static KwHammockMount Mount(bool post, Vector3 face) => post
            ? new KwHammockMount(true, new Vector3(KwHammockParts.PegIn, 0f, KwHammockRules.Hook), Vector3.zero, Up)
            : new KwHammockMount(false, KwHammockParts.HookRope(Vector3.zero, face, Up), Vector3.zero, face);

        private static Mesh WholeMesh(KwHammockSpec spec)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            KwHammockParts.Posts(b, spec);
            KwHammockParts.Hooks(b, spec);
            KwHammockShape.Preview(b, new KwHammockSling(spec));
            return b.ToMesh("bs_hammock_preview");
        }

        private static Mesh EndMesh(KwHammockMount mount)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            if (mount.Post)
                KwHammockParts.Post(b, mount);
            else
                KwHammockParts.Hook(b, mount);
            return b.ToMesh("bs_hammock_end_preview");
        }

        // The preview placed, its mesh made again only when what it shows (key) changed.
        private static void Show(string key, Vector3 at, Quaternion turn, bool refused, System.Func<Mesh> build)
        {
            GameObject view = View();
            view.SetActive(true);
            view.transform.SetPositionAndRotation(at, turn);
            if (key == drawn)
                return;
            drawn = key;
            if (mesh != null)
                Object.Destroy(mesh);
            mesh = build();
            view.GetComponent<MeshFilter>().sharedMesh = mesh;
            view.GetComponent<MeshRenderer>().sharedMaterial = Glass(KwPrefabs.Planks, refused);
        }

        private static GameObject View()
        {
            if (shown != null)
                return shown;
            // a new one (the old went with the world's scene) shows nothing drawn yet
            (shown, drawn) = (new GameObject("KwHammockPreview"), "");
            shown.AddComponent<MeshFilter>();
            MeshRenderer renderer = shown.AddComponent<MeshRenderer>();
            (renderer.shadowCastingMode, renderer.receiveShadows) = (ShadowCastingMode.Off, false);
            return shown;
        }

        // The see-through look, or a red copy of it.
        private static Material? Glass(Material? solid, bool refused)
        {
            Material? clear = KwPreview.Glass(solid);
            if (!refused || clear == null)
                return clear;
            if (!red.TryGetValue(clear, out Material tinted))
                red[clear] = tinted = new Material(clear) { color = Refused };
            return tinted;
        }
    }
}
