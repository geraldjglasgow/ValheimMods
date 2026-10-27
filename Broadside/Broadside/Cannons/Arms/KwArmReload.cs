using System.Linq;
using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a cannon loads, on the game that owns it: the gunner's, whose library reload (one step, <see cref="KwArmDesigns"/>)
    /// starts by itself once the shot's motion has run, when <see cref="Find"/> finds a load, and takes it as the step
    /// begins (<see cref="Take"/>); or the captain's, reloading what his broadside fired (<see cref="KwBroadsideReload"/>).
    /// The load is for a trigger (the gunner's last pulled, <see cref="KwArm.Want"/>; the broadside's, the one it fired), or
    /// the other one when the store has nothing for that: its shot's ball (the first of its ammunition the store has enough
    /// of) and its powder (<see cref="KwShot.Charge"/>), all of which are taken from the store then (<see cref="Load"/>: a
    /// gunner's pack, or the captain's, <see cref="KwAmmoStore"/>); which trigger the load is for and the ball loaded go to
    /// the ZDO (the shot's projectile carries the ball). With nothing to load it waits.
    /// </summary>
    public static class KwArmReload
    {
        /// <summary>What the gunner would load now (the ball's item); null when he carries nothing a trigger's shot takes.</summary>
        public static ItemDrop.ItemData? Find(KwArm arm, Player gunner) =>
            Choose(arm.Def, KwAmmoStore.Pack(gunner), arm.Want, out string ammo) >= 0 ? Item(ammo) : null;

        /// <summary>The gunner's reload begins: the shot's powder and ball taken from him, the trigger and the ball written to the ZDO.</summary>
        public static void Take(KwArm arm, Player gunner) => Load(arm, KwAmmoStore.Pack(gunner), arm.Want);

        /// <summary>
        /// On the cannon's owner: a load for `want` (else the other trigger) taken from `store` and written to the ZDO (the
        /// trigger it is for, the ball); false, nothing taken, when the store has none.
        /// </summary>
        public static bool Load(KwArm arm, KwAmmoStore store, int want)
        {
            int kind = Pay(arm.Def, store, want, out string ammo);
            if (kind < 0)
                return false;
            arm.Link.Zdo.Set(arm.Link.LoadKind, kind);
            arm.Link.Zdo.Set(arm.Link.Ammo, ammo);
            return true;
        }

        /// <summary>A load for `want` (else the other trigger) taken from `store`: the trigger it is for and its ball; -1, nothing taken, when the store has none.</summary>
        public static int Pay(KwArmDef def, KwAmmoStore store, int want, out string ammo)
        {
            int kind = Choose(def, store, want, out ammo);
            if (kind >= 0)
                Take(store, KwArmTrigger.Of(def, kind), ammo);
            return kind;
        }

        /// <summary>The first item a load for `want` lacks in the store (its ball, else its powder), an item prefab; null when the store holds a load for either trigger.</summary>
        public static string? Short(KwArmDef def, KwAmmoStore store, int want) =>
            Choose(def, store, want, out _) >= 0 ? null : Missing(store, KwArmTrigger.Of(def, want));

        /// <summary>How far a reload under way has gone, 0 to 1 (0 with none).</summary>
        public static float Progress(KwArm arm, in WeaponState s) => s.Busy ? Mathf.Clamp01(s.StepTime / arm.Design.Reload.Steps[s.Step - 1]) : 0f;

        /// <summary>Seconds a reload under way has still to run.</summary>
        public static float Left(KwArm arm, in WeaponState s) => s.Busy ? Mathf.Max(0f, arm.Design.Reload.Steps[s.Step - 1] - s.StepTime) : 0f;

        /// <summary>Why the gunner cannot load the shot (out of its ammunition or its charge), or null; the ammunition he would load ("" for none).</summary>
        public static string? Lacks(Player gunner, KwShot shot, out string ammo) => Lacks(KwAmmoStore.Pack(gunner), shot, out ammo);

        /// <summary>Why the store cannot load the shot, or null; the ammunition it would load ("" for none).</summary>
        public static string? Lacks(KwAmmoStore store, KwShot shot, out string ammo)
        {
            string? carried = Carried(store, shot);
            ammo = carried ?? "";
            if (carried == null)
                return KwArmWords.OutOf(shot);
            string? powder = Uncharged(store, shot);
            return powder == null ? null : KwArmWords.OutOfCharge(powder, shot);
        }

        /// <summary>Whether a load for one trigger fires from another: the same trigger, or two shots of the same ammunition and charge.</summary>
        public static bool Fits(KwArmDef def, int loaded, int trigger)
        {
            if (loaded == trigger)
                return true;
            KwShot a = KwArmTrigger.Of(def, loaded), b = KwArmTrigger.Of(def, trigger);
            return a.AmmoPerShot == b.AmmoPerShot && a.Ammo.SequenceEqual(b.Ammo) && a.Charge.SequenceEqual(b.Charge);
        }

        /// <summary>A loaded ammunition's item data, which the shot's projectiles carry; null for none.</summary>
        public static ItemDrop.ItemData? Item(string prefab)
        {
            GameObject? item = prefab.Length > 0 && ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            ItemDrop? drop = item != null ? item.GetComponent<ItemDrop>() : null;
            return drop != null ? drop.m_itemData : null;
        }

        // The trigger to load for: the one wanted when the store has its shot's ammunition and charge, else the other; -1 for neither.
        private static int Choose(KwArmDef def, KwAmmoStore store, int want, out string ammo)
        {
            int count = def.Alternate != null ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                int kind = (want + i) % count;
                if (Lacks(store, KwArmTrigger.Of(def, kind), out ammo) == null)
                    return kind;
            }
            ammo = "";
            return -1;
        }

        // The item the shot lacks first: its (first) ammunition when the store has none of it, else the first powder short.
        private static string? Missing(KwAmmoStore store, KwShot shot) =>
            Carried(store, shot) == null ? shot.Ammo.FirstOrDefault() : Uncharged(store, shot);

        // The first of the shot's charge the store holds too little of (an item prefab); null when it holds it all.
        private static string? Uncharged(KwAmmoStore store, KwShot shot)
        {
            foreach ((string prefab, int amount) in shot.Charge)
            {
                string? name = KwArmWords.SharedName(prefab);
                if (name == null || store.Count(name) < amount)
                    return prefab;
            }
            return null;
        }

        // The first of the shot's ammunition the store holds enough of; null for none.
        private static string? Carried(KwAmmoStore store, KwShot shot)
        {
            foreach (string prefab in shot.Ammo)
            {
                string? name = KwArmWords.SharedName(prefab);
                if (name != null && store.Count(name) >= shot.AmmoPerShot)
                    return prefab;
            }
            return null;
        }

        // Takes the shot's charge, then its ammunition.
        private static void Take(KwAmmoStore store, KwShot shot, string prefab)
        {
            foreach ((string item, int amount) in shot.Charge)
                store.Remove(KwArmWords.SharedName(item) ?? item, amount);
            string? name = KwArmWords.SharedName(prefab);
            if (name != null)
                store.Remove(name, shot.AmmoPerShot);
        }
    }
}
