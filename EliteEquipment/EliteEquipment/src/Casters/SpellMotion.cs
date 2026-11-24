using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Each spell's own body motion (features/casters.md, "Spell animations": "these spells shouldn't just have that base
    /// white animation when charging/casting"): <see cref="Spell.Motion"/>, a workshop clip (<see cref="SpellClips"/>) played
    /// on the upper body in the charged staff's cast state, as the bolt's moves are (<see cref="WandMoves"/>). On the
    /// caster's machine a spell with a motion that plays there is cast as the bolt is: the wand's secondary attack names
    /// the cast state (<see cref="Choose"/>), whose trigger is held back (<see cref="WandAnimations"/>), and the clip's own
    /// <c>Hit</c> casts the spell (<see cref="SpellHits"/>); without the clip the game's cast plays as before. When the cast
    /// starts the caster's machine sends every player near the spell through the bolt's move message
    /// (<see cref="WandMoves"/>, the move numbers from <see cref="Base"/> on), handled at once there too; each machine puts
    /// the clip in that player's own cast slot (<see cref="PlayerControllers"/>), cross-fades the upper body in, shows the
    /// spell's charge (<see cref="SpellFx.Charge"/>) until the first <c>Hit</c>, fades out where the motion ends and puts the
    /// wand's casts back. A bolt's move or charge for the same player ends the motion at once.
    /// </summary>
    public static class SpellMotion
    {
        /// <summary>The bolt's move numbers end at <see cref="WandMoves.ToCharge"/>; from here on each is a spell (its place in <see cref="Spells.All"/>).</summary>
        public const int Base = WandMoves.ToCharge + 1;
        private const string CastState = "staff_charge_attack";
        private const float FadeIn = 0.05f;
        private const float FadeOut = 0.15f;
        private const float Wait = 1f;
        // The animator steps with the physics: a reading further into the clip than this past the time since the cross-fade
        // was asked for is still the state before it.
        private const float Slack = 0.1f;
        // A Hit this much sooner after the cross-fade than the clip's first is the state before (its clip changed under it).
        private const float EarlyHit = 0.15f;

        private static readonly int Casts = Animator.StringToHash(CastState);
        private static readonly int None = Animator.StringToHash("None");

        private sealed class Playing
        {
            public Player Player;
            public Spell Spell;
            public AnimationClip Clip;
            public GameObject Charge;
            public float Since;
            public bool Started;
            public bool Seen;
            public bool Leaving;
        }

        private static readonly List<Playing> playing = new List<Playing>();
        private static string gameCast;
        private static Spell driving;

        public static int Last => Base + Spells.All.Count - 1;

        /// <summary>The local player's current cast is moved by its own clip, whose <c>Hit</c>s cast it.</summary>
        public static bool Drives(Spell spell) => spell != null && spell == driving;

        /// <summary>
        /// Before the game clones the wand's secondary attack: it names the cast state when this spell's own motion plays for
        /// this player here, else the game's cast it was built with.
        /// </summary>
        public static void Choose(Player player, ItemDrop.ItemData wand, Spell spell)
        {
            Attack attack = wand.m_shared.m_secondaryAttack;
            if (attack.m_attackAnimation != CastState)
                gameCast = attack.m_attackAnimation;
            bool own = spell != null && SpellClips.Get(spell.Motion) != null && PlayerControllers.Owns(player, player.m_animator.runtimeAnimatorController);
            driving = own ? spell : null;
            attack.m_attackAnimation = own ? CastState : gameCast ?? attack.m_attackAnimation;
        }

        /// <summary>The game started the cast: its motion to every player near (this one at once).</summary>
        public static void Started(Player caster)
        {
            int index = driving != null ? IndexOf(driving) : -1;
            if (index >= 0)
                WandMoves.Send(caster, Base + index);
        }

        /// <summary>
        /// The player's spell motion is playing and its first <c>Hit</c> may have come: a <c>Hit</c> before that is the bolt's
        /// cast state fading out as the motion fades in, with the spell's clip now in it.
        /// </summary>
        public static bool PastFirstHit(Player player)
        {
            foreach (Playing now in playing)
            {
                if (now.Player == player)
                    return now.Started && Time.time - now.Since >= SpellClips.Hit(now.Spell.Motion, now.Clip) - EarlyHit;
            }
            return false;
        }

        /// <summary>A move heard for a player: any spell motion of theirs ends; true when the move is a spell's, begun here.</summary>
        public static bool Heard(Player player, int move)
        {
            Drop(player);
            int index = move - Base;
            if (index < 0 || index >= Spells.All.Count)
                return false;
            playing.Add(new Playing { Player = player, Spell = Spells.All[index], Since = Time.time });
            return true;
        }

        /// <summary>Every frame on a machine that draws: each heard motion started, followed, and ended.</summary>
        public static void Tick()
        {
            for (int i = playing.Count - 1; i >= 0; i--)
            {
                if (Step(playing[i]))
                    continue;
                End(playing[i]);
                playing.RemoveAt(i);
            }
        }

        private static void Drop(Player player)
        {
            for (int i = playing.Count - 1; i >= 0; i--)
            {
                if (playing[i].Player != player)
                    continue;
                End(playing[i]);
                playing.RemoveAt(i);
            }
        }

        private static bool Step(Playing now)
        {
            Animator animator = now.Player != null ? now.Player.m_animator : null;
            int layer = animator != null ? animator.GetLayerIndex("upperbody") : -1;
            if (layer < 0)
                return false;
            return now.Started ? Follow(now, animator, layer) : Begin(now, animator, layer);
        }

        private static bool Begin(Playing now, Animator animator, int layer)
        {
            now.Clip = SpellClips.Get(now.Spell.Motion);
            if (!PlayerControllers.Put(now.Player, now.Clip))
                return false;
            animator.CrossFadeInFixedTime(Casts, FadeIn, layer, 0f);
            now.Started = true;
            now.Since = Time.time;
            now.Charge = SpellFx.Charge(now.Player, now.Spell);
            return true;
        }

        /// <summary>The charge gone at the first <c>Hit</c>, the upper body faded out where the motion ends; false once out.</summary>
        private static bool Follow(Playing now, Animator animator, int layer)
        {
            float since = Time.time - now.Since;
            if (animator.IsInTransition(layer))
                return since < now.Clip.length + Wait;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
            float at = state.normalizedTime * now.Clip.length;
            if (state.shortNameHash != Casts || at > since + Slack)
                return !now.Seen && since < Wait;
            now.Seen = true;
            if (now.Charge != null && at >= SpellClips.Hit(now.Spell.Motion, now.Clip))
            {
                SpellFx.Remove(now.Charge);
                now.Charge = null;
            }
            if (!now.Leaving && at >= SpellClips.End(now.Spell.Motion, now.Clip))
            {
                animator.CrossFadeInFixedTime(None, FadeOut, layer);
                now.Leaving = true;
            }
            return since < now.Clip.length + Wait;
        }

        private static void End(Playing now)
        {
            SpellFx.Remove(now.Charge);
            now.Charge = null;
            if (now.Started)
                PlayerControllers.Reset(now.Player);
        }

        private static int IndexOf(Spell spell)
        {
            for (int i = 0; i < Spells.All.Count; i++)
            {
                if (Spells.All[i] == spell)
                    return i;
            }
            return -1;
        }
    }
}
