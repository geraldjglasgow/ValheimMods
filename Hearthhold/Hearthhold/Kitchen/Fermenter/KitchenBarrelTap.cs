using System.Collections.Generic;
using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// The meads of a batch carry the base's stars exactly, with no new roll. Tapping runs RPC_Tap on the barrel's ZDO
    /// owner: when Ready it remembers the content, clears the content and start time, and Invokes DelayedTap a moment
    /// later, which instantiates the produced meads there (ItemDrop.Awake runs inside each Instantiate). When RPC_Tap
    /// did tap (the content went from set to 0) the base's stars move from the barrel's ZDO into memory for that
    /// barrel and the key is cleared, so a batch put in before DelayedTap starts clean; DelayedTap then opens a
    /// <see cref="SpawnStars"/> scope giving every new star item those stars. Fermenter.DropAllItems (never called by the
    /// game today, since a destroyed barrel's OnDestroyed does nothing, but a mod may) drops the base, or the meads when
    /// Ready, then clears the content: the dropped items get the base's stars the same way and the key is cleared.
    /// GrindstoneSkills clears its own key on the same two methods; the two never touch each other's.
    /// </summary>
    public static class KitchenBarrelTap
    {
        private static readonly Dictionary<Fermenter, int> tapped = new Dictionary<Fermenter, int>();
        private static readonly List<Fermenter> dead = new List<Fermenter>();
        private static readonly SpawnStars.Roller roller = Give;
        private static SpawnStars.Roller previous;
        private static int giving;

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.RPC_Tap))]
        private static class Tap
        {
            [HarmonyPrefix]
            private static void Prefix(Fermenter __instance, out int __state) =>
                __state = HookGuard.Run("barrel tap", () => __instance.GetContent(), 0);

            [HarmonyPostfix]
            private static void Postfix(Fermenter __instance, int __state)
            {
                if (__state != 0)
                    HookGuard.Run("barrel tap", static fermenter => Tapped(fermenter), __instance);
            }
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.DelayedTap))]
        private static class Spawn
        {
            [HarmonyPrefix]
            private static void Prefix(Fermenter __instance, out bool __state) =>
                __state = HookGuard.Run("barrel spawn", () => Open(TakeTapped(__instance)), false);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state) => Close(__state);
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.DropAllItems))]
        private static class Drop
        {
            [HarmonyPrefix]
            private static void Prefix(Fermenter __instance, out bool __state) =>
                __state = HookGuard.Run("barrel drops", () => __instance.GetContent() != 0 && Open(KitchenBarrel.Get(__instance.m_nview.GetZDO())), false);

            [HarmonyFinalizer]
            private static void Finalizer(Fermenter __instance, bool __state)
            {
                Close(__state);
                HookGuard.Run("barrel drops", static fermenter => ClearWhenEmpty(fermenter), __instance);
            }
        }

        /// <summary>After a tap emptied the barrel: its base's stars kept for DelayedTap, the key cleared.</summary>
        private static void Tapped(Fermenter fermenter)
        {
            ZNetView nview = fermenter.m_nview;
            if (nview == null || !nview.IsValid() || fermenter.GetContent() != 0)
                return;
            ZDO zdo = nview.GetZDO();
            int stars = KitchenBarrel.Get(zdo);
            KitchenBarrel.Clear(zdo);
            Prune();
            if (stars > 0)
                tapped[fermenter] = stars;
            else
                tapped.Remove(fermenter);
        }

        /// <summary>The stars kept for this barrel's tap, forgotten; 0 when none are.</summary>
        private static int TakeTapped(Fermenter fermenter)
        {
            if (!tapped.TryGetValue(fermenter, out int stars))
                return 0;
            tapped.Remove(fermenter);
            return stars;
        }

        private static void ClearWhenEmpty(Fermenter fermenter)
        {
            ZNetView nview = fermenter.m_nview;
            if (nview != null && nview.IsValid() && fermenter.GetContent() == 0)
                KitchenBarrel.Clear(nview.GetZDO());
        }

        /// <summary>Forgets barrels destroyed between a tap and its DelayedTap.</summary>
        private static void Prune()
        {
            foreach (Fermenter fermenter in tapped.Keys)
            {
                if (fermenter == null)
                    dead.Add(fermenter);
            }
            foreach (Fermenter fermenter in dead)
                tapped.Remove(fermenter);
            dead.Clear();
        }

        /// <summary>Opens a scope giving every new star item <paramref name="stars"/>; false (nothing opened) for 0.</summary>
        private static bool Open(int stars)
        {
            if (stars <= 0)
                return false;
            giving = stars;
            previous = SpawnStars.Open(roller);
            return true;
        }

        private static void Close(bool opened)
        {
            if (!opened)
                return;
            SpawnStars.Close(previous);
            previous = null;
            giving = 0;
        }

        private static int Give(ItemDrop.ItemData item) => giving;
    }
}
