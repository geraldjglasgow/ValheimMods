using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// The player before a take: placed (at=, yaw= or face=) within the loaded area, god and ghost modes, gear equipped
    /// by prefab name (added to the inventory when missing), all gear taken off first with clear=true, hidden or shown;
    /// walkSpeed= for a slower (or faster) walk during the shot, put back when it ends.
    /// </summary>
    internal static class PlayerSetup
    {
        private static float? savedWalk;

        internal static void Apply(JObject spec, ShotFrame frame)
        {
            Player player = Player.m_localPlayer ? Player.m_localPlayer : throw new BridgeException("no local player");
            if (spec["at"] != null) Place(player, spec, frame);
            if (spec["god"] != null) player.SetGodMode(spec.Value<bool>("god"));
            if (spec["ghost"] != null) player.SetGhostMode(spec.Value<bool>("ghost"));
            if (spec.Value<bool?>("clear") == true) player.UnequipAllItems();
            if (spec["equip"] is JArray items) Dresser.Put(player, items.Select(item => item.Value<string>()));
            if (spec["unequip"] is JArray off) foreach (JToken item in off) Unequip(player, item.Value<string>());
            if (spec["hide"] != null) Hider.Set(player.gameObject, spec.Value<bool>("hide"));
            if (spec.Value<bool?>("heal") == true) player.Heal(player.GetMaxHealth() - player.GetHealth(), false);
            if (spec["walkSpeed"] != null) WalkSpeed(player, spec.Value<float>("walkSpeed"));
        }

        private static void WalkSpeed(Player player, float speed)
        {
            if (!savedWalk.HasValue) savedWalk = player.m_walkSpeed;
            player.m_walkSpeed = speed;
        }

        /// <summary>The player's own walk again, after a shot that changed it.</summary>
        internal static void RestoreWalk()
        {
            if (savedWalk.HasValue && Player.m_localPlayer) Player.m_localPlayer.m_walkSpeed = savedWalk.Value;
            savedWalk = null;
        }

        private static void Place(Player player, JObject spec, ShotFrame frame)
        {
            Vector3 feet = Spot.Parse(spec["at"], "player at").Resolve(frame);
            Move(player, feet, Cues.Facing(spec, frame, feet), spec.Value<float?>("pitch") ?? 0f);
        }

        /// <summary>The player set down at feet, turned to yaw, within the loaded area (no teleport, no fade).</summary>
        internal static void Move(Player player, Vector3 feet, Quaternion yaw, float pitch = 0f)
        {
            player.transform.SetPositionAndRotation(feet, yaw);
            if (player.m_body) (player.m_body.position, player.m_body.rotation, player.m_body.linearVelocity) = (feet, yaw, Vector3.zero);
            player.m_lookYaw = yaw;
            player.m_lookPitch = pitch;
            player.SetMouseLook(Vector2.zero);
        }

        private static void Unequip(Player player, string prefab)
        {
            ItemDrop.ItemData item = Find(player.GetInventory(), prefab);
            if (item != null) player.UnequipItem(item, true);
        }

        private static ItemDrop.ItemData Find(Inventory inventory, string prefab) =>
            inventory.GetAllItems().FirstOrDefault(i => i.m_dropPrefab && i.m_dropPrefab.name == prefab);
    }

    /// <summary>Actors made invisible to the camera: their renderers off, checked again now and then for new gear.</summary>
    internal static class Hider
    {
        private static readonly Dictionary<GameObject, HashSet<Renderer>> Hidden = new Dictionary<GameObject, HashSet<Renderer>>();
        private static int nextCheck;

        internal static void Set(GameObject go, bool hide)
        {
            if (hide)
            {
                if (!Hidden.ContainsKey(go)) Hidden[go] = new HashSet<Renderer>();
                Sweep(go, Hidden[go]);
                return;
            }
            if (!Hidden.TryGetValue(go, out HashSet<Renderer> off)) return;
            foreach (Renderer renderer in off) if (renderer) renderer.enabled = true;
            Hidden.Remove(go);
        }

        internal static void Tick()
        {
            if (Hidden.Count == 0 || Time.frameCount < nextCheck) return;
            nextCheck = Time.frameCount + 10;
            foreach (KeyValuePair<GameObject, HashSet<Renderer>> pair in Hidden.ToList())
                if (pair.Key) Sweep(pair.Key, pair.Value);
                else Hidden.Remove(pair.Key);
        }

        private static void Sweep(GameObject go, HashSet<Renderer> off)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                renderer.enabled = false;
                off.Add(renderer);
            }
        }

        internal static void Clear()
        {
            foreach (GameObject go in Hidden.Keys.ToList()) if (go) Set(go, false);
            Hidden.Clear();
        }
    }
}
