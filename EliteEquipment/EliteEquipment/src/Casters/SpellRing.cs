using System.Collections.Generic;
using HaloRing;
using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Choosing a spell with a wand drawn (features/casters.md, "How spells are chosen"; user, 2026-11-02: "right clicking
    /// and holding brings up the halo menu and you drag the mouse to the spell you want"). Right mouse held past
    /// <see cref="HoldSeconds"/> opens a ring (the HaloRing library) of every page in the spellbook in the left hand; the
    /// direction of the mouse from the screen centre picks one, and letting go casts it at once and remembers it on the
    /// wand (<see cref="LastSpell"/>); letting go in the centre, or Esc, casts nothing. A quick tap, let go sooner, casts
    /// the remembered spell again. The cast itself goes the game's way: <see cref="CasterControls"/> turns the request
    /// into the wand's secondary attack (<see cref="TakeCast"/>) and <see cref="SpellCast"/> casts the wand's spell.
    /// Without a book the hold opens nothing and says so. Read once a frame from the raw buttons (the game's "Block",
    /// which the ring's own input hold-back keeps from the player's controls while it is open). Local UI only.
    /// </summary>
    public static class SpellRing
    {
        /// <summary>Seconds right mouse is held before the ring opens; let go sooner, it is a tap.</summary>
        public const float HoldSeconds = 0.2f;

        private const float CastWithin = 0.5f;

        private enum Phase
        {
            Idle,
            Pressed,
            Open,
            Spent,
        }

        private static HaloRingMenu ring;
        private static Phase phase;
        private static bool wasHeld;
        private static float pressedAt;
        private static float castAt = -1f;
        private static int styledFor;

        /// <summary>The ring is open, or a cast waits for the player's controls: the heal target stays as it was.</summary>
        public static bool Aiming => phase == Phase.Open || castAt >= 0f;

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            bool held = ZInput.GetButton("Block") || ZInput.GetButton("JoyBlock");
            bool down = held && !wasHeld;
            wasHeld = held;
            if (!Ready(player))
            {
                Drop();
                return;
            }
            if (phase == Phase.Idle && down && player.TakeInput() && !Hud.InRadial() && !HaloRingMenu.Blocked)
                (phase, pressedAt) = (Phase.Pressed, Time.unscaledTime);
            else if (phase == Phase.Pressed)
                Pressing(player, held);
            else if (phase == Phase.Open)
                Choosing(player, held);
            else if (phase == Phase.Spent && !held)
                phase = Phase.Idle;
        }

        /// <summary>Taken once by the player's controls: a cast asked for within the last half second.</summary>
        public static bool TakeCast()
        {
            bool now = castAt >= 0f && Time.unscaledTime - castAt <= CastWithin;
            castAt = -1f;
            return now;
        }

        /// <summary>Right mouse and jump blinked: this press opens no ring and casts nothing.</summary>
        public static void Spend()
        {
            if (phase == Phase.Pressed)
                phase = Phase.Spent;
        }

        private static bool Ready(Player player) =>
            player != null && !player.IsDead() && CastersSettings.On && CasterGearWorn.Wand(player) != null;

        private static void Drop()
        {
            if (ring != null && ring.IsOpen)
                ring.Cancel();
            phase = Phase.Idle;
            castAt = -1f;
        }

        private static void Pressing(Player player, bool held)
        {
            if (!held)
            {
                phase = Phase.Idle;
                castAt = Time.unscaledTime;
                return;
            }
            if (Time.unscaledTime - pressedAt >= HoldSeconds)
                phase = Opened(player) ? Phase.Open : Phase.Spent;
        }

        /// <summary>The ring with the held book's pages; none, and why, without a book or with an empty one.</summary>
        private static bool Opened(Player player)
        {
            ItemDrop.ItemData book = CasterGearWorn.Book(player);
            List<Spell> pages = BookPages.Of(book);
            if (book == null || pages.Count == 0)
            {
                SpellCast.Tell(Lang.Get(book == null ? "ee_spell_no_book" : "ee_spellbook_empty"));
                return false;
            }
            ring ??= new HaloRingMenu("EliteEquipment", "spells", message => Plugin.Log?.LogError(message));
            if (styledFor != pages.Count)
                (ring.Style, styledFor) = (SpellRingItems.Style(pages.Count), pages.Count);
            return ring.Open(SpellRingItems.For(player, pages), pages.Count);
        }

        /// <summary>While open: follows the mouse; let go, the spell under it is remembered on the wand and cast.</summary>
        private static void Choosing(Player player, bool held)
        {
            if (!ring.Tick(Time.unscaledDeltaTime))
            {
                phase = Phase.Spent;
                return;
            }
            if (held)
                return;
            HaloRingPick pick = ring.Release(refuseDisabled: false);
            phase = Phase.Idle;
            ItemDrop.ItemData wand = CasterGearWorn.Wand(player);
            if (pick.Outcome != RingOutcome.Picked || !(pick.Item.Tag is Spell spell) || wand == null)
                return;
            LastSpell.Set(wand, spell);
            castAt = Time.unscaledTime;
        }
    }
}
