using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// paths: each creature's navigation path as BaseAI holds it (m_path, the corners it still has to walk, from the
    /// last BaseAI.FindPath) as an orange line from its feet, and the point it last asked a path to as a ring, green when
    /// a path was found and red when not, as the game's own editor gizmo shows them. Paths older than ten seconds are left
    /// out (the AI asks again at least every five while it walks), and paths exist only where the AI runs (its owner).
    /// </summary>
    internal static class PathDrawer
    {
        private const float Fresh = 10f;
        private static readonly Color Path = new Color(1f, 0.6f, 0.1f, 0.95f);
        private static readonly Color Found = new Color(0.3f, 1f, 0.3f, 0.9f);
        private static readonly Color Failed = new Color(1f, 0.2f, 0.2f, 0.9f);

        internal static void Draw(OverlayArea area, Category into)
        {
            foreach (BaseAI ai in BaseAI.GetAllInstances())
            {
                if (!ai || !area.Holds(ai.transform.position)) continue;
                if (Time.time - ai.m_lastFindPathTime > Fresh)
                {
                    into.Count("idle");
                    continue;
                }
                into.Count(ai.m_lastFindPathResult ? "found" : "not_found");
                if (ai.m_path.Count > 0) into.Lines.Add(Route(ai), Path, 0.06f);
                Color goal = ai.m_lastFindPathResult ? Found : Failed;
                into.Lines.Add(Shapes.Ring(ai.m_lastFindPathTarget + Vector3.up * 0.1f, 0.4f), goal, 0.05f);
            }
        }

        private static Vector3[] Route(BaseAI ai)
        {
            var points = new Vector3[ai.m_path.Count + 1];
            points[0] = ai.transform.position + Vector3.up * 0.1f;
            for (int i = 0; i < ai.m_path.Count; i++) points[i + 1] = ai.m_path[i] + Vector3.up * 0.1f;
            return points;
        }
    }
}
