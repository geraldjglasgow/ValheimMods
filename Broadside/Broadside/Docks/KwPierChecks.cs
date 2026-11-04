using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The game's building rules for a planned dock (<see cref="KwPierPlan"/>), at every station along its centre line: not
    /// in a no-build place (<c>Location.IsInsideNoBuildLocation</c>), and every ward there one the player may use
    /// (<c>PrivateArea.CheckAccess</c>, flashing as the game's do on the click only); and no other dock section in the way
    /// (its deck's box, a little short of each end so a dock it starts on never counts), and no dock ladder hanging where its
    /// deck would be (a branch or an extension built over one). The workbench in range is the game's own check before the
    /// click (the tool's station). On the placing player's game.
    /// </summary>
    public static class KwPierChecks
    {
        private const float Shy = 0.2f;
        private static readonly Collider[] touching = new Collider[64];
        private static readonly int Pieces = LayerMask.GetMask("piece");

        public static void Run(KwPierPlan plan, bool flash)
        {
            for (int i = 0; i < plan.Sections.Count && plan.Why == null; i++)
            {
                KwPierSpec spec = plan.Sections[i];
                for (int j = 0; j < spec.Stations && plan.Why == null; j++)
                    Place(plan, plan.SectionAt(i) + plan.Way * spec.StationZ(j), flash);
                Clear(plan, i);
            }
            Ladders(plan);
        }

        // No dock ladder's top inside the planned deck (a little past its sides and far end too).
        private static void Ladders(KwPierPlan plan)
        {
            foreach (KwPierLadder ladder in KwPierLadder.Loaded)
            {
                if (plan.Why != null || ladder == null)
                    return;
                Vector3 d = ladder.transform.position - plan.At;
                float along = Vector3.Dot(d, plan.Way), across = Vector3.Dot(d, plan.Right);
                if (along > -0.05f && along < plan.Length + Shy && Mathf.Abs(across) < plan.Width * 0.5f + Shy && Mathf.Abs(d.y) < 0.3f)
                    plan.Refuse("A dock ladder is in the way (the hammer takes it down)");
            }
        }

        private static void Place(KwPierPlan plan, Vector3 point, bool flash)
        {
            if (Location.IsInsideNoBuildLocation(point))
                plan.Refuse("$msg_nobuildzone");
            else if (!PrivateArea.CheckAccess(point, 0f, flash, false))
                plan.Refuse("$msg_privatezone");
        }

        // No other dock section's colliders inside the section's deck.
        private static void Clear(KwPierPlan plan, int section)
        {
            if (plan.Why != null)
                return;
            float each = plan.Sections[section].Length, cap = -plan.Kind.CapFoot;
            Vector3 middle = plan.SectionAt(section) + plan.Way * (each * 0.5f) + Vector3.up * (cap * -0.5f);
            Vector3 half = new Vector3(plan.Width * 0.5f - 0.05f, cap * 0.5f, Mathf.Max(0.05f, each * 0.5f - Shy));
            int count = Physics.OverlapBoxNonAlloc(middle, half, touching, plan.Turn, Pieces);
            for (int k = 0; k < count; k++)
            {
                if (touching[k] != null && touching[k].GetComponentInParent<KwPier>() != null)
                {
                    plan.Refuse("Another dock is in the way");
                    return;
                }
            }
        }
    }
}
