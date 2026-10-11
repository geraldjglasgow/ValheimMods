using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>
    /// Reads the blocks of the creature's mind: `senses:` (<see cref="SensesBlock"/>), `movement:`
    /// (<see cref="MovementBlock"/>), `behaviour:` (<see cref="BehaviourBlock"/>), `taming:` (<see cref="TamingBlock"/>)
    /// and `sounds:` (<see cref="SoundsBlock"/>).
    /// </summary>
    internal static class AiReader
    {
        private const float Far = 10000f;

        public static SensesBlock? ReadSenses(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "senses");
            if (block == null)
            {
                return null;
            }
            return new SensesBlock
            {
                SightRange = fields.Number(block, "sight range", 0f, 1000f),
                SightAngle = fields.Number(block, "sight angle", 0f, 360f),
                HearingRange = fields.Number(block, "hearing range", 0f, Far),
                AlertRange = fields.Number(block, "alert range", 0f, Far),
            };
        }

        public static MovementBlock? ReadMovement(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "movement");
            if (block == null)
            {
                return null;
            }
            return new MovementBlock
            {
                WanderEvery = fields.Number(block, "wander every", 0f, 600f),
                WanderRange = fields.Number(block, "wander range", 0f, 1000f),
                CircleEvery = fields.Number(block, "circle every", 0f, 600f),
                TakeOffChance = fields.Number(block, "take off chance", 0f, 1f),
                LandChance = fields.Number(block, "land chance", 0f, 1f),
                TimeOnGround = fields.Number(block, "time on ground", 0f, 3600f),
                TimeInAir = fields.Number(block, "time in air", 0f, 3600f),
                FlyHeight = fields.Range(block, "fly height", 0f, 1000f),
            };
        }

        public static BehaviourBlock? ReadBehaviour(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "behaviour");
            if (block == null)
            {
                return null;
            }
            BehaviourBlock behaviour = new BehaviourBlock
            {
                FleeAtHealth = fields.Number(block, "flee at health", 0f, 1f),
                FleeWhenUnreachable = fields.Switch(block, "flee when unreachable"),
                AvoidFire = fields.Switch(block, "avoid fire"),
                FearFire = fields.Switch(block, "fear fire"),
                AvoidWater = fields.Switch(block, "avoid water"),
                HuntPlayers = fields.Switch(block, "hunt players"),
                AttackBuildings = fields.Switch(block, "attack buildings"),
            };
            ReadFight(block, fields, behaviour);
            ReadRest(block, fields, behaviour);
            return behaviour;
        }

        public static TamingBlock? ReadTaming(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "taming");
            if (block == null)
            {
                return null;
            }
            return new TamingBlock
            {
                Tameable = fields.Switch(block, "tameable"),
                BornTame = fields.Switch(block, "born tame"),
                FollowsCommands = fields.Switch(block, "follows commands"),
                Breeds = fields.Switch(block, "breeds"),
            };
        }

        public static SoundsBlock? ReadSounds(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "sounds");
            if (block == null)
            {
                return null;
            }
            SoundsBlock sounds = new SoundsBlock();
            YamlNode mute = fields.At(block, "mute");
            if (FieldReader.Has(mute) && mute.Kind == YamlNodeKind.Scalar)
            {
                AddMuted(sounds, fields.WordOf<SoundKind>(mute));
                return sounds;
            }
            foreach (YamlNode item in mute.Items)
            {
                AddMuted(sounds, fields.WordOf<SoundKind>(item));
            }
            return sounds;
        }

        private static void AddMuted(SoundsBlock sounds, SoundKind? kind)
        {
            if (kind != null)
            {
                sounds.Mute.Add(kind.Value);
            }
        }

        private static void ReadFight(YamlNode block, FieldReader fields, BehaviourBlock behaviour)
        {
            behaviour.ChaseDistance = fields.Number(block, "chase distance", 0f, Far);
            behaviour.TimeBetweenAttacks = fields.Number(block, "time between attacks", 0f, 600f);
            behaviour.CircleBeforeCharging = fields.Switch(block, "circle before charging");
            behaviour.CircleTargetEvery = fields.Number(block, "circle target every", 0f, 600f);
            behaviour.CircleTargetFor = fields.Number(block, "circle target for", 0f, 600f);
            behaviour.CircleTargetDistance = fields.Number(block, "circle target distance", 0f, 100f);
        }

        private static void ReadRest(YamlNode block, FieldReader fields, BehaviourBlock behaviour)
        {
            behaviour.StartsAsleep = fields.Switch(block, "starts asleep");
            behaviour.WakesOnNoise = fields.Switch(block, "wakes on noise");
            behaviour.WakeDistance = fields.Number(block, "wake distance", 0f, 1000f);
            behaviour.Eats = fields.Names(block, "eats");
            behaviour.EatSearchRange = fields.Number(block, "eat search range", 0f, 100f);
            behaviour.EatHeal = fields.Number(block, "eat heal", 0f, 1000000f);
        }
    }
}
