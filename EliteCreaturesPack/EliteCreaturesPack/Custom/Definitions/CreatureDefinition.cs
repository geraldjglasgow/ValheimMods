using System;
using System.Collections.Generic;
using EliteCreaturesPack.Custom.Humans;

namespace EliteCreaturesPack.Custom.Definitions
{
    /// <summary>
    /// One creature of the custom creature files, as read and checked (features/custom-creatures.md, sections 3, 4 and
    /// 8). Plain data: the build steps (Custom/Nature, Combat, Look, Humans, Elite) put it on the creature's prefab.
    /// Only <see cref="Name"/> and <see cref="Base"/> are always set. Every block is null when the file leaves it out, and
    /// inside a block every value is null when the file leaves that out: whatever is unset keeps the base's value. The
    /// YAML key of each field is given in its summary, in backticks.
    /// </summary>
    public sealed class CreatureDefinition
    {
        /// <summary>`name`: the prefab name, one word (letters, digits, <c>_ - .</c>). Hashed into saved worlds, so a
        /// rename makes a new creature. A name that is already a prefab is refused when the creatures are built.</summary>
        public string Name = "";

        /// <summary>`base`: the creature it is a copy of - a game, mod, Elite Creatures Pack or custom creature prefab, or
        /// <c>Human</c> (<see cref="HumanBody.Base"/>) for a person on the player's body.</summary>
        public string Base = "";

        /// <summary>`enabled`: false leaves the creature out (its creatures in the world are kept, hidden, and it can still
        /// be another definition's base). Default true; the ready-made creatures ship with false.</summary>
        public bool Enabled = true;

        /// <summary>`character:` name, health, speeds, faction, boss, resistances, what hurts it, staggering.</summary>
        public CharacterBlock? Character;

        /// <summary>`progress:` the world key set on its death, the spawn and death messages.</summary>
        public ProgressBlock? Progress;

        /// <summary>`senses:` sight, hearing and alert ranges.</summary>
        public SensesBlock? Senses;

        /// <summary>`movement:` wandering, idle circling, and a flyer's take-off, landing and heights.</summary>
        public MovementBlock? Movement;

        /// <summary>`behaviour:` fleeing, fire and water, hunting, buildings, chasing, attack pace, circling, sleep, eating.</summary>
        public BehaviourBlock? Behaviour;

        /// <summary>`drops:` rows added to the base's drop table, or replacing it.</summary>
        public DropsBlock? Drops;

        /// <summary>`gear:` items always carried, one pick from each of several lists, one set from a list of sets.</summary>
        public GearBlock? Gear;

        /// <summary>`damage:` every attack's damage, per type or scaled to a total that keeps the mix.</summary>
        public DamageBlock? Damage;

        /// <summary>`projectile:` a projectile prefab every projectile attack fires instead of its own.</summary>
        public string? Projectile;

        /// <summary>`attacks:` new attacks, each built from a weapon or creature attack item. Null: none added.</summary>
        public List<AttackDefinition>? Attacks;

        /// <summary>`effects:` its hit, death, alert and idle effects replaced; an effect may be a creature it spawns.</summary>
        public EffectsBlock? Effects;

        /// <summary>`look:` size, body and item tints, a coloured smoke or flame overlay.</summary>
        public LookBlock? Look;

        /// <summary>`texture`: an image file in the config folder that replaces the body texture (never sent over the
        /// network: a player without the file sees the base's texture).</summary>
        public string? Texture;

        /// <summary>`taming:` tameable, born tame, follows commands, breeds.</summary>
        public TamingBlock? Taming;

        /// <summary>`sounds:` which of its sounds are muted.</summary>
        public SoundsBlock? Sounds;

        /// <summary>`human:` a person's random look (gender, hair, beard, colours). Only for a creature whose base is, or
        /// comes down from, <c>Human</c>; null there means the default look (anything the game draws on players).</summary>
        public HumanLook? Human;

        /// <summary>`elite:` Elite Creatures Reborn's mutations, aspects and their ability mapping (inactive without ECR).</summary>
        public EliteBlock? Elite;

        /// <summary>The file it was read from (name only, as messages show it).</summary>
        public string File = "";

        /// <summary>The line its entry starts on, 0 when unknown.</summary>
        public int Line;

        /// <summary>The line of every value read, by its path inside the entry (<c>character.health</c>, <c>drops.items[1].item</c>).</summary>
        public readonly Dictionary<string, int> FieldLines = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Whether its own base is the player's body (a custom base that is human is resolved when building).</summary>
        public bool BaseIsHuman => string.Equals(Base, HumanBody.Base, StringComparison.OrdinalIgnoreCase);

        /// <summary>Whether it asks Elite Creatures Reborn for anything.</summary>
        public bool HasEliteLines => Elite != null && Elite.HasAny;

        /// <summary>
        /// The line of a field (a path inside the entry, like <c>attacks[0].projectile</c>): the nearest one read, walking
        /// up the path, and the entry's own line when none was.
        /// </summary>
        public int LineOf(string? field)
        {
            string path = field ?? "";
            while (path.Length > 0)
            {
                if (FieldLines.TryGetValue(path, out int line) && line > 0)
                {
                    return line;
                }
                int cut = Math.Max(path.LastIndexOf('.'), path.LastIndexOf('['));
                path = cut > 0 ? path.Substring(0, cut) : "";
            }
            return Line;
        }

        public override string ToString() => $"'{Name}' ({File}, line {Line})";
    }
}
