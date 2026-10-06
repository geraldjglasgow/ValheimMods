using System.Collections;
using System.Collections.Generic;
using EliteCreaturesReborn.Util;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The robbed player's half of a Thieving steal. The stack leaves the inventory at once - the player sees the theft
    /// and cannot use, drop or move an item that is already on its way - and waits here under the request's id until
    /// the creature's owner answers. The owner always answers: "banked" lets it go, "refused" (no room left, no longer
    /// the owner, the creature dead or the prefab unknown) hands it back. No answer at all - the creature unloaded or died
    /// on its owner before the request arrived - hands it back after <see cref="Timeout"/>. Back means into the cell it
    /// came from, else anywhere in the inventory, else at the player's feet. A "banked" that arrives after a give-back
    /// takes the returned stack again while it is still in the inventory, so a late answer cannot make two of it.
    /// </summary>
    public static class StealEscrow
    {
        public const string Reply = "ecr_steal_reply";

        // Far longer than any answer takes on a working connection, short enough that a lost item is back in seconds.
        private const float Timeout = 10f;

        // How long a given-back stack is remembered for a late "banked": past this the request is forgotten.
        private const float LateWindow = 60f;

        private sealed class Held
        {
            public ItemDrop.ItemData Item = null!;
            public Vector2i Cell;
            public float GivenBackAt = -1f;
            public bool GivenBack => GivenBackAt >= 0f;
        }

        private static readonly Dictionary<int, Held> Waiting = new Dictionary<int, Held>();
        private static readonly List<int> Expired = new List<int>();
        private static readonly WaitForSeconds TimeoutWait = new WaitForSeconds(Timeout);
        private static readonly WaitForSeconds RetryWait = new WaitForSeconds(1f);
        private static ZRoutedRpc? _registeredOn;
        private static int _counter;

        /// <summary>
        /// Takes <paramref name="item"/> out of the player's inventory and holds it under a fresh request id, which it
        /// returns. Registers the answer handler first, so even an answer delivered locally in the same call is caught.
        /// Returns 0 and takes nothing when there is no network or no game to run the timeout on.
        /// </summary>
        public static int Hold(Inventory inventory, ItemDrop.ItemData item)
        {
            EnsureRegistered();
            if (ZRoutedRpc.instance == null || Game.instance == null)
            {
                return 0;
            }
            ForgetOld();
            int id = ++_counter;
            Waiting[id] = new Held { Item = item, Cell = item.m_gridPos };
            inventory.RemoveItem(item); // takes the whole stack, whatever it was
            Game.instance.StartCoroutine(Expire(id));
            return id;
        }

        /// <summary>The owner's answer, sent straight to the robbed player over the world bus: the creature itself may
        /// have unloaded on the player's machine by the time it arrives. Delivered locally when the player owns it.</summary>
        public static void Answer(long asker, int id, bool banked)
        {
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(asker, Reply, id, banked);
            }
        }

        // Keyed on the bus, not a bool, so a new world (a fresh bus) registers again; the held items of the last
        // world went with its save and are forgotten.
        private static void EnsureRegistered()
        {
            ZRoutedRpc bus = ZRoutedRpc.instance;
            if (bus == null || ReferenceEquals(bus, _registeredOn))
            {
                return;
            }
            _registeredOn = bus;
            Waiting.Clear();
            bus.Register<int, bool>(Reply, (sender, id, banked) =>
                Guard.Run("StealEscrow.Reply", static answer => Settle(answer.id, answer.banked), (id, banked)));
        }

        private static void Settle(int id, bool banked)
        {
            if (!Waiting.TryGetValue(id, out Held held))
            {
                return; // already settled, or forgotten
            }
            if (banked)
            {
                Waiting.Remove(id);
                if (held.GivenBack)
                {
                    TakeAgain(held); // banked after all, after the timeout gave it back
                }
            }
            else if (held.GivenBack || GiveBack(held))
            {
                Waiting.Remove(id); // refused is final; while the player is dead, the timeout's wait delivers it
            }
        }

        private static IEnumerator Expire(int id)
        {
            yield return TimeoutWait;
            while (Player.m_localPlayer == null || Player.m_localPlayer.IsDead())
            {
                yield return RetryWait; // a player who died meanwhile gets it back once respawned
            }
            if (Waiting.TryGetValue(id, out Held held) && !held.GivenBack)
            {
                Log.Diag("a steal request got no answer; the item goes back");
                Guard.Run("StealEscrow.Expire", static entry => GiveBack(entry), held);
            }
        }

        // False with no living player to hand it to (dead, respawning): it stays held until there is one.
        private static bool GiveBack(Held held)
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                return false;
            }
            held.GivenBackAt = Time.time;
            string text = $"{held.Item.m_shared.m_name} x{held.Item.m_stack} was not stolen";
            Return(player, held.Item, held.Cell);
            Announce(text);
            return true;
        }

        private static void Return(Player player, ItemDrop.ItemData item, Vector2i cell)
        {
            Inventory inventory = player.GetInventory();
            bool free = cell.x < inventory.GetWidth() && cell.y < inventory.GetHeight() && inventory.GetItemAt(cell.x, cell.y) == null;
            bool added = free ? inventory.AddItem(item, cell) : inventory.AddItem(item);
            if (!added && item.m_stack > 0)
            {
                Vector3 feet = player.transform.position + Vector3.up * 0.5f;
                ItemDrop.DropItem(item, item.m_stack, feet, Quaternion.identity); // no room: the remainder lands at the feet
            }
        }

        private static void TakeAgain(Held held)
        {
            Inventory? inventory = Player.m_localPlayer != null ? Player.m_localPlayer.GetInventory() : null;
            if (inventory != null && inventory.ContainsItem(held.Item))
            {
                inventory.RemoveItem(held.Item);
                Announce($"{held.Item.m_shared.m_name} x{held.Item.m_stack} was stolen after all");
            }
        }

        private static void ForgetOld()
        {
            Expired.Clear();
            foreach (KeyValuePair<int, Held> pair in Waiting)
            {
                if (pair.Value.GivenBack && Time.time - pair.Value.GivenBackAt > LateWindow)
                {
                    Expired.Add(pair.Key);
                }
            }
            foreach (int id in Expired)
            {
                Waiting.Remove(id);
            }
        }

        private static void Announce(string text)
        {
            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text);
            }
        }
    }
}
