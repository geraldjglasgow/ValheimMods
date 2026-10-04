using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// ai: what each creature's BaseAI can sense, as BaseAI.CanSeeTarget and CanHearTarget test it: the view range as a
    /// wedge of the view angle either side of its facing (a full circle while alerted, when the angle is not checked),
    /// the hearing range as a fainter ring (12 m at most indoors; none drawn for the game's unlimited 9999), cyan while
    /// calm and red while alerted, and a magenta line from its eyes to the creature or building it targets. The target is
    /// known only where the AI runs (the creature's owner); have_target counts the ones the network says have one.
    /// </summary>
    internal static class AiDrawer
    {
        private const float Unlimited = 1000f;
        private static readonly Color Calm = new Color(0.3f, 0.9f, 1f, 0.85f);
        private static readonly Color Alerted = new Color(1f, 0.25f, 0.2f, 0.9f);
        private static readonly Color Target = new Color(1f, 0.3f, 1f, 0.95f);

        internal static void Draw(OverlayArea area, Category into)
        {
            foreach (BaseAI ai in BaseAI.GetAllInstances())
            {
                if (!ai || !ai.m_nview || !ai.m_nview.IsValid() || !area.Holds(ai.transform.position)) continue;
                bool alerted = ai.IsAlerted();
                into.Count(alerted ? "alerted" : "calm");
                if (ai.HaveTarget()) into.Count("have_target");
                if (!ai.m_nview.IsOwner()) into.Count("not_owner");
                Color colour = alerted ? Alerted : Calm;
                Vector3 feet = ai.transform.position + Vector3.up * 0.1f;
                if (ai.m_viewRange > 0f) into.Lines.Add(Sight(ai, feet, alerted), colour);
                Hearing(ai, feet, colour, into);
                TargetLine(ai, into);
            }
        }

        private static Vector3[] Sight(BaseAI ai, Vector3 feet, bool alerted) =>
            alerted || ai.m_viewAngle >= 180f ? Shapes.Ring(feet, ai.m_viewRange) : Shapes.Wedge(feet, ai.transform.forward, ai.m_viewAngle, ai.m_viewRange);

        private static void Hearing(BaseAI ai, Vector3 feet, Color colour, Category into)
        {
            float range = Character.InInterior(ai.transform) ? Mathf.Min(12f, ai.m_hearRange) : ai.m_hearRange;
            if (range >= Unlimited || range <= 0f) return;
            into.Lines.Add(Shapes.Ring(feet, range), new Color(colour.r, colour.g, colour.b, 0.35f), 0.04f);
        }

        private static void TargetLine(BaseAI ai, Category into)
        {
            Vector3? target = TargetPoint(ai);
            if (target == null) return;
            into.Count("targets");
            Character body = ai.m_character;
            Vector3 eye = body && body.m_eye ? body.m_eye.position : ai.transform.position;
            into.Lines.Add(new[] { eye, target.Value }, Target, 0.06f);
        }

        // MonsterAI's creature or building target, or the creature an AnimalAI flees from.
        private static Vector3? TargetPoint(BaseAI ai)
        {
            Character creature = ai.GetTargetCreature();
            if (!creature && ai is AnimalAI animal) creature = animal.m_target;
            if (creature) return creature.GetCenterPoint();
            StaticTarget building = ai is MonsterAI monster ? monster.GetStaticTarget() : null;
            return building ? building.GetCenter() : (Vector3?)null;
        }
    }
}
