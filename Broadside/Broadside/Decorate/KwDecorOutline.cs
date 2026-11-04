using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The area's square drawn on what is there (the ground, a floor, the water's surface), 12 cm over it, as one line
    /// loop: pale while aiming, warm while a locked area is read and its layout worked out, green once the layout shows,
    /// red when nothing fits. Laid again only when the square moves, turns, grows or changes colour (each of its points
    /// one ray down: 16 a side, up to 32 on a large area, about 2 m apart). Only drawn, on this player's screen.
    /// </summary>
    public static class KwDecorOutline
    {
        private const int FewestPerSide = 16, MostPerSide = 32;
        private const float Width = 0.07f, Widen = 0.004f, Over = 0.12f, Above = 10f, Down = 30f;
        public static readonly Color Aiming = new Color(1f, 1f, 1f, 0.7f), Working = new Color(1f, 0.8f, 0.3f, 0.85f);
        public static readonly Color Showing = new Color(0.45f, 1f, 0.5f, 0.85f), Refused = new Color(1f, 0.35f, 0.3f, 0.8f);
        private static LineRenderer? line;
        private static (Vector3 Centre, float Yaw, float Half, Color Colour) drawn;

        public static void Show(Vector3 centre, float yaw, float half, Color colour)
        {
            LineRenderer l = Line();
            l.enabled = true;
            int perSide = Mathf.Clamp(Mathf.CeilToInt(half), FewestPerSide, MostPerSide);
            if (l.positionCount == perSide * 4 && drawn == (centre, yaw, half, colour))
                return;
            drawn = (centre, yaw, half, colour);
            (l.startColor, l.endColor, l.widthMultiplier) = (colour, colour, Width + half * Widen);
            l.positionCount = perSide * 4;
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < perSide * 4; i++)
                l.SetPosition(i, Surface(centre + turn * Corner(i, half, perSide), centre.y));
        }

        public static void Hide()
        {
            if (line != null)
                line.enabled = false;
        }

        // The i-th point round the square, counter-clockwise from its back left corner.
        private static Vector3 Corner(int i, float half, int perSide)
        {
            int side = i / perSide;
            float t = (i % perSide) / (float)perSide * 2f - 1f;
            return side switch
            {
                0 => new Vector3(t * half, 0f, -half),
                1 => new Vector3(half, 0f, t * half),
                2 => new Vector3(-t * half, 0f, half),
                _ => new Vector3(-half, 0f, -t * half),
            };
        }

        // What is there: the surface a ray down meets, or the water over it, whichever is higher.
        private static Vector3 Surface(Vector3 at, float near)
        {
            float y = near;
            if (Physics.Raycast(new Vector3(at.x, near + Above, at.z), Vector3.down, out RaycastHit hit, Down, KwDecorSurface.RayMask, QueryTriggerInteraction.Ignore))
                y = Mathf.Max(hit.point.y, Floating.GetLiquidLevel(hit.point, 0f));
            return new Vector3(at.x, y + Over, at.z);
        }

        private static LineRenderer Line()
        {
            if (line != null)
                return line;
            GameObject holder = new GameObject("BS_decor_outline");
            Object.DontDestroyOnLoad(holder);
            line = holder.AddComponent<LineRenderer>();
            (line.loop, line.useWorldSpace, line.widthMultiplier, line.numCapVertices) = (true, true, Width, 2);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            line.sharedMaterial = new Material(shader) { renderQueue = 3100 };
            return line;
        }
    }
}
