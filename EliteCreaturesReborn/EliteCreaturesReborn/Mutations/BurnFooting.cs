using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// What Flamebound's burning patches do to this machine's own player: once a second while they stand in one, on the
    /// ground, as much fire as the hottest patch under them, added to the game's own Burning - the flames on the body,
    /// the icon, the damage spread over the next seconds, shortened by Wet. Fire resistance cuts it; armour does not,
    /// as it does not cut the game's own burning ticks. Sent as a hit, armour cut a 6 fire hit below the least burn the
    /// game starts (1 fire over Burning's 5 s), so the patches did nothing past 9 armour. Names no attacker, so it
    /// neither scales again with the creature's mutations nor feeds Leeching. Decided on the player's own machine from
    /// the patches it has drawn, like every trail, so nothing burns them unseen; a jump over a patch clears it.
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
                Burn(player, fire);
            }
        }

        public void Drop()
        {
        }

        // The player's own fire resistance (gear, meads), then straight into Burning; immune leaves nothing to add.
        private static void Burn(Player player, float fire)
        {
            HitData hit = new HitData();
            hit.m_damage.m_fire = fire;
            hit.ApplyResistance(player.GetDamageModifiers(), out _);
            player.AddFireDamage(hit.m_damage.m_fire, hit.m_variant);
        }
    }
}
