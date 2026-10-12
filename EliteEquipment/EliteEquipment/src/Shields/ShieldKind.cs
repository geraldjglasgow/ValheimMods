using UnityEngine;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// What a kind of shield shares with the game's own of that kind: its parry bonus, the adrenaline a perfect block
    /// gives, its movement penalty and how it lies in the hand. The hold is measured from the game's items in their
    /// <c>attach</c> (2026-10-10): the face's direction (the boss side) and the top, up the forearm; the workshop builds
    /// every model front +Z, up +Y, so one turn lays it as the game's.
    /// </summary>
    public sealed class ShieldKind
    {
        /// <summary>The game's Bronze and Iron Buckler: boss out, turned 29 degrees towards the attach's +X.</summary>
        public static readonly ShieldKind Buckler = new ShieldKind(2.5f, 5f, -0.05f,
            new Vector3(0.490f, -0.870f, -0.055f), new Vector3(0.012f, -0.056f, 0.998f));

        /// <summary>The game's Wood and Banded Shield: face out, turned 17 degrees towards the attach's +X.</summary>
        public static readonly ShieldKind Round = new ShieldKind(1.5f, 5f, -0.05f,
            new Vector3(0.296f, -0.955f, -0.023f), new Vector3(0f, 0f, 1f));

        /// <summary>The game's Iron Tower Shield: face out, turned 13 degrees towards +X; no parry, -10% movement.</summary>
        public static readonly ShieldKind Tower = new ShieldKind(0f, 0f, -0.1f,
            new Vector3(0.225f, -0.972f, -0.064f), new Vector3(-0.155f, -0.100f, 0.983f));

        /// <summary>The parry (timed block) bonus.</summary>
        public readonly float Parry;

        /// <summary>The adrenaline a perfect block gives, and the movement modifier while it is worn.</summary>
        public readonly float Adrenaline;
        public readonly float Movement;

        /// <summary>The model's turn in the item's <c>attach</c>.</summary>
        public readonly Quaternion Hold;

        private ShieldKind(float parry, float adrenaline, float movement, Vector3 front, Vector3 up)
        {
            Parry = parry;
            Adrenaline = adrenaline;
            Movement = movement;
            Hold = Quaternion.LookRotation(front, up);
        }
    }
}
