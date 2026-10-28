using System.Collections;
using System.Globalization;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Back on deck after logging in (the user, 2026-10-27: "I logged off on a ship that doesn't have a hatch and then when
    /// I logged on I was inside of the ship and there is literally no way out ... remember where the player logged off at
    /// on the ship so they respawn there"). Whenever the game saves the local player (logout, quit, its autosave) aboard a
    /// launched ship, his own save keeps, for this world, the ship's id (<see cref="KwShipRiders.Id"/>) and where he
    /// stood and faced in the ship's frame. Spawning in that world again at that place (not a respawn at a bed), he is
    /// put back on that spot as soon as the ship is loaded and built (<see cref="KwShipBody.Built"/>): the game puts him at its logout point before the
    /// ship's boxes exist, and the ship floats a little differently each load. Looked for <see cref="Patience"/> seconds;
    /// a ship gone by then leaves him where the game put him. Local only: his own save and his own position, which his
    /// own sync carries to the other peers.
    /// </summary>
    [HarmonyPatch]
    public static class KwShipLogout
    {
        private const string Prefix = "broadside_aboard_";
        private const float Patience = 30f, Near = 40f, Lift = 1.5f, Step = 0.1f;

        [HarmonyPatch(typeof(Player), nameof(Player.Save)), HarmonyPrefix]
        private static void Saving(Player __instance) => Guard.Run("Broadside aboard at save", Remember, __instance);

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPostfix]
        private static void Spawned(Player __instance) => Guard.Run("Broadside aboard at login", Arm, __instance);

        private static string? Key => ZNet.instance != null ? Prefix + ZNet.instance.GetWorldUID() : null;

        private static void Remember(Player player)
        {
            string? key = Key;
            if (player != Player.m_localPlayer || key == null)
                return;
            KwShipRiders? ship = Aboard(player);
            if (ship == null || ship.Id == 0L)
            {
                player.m_customData.Remove(key);
                return;
            }
            Transform frame = ship.transform;
            Vector3 at = frame.InverseTransformPoint(player.transform.position), world = player.transform.position;
            float yaw = Mathf.DeltaAngle(frame.eulerAngles.y, player.transform.eulerAngles.y);
            player.m_customData[key] = string.Join(";", ship.Id.ToString(CultureInfo.InvariantCulture),
                F(at.x), F(at.y), F(at.z), F(yaw), F(world.x), F(world.y), F(world.z));
        }

        // The launched ship he is on: the game's onboard trigger, else over its hull's plan higher up (a cabin's roof, a mast).
        private static KwShipRiders? Aboard(Player player)
        {
            Ship? local = Ship.GetLocalShip();
            KwShipRiders? found = local != null ? local.GetComponent<KwShipRiders>() : null;
            if (found != null)
                return found;
            foreach (KwShipRiders ship in KwShipRiders.Loaded)
            {
                KwBpHull? hull = ship != null ? ship.GetComponent<KwShipBody>()?.Hull : null;
                if (hull != null && KwShipBreakRules.Over(ship!.transform.InverseTransformPoint(player.transform.position), hull))
                    return ship;
            }
            return null;
        }

        // Spawned at the place he was saved aboard: the record is used once, and he is put back when the ship is there.
        private static void Arm(Player player)
        {
            string? key = Key;
            if (player != Player.m_localPlayer || key == null || !player.m_customData.TryGetValue(key, out string text))
                return;
            player.m_customData.Remove(key);
            if (!Read(text, out long id, out Vector3 at, out float yaw, out Vector3 world))
                return;
            if ((player.transform.position - world).sqrMagnitude > Near * Near)
                return;
            KwBpRunner.Run("Broadside aboard at login", Return(player, id, at, yaw), () => { });
        }

        private static IEnumerator Return(Player player, long id, Vector3 at, float yaw)
        {
            float until = Time.time + Patience;
            KwShipRiders? ship = null;
            while (Time.time < until && player != null && (ship = Built(id)) == null)
                yield return null;
            // one frame more, so the ship's boxes are in the physics scene
            yield return null;
            if (ship == null || player == null || player.IsDead())
                yield break;
            Transform frame = ship.transform;
            KwClimbRoom.Place(player, Clear(player, frame.TransformPoint(at)));
            player.transform.rotation = Quaternion.Euler(0f, frame.eulerAngles.y + yaw, 0f);
            Log.Info("Broadside: back on the ship you logged off on.");
        }

        private static KwShipRiders? Built(long id)
        {
            foreach (KwShipRiders ship in KwShipRiders.Loaded)
            {
                if (ship != null && ship.Id == id && ship.GetComponent<KwShipBody>()?.Built == true)
                    return ship;
            }
            return null;
        }

        // The spot itself when his capsule fits there, else the nearest clear one up to Lift above it.
        private static Vector3 Clear(Player player, Vector3 feet)
        {
            for (float up = 0.05f; up <= Lift; up += Step)
            {
                if (KwClimbRoom.Fits(player, feet + Vector3.up * up))
                    return feet + Vector3.up * up;
            }
            return feet + Vector3.up * 0.05f;
        }

        private static bool Read(string text, out long id, out Vector3 at, out float yaw, out Vector3 world)
        {
            (at, world, yaw) = (Vector3.zero, Vector3.zero, 0f);
            string[] parts = text.Split(';');
            float[] n = new float[7];
            if (parts.Length != 8 || !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
            {
                id = 0L;
                return false;
            }
            for (int i = 0; i < 7; i++)
            {
                if (!float.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out n[i]))
                    return false;
            }
            (at, yaw, world) = (new Vector3(n[0], n[1], n[2]), n[3], new Vector3(n[4], n[5], n[6]));
            return true;
        }

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
