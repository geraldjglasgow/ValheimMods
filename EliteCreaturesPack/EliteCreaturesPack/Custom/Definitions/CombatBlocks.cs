using System.Collections.Generic;

namespace EliteCreaturesPack.Custom.Definitions
{
    /// <summary>`drops:` its loot. Applied by the combat step (Custom/Combat).</summary>
    public sealed class DropsBlock
    {
        /// <summary>`replace`: true throws the base's drop table away first; false (the default) adds to it.</summary>
        public bool Replace;

        /// <summary>`items:` the rows, in file order.</summary>
        public readonly List<DropRow> Items = new List<DropRow>();
    }

    /// <summary>One row of `drops: items:`. Unset values take the game's own defaults for a drop row.</summary>
    public sealed class DropRow
    {
        /// <summary>`item`: the item prefab (required).</summary>
        public string Item = "";

        /// <summary>`amount`: how many, one number or [low, high] with both ends included, 0 to 10,000. Default 1. The
        /// game's drop table leaves its top out (<c>m_amountMax</c> is one above the most that drops).</summary>
        public CountRange Amount = new CountRange(1, 1);

        /// <summary>`chance`: 0 to 1. Default 1.</summary>
        public float Chance = 1f;

        /// <summary>`one per player`: one for each player nearby instead of the amount. Default false.</summary>
        public bool OnePerPlayer;

        /// <summary>`more for higher levels`: the amount grows with its stars. Default true.</summary>
        public bool MoreForHigherLevels = true;

        /// <summary>The row's path inside the entry (<c>drops.items[2]</c>), for messages.</summary>
        public string Field = "";
    }

    /// <summary>
    /// `gear:` what it carries, as item prefab names. Applied by the combat step. Humans use the player's animations
    /// with it (features/custom-creatures.md section 4). A human's stand-in kit (a club and rags) goes as soon as any
    /// definition of its chain gives gear (an item, or <c>replace</c>), taken off by the human step.
    /// </summary>
    public sealed class GearBlock
    {
        /// <summary>`replace`: true empties the base's own gear first (default items, random weapons, armour, shields,
        /// sets and chance items: a creature's attacks are among them); false (the default) adds to it.</summary>
        public bool Replace;

        /// <summary>`always`: items it always carries.</summary>
        public readonly List<string> Always = new List<string>();

        /// <summary>`pick one from`: several lists; one random item from each list.</summary>
        public readonly List<List<string>> PickOneFrom = new List<List<string>>();

        /// <summary>`one set from`: several sets (each a list of items); one random set.</summary>
        public readonly List<List<string>> OneSetFrom = new List<List<string>>();
    }

    /// <summary>
    /// Damage, either per type or as a total: `damage:` on a creature (every attack it has) and on a new attack (that
    /// attack). Written as a map of damage type to number, or <c>total: n</c> alone, never both. Applied by the combat step.
    /// </summary>
    public sealed class DamageBlock
    {
        /// <summary>Damage types set outright, 0 to 1,000,000 each; types left out keep the attack's own.</summary>
        public readonly Dictionary<DamageKind, float> PerType = new Dictionary<DamageKind, float>();

        /// <summary>`total`: the attack's damage scaled so its types add up to this, keeping their mix, 0 to 1,000,000.</summary>
        public float? Total;
    }

    /// <summary>
    /// One entry of `attacks:`, a new attack built from a weapon or creature attack item of the game or a mod. Unset
    /// values keep the source item's. Applied by the combat step (a copy of the item, kept with the creature).
    /// </summary>
    public sealed class AttackDefinition
    {
        /// <summary>`from`: the weapon or creature attack item it is a copy of (required).</summary>
        public string From = "";

        /// <summary>`replace`: true changes the attack the creature already makes with that item (its own ground slam,
        /// harder), wherever it carries it, instead of adding a second one; false (the default) adds a new attack.</summary>
        public bool Replace;

        /// <summary>`projectiles`: how many projectiles one shot fires at once, 1 to 50 (a projectile attack only).</summary>
        public int? Projectiles;

        /// <summary>`spread`: degrees each projectile may stray from the aim, 0 to 180 (a projectile attack only).</summary>
        public float? Spread;

        /// <summary>`animation`: the animator trigger it plays.</summary>
        public string? Animation;

        /// <summary>`projectile`: the projectile prefab it fires.</summary>
        public string? Projectile;

        /// <summary>`cooldown`: seconds before the AI uses it again, 0 to 600.</summary>
        public float? Cooldown;

        /// <summary>`damage:` per type or a total, as <see cref="DamageBlock"/>.</summary>
        public DamageBlock? Damage;

        /// <summary>`reach`: metres the hit reaches, 0 to 100 (for a blast, how far ahead its centre is; for a shot, how
        /// far ahead the projectile starts).</summary>
        public float? Reach;

        /// <summary>`height`: metres above its attack's origin the hit is aimed, 0 to 100.</summary>
        public float? Height;

        /// <summary>`width`: the hit's thickness in metres, 0 to 100: the radius of a swing's sweep, or of a blast.</summary>
        public float? Width;

        /// <summary>`min range`: the AI uses it no closer than this, metres, 0 to 1,000.</summary>
        public float? MinRange;

        /// <summary>`max range`: the AI uses it within this, metres, 0 to 1,000.</summary>
        public float? MaxRange;

        /// <summary>`angle`: degrees off its facing the AI still uses it, 1 to 360 (the game's AI never attacks at 0).</summary>
        public float? Angle;

        /// <summary>`preferred`: the AI picks it before its other attacks when it can.</summary>
        public bool? Preferred;

        /// <summary>`targets`: enemy, hurt friend or friend.</summary>
        public AttackTarget? Targets;

        /// <summary>`blockable`: it can be blocked.</summary>
        public bool? Blockable;

        /// <summary>`dodgeable`: it can be dodged.</summary>
        public bool? Dodgeable;

        /// <summary>`status effect`: a status effect it puts on what it hits.</summary>
        public string? StatusEffect;

        /// <summary>The entry's path inside the creature (<c>attacks[1]</c>), for messages.</summary>
        public string Field = "";
    }
}
