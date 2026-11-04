using BundlePrefabs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// The crane's moving parts, set on every peer from a pose (<see cref="KwCranePose"/>): the turret (<c>turn</c>) about
    /// the footing's upright axis, the boom (<c>boom</c>) sliding out along the turret's front, the windlass (<c>drum</c>)
    /// turning with the rope paid out, and the head (<c>head</c>) hanging straight under the boom's tip, with the running
    /// rope drawn from the drum over the mast-top pulley and the boom's tip down to the head in the game's own rope. The
    /// bundle's parts are children of its root; <see cref="Arrange"/> hangs the boom, the drum, the works (controls and
    /// gears, <see cref="KwCraneWorks"/>) and the turret's points under the turret, the hook under the head and each grip
    /// under its control, once, on the prefab. A dedicated server sets the parts too (the hook's place is where a crate
    /// hangs) but draws no rope; the works and the operator's hands are turned on a screen after this
    /// (<see cref="KwCraneHands"/>, from <see cref="Posed"/>).
    /// </summary>
    public sealed class KwCraneRig : MonoBehaviour
    {
        public const string RopeName = "kwc_rope";
        private const string TurnName = "turn", BoomName = "boom", DrumName = "drum", HeadName = "head";
        private const string OperatorName = "operator", PulleyName = "mast_pulley", TipName = "tip", DrumRopeName = "drum_rope", HookName = "hook";
        private const float DrumRadius = 0.15f;

        private Transform? turn, boom, drum, head, pulley, drumRope;
        private Vector3 boomRest;
        private Quaternion turnRest, drumRest;
        private LineRenderer? rope;
        private readonly Vector3[] ropePoints = new Vector3[4];

        /// <summary>Where the operator stands, the boom's tip the rope hangs from, and the hook's bowl where a crate hangs.</summary>
        public Transform? Operator { get; private set; }
        public Transform? Tip { get; private set; }
        public Transform? Hook { get; private set; }

        /// <summary>The head's drop under the tip as the model was made, and how far the hook's bowl sits under the head's top.</summary>
        public float RestDrop { get; private set; } = 2f;
        public float HookDepth { get; private set; } = 0.5f;

        public bool Ready => turn != null && boom != null && head != null && Operator != null && Tip != null && Hook != null;

        /// <summary>The turret's world rotation (the piece's turn and the turret's own).</summary>
        public Quaternion Turret => turn != null ? turn.rotation : transform.rotation;

        /// <summary>The pose the parts were last set to (the one drawn), for the works and the operator's hands after it.</summary>
        public KwCranePose? Posed { get; private set; }

        /// <summary>
        /// The windlass's turn about its axle for a pose (degrees from rest): the rope paid out (the boom's slide and the
        /// head's drop) over its radius, winding in (the head going up) turning it +X, as the model's rope leaves over the
        /// drum's front (the workshop's HANDOFF; it turned the other way until 2026-11-03).
        /// </summary>
        public static float DrumAngle(KwCranePose pose) => -(pose.Slide + pose.Drop) / DrumRadius * Mathf.Rad2Deg;

        /// <summary>On the prefab, once: the boom, the drum, the works and the turret's points under the turret, the tip under the boom, the hook under the head, each grip under its control.</summary>
        public static void Arrange(Transform model)
        {
            Transform? turret = GameMaterials.Find(model, TurnName), slider = GameMaterials.Find(model, BoomName);
            Transform? hanger = GameMaterials.Find(model, HeadName);
            if (turret == null || slider == null || hanger == null)
                return;
            foreach (string name in new[] { BoomName, DrumName, OperatorName, PulleyName, DrumRopeName })
                GameMaterials.Find(model, name)?.SetParent(turret, true);
            GameMaterials.Find(model, TipName)?.SetParent(slider, true);
            GameMaterials.Find(model, HookName)?.SetParent(hanger, true);
            KwCraneWorks.Arrange(model, turret);
        }

        private void Awake()
        {
            (turn, boom, drum, head) = (Find(TurnName), Find(BoomName), Find(DrumName), Find(HeadName));
            (Operator, Tip, Hook, pulley, drumRope) = (Find(OperatorName), Find(TipName), Find(HookName), Find(PulleyName), Find(DrumRopeName));
            if (!Ready)
                return;
            turnRest = turn!.localRotation;
            boomRest = boom!.localPosition;
            drumRest = drum != null ? drum.localRotation : Quaternion.identity;
            RestDrop = Tip!.position.y - head!.position.y;
            HookDepth = head.position.y - Hook!.position.y;
            Transform? line = transform.Find(RopeName);
            rope = line != null && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null ? line.GetComponent<LineRenderer>() : null;
        }

        private Transform? Find(string name) => GameMaterials.Find(transform, name);

        /// <summary>Sets every moving part to the pose: turret turn (degrees), boom slide (m), the head's drop under the tip (m).</summary>
        public void Pose(KwCranePose pose)
        {
            if (!Ready)
                return;
            turn!.localRotation = turnRest * Quaternion.Euler(0f, pose.Turn, 0f);
            boom!.localPosition = boomRest + Vector3.forward * pose.Slide;
            if (drum != null)
                drum.localRotation = drumRest * Quaternion.Euler(Mathf.Repeat(DrumAngle(pose), 360f), 0f, 0f);
            head!.SetPositionAndRotation(Tip!.position + Vector3.down * pose.Drop, turn.rotation);
            Posed = pose;
            DrawRope();
        }

        // Drum, mast-top pulley, boom tip, head: the points the rope runs over, in the world.
        private void DrawRope()
        {
            if (rope == null)
                return;
            ropePoints[0] = drumRope != null ? drumRope.position : turn!.position;
            ropePoints[1] = pulley != null ? pulley.position : Tip!.position;
            ropePoints[2] = Tip!.position;
            ropePoints[3] = head!.position;
            rope.positionCount = ropePoints.Length;
            rope.SetPositions(ropePoints);
        }
    }
}
