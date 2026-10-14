using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Shattering Screech's deafness. The screech's attack item carries this status effect, so the game hands it to
    /// every player the screech hits, on that player's own machine (<c>Character.RPC_Damage</c> on the owner; god mode
    /// too). It waits out the rest of the scream (<see cref="BatFiendNumbers.ScreechDeafDelay"/>) so the scream itself is
    /// heard, then deafens the player the way a Screecher's shriek does (<see cref="Mutations.ShriekDeafness"/>: the game
    /// near-silent under a faint ringing, the "Ringing ears" icon and countdown, no spells) for
    /// <see cref="BatFiendNumbers.ScreechDeafSeconds"/>. It has no icon of its own, so the HUD never shows it. It sits in
    /// every object database, on every peer (<see cref="BatFiendStatuses"/>), so the hit's status hash finds it.
    /// </summary>
    public sealed class BatFiendScreech : StatusEffect
    {
        private const string Key = "ecr_batfiend_screech";

        private static BatFiendScreech? _template;

        private bool _fired;

        /// <summary>The effect the screech item carries, made once.</summary>
        public static StatusEffect Template()
        {
            if (_template == null)
            {
                _template = ScriptableObject.CreateInstance<BatFiendScreech>();
                _template.name = Key;
                _template.m_name = "$enemy_ecr_batfiend";
                _template.m_ttl = BatFiendNumbers.ScreechDeafDelay + 0.2f;
                _template.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            return _template;
        }

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
