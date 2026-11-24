using LocalEffects;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The healer's highlight (user, 2026-10-09: "when you point at a friendly person they kinda get highlighted"): while
    /// the local player holds a wand and a spellbook with a heal or Ward page (any page can be picked from the spell ring), the
    /// friend it would hit (<see cref="FriendTarget"/>) wears
    /// the workshop's looping marker (<c>ee_fx_heal_target</c>, a local copy under them, seen only by the healer). Looked
    /// for <see cref="Every"/> seconds; the marker moves when the pick does and goes when nobody is picked, the spell
    /// leaves the book, the wand leaves the hand or the inventory opens. <see cref="Current"/> is who a cast locks on to;
    /// it stays as it was while the spell ring is open and until its cast starts (<see cref="SpellRing.Aiming"/>), so a
    /// heal goes to the friend marked as the ring opened.
    /// </summary>
    public static class HealTarget
    {
        private const float Every = 0.1f;
        private const float PlayerHeight = 1.8f;
        private const float RingRadius = 0.4f;
        private const float Smallest = 0.6f;
        private const float Largest = 2.5f;

        private static float next;
        private static GameObject marker;
        private static Character marked;

        public static Character Current { get; private set; }

        public static void Tick()
        {
            if (Time.time < next || SpellRing.Aiming)
                return;
            next = Time.time + Every;
            Player player = Player.m_localPlayer;
            Current = Healing(player) ? FriendTarget.Find(player, Current) : null;
            Mark(Current);
        }

        private static bool Healing(Player player)
        {
            ItemDrop.ItemData wand = CastersSettings.On && player != null && !player.IsDead() ? CasterGearWorn.Wand(player) : null;
            return wand != null && !InventoryGui.IsVisible() && Marks(CasterGearWorn.Book(player));
        }

        /// <summary>The book holds a page aimed at a friend: a heal, or Ward.</summary>
        private static bool Marks(ItemDrop.ItemData book) =>
            BookPages.HasKind(book, SpellKind.Heal) || BookPages.Holds(book, Spells.Ward);

        private static void Mark(Character target)
        {
            if (target == marked && (target == null || marker != null))
                return;
            if (marker != null)
                Object.Destroy(marker);
            marker = null;
            marked = target;
            if (target != null)
                marker = LocalEffect.AttachScaled(CasterFx.Prepared(CasterFx.HealMarker), target.transform, target.transform.position, endless: true, Size(target));
        }

        /// <summary>
        /// The marker's size for a body (the workshop's numbers, ee_casterfx_meadows/BRIEF.md): authored for a player's
        /// 1.8 m, by height within <see cref="Smallest"/> to <see cref="Largest"/>, or wider for a long body so its feet
        /// stay inside the ring.
        /// </summary>
        private static float Size(Character target)
        {
            float tall = Mathf.Clamp(target.GetHeight() / PlayerHeight, Smallest, Largest);
            return Mathf.Clamp(Mathf.Max(tall, target.GetRadius() / RingRadius), Smallest, Largest);
        }
    }
}
