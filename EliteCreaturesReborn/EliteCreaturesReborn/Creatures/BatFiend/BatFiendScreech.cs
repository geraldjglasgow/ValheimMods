using StatusTemplates;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Shattering Screech's deafness. The screech's attack item carries this status effect, so the game hands it to
    /// every player the screech hits, on that player's own machine (<c>Character.RPC_Damage</c> on the owner; god mode
    /// too). It waits out the rest of the scream (<see cref="BatFiendNumbers.ScreechDeafDelay"/>) so the scream itself is
    /// heard, then deafens the player the way a Screecher's shriek does (<see cref="Mutations.ShriekDeafness"/>: the game
    /// near-silent under a faint ringing, the "Ringing ears" icon and countdown, no spells) for
    /// <see cref="BatFiendNumbers.ScreechDeafSeconds"/>. It has no icon of its own, so the HUD never shows it. It sits in
    /// every object database, on every peer (<see cref="BatFiendPrefabs"/>), so the hit's status hash finds it.
    /// </summary>
    public sealed class BatFiendScreech : StatusEffect
    {
        /// <summary>The effect the screech item carries.</summary>
        public static readonly StatusTemplate<BatFiendScreech> Template = new StatusTemplate<BatFiendScreech>(
            "ecr_batfiend_screech", se => (se.m_name, se.m_ttl) = ("$enemy_ecr_batfiend", BatFiendNumbers.ScreechDeafDelay + 0.2f));

        private bool _fired;

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);
            if (!_fired && m_time >= BatFiendNumbers.ScreechDeafDelay && m_character is Player player && player == Player.m_localPlayer)
            {
                _fired = true;
                Mutations.ShriekDeafness.Apply(player, BatFiendNumbers.ScreechDeafSeconds);
            }
        }
    }
}
