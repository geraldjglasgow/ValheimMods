using System;
using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The raw hide sail's look (<see cref="KwSailCloth"/>): the game's own hide sail, the one the raft's sail wears (its
    /// <c>sail_hide</c> texture and normal map), copied at runtime and never shipped, as the doors wear the game's door
    /// wood. The raft shows nearly the whole hide on a 5.6 by 3.6 m sail, its ragged outline, light rim and lacing holes
    /// with it; a Broadside sail hangs from a yard, a boom or a stay with a bolt rope round its free edges, so it shows
    /// the hide's middle (<see cref="Window"/>, the largest part with no hole or edge in it), whole and opaque, as one even
    /// hide. That middle is drawn out to the same 256 texels the other cloths have, so it spans a sail as theirs do, a
    /// jib's triangle is cut from it (<see cref="KwJibUv"/>) and a pattern is painted on it (<see cref="KwSailLooks"/>)
    /// as on them. A sail wears it worn (<see cref="KwSailWear"/>): holes, patches, stains and torn free edges painted over
    /// it for each sail's shape; this whole hide is what the wear starts from, and what a masthead pennant wears. Read back
    /// from the graphics card once a game (the game's textures are not readable); nothing on a dedicated server, which
    /// draws no sail.
    /// </summary>
    public static class KwSailHide
    {
        /// <summary>Marks the raw hide sail's texture in <see cref="KwSailCloth"/>'s list: the game's hide, not an embedded file.</summary>
        public const string Game = "game:sail_hide";

        /// <summary>The copies' size, the other cloths' and the painted patterns' (<see cref="KwSailLooks.Size"/>).</summary>
        public const int Size = KwSailLooks.Size;

        private const string Hide = "sail_hide", Ship = "Raft";

        /// <summary>
        /// The hide's middle in its texture, in shares of it (x from the left, y from the bottom): texels 32 to 225 across
        /// and 74 to 235 up of the game's 256, inside its outline and clear of its six lacing holes (alpha 210 and more),
        /// as wide for its height as a 6 by 5 m sail.
        /// </summary>
        public static readonly Rect Window = Rect.MinMaxRect(32f / 256f, 74f / 256f, 226f / 256f, 236f / 256f);

        private static Texture2D? albedo, relief;

        /// <summary>The raft's sail material: the one wearing the game's hide; null if the game has none.</summary>
        public static Material? Raft(ZNetScene scene)
        {
            GameObject? raft = scene.GetPrefab(Ship);
            foreach (Renderer renderer in raft != null ? raft.GetComponentsInChildren<Renderer>(true) : new Renderer[0])
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.mainTexture != null && material.mainTexture.name.StartsWith(Hide))
                        return material;
                }
            }
            return null;
        }

        /// <summary>
        /// A copy of the linen sail dressed as the raft's hide: the hide's middle and its relief, the raft's gloss and bump
        /// strength; false (left as it was) where the hide cannot be read, on a dedicated server or if reading it fails.
        /// </summary>
        public static bool Dress(Material made, Material raft)
        {
            if (!Copied(raft))
                return false;
            made.mainTexture = albedo;
            made.color = Color.white;
            if (relief != null && made.HasProperty("_BumpMap"))
            {
                made.SetTexture("_BumpMap", relief);
                made.SetFloat("_BumpScale", Number(raft, "_BumpScale", 1f));
            }
            if (made.HasProperty("_Glossiness"))
                made.SetFloat("_Glossiness", Number(raft, "_Glossiness", 0.1f));
            return true;
        }

        // The hide's copies, made the first time and kept for the game (the raft's textures do not change); said once if it fails.
        private static bool Copied(Material raft)
        {
            if (albedo != null)
                return true;
            try
            {
                albedo = Copy(raft.mainTexture, false);
                relief = albedo != null && raft.HasProperty("_BumpMap") ? Copy(raft.GetTexture("_BumpMap"), true) : null;
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the raft's hide could not be read ({e.Message}); the raw hide sail is tinted linen.");
                albedo = null;
            }
            return albedo != null;
        }

        // A texture's middle as a texture of our own, point filtered as the game's and kept readable for the patterns:
        // colours read as colours, a normal map as stored (its texels are not colours); null where nothing is drawn.
        private static Texture2D? Copy(Texture? source, bool normal)
        {
            if (source == null)
                return null;
            Color32[]? pixels = normal ? TexturePixels.ReadLinear(source, out int width, out int height) : TexturePixels.Read(source, out width, out height);
            if (pixels == null)
                return null;
            Texture2D made = new Texture2D(Size, Size, TextureFormat.RGBA32, true, normal)
            {
                name = "broadside_sail_rawhide" + (normal ? "_n" : ""), filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            };
            made.SetPixels32(Middle(pixels, width, height, !normal));
            made.Apply(true, false);
            return made;
        }

        /// <summary>
        /// The <see cref="Window"/> of a texture's pixels (width by height, bottom row first) drawn out to <see cref="Size"/>
        /// square, each texel between the four under it, never one outside the window; opaque makes every texel whole (a
        /// normal map keeps its alpha, which holds its slope). Plain arithmetic, the same on every machine.
        /// </summary>
        public static Color32[] Middle(Color32[] pixels, int width, int height, bool opaque)
        {
            Color32[] made = new Color32[Size * Size];
            int x0 = Mathf.RoundToInt(Window.xMin * width), y0 = Mathf.RoundToInt(Window.yMin * height);
            RectInt inside = new RectInt(x0, y0, Mathf.RoundToInt(Window.xMax * width) - x0, Mathf.RoundToInt(Window.yMax * height) - y0);
            for (int j = 0; j < Size; j++)
            {
                for (int i = 0; i < Size; i++)
                {
                    float x = inside.xMin + (i + 0.5f) / Size * inside.width - 0.5f, y = inside.yMin + (j + 0.5f) / Size * inside.height - 0.5f;
                    Color32 c = Between(pixels, width, inside, x, y);
                    made[j * Size + i] = opaque ? new Color32(c.r, c.g, c.b, 255) : c;
                }
            }
            return made;
        }

        // The colour at a place in texels (texel centres on whole numbers) between the four texels round it, kept inside.
        private static Color32 Between(Color32[] pixels, int width, RectInt inside, float x, float y)
        {
            x = Mathf.Clamp(x, inside.xMin, inside.xMax - 1);
            y = Mathf.Clamp(y, inside.yMin, inside.yMax - 1);
            int i = (int)x, j = (int)y, i1 = Mathf.Min(i + 1, inside.xMax - 1), j1 = Mathf.Min(j + 1, inside.yMax - 1);
            float tx = x - i, ty = y - j;
            Color low = Color.Lerp(pixels[j * width + i], pixels[j * width + i1], tx);
            Color high = Color.Lerp(pixels[j1 * width + i], pixels[j1 * width + i1], tx);
            return Color.Lerp(low, high, ty);
        }

        private static float Number(Material material, string name, float otherwise) => material.HasProperty(name) ? material.GetFloat(name) : otherwise;
    }
}
