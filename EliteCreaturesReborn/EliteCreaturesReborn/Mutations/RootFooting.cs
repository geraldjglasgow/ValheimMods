using System.Collections.Generic;
using EliteCreaturesReborn.Visuals;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// What Binding's root patches do to this machine's own player: stepping into one on the ground, they are held fast
    /// for as long as the patch holds (the "Rooted" status, <see cref="TrailStatus.Root"/>): no walking, running,
    /// jumping or rolling, though they can still turn, block and strike. The roots burst out of the ground at their feet
    /// as they take hold. Once they let go the player has <see cref="Free"/> seconds in which no root takes them, enough
    /// to walk out of the patch and past the next. Decided on the player's own machine from the patches it has drawn,
    /// like every trail, so nothing holds them unseen.
    /// </summary>
    internal sealed class RootFooting : IPatchFooting
    {
        /// <summary>Seconds after a hold ends before roots can take the player again.</summary>
        public const float Free = 3f;

        private const string BurstEffect = "fx_gdking_rootspawn";
        private static readonly string[] Burst = { "rootspawn", "root", "vine" };

        private readonly TrailKind _kind;
        private float _freeAt;

        public RootFooting(TrailKind kind) => _kind = kind;

        public void Tick(Player? player, List<GroundPatch> patches)
        {
            if (player == null || player.IsDead() || Time.time < _freeAt || !PatchContact.Feels(player))
            {
                return;
            }
            Vector3 feet = player.transform.position;
            if (PatchContact.Strongest(feet, patches, out float seconds) && TrailStatus.Root(player, _kind, seconds) != null)
            {
                _freeAt = Time.time + seconds + Free;
                CosmeticClone.Flash(EffectResolver.Resolve(BurstEffect, Burst, "Binding root burst"), feet, 1f);
            }
        }

        public void Drop()
        {
        }
    }
}
