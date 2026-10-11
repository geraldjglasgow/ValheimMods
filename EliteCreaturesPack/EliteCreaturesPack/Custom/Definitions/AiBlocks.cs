using System.Collections.Generic;

namespace EliteCreaturesPack.Custom.Definitions
{
    /// <summary>`senses:` how far and how wide it notices things. Applied by the character step (Custom/Nature).</summary>
    public sealed class SensesBlock
    {
        /// <summary>`sight range`: metres it sees, 0 to 1,000 (BaseAI.m_viewRange).</summary>
        public float? SightRange;

        /// <summary>`sight angle`: degrees of its field of view, 0 to 360 (BaseAI.m_viewAngle).</summary>
        public float? SightAngle;

        /// <summary>`hearing range`: metres it hears, 0 to 10,000 (BaseAI.m_hearRange).</summary>
        public float? HearingRange;

        /// <summary>`alert range`: a target it sees closer than this many metres alerts it (and a tame one fights only within
        /// it of where it follows or guards), 0 to 10,000 (MonsterAI.m_alertRange; a monster's mind only).</summary>
        public float? AlertRange;
    }

    /// <summary>
    /// `movement:` how it moves when nothing holds its attention, and for flyers when to take off and land and how high to
    /// fly. Applied by the character step. The flyer values only mean something on a base that flies.
    /// </summary>
    public sealed class MovementBlock
    {
        /// <summary>`wander every`: seconds between its idle wanders, 0 to 600 (BaseAI.m_randomMoveInterval).</summary>
        public float? WanderEvery;

        /// <summary>`wander range`: metres it wanders from where it stands, 0 to 1,000 (BaseAI.m_randomMoveRange).</summary>
        public float? WanderRange;

        /// <summary>`circle every`: seconds between its idle circles, 0 to 600 (BaseAI.m_randomCircleInterval).</summary>
        public float? CircleEvery;

        /// <summary>`take off chance`: chance, 0 to 1, that a landed flyer takes off when its time on the ground is up.</summary>
        public float? TakeOffChance;

        /// <summary>`land chance`: chance, 0 to 1, that a flyer lands when its time in the air is up.</summary>
        public float? LandChance;

        /// <summary>`time on ground`: seconds a flyer stays down, 0 to 3,600 (BaseAI.m_groundDuration).</summary>
        public float? TimeOnGround;

        /// <summary>`time in air`: seconds a flyer stays up, 0 to 3,600 (BaseAI.m_airDuration).</summary>
        public float? TimeInAir;

        /// <summary>`fly height`: metres above the ground a flyer keeps to, one number or [low, high], 0 to 1,000.</summary>
        public NumberRange? FlyHeight;
    }

    /// <summary>
    /// `behaviour:` the switches and numbers of its fight and its rest. Applied by the character step (Custom/Nature);
    /// eating to heal needs a component of its own (decided on the owner).
    /// </summary>
    public sealed class BehaviourBlock
    {
        /// <summary>`flee at health`: it flees below this share of its health, 0 to 1; 0 never (MonsterAI.m_fleeIfLowHealth).</summary>
        public float? FleeAtHealth;

        /// <summary>`flee when unreachable`: it flees when hurt by a target it cannot reach.</summary>
        public bool? FleeWhenUnreachable;

        /// <summary>`avoid fire`: it keeps away from fires.</summary>
        public bool? AvoidFire;

        /// <summary>`fear fire`: fire makes it flee.</summary>
        public bool? FearFire;

        /// <summary>`avoid water`: it keeps out of water.</summary>
        public bool? AvoidWater;

        /// <summary>`hunt players`: it seeks players out instead of waiting to see one.</summary>
        public bool? HuntPlayers;

        /// <summary>`attack buildings`: it attacks player-built pieces.</summary>
        public bool? AttackBuildings;

        /// <summary>`chase distance`: metres it chases a target it has lost sight of before giving up, 0 to 10,000; 0 sets
        /// no distance (it still gives up after the game's own times).</summary>
        public float? ChaseDistance;

        /// <summary>`time between attacks`: seconds it waits between attacks at the least, 0 to 600.</summary>
        public float? TimeBetweenAttacks;

        /// <summary>`circle before charging`: it circles its target while closing in (MonsterAI.m_circulateWhileCharging).</summary>
        public bool? CircleBeforeCharging;

        /// <summary>`circle target every`: seconds between its circling of the target, 0 to 600; 0 never.</summary>
        public float? CircleTargetEvery;

        /// <summary>`circle target for`: seconds each circling lasts, 0 to 600.</summary>
        public float? CircleTargetFor;

        /// <summary>`circle target distance`: metres from the target it circles at, 0 to 100.</summary>
        public float? CircleTargetDistance;

        /// <summary>`starts asleep`: it spawns asleep.</summary>
        public bool? StartsAsleep;

        /// <summary>`wakes on noise`: noise wakes it.</summary>
        public bool? WakesOnNoise;

        /// <summary>`wake distance`: a player this many metres away wakes it, 0 to 1,000.</summary>
        public float? WakeDistance;

        /// <summary>`eats`: item prefabs it eats off the ground (replacing the base's list).</summary>
        public List<string>? Eats;

        /// <summary>`eat search range`: metres it looks for food, 0 to 100.</summary>
        public float? EatSearchRange;

        /// <summary>`eat heal`: health one eaten item heals, 0 to 1,000,000.</summary>
        public float? EatHeal;
    }

    /// <summary>`taming:` whether it can be tamed and what a tame one does. Applied by the character step.</summary>
    public sealed class TamingBlock
    {
        /// <summary>`tameable`: it can be tamed (gets the game's taming if the base has none; false removes it).</summary>
        public bool? Tameable;

        /// <summary>`born tame`: it spawns tame.</summary>
        public bool? BornTame;

        /// <summary>`follows commands`: a tame one follows and stays when told.</summary>
        public bool? FollowsCommands;

        /// <summary>`breeds`: a tame one breeds.</summary>
        public bool? Breeds;
    }

    /// <summary>`sounds:` which of its sounds are muted. Applied by the character step; a muted sound is never played.</summary>
    public sealed class SoundsBlock
    {
        /// <summary>`mute`: a list of alert, idle, hurt, death.</summary>
        public readonly HashSet<SoundKind> Mute = new HashSet<SoundKind>();
    }
}
