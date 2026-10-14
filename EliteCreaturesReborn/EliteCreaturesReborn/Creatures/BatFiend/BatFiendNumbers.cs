using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// Every number of the Bat-fiend in one place, chosen 2026-10-13 for a Hel's arena creature at about Mistlands and
    /// Ashlands strength (the game's Asksvin has 800 health, the Charred warrior 600, the Seeker brute 1,500) and not yet
    /// tuned in play. Sizes are the workshop model's (ValheimAssets Assets/Creatures/HelsCreatures/BatFiend, v004) at
    /// scale 1, before <see cref="Scale"/>; speeds are in the world, after it.
    /// </summary>
    internal static class BatFiendNumbers
    {
        /// <summary>The creature's root scale: bigger than a wolf, much smaller than a troll (head 1.14 m up, wing tips
        /// 2.9 m, 2.6 m long). The bundle's blend tree is built for it (HCBatFiendBundleV004.GameScale): change both.</summary>
        public const float Scale = 1.2f;

        public const float Health = 800f;

        /// <summary>The clips' own speeds (walk 1.4, run 5.5 m/s at scale 1) times <see cref="Scale"/>: feet stay planted.</summary>
        public const float WalkSpeed = 1.4f * Scale, RunSpeed = 5.5f * Scale;

        /// <summary>Degrees a second: the turn clips' own 90 in place; faster on the run.</summary>
        public const float TurnSpeed = 90f, RunTurnSpeed = 160f;

        /// <summary>Sight and alert in metres; at least 2.2 s between two attacks (the user: a second more than 1.2).</summary>
        public const float ViewRange = 30f, AlertRange = 14f, MinAttackInterval = 2.2f;

        /// <summary>The screech's deafness: it starts this long after the hit (0.7 s into the clip; the scream holds to
        /// 1.4 s), so the scream is heard first, then lasts this many seconds.</summary>
        public const float ScreechDeafDelay = 0.45f, ScreechDeafSeconds = 7f;

        /// <summary>Seconds of the game's Frost the gravewind puts on whoever it hits (the user: about 5), before resistance.</summary>
        public const float GravewindFrostSeconds = 5f;

        /// <summary>The character capsule, upright round the middle, at scale 1 (the root's scale grows it).</summary>
        public const float CapsuleRadius = 0.5f, CapsuleHeight = 1.3f;

        /// <summary>A box over the trunk from snout to rump, lifted off the ground, so every part of the body can be hit:
        /// the trunk without wings or ears measured at rest (v003 reach measurement: 1.12 wide, 1.37 tall, 2.2 long).</summary>
        public static readonly Vector3 ShellSize = new Vector3(1.15f, 1.0f, 2.2f), ShellCenter = new Vector3(0f, 0.85f, 0.07f);

        /// <summary>Times the game Wolf's body mass (a wolf is easily shoved; this is not).</summary>
        public const float MassFactor = 4f;

        /// <summary>The bite heals the Bat-fiend this much each time it hits a creature (the game's attack health return).</summary>
        public const float BiteHeal = 20f;

        /// <summary>Its voice is the game Bat's at this share of its pitch: a far bigger throat.</summary>
        public const float VoicePitch = 0.5f;

        /// <summary>Seconds of the death clip (66 frames), before the Wolf corpse's own wait starts.</summary>
        public const float DeathClip = 2.2f;

        /// <summary>
        /// The Raking Pounce's travel at scale 1, metres covered by each of the clip's 45 frames (30 fps): nothing until
        /// frame 11 (0.37 s), 2.4 m by frame 18 (0.6 s). From Unity/animation_timing_v004.json, travel.per_frame_m.
        /// </summary>
        public static readonly float[] PounceTravel =
        {
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.0843f, 0.4918f, 1.0148f, 1.5047f, 2.0769f, 2.3967f, 2.4f,
        };

        public const float PounceFrames = 45f;

        /// <summary>
        /// The Bloodletting Bite's lunge at scale 1, the same way: nothing until frame 7, 1.2 m by frame 14 (the hit is
        /// frame 13, 0.433 s, at 1.197 m) of the clip's 39. From Unity/animation_timing_v004.json.
        /// </summary>
        public static readonly float[] BiteTravel =
        {
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.0127f, 0.1391f, 0.3606f, 0.6273f, 0.8889f, 1.0954f, 1.1968f, 1.2f,
        };

        public const float BiteFrames = 39f;
    }
}
