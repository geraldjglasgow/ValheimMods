using LocalEffects;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The healer's highlight (user, 2026-10-09: "when you point at a friendly person they kinda get highlighted"): while
    /// the local player holds a wand with a heal in either slot, the friend it would hit (<see cref="FriendTarget"/>) wears
    /// the workshop's looping marker (<c>ee_fx_heal_target</c>, a local copy under them, seen only by the healer). Looked
    /// for <see cref="Every"/> seconds; the marker moves when the pick does and goes when nobody is picked, the spell
    /// changes, the wand leaves the hand or the inventory opens. <see cref="Current"/> is who a cast locks on to.
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
            if (Time.time < next)
                return;
            next = Time.time + Every;
            Player player = Player.m_localPlayer;
            Current = Healing(player) ? FriendTarget.Find(player, Current) : null;
            Mark(Current);
        }

        private static bool Healing(Player player)
        {
            ItemDrop.ItemData wand = CastersSettings.On && player != null && !player.IsDead() ? CasterGearWorn.Wand(player) : null;
            return wand != null && !InventoryGui.IsVisible() && Loadout.Holds(player, wand, SpellKind.Heal);
        }

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
