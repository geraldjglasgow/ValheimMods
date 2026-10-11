namespace EliteCreaturesPack.Custom.Definitions
{
    /// <summary>
    /// The damage types a definition names, as the files write them (<c>blunt</c>, <c>fire</c>...): for resistances, for
    /// damage overrides and for a new attack's damage. The steps map them onto the game's <c>HitData</c> fields.
    /// </summary>
    public enum DamageKind
    {
        Blunt,
        Slash,
        Pierce,
        Chop,
        Pickaxe,
        Fire,
        Frost,
        Lightning,
        Poison,
        Spirit,
    }

    /// <summary>Who a new attack is aimed at (<c>targets:</c>), the game's three AI targets: enemy, hurt friend, friend.</summary>
    public enum AttackTarget
    {
        Enemy,
        HurtFriend,
        Friend,
    }

    /// <summary>The coloured overlay a creature's whole body can wear (<c>look: overlay:</c>).</summary>
    public enum OverlayKind
    {
        Smoke,
        Flame,
    }

    /// <summary>The sounds a definition can mute (<c>sounds: mute:</c>).</summary>
    public enum SoundKind
    {
        Alert,
        Idle,
        Hurt,
        Death,
    }
}
