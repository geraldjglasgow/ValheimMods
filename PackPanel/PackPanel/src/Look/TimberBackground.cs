using System.Collections.Generic;
using PackPanel.Ring;
using UnityEngine.UI;
using UnityEngine;

namespace PackPanel.Look
{
    /// <summary>
    /// A panel-sized window onto canvas-aligned wood. The fill and softly chipped bevel form one mesh;
    /// transparent silhouette cuts affect only this surface, never the inventory's controls or icons.
    /// </summary>
    public sealed class TimberBackground : MaskableGraphic
    {
        private const string Name = "PackPanel_timberwood";
        private static readonly Color PanelTint = new Color(0.92f, 0.92f, 0.92f, 1f);
        private Image frame;
        private Sprite source;
        private bool originalFill;
        private Sprite originalSprite;
        private Material originalMaterial;
        private Image.Type originalType;
        private Color originalColor;
        private bool round;
        private readonly List<Vector2> contour = new List<Vector2>();
        private readonly List<Vector2> normals = new List<Vector2>();
        private readonly List<Vector2> interior = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();
        private Matrix4x4 lastMatrix;
        private Rect lastCanvas;
        private int previewVersion = -1;
        private int frameVersion = -1;
        private Rect geometryRect;
        private bool geometryRound;
        private int geometryVersion = -1;
        private float geometryWidth;

        public override Texture mainTexture => BackgroundPreview.Texture != null ? BackgroundPreview.Texture
            : source != null ? source.texture : base.mainTexture;

        public static void Apply(Image image, bool on)
        {
            Transform child = image.transform.Find(Name);
            TimberBackground wood = child != null ? child.GetComponent<TimberBackground>() : null;
            if (!on)
            {
                if (wood != null && wood.gameObject.activeSelf)
                {
                    image.fillCenter = wood.originalFill;
                    image.sprite = wood.originalSprite;
                    image.material = wood.originalMaterial;
                    image.type = wood.originalType;
                    image.color = wood.originalColor;
                    wood.gameObject.SetActive(false);
                }
                return;
            }
            if (wood != null && wood.gameObject.activeSelf && image.color == Color.clear
                && wood.source == SkinArt.Wallpaper && wood.color == PanelTint)
                return;
            if (wood == null)
            {
                GameObject go = new GameObject(Name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TimberBackground));
                go.transform.SetParent(image.transform, false);
                go.transform.SetAsFirstSibling();
                wood = go.GetComponent<TimberBackground>();
                wood.frame = image;
                wood.raycastTarget = false;
                go.SetActive(false);
            }
            if (!wood.gameObject.activeSelf)
            {
                wood.originalFill = image.fillCenter;
                wood.originalSprite = image.sprite;
                wood.originalMaterial = image.material;
                wood.originalType = image.type;
                wood.originalColor = image.color;
            }
            wood.round = image.name == KeyRingPopup.Name;
            Transform previousRim = image.transform.Find("PackPanel_frame");
            if (previousRim != null) previousRim.gameObject.SetActive(false);
            wood.frameVersion = TimberFrame.Version;
            // The original Image remains a raycast surface, but none of its native art is rendered.
            image.material = null;
            image.color = Color.clear;
            image.fillCenter = false;
            // The background artwork and the game's frame are independent textures.
            // In particular, native frame sprites live in an atlas and must not be sampled as wallpaper.
            wood.source = SkinArt.Wallpaper;
            wood.color = PanelTint;
            wood.gameObject.SetActive(true);
            wood.Inset();
            wood.SetAllDirty();
        }

        private RectTransform Root => canvas != null ? canvas.rootCanvas.transform as RectTransform : null;

