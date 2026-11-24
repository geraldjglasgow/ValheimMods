using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The spellbook held open (features/casters.md, "The spellbook", "Held open"): after each player's animator, on every
    /// machine that draws (never a dedicated server: no graphics device), a player with a spellbook in the left hand (the
    /// item the game already shares: <see cref="VisEquipment"/>'s left item hash) within sight gets a <see cref="BookHold"/>
    /// that brings the arm forward, palm up, the book open on it. A player without a book costs one int compare; the hold
    /// goes once the book is gone and the arm has blended back. Only while <c>Wands and Spells</c> is on: off, the book
    /// stays closed in the shield hold.
    /// </summary>
    [HarmonyPatch(typeof(Player), "LateUpdate")]
    public static class BookPose
    {
        // Farther from the camera than this, the arm is left to the game.
        private const float Sight = 60f;
        private static readonly bool Headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        private static readonly Dictionary<Player, BookHold> holds = new Dictionary<Player, BookHold>();
        private static readonly Action<Player> tick = Tick;
        private static int[] books;

        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (!Headless)
                CasterSafe.Run("spellbook pose", tick, __instance);
        }

        private static void Tick(Player player)
        {
            bool held = Held(player);
            if (!held && holds.Count == 0)
                return;
            if (!holds.TryGetValue(player, out BookHold hold))
            {
                if (!held)
                    return;
                hold = BookHold.Of(player);
                holds[player] = hold;
            }
            bool busy = hold != null ? hold.Step(player, held, Time.deltaTime) : held;
            if (!busy)
                holds.Remove(player);
        }

        private static bool Held(Player player)
        {
            VisEquipment vis = player.m_visEquipment;
            return CastersSettings.On && vis != null && IsBook(vis.m_currentLeftItemHash) && vis.m_leftItemInstance != null
                && player.m_nview != null && player.m_nview.IsValid() && InSight(player);
        }

        private static bool IsBook(int hash)
        {
            if (hash == 0)
                return false;
            if (books == null)
                books = CasterGear.Books.Select(book => book.Prefab.GetStableHashCode()).ToArray();
            return Array.IndexOf(books, hash) >= 0;
        }

        private static bool InSight(Player player)
        {
            GameCamera camera = GameCamera.instance;
            return camera == null || (camera.transform.position - player.transform.position).sqrMagnitude < Sight * Sight;
        }

        /// <summary>A player gone from the scene takes its hold with it.</summary>
        [HarmonyPatch(typeof(Player), "OnDestroy")]
        private static class Gone
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance) => holds.Remove(__instance);
        }
    }
}
