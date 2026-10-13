using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The gate's two doors (features/arena.md section 1, "The gate doors"), on every peer, the dedicated server
    /// included, because their colliders keep the trial's creatures in: open between trials, shut while one runs (from
    /// <see cref="TrialView"/>, the core's ZDO as this peer sees it). Each leaf turns on its hinge about Y, from shut at
    /// its rest rotation to open into the passage (<c>door_left</c> -90 degrees, <c>door_right</c> +90: the walls'
    /// README), eased in and out over a second and a half; its collider child turns with it. Nothing is sent. Settled,
    /// it costs one comparison a frame. The first frame after the hall is built snaps to the state, with no swing.
    /// </summary>
    internal sealed class HallDoors : MonoBehaviour
    {
        public const string Left = "door_left";
        public const string Right = "door_right";

        private const float LeftOpen = -90f;
        private const float RightOpen = 90f;
        private const float Seconds = 1.5f;

        private Transform? _left;
        private Transform? _right;
        private Quaternion _leftShut;
        private Quaternion _rightShut;
        private float _open = -1f;

        private void Awake()
        {
            HallPoints.TryGet(Left, out _left);
            HallPoints.TryGet(Right, out _right);
            if (_left != null)
            {
                _leftShut = _left.localRotation;
            }
            if (_right != null)
            {
                _rightShut = _right.localRotation;
            }
            enabled = _left != null || _right != null;
        }

        private void Update()
        {
            TrialView.Refresh();
            float target = TrialView.Running ? 0f : 1f;
            if (_open == target)
            {
                return;
            }
            _open = _open < 0f ? target : Mathf.MoveTowards(_open, target, Time.deltaTime / Seconds);
            float eased = Mathf.SmoothStep(0f, 1f, _open);
            if (_left != null)
            {
                _left.localRotation = _leftShut * Quaternion.Euler(0f, LeftOpen * eased, 0f);
            }
            if (_right != null)
            {
                _right.localRotation = _rightShut * Quaternion.Euler(0f, RightOpen * eased, 0f);
            }
        }
    }
}
