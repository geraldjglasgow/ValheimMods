using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// Puts a creature's fight and loot on its shell (features/custom-creatures.md, section 3: drops, gear, damage, new
    /// attacks; section 4: a human's gear), from <see cref="CreatureBuild.Definition"/>, in this order:
    /// <list type="number">
    /// <item><c>drops</c>: the drop table (<see cref="DropTable"/>), on any creature.</item>
    /// <item><c>gear</c>: what it carries (<see cref="GearLists"/>).</item>
    /// <item><c>damage</c> and <c>projectile</c>: on every attack it carries (<see cref="CreatureWide"/>).</item>
    /// <item><c>attacks</c>: new or changed attacks (<see cref="NewAttacks"/>, <see cref="AttackFields"/>).</item>
    /// <item>then every attack it carries is checked against its body (<see cref="AttackJoints"/>).</item>
    /// </list>
    /// Everything left out keeps the base's (or an earlier definition's) value, and nothing of the base or of any game item
    /// is ever changed: the drop table is a new list, every changed item is the creature's own copy (<see cref="OwnItems"/>),
    /// put in the world's item database once the creature is built (<see cref="PartItems"/>). An unknown item, projectile
    /// or status effect fails the creature at its field. Gear and attacks need a Humanoid, the game's creature that carries
    /// its attacks as items; any other base gets a warning and keeps its own fight.
    /// <para>
    /// All of it is prefab data, the same on every peer from the same definitions: the game hands out the gear (from the
    /// creature's seed), rolls the drops and runs the attacks on the creature's owner as it does for its own creatures, so
    /// nothing here runs while the game plays.
    /// </para>
    /// </summary>
    internal sealed class CombatStep : ICreatureStep
    {
        public string Name => "combat";

        public void Apply(CreatureBuild build)
        {
            CreatureDefinition definition = build.Definition;
            if (definition.Drops != null)
            {
                DropTable.Apply(build, definition.Drops);
            }
            if (build.Report.Failed || !Fights(definition))
            {
                return;
            }
            Humanoid? humanoid = build.Shell.GetComponent<Humanoid>();
            if (humanoid == null)
            {
                build.Report.Warn($"'{build.Base?.name}' carries no attacks as items (it is no Humanoid), so its gear, damage, projectile and attacks are left as they are");
                return;
            }
            Arm(build, humanoid, definition);
        }

        private static void Arm(CreatureBuild build, Humanoid humanoid, CreatureDefinition definition)
        {
            if (definition.Gear != null)
            {
                GearLists.Apply(build, humanoid, definition.Gear);
            }
            CreatureWide? wide = build.Report.Failed ? null : CreatureWide.Read(build);
            if (build.Report.Failed)
            {
                return;
            }
            wide?.ApplyToCarried(humanoid);
            if (definition.Attacks != null && definition.Attacks.Count > 0 && !build.Report.Failed)
            {
                NewAttacks.Apply(build, humanoid, wide, definition.Attacks);
            }
            if (!build.Report.Failed)
            {
                AttackJoints.Fit(build, humanoid);
            }
        }

        /// <summary>Whether the definition sets anything of the creature's fight (everything here but its drops).</summary>
        private static bool Fights(CreatureDefinition definition) =>
            definition.Gear != null || definition.Damage != null || definition.Projectile != null || (definition.Attacks?.Count ?? 0) > 0;
    }
}
