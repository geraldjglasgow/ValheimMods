using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hauler's hands on a ship cannon's two grips (its brackets' rear ends), on every peer that draws, after the
    /// animator has posed him (two-bone IK, the MannedWeapons library's <see cref="OperatorRig"/>, elbows square to his
    /// arms, as a gunner's), from where his character and the cannon are drawn here, so every screen shows the same; eased
    /// on in a moment and off as quickly. Each fist to the grip on its own side of him, whichever way he faces.
    /// </summary>
    public sealed class KwTowHands
    {
        // Easing on and off (a second's share).
        private const float Ease = 6f;

        private Player? puller;
        private OperatorRig? body;
        private float weight;

        public void Hold(KwArm arm, Transform? left, Transform? right)
        {
            Player? now = Puller(arm);
            if (now != null && now != puller)
                (puller, body, weight) = (now, OperatorRig.Of(now, ElbowHint.Square), 0f);
            float wanted = now == null || left == null || right == null ? 0f : 1f;
            weight = Mathf.MoveTowards(weight, wanted, Ease * Time.deltaTime);
            if (weight <= 0.001f && now == null)
            {
                (puller, body) = (null, null);
                return;
            }
            if (puller == null || body == null || left == null || right == null)
                return;
            Transform him = puller.transform;
            // each fist to the grip on its own side of him, whichever way he faces the cannon
            bool facing = Vector3.Dot(left.position - him.position, him.right) < 0f;
            (Vector3 fistLeft, Vector3 fistRight) = facing ? (left.position, right.position) : (right.position, left.position);
            body.Left.Reach(fistLeft, weight, him);
            body.Right.Reach(fistRight, weight, him);
        }

        // The hauler named in the ZDO, if his character is loaded here.
        private Player? Puller(KwArm arm)
        {
            ZDOID id = arm.Tower;
            if (id.IsNone() || ZNetScene.instance == null)
                return null;
            if (puller != null && puller.GetZDOID() == id)
                return puller;
            GameObject? found = ZNetScene.instance.FindInstance(id);
            return found != null ? found.GetComponent<Player>() : null;
        }
    }
}
