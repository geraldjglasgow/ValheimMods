using UnityEngine;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// Keeps <see cref="EcfAggregate"/> on the local player: creates the template once, adds a copy when it is missing
    /// (first spawn, respawn after death, anything that cleared the player's status effects), removes it when effects
    /// are switched off, and pushes the active value set into it. Local player only; never on a dedicated server.
    /// <para>
    /// The game adds a shallow clone of the template; we never keep a reference to that clone across frames but look it
    /// up by name hash when needed (one hash-set check), because death and logout destroy it under us.
    /// </para>
    /// </summary>
    internal static class AggregateHost
    {
        private static EcfAggregate? _template;

        /// <summary>The value set the overrides read: <see cref="AggregateBuilder.Normal"/> or, while health-critical, <see cref="AggregateBuilder.Critical"/>.</summary>
        public static AggregateValues Current { get; private set; } = AggregateBuilder.Normal;

        public static bool IsPresent(Player player) => player.GetSEMan().HaveStatusEffect(EcfAggregate.Hash);

        /// <summary>After a rebuild: make sure the effect is on the player and holds the current values.</summary>
        public static void Apply(Player player)
        {
            if (player.IsDead())
            {
                // Death removed every status effect; the respawned Player gets a fresh one.
                HealthCritical.Reset();
                SelectCurrent();
                return;
            }
            SelectCurrent();
            EcfAggregate? effect = Find(player) ?? player.GetSEMan().AddStatusEffect(Template) as EcfAggregate;
            effect?.ApplyFields(Current);
        }

        /// <summary>Effects switched off: take the status effect away (quietly) and forget the condition.</summary>
        public static void Remove(Player player)
        {
            player.GetSEMan().RemoveStatusEffect(EcfAggregate.Hash, quiet: true);
            HealthCritical.Reset();
            SelectCurrent();
        }

        /// <summary>The health-critical state flipped (from the effect's own tick): swap sets, rewrite fields.</summary>
        public static void OnCriticalFlip(EcfAggregate effect)
        {
            SelectCurrent();
            if (HealthCritical.Active)
            {
                Meads.OnBecameCritical();
            }
            effect.ApplyFields(Current);
            if (effect.m_character is Player player)
            {
                FieldWrites.Apply(player, Current);
            }
        }

        private static void SelectCurrent() =>
            Current = HealthCritical.Active ? AggregateBuilder.Critical : AggregateBuilder.Normal;

        private static EcfAggregate? Find(Player player) =>
            player.GetSEMan().GetStatusEffect(EcfAggregate.Hash) as EcfAggregate;

        // Built once per process and kept alive: every clone the game makes shares the template's native object
        // (MemberwiseClone), so it must never be unloaded or destroyed. Never registered in ObjectDB.
        private static EcfAggregate Template
        {
            get
            {
                if (_template == null)
                {
                    _template = ScriptableObject.CreateInstance<EcfAggregate>();
                    _template.name = EcfAggregate.EffectName;
                    _template.hideFlags = HideFlags.HideAndDontSave;
                    _template.m_name = "EliteCrafting";
                    _template.m_hidden = true;
                    _template.m_icon = null;
                    _template.m_ttl = 0f;
                    _template.m_startMessage = "";
                    _template.m_stopMessage = "";
                }
                return _template;
            }
        }
    }
}
