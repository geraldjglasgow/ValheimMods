using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Who a heal goes to (user, 2026-10-09: "you dont' have to have cursor directly on them, just kinda in a bubble
    /// around them"): of the friends within <see cref="Range"/> of the caster (other players, tamed creatures) with nothing
    /// solid between, the one whose bubble the crosshair is most inside. A friend's bubble is a sphere of
    /// <see cref="Bubble"/> metres round their middle as seen from the camera: wide up close, never narrower than
    /// <see cref="MinCone"/> or wider than <see cref="MaxCone"/> degrees. The friend already picked keeps the pick until
    /// the crosshair leaves a slightly larger bubble (<see cref="Keep"/>), so two friends side by side do not flicker. The
    /// body need not face them: the cast turns the caster to them (<see cref="CastTurn"/>).
    /// None when there is none (the caster heals themselves).
    /// </summary>
    public static class FriendTarget
    {
        public const float Range = 30f;
        private const float Bubble = 1.5f;
        private const float MinCone = 4f;
        private const float MaxCone = 30f;
        private const float Keep = 1.3f;

        private static readonly List<Character> near = new List<Character>();
        private static int blockMask;

        public static bool Friendly(Character character, Player caster) =>
            character != null && !character.IsDead() && (character == caster || character.IsPlayer() || character.IsTamed());

        public static Character Find(Player caster, Character current)
        {
            Transform eye = GameCamera.instance != null ? GameCamera.instance.transform : null;
            if (eye == null)
                return null;
            Character best = null;
            float bestScore = 1f;
            near.Clear();
            Character.GetCharactersInRange(caster.transform.position, Range, near);
            foreach (Character character in near)
            {
                if (character == caster || !Friendly(character, caster))
                    continue;
                float score = Score(eye, character) / (character == current ? Keep : 1f);
                if (score <= bestScore && InSight(eye.position, character.GetCenterPoint()))
                {
                    best = character;
                    bestScore = score;
                }
            }
            near.Clear();
            return best;
        }

        /// <summary>Whether a locked friend can still be healed: alive, a friend, in reach.</summary>
        public static bool StillValid(Character target, Player caster) =>
            target != null && Friendly(target, caster) && Vector3.Distance(target.transform.position, caster.transform.position) <= Range + 5f;

        /// <summary>The crosshair's angle off the friend's middle over their bubble's: 1 is the bubble's edge.</summary>
        private static float Score(Transform eye, Character character)
        {
            Vector3 toward = character.GetCenterPoint() - eye.position;
            float distance = Mathf.Max(toward.magnitude, 0.1f);
            float cone = Mathf.Clamp(Mathf.Atan(Bubble / distance) * Mathf.Rad2Deg, MinCone, MaxCone);
            return Vector3.Angle(eye.forward, toward) / cone;
        }

        private static bool InSight(Vector3 from, Vector3 to)
        {
            if (blockMask == 0)
                blockMask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
            return !Physics.Linecast(from, to, blockMask, QueryTriggerInteraction.Ignore);
        }
    }
}
