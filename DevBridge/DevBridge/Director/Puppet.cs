using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// A human's controls under the director: what a player controller would give (move, run, walk, block, attack,
    /// secondary attack, dodge, jump) and where they look. Presses last two physics steps; holds stay until changed.
    /// </summary>
    internal sealed class Pilot
    {
        private Vector2 move;
        private bool run, block, crouch, walk;
        private int attack, secondary, jump, dodge;
        private Spot look, walkTo;
        private float lookPitch;

        internal void Set(JObject spec)
        {
            if (spec["move"] is JArray m && m.Count == 2) move = new Vector2(m[0].Value<float>(), m[1].Value<float>());
            if (spec["stop"] != null) (move, walkTo) = (Vector2.zero, null);
            run = spec.Value<bool?>("run") ?? run;
            block = spec.Value<bool?>("block") ?? block;
            crouch = spec.Value<bool?>("crouch") ?? crouch;
            walk = spec.Value<bool?>("walk") ?? walk;
            lookPitch = spec.Value<float?>("pitch") ?? lookPitch;
            if (spec["look"] != null) look = Spot.Parse(spec["look"], "look");
            if (spec["walkTo"] != null) walkTo = Spot.Parse(spec["walkTo"], "walkTo");
            if (spec.Value<bool?>("attack") == true) attack = 2;
            if (spec.Value<bool?>("secondary") == true) secondary = 2;
            if (spec.Value<bool?>("jump") == true) jump = 2;
            if (spec.Value<bool?>("dodge") == true) dodge = 2;
        }

        /// <summary>One physics step of controls.</summary>
        internal void Control(Player player)
        {
            Aim(player);
            Vector3 dir = new Vector3(move.x, 0f, move.y);
            if (walkTo != null) dir = Toward(player, walkTo.Resolve(Cast.Frame));
            bool a = attack > 0, s = secondary > 0, j = jump > 0, d = dodge > 0;
            player.SetControls(dir, a, a, s, s, block, block, j, crouch, run, false, d);
            player.m_walk = walk && !run;
            (attack, secondary, jump, dodge) = (Mathf.Max(0, attack - 1), Mathf.Max(0, secondary - 1), Mathf.Max(0, jump - 1), Mathf.Max(0, dodge - 1));
        }

        /// <summary>A move direction relative to the look, toward a point; nothing once within half a metre.</summary>
        private Vector3 Toward(Player player, Vector3 target)
        {
            Vector3 flat = Vector3.ProjectOnPlane(target - player.transform.position, Vector3.up);
            if (flat.magnitude < 0.5f)
            {
                walkTo = null;
                return Vector3.zero;
            }
            Vector3 lookDir = Vector3.ProjectOnPlane(player.m_lookDir, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, lookDir);
            return new Vector3(Vector3.Dot(flat.normalized, right), 0f, Vector3.Dot(flat.normalized, lookDir));
        }

        /// <summary>Turns the look toward the look spot (or the walk target), with the set pitch.</summary>
        internal void Aim(Player player)
        {
            Spot target = look ?? walkTo;
            if (target == null) return;
            Vector3 flat = Vector3.ProjectOnPlane(target.Resolve(Cast.Frame) - player.m_eye.position, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f) return;
            player.m_lookYaw = Quaternion.LookRotation(flat);
            player.m_lookPitch = lookPitch;
            player.SetMouseLook(Vector2.zero);
        }
    }

    /// <summary>The local player's pilot: while active, the mouse and keys do nothing and the director drives them.</summary>
    internal static class Puppet
    {
        internal static Pilot Local { get; private set; } = new Pilot();
        internal static bool Active { get; private set; }

        internal static void Set(JObject spec)
        {
            Active = true;
            Local.Set(spec);
        }

        internal static void Off()
        {
            Active = false;
            Local = new Pilot();
        }
    }

    [HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
    internal static class PuppetControlPatch
    {
        private static bool Prefix(PlayerController __instance)
        {
            if (!Puppet.Active || !(__instance.m_character is Player player) || player != Player.m_localPlayer) return true;
            Puppet.Local.Control(player);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerController), "LateUpdate")]
    internal static class PuppetLookPatch
    {
        private static bool Prefix(PlayerController __instance)
        {
            if (!Puppet.Active || !(__instance.m_character is Player player) || player != Player.m_localPlayer) return true;
            Puppet.Local.Aim(player);
            return false;
        }
    }
}
