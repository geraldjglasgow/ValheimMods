using System.Collections.Generic;
using System.Linq;
using DevBridge.Capture;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Studio
{
    /// <summary>How the booth shows a model: yaw degrees turned to the right, pitch tilting its top towards the viewer, zoom (1 fits it), pixels square.</summary>
    internal struct BoothView
    {
        internal float Yaw;
        internal float Pitch;
        internal float Zoom;
        internal int Size;
        internal string Look; // a workshop model's dress (BoothLook); null keeps its materials
    }

    /// <summary>
    /// Pictures of one prefab by itself for the studio page: a still local copy (<see cref="LocalCopy.Still"/>, never
    /// networked) in a slot of its own deep under the world, on a layer the game names nothing, seen by a camera of the
    /// booth's own that draws that layer only and renders on request, under the booth's light (<see cref="BoothLight"/>).
    /// Nothing shows in the game's own view. The dozen copies used last stay for the next picture; leaving the world
    /// removes the booth.
    /// </summary>
    internal static class StudioBooth
    {
        internal const int Smallest = 32;
        internal const int Largest = 2048;
        private const int Kept = 12;
        private const float Spacing = 40f;
        private static readonly Vector3 Origin = new Vector3(0f, -4000f, 0f);
        private static readonly Color Background = new Color32(0x22, 0x25, 0x2d, 0xff);

        private sealed class Model
        {
            internal GameObject Source; // gone when its bundle was reloaded or unloaded
            internal string Look;
            internal GameObject Copy;
            internal List<Material> Made; // the dressed materials, destroyed with the copy
            internal int Slot;
            internal Vector3 Offset; // from its pivot to the middle of its meshes, unturned
            internal float Radius;
            internal Quaternion Rest;
            internal float Distance;
        }

        private static GameObject root;
        private static Camera lens;
        private static int layer;
        private static readonly List<Model> Models = new List<Model>(); // the latest last

        /// <summary>A JPG of the prefab as the view asks.</summary>
        internal static byte[] Render(GameObject prefab, BoothView view)
        {
            FrameGrab.RequireGraphics();
            Ensure();
            Model model = Get(prefab, view.Look);
            Vector3 middle = Place(model.Slot);
            Quaternion turn = Quaternion.AngleAxis(-view.Pitch, Vector3.right) * Quaternion.AngleAxis(-view.Yaw, Vector3.up) * model.Rest;
            model.Copy.transform.SetPositionAndRotation(middle - turn * model.Offset, turn);
            Aim(middle, model, view.Zoom);
            return Picture(Mathf.Clamp(view.Size, Smallest, Largest));
        }

        private static void Ensure()
        {
            if (root) return;
            if (!ZNetScene.instance || !Player.m_localPlayer) throw new BridgeException("no world loaded: the booth draws in a world");
            foreach (Model gone in Models) gone.Made.ForEach(Object.Destroy); // the copies went with the old booth
            Models.Clear();
            layer = FreeLayer();
            root = new GameObject("DevBridge_StudioBooth");
            root.transform.position = Origin;
            lens = Lens(root.transform);
            BoothLight.Make(root.transform, layer);
        }

        // A layer the game gives no name, so nothing of the game's is on it.
        private static int FreeLayer()
        {
            for (int candidate = 31; candidate >= 3; candidate--)
                if (LayerMask.LayerToName(candidate).Length == 0) return candidate;
            return 30;
        }

        private static Camera Lens(Transform parent)
        {
            Camera camera = new GameObject("camera").AddComponent<Camera>();
            camera.transform.SetParent(parent, false);
            camera.enabled = false;
            (camera.clearFlags, camera.backgroundColor, camera.cullingMask) = (CameraClearFlags.SolidColor, Background, 1 << layer);
            (camera.renderingPath, camera.allowHDR, camera.allowMSAA) = (RenderingPath.Forward, false, true);
            camera.useOcclusionCulling = false;
            camera.fieldOfView = BoothPose.Fov;
            return camera;
        }

        private static Model Get(GameObject prefab, string look)
        {
            Prune();
            Model model = Models.FirstOrDefault(m => m.Source == prefab && m.Look == look);
            if (model != null) Models.Remove(model);
            else model = Make(prefab, look, FreeSlot());
            Models.Add(model);
            return model;
        }

        // A copy whose prefab is gone (its bundle reloaded or unloaded) lost its meshes and materials with it.
        private static void Prune()
        {
            foreach (Model stale in Models.Where(m => !m.Copy || !m.Source)) Drop(stale);
            Models.RemoveAll(m => !m.Copy || !m.Source);
        }

        // The slot no copy holds, or the one used longest ago, its copy gone now.
        private static int FreeSlot()
        {
            if (Models.Count < Kept) return Enumerable.Range(0, Kept).First(slot => Models.All(m => m.Slot != slot));
            Model oldest = Models[0];
            Models.RemoveAt(0);
            Drop(oldest);
            return oldest.Slot;
        }

        // Switched off first: Destroy waits for the frame's end.
        private static void Drop(Model model)
        {
            if (model.Copy)
            {
                model.Copy.SetActive(false);
                Object.Destroy(model.Copy);
            }
            model.Made.ForEach(Object.Destroy);
        }

        private static Model Make(GameObject prefab, string look, int slot)
        {
            GameObject copy = LocalCopy.Still(prefab, Place(slot), Quaternion.identity);
            copy.transform.SetParent(root.transform, true);
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = layer;
            List<Material> made = BoothLook.Apply(copy, look);
            BoothParticles.Hold(copy);
            Bounds box = BoothParticles.MeshesDrawn(copy) ? AssetInfo.Bounds(copy) : BoothParticles.Bounds(copy) ?? AssetInfo.Bounds(copy);
            Quaternion rest = BoothPose.Standing(prefab) ? BoothPose.Facing : BoothPose.Rest(box.extents);
            return new Model
            {
                Source = prefab, Look = look, Copy = copy, Made = made, Slot = slot, Offset = box.center - copy.transform.position,
                Radius = box.extents.magnitude, Rest = rest, Distance = BoothPose.Distance(box.extents, rest),
            };
        }

        private static Vector3 Place(int slot) => Origin + Vector3.right * (Spacing * (slot + 1));

        private static void Aim(Vector3 middle, Model model, float zoom)
        {
            float distance = model.Distance / Mathf.Clamp(zoom, 0.2f, 8f);
            lens.transform.SetPositionAndRotation(middle - Vector3.forward * distance, Quaternion.identity);
            lens.nearClipPlane = Mathf.Max(0.01f, distance - model.Radius * 1.5f);
            lens.farClipPlane = distance + model.Radius * 1.5f + 0.1f;
        }

        // Drawn with 4x multisampling, resolved, read back and encoded.
        private static byte[] Picture(int size)
        {
            RenderTexture drawn = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture plain = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(size, size, TextureFormat.RGB24, false);
            try
            {
                lens.targetTexture = drawn;
                BoothLight.During(lens.Render);
                Graphics.Blit(drawn, plain);
                Read(plain, image);
                return image.EncodeToJPG(90);
            }
            finally
            {
                lens.targetTexture = null;
                RenderTexture.ReleaseTemporary(drawn);
                RenderTexture.ReleaseTemporary(plain);
                Object.Destroy(image);
            }
        }

        private static void Read(RenderTexture source, Texture2D into)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;
            into.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            into.Apply(false);
            RenderTexture.active = previous;
        }
    }
}
