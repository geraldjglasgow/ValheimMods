using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// What Flamebound's burning patches do to this machine's own player: once a second while they stand in one, on the
    /// ground, a fire hit as strong as the hottest patch under them. The game turns it into its own Burning - the
    /// flames on the body, the icon, the damage spread over the next seconds, shortened by Wet - and fire resistance
    /// and armour cut it as they cut any fire. The hit names no attacker, so it neither scales again with the creature's
    /// mutations nor feeds Leeching. Decided on the player's own machine from the patches it has drawn, like every trail,
    /// so nothing burns them unseen; a jump over a patch clears it.
    /// </summary>
    internal sealed class BurnFooting : IPatchFooting
    {
        /// <summary>Seconds between two burns while the player stays in the fire.</summary>
        private const float Every = 1f;

        private float _nextBurn;

        public void Tick(Player? player, List<GroundPatch> patches)
        {
            if (player == null || player.IsDead() || Time.time < _nextBurn || !PatchContact.Feels(player))
            {
                return;
            }
            if (PatchContact.Strongest(player.transform.position, patches, out float fire))
            {
                _nextBurn = Time.time + Every;
                player.Damage(Burn(player, fire));
            }
        }

        public void Drop()
        {
        }

        private static HitData Burn(Player player, float fire)
        {
            HitData hit = new HitData();
            hit.m_damage.m_fire = fire;
            hit.m_point = player.transform.position;
            hit.m_hitType = HitData.HitType.Burning;
            return hit;
        }
    }
}
