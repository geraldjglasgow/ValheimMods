using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace HighSeatPlus.Crowd
{
    /// <summary>
    /// One other player as this PC draws them: their distance at the last decision, whether they are drawn simpler, and
    /// exactly what was changed to do it (the animator stepped by hand, which cloth was stopped), so that and nothing else
    /// is put back. Made when <see cref="CrowdList"/> first finds the player and dropped when the game stops listing them;
    /// it only points at the player's own parts. A new record stays in full for <see cref="CrowdDecision.KeepFullSeconds"/>,
    /// so the player's gear and animator are set up before anything is changed.
    /// </summary>
    internal sealed class FarPlayer
    {
        public readonly Player Player;
        public readonly int Id;
        public readonly Animator Animator;
        public readonly VisEquipment Vis;

        /// <summary>Squared metres to the nearer of the local player and the camera, at the last decision.</summary>
        public float SqrDistance;

        /// <summary>The decision round that last found the player in the game's list.</summary>
        public int Seen;

        /// <summary>The character's player id (Party's key), read once it is known.</summary>
        public long PlayerId;

        public bool Simple;

        /// <summary>Time.time before which the player stays in full (just loaded, or just come back in full).</summary>
        public float FullUntil;

        // The animator run by hand (SteppedAnimator): the time it is owed and its own disable settings.
        public bool Stepping;
        public float Pending;
        public bool KeptState;
        public bool WroteDefaults;

        // Cloth stopped by StillCloth, each started again on its own.
        public readonly List<MagicaCloth> Cloths = new List<MagicaCloth>();
        public readonly List<PlayerClothWindShelter> Shelters = new List<PlayerClothWindShelter>();
        public readonly List<Cloth> UnityCloths = new List<Cloth>();

        public FarPlayer(Player player, float now)
        {
            Player = player;
            Id = player.GetInstanceID();
            Animator = player.m_animator;
            Vis = player.m_visEquipment;
            FullUntil = now + CrowdDecision.KeepFullSeconds;
        }

        /// <summary>Drawn simpler: cloth held still, the animator stepped by hand. A part that cannot change now is left as it is.</summary>
        public void Simplify()
        {
            Simple = true;
            StillCloth.Stop(this);
            SteppedAnimator.Start(this);
        }

        /// <summary>Back in full at once: the animator caught up and running on its own, every stopped cloth moving again.</summary>
        public void Restore(float now)
        {
            if (!Simple)
            {
                return;
            }
            Simple = false;
            FullUntil = now + CrowdDecision.KeepFullSeconds;
            SteppedAnimator.Stop(this);
            StillCloth.Start(this);
        }
    }
}
