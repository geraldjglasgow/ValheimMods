using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The camera that renders hammer icons, made once for a batch of them: orthographic, forward rendered with 8x
    /// multisampling at twice the icon's size, drawing only a layer the game gives no name, where a copy of the piece's
    /// drawn parts is set out far below the world (<see cref="KwIconSubject"/>). The view faces the subject's longer level
    /// side, turned 40 degrees from square toward its front and 30 degrees above (a ship from its starboard bow, its
    /// length across the picture), and is framed to the parts' own outline, centred, with a small margin. Each icon is
    /// drawn on black and on white under the icon's light (<see cref="KwIconLight"/>) and made into a sprite from the two
    /// (<see cref="KwIconPicture"/>). Clients only: the caller checks for a graphics device.
    /// </summary>
    public sealed class KwIconRig : IDisposable
    {
        public const int Size = 128;
        private const int Supersample = 2;
        private const float Margin = 1.12f, Turn = 40f, Above = 30f;
        private static readonly Vector3 Stage = new Vector3(0f, -10000f, 0f);

        private readonly GameObject root = new GameObject("KwIconRig");
        private readonly int layer = FreeLayer();
        private readonly Camera camera;
        private readonly KwIconLight light;
        private readonly RenderTexture drawn, plain;
        private readonly Texture2D read;

        public KwIconRig()
        {
            int samples = Size * Supersample;
            RenderTextureDescriptor described = new RenderTextureDescriptor(samples, samples, RenderTextureFormat.ARGB32, 24) { sRGB = true, msaaSamples = 8 };
            described.msaaSamples = Mathf.Max(1, SystemInfo.GetRenderTextureSupportedMSAASampleCount(described));
            drawn = new RenderTexture(described);
            plain = new RenderTexture(samples, samples, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            read = new Texture2D(samples, samples, TextureFormat.RGBA32, false);
            camera = Lens();
            light = new KwIconLight(root.transform, layer);
        }

        // The highest layer the game leaves unnamed, so nothing of the game's is on it.
        private static int FreeLayer()
        {
            for (int candidate = 31; candidate >= 8; candidate--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(candidate)))
                    return candidate;
            }
            return 31;
        }

        private Camera Lens()
        {
            Camera lens = root.AddComponent<Camera>();
            lens.enabled = false;
            lens.orthographic = true;
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.cullingMask = 1 << layer;
            lens.renderingPath = RenderingPath.Forward;
            lens.allowHDR = false;
            lens.allowMSAA = true;
            lens.useOcclusionCulling = false;
            lens.depthTextureMode = DepthTextureMode.None;
            lens.targetTexture = drawn;
            return lens;
        }

        /// <summary>
        /// An icon of every drawn part under a piece (or a saved ship's entry), named for it, and its pixels for the icon
        /// cache; null when nothing draws.
        /// </summary>
        public Sprite? Shoot(Transform piece, string name, out Color32[]? pixels)
        {
            pixels = null;
            using (KwIconSubject? subject = KwIconSubject.Make(piece, Stage, layer))
            {
                if (subject == null)
                    return null;
                Aim(subject);
                Color32[] black = Grab(Color.black), white = Grab(Color.white);
                return KwIconPicture.Sprite(black, white, Size * Supersample, Supersample, name, out pixels);
            }
        }

        // The view along the subject's longer side, framed to its outline: the camera moved across to the outline's
        // middle and far enough back that the whole box is in front of it.
        private void Aim(KwIconSubject subject)
        {
            Bounds box = subject.Box;
            Vector3 toCamera = View(box.size);
            Quaternion turn = Quaternion.LookRotation(-toCamera, Vector3.up);
            Rect seen = subject.Seen(turn);
            float reach = box.extents.magnitude + 1f;
            Vector3 middle = Stage + box.center + turn * new Vector3(seen.center.x, seen.center.y, 0f);
            camera.transform.SetPositionAndRotation(middle + toCamera * reach, turn);
            camera.orthographicSize = Mathf.Max(seen.width, seen.height, 0.02f) / 2f * Margin;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = reach * 2f + 1f;
            light.Face(turn);
        }

        /// <summary>From the subject to the camera: off the broadside of its longer level side toward its front, and above.</summary>
        public static Vector3 View(Vector3 size)
        {
            float yaw = (size.z >= size.x ? Turn : 90f - Turn) * Mathf.Deg2Rad, pitch = Above * Mathf.Deg2Rad;
            // yaw from +x (a ship's starboard) toward +z (its bow)
            return new Vector3(Mathf.Cos(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Sin(yaw) * Mathf.Cos(pitch));
        }

        // The render read back. The active target is taken before the blit, which leaves its own target active: taken
        // after, the read texture stayed active and Unity warned as the rig let it go.
        private Color32[] Grab(Color background)
        {
            RenderTexture previous = RenderTexture.active;
            camera.backgroundColor = background;
            light.During(camera.Render);
            Graphics.Blit(drawn, plain);
            RenderTexture.active = plain;
            read.ReadPixels(new Rect(0, 0, plain.width, plain.height), 0, 0);
            read.Apply(false);
            RenderTexture.active = previous;
            return read.GetPixels32();
        }

        public void Dispose()
        {
            camera.targetTexture = null;
            Object.DestroyImmediate(drawn);
            Object.DestroyImmediate(plain);
            Object.DestroyImmediate(read);
            Object.DestroyImmediate(root);
        }
    }
}
