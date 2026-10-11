using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// One entry of <c>attacks:</c> put on the creature's own copy of its item. Every value left out keeps the item's.
    /// Where each goes in the game (the export command maps the same fields back):
    /// <list type="bullet">
    /// <item><c>animation</c>: <c>m_attack.m_attackAnimation</c>, the animator trigger.</item>
    /// <item><c>projectile</c>, <c>projectiles</c>, <c>spread</c>: <c>m_attack.m_attackProjectile</c>,
    /// <c>m_attack.m_projectiles</c> (fired at once), and both <c>m_attack.m_projectileAccuracy</c> and
    /// <c>m_projectileAccuracyMin</c> (degrees of stray; a creature has no skill to narrow it), on a projectile attack only.</item>
    /// <item><c>reach</c>, <c>height</c>, <c>width</c>: <c>m_attack.m_attackRange</c>, <c>m_attackHeight</c>,
    /// <c>m_attackRayWidth</c>.</item>
    /// <item><c>cooldown</c>, <c>min range</c>, <c>max range</c>, <c>angle</c>, <c>preferred</c>, <c>targets</c>: the shared
    /// data's <c>m_aiAttackInterval</c>, <c>m_aiAttackRangeMin</c>, <c>m_aiAttackRange</c>, <c>m_aiAttackMaxAngle</c>,
    /// <c>m_aiPrioritized</c>, <c>m_aiTargetType</c>.</item>
    /// <item><c>blockable</c>, <c>dodgeable</c>: <c>m_blockable</c>, <c>m_dodgeable</c>; <c>status effect</c>:
    /// <c>m_attackStatusEffect</c>, put on every hit (<c>m_attackStatusEffectChance</c> 1).</item>
    /// <item><c>damage</c>: <see cref="DamageRules"/>, on <c>m_damages</c>.</item>
    /// </list>
    /// The primary attack is the one changed: it is the one the AI uses.
    /// </summary>
    internal static class AttackFields
    {
        public static void Apply(CreatureBuild build, ItemDrop.ItemData.SharedData shared, AttackDefinition attack)
        {
            Shot(build, shared.m_attack, attack);
            Hit(shared.m_attack, attack);
            Ai(shared, attack);
            Effect(build, shared, attack);
            if (attack.Damage != null)
            {
                DamageRules.Apply(build, shared, attack.Damage, attack.Field + ".damage", attack.From);
            }
        }

        private static void Shot(CreatureBuild build, Attack hit, AttackDefinition attack)
        {
            if (attack.Projectile == null && attack.Projectiles == null && attack.Spread == null)
            {
                return;
            }
            if (!ProjectileRules.Shoots(hit))
            {
                build.Report.Warn($"'{attack.From}' is not a projectile attack, so its projectile, projectiles and spread do nothing", attack.Field);
                return;
            }
            GameObject? projectile = attack.Projectile != null ? ProjectileRules.Find(build, attack.Projectile, attack.Field + ".projectile") : null;
            hit.m_attackProjectile = projectile != null ? projectile : hit.m_attackProjectile;
            hit.m_projectiles = attack.Projectiles ?? hit.m_projectiles;
            hit.m_projectileAccuracy = attack.Spread ?? hit.m_projectileAccuracy;
            hit.m_projectileAccuracyMin = attack.Spread ?? hit.m_projectileAccuracyMin;
        }

        private static void Hit(Attack hit, AttackDefinition attack)
        {
            hit.m_attackAnimation = attack.Animation ?? hit.m_attackAnimation;
            hit.m_attackRange = attack.Reach ?? hit.m_attackRange;
            hit.m_attackHeight = attack.Height ?? hit.m_attackHeight;
            hit.m_attackRayWidth = attack.Width ?? hit.m_attackRayWidth;
        }

        private static void Ai(ItemDrop.ItemData.SharedData shared, AttackDefinition attack)
        {
            shared.m_aiAttackInterval = attack.Cooldown ?? shared.m_aiAttackInterval;
            shared.m_aiAttackRangeMin = attack.MinRange ?? shared.m_aiAttackRangeMin;
            shared.m_aiAttackRange = attack.MaxRange ?? shared.m_aiAttackRange;
            shared.m_aiAttackMaxAngle = attack.Angle ?? shared.m_aiAttackMaxAngle;
            shared.m_aiPrioritized = attack.Preferred ?? shared.m_aiPrioritized;
            shared.m_aiTargetType = attack.Targets is AttackTarget target ? Target(target) : shared.m_aiTargetType;
        }

        private static void Effect(CreatureBuild build, ItemDrop.ItemData.SharedData shared, AttackDefinition attack)
        {
            shared.m_blockable = attack.Blockable ?? shared.m_blockable;
            shared.m_dodgeable = attack.Dodgeable ?? shared.m_dodgeable;
            if (attack.StatusEffect == null)
            {
                return;
            }
            StatusEffect? effect = build.Find.StatusEffect(attack.StatusEffect);
            if (effect == null)
            {
                build.Report.Fail($"unknown status effect '{attack.StatusEffect}'", attack.Field + ".status effect");
                return;
            }
            shared.m_attackStatusEffect = effect;
            shared.m_attackStatusEffectChance = 1f;
        }

        private static ItemDrop.ItemData.AiTarget Target(AttackTarget target) => target switch
        {
            AttackTarget.HurtFriend => ItemDrop.ItemData.AiTarget.FriendHurt,
            AttackTarget.Friend => ItemDrop.ItemData.AiTarget.Friend,
            _ => ItemDrop.ItemData.AiTarget.Enemy,
        };
    }
}
