using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a helmsman holds (<see cref="KwHelmsman"/>): a tiller's peg (<see cref="KwTillerGrip"/>) or a captain's wheel
    /// (<see cref="KwWheelGrip"/>). Each frame it says where his fists go and how his upper body turns and leans, worked
    /// out from what every peer draws (the blade's turn, the wheel's turn) and the helmsman's own synced place, so every
    /// screen poses him alike; on a launched ship also where his feet go and which way he faces.
    /// </summary>
    public interface IKwHelmGrip
    {
        /// <summary>Where the helmsman's hands go this frame, and his turn and lean.</summary>
        KwHelmHold Hold(Player player, float dt);

        /// <summary>A new helmsman: his hands take hold afresh.</summary>
        void Reset();

        /// <summary>The parts whose colliders the helmsman passes through while he holds them.</summary>
        Transform Parts { get; }

        /// <summary>Where his feet go on a launched ship, in the world.</summary>
        Vector3 Stand { get; }

        /// <summary>The way he faces there, in the world.</summary>
        Vector3 Bow { get; }

        /// <summary>Whether what he holds has come in too close to his middle for his hands: he steps away.</summary>
        bool Crowded(Player player);
    }

    /// <summary>
    /// A helmsman's pose for one frame (<see cref="IKwHelmGrip.Hold"/>): each fist's target in the world (none: that arm
    /// as the animator has it), how far his upper body leans forward and turns to his right (degrees; negative turns him
    /// left). Worked out from what every peer draws (the blade's turn, the wheel's) and the helmsman's own synced place, so
    /// every screen poses him alike.
    /// </summary>
    public readonly struct KwHelmHold
    {
        public KwHelmHold(Vector3? left, Vector3? right, float lean, float twist)
        {
            Left = left;
            Right = right;
            Lean = lean;
            Twist = twist;
        }

        public Vector3? Left { get; }
        public Vector3? Right { get; }
        public float Lean { get; }
        public float Twist { get; }
    }
}