        private void Inset()
        {
            if (frame == null)
                return;
            RectTransform rect = rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void LateUpdate()
        {
            TimberFrame.Poll();
            if (frameVersion != TimberFrame.Version)
            {
                frameVersion = TimberFrame.Version;
                Inset();
                SetAllDirty();
            }
            BackgroundPreview.Poll();
            if (previewVersion != BackgroundPreview.Version)
            {
                previewVersion = BackgroundPreview.Version;
                SetAllDirty();
            }
            RectTransform root = Root;
            if (root == null || frame == null)
                return;
            Matrix4x4 matrix = root.worldToLocalMatrix * rectTransform.localToWorldMatrix;
            // Moving a panel changes the portion of the wallpaper it reveals, even if its size stays the same.
            if (matrix != lastMatrix || root.rect != lastCanvas)
                SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            RectTransform root = Root;
            if (source == null || root == null)
                return;
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;
            lastMatrix = root.worldToLocalMatrix * rectTransform.localToWorldMatrix;
            lastCanvas = root.rect;
            Vector4 border = source.border;
            Rect area = source.rect;
            Rect wood = Rect.MinMaxRect(area.xMin + border.x, area.yMin + border.y,
                area.xMax - border.z, area.yMax - border.w);
            Texture2D preview = BackgroundPreview.Texture;
            if (preview != null)
                wood = new Rect(0f, 0f, preview.width, preview.height);
            Texture texture = mainTexture;
            Vector2 textureSize = new Vector2(texture.width, texture.height);
            WoodPanel(mesh, rect, wood, textureSize);
        }

        // The bevel and fill share exactly the same vertices and wallpaper UVs. There can be no
        // transparent gap between a separate rim and the panel. Only this graphic is clipped.
        private void WoodPanel(VertexHelper mesh, Rect rect, Rect wood, Vector2 textureSize)
        {
            // Opening, fading or moving the panel only changes visibility/UVs. Retain the expensive
            // concave triangulation across those operations, including disable/enable cycles.
            if (geometryVersion != TimberFrame.Version || geometryRect != rect || geometryRound != round)
            {
                TimberFrame.Contour(rect, round, contour, normals);
                geometryWidth = TimberFrame.BevelWidth(rect, round);
                interior.Clear();
                for (int i = 0; i < contour.Count; i++)
                    interior.Add(contour[i] - normals[i] * geometryWidth);
                TimberFrame.Triangulate(interior, triangles);
                geometryRect = rect;
                geometryRound = round;
                geometryVersion = TimberFrame.Version;
            }
            int count = contour.Count;
            float width = geometryWidth;
            for (int ring = 0; ring < 4; ring++)
            {
                float depth = ring == 0 ? 0f : ring == 1 ? Mathf.Min(0.6f, width * 0.2f)
                    : ring == 2 ? Mathf.Min(1.4f, width * 0.45f) : width;
                for (int i = 0; i < count; i++)
                {
                    Vector2 n = normals[i];
                    float light = Mathf.Clamp(n.y - n.x * 0.35f, -1f, 1f);
                    float shade = ring == 0 ? 0.7f : ring == 1 ? 0.80f + light * 0.12f
                        : ring == 2 ? 0.96f + light * 0.13f : 1f;
                    Color tint = color * new Color(shade, shade, shade, ring == 0 ? 0f : 1f);
                    Vertex(mesh, contour[i] - n * depth, wood, textureSize, tint);
                }
            }
            for (int ring = 0; ring < 3; ring++)
                for (int i = 0; i < count; i++)
                {
                    int next = (i + 1) % count;
                    int outer = ring * count, inner = outer + count;
                    mesh.AddTriangle(outer + i, outer + next, inner + next);
                    mesh.AddTriangle(outer + i, inner + next, inner + i);
                }
            for (int i = 0; i < triangles.Count; i += 3)
                mesh.AddTriangle(3 * count + triangles[i], 3 * count + triangles[i + 1], 3 * count + triangles[i + 2]);
        }
        private void Vertex(VertexHelper mesh, Vector2 point, Rect wood, Vector2 textureSize, Color tint)
        {
            Vector2 canvasPoint = lastMatrix.MultiplyPoint3x4(point);
            mesh.AddVert(point, tint, TimberCoordinates.Uv(canvasPoint, lastCanvas, wood, textureSize));
        }
    }
}

