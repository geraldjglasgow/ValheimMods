using StatusTemplates;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// Keeps <see cref="EcfAggregate"/> on the local player: creates the template once, adds a copy when it is missing
    /// (first spawn, respawn after death, anything that cleared the player's status effects), removes it when effects
    /// are switched off, and pushes the active value set into it. Local player only; never on a dedicated server.
    /// <para>
    /// The game adds a shallow clone of the template; we never keep a reference to that clone across frames but look it
    /// up by name hash when needed (one hash-set check), because death and logout destroy it under us. The template is
    /// never registered in ObjectDB.
    /// </para>
    /// </summary>
    internal static class AggregateHost
    {
        private static readonly StatusTemplate<EcfAggregate> Aggregate = new StatusTemplate<EcfAggregate>(
            EcfAggregate.EffectName, se => (se.m_name, se.m_hidden) = ("EliteCrafting", true));

        /// <summary>The value set the overrides read: <see cref="AggregateBuilder.Values"/>.</summary>
        public static AggregateValues Current => AggregateBuilder.Values;

        public static bool IsPresent(Player player) => Aggregate.Has(player);

        /// <summary>After a rebuild: make sure the effect is on the player and holds the current values.</summary>
        public static void Apply(Player player)
        {
            if (player.IsDead())
            {
                // Death removed every status effect; the respawned Player gets a fresh one.
                return;
            }
            Aggregate.Hold(player)?.ApplyFields(Current);
        }

        /// <summary>Effects switched off: take the status effect away (quietly).</summary>
        public static void Remove(Player player) => Aggregate.Drop(player);
    }
}
