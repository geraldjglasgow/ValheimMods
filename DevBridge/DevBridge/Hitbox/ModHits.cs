using System.Reflection;
using DevBridge.Events;
using HarmonyLib;
using UnityEngine;

namespace DevBridge.Hitbox
{
    /// <summary>
    /// Hit shapes a workspace mod makes in its own code rather than through the game's attack: the Crypt Executioner's
    /// axe head (Elite Creatures Pack's HeadsmanCut.Sweep, a sphere swept from where the blade's edge was last frame to
    /// where it is now) and every move of Old Gorehide (GoreStrikes.Sweep, in AssetLab until it ships: the tusk tips'
    /// path, the charge's body, the flop and the trample's area). Found by name when /hitbox is turned on (the mods load
    /// after this plugin's Awake), each skipped when its mod is not there. Each swept sphere is drawn as a ring at the edge's height (red) and under it on the
    /// ground (orange), so a swing leaves the trail of what it could touch.
    /// </summary>
    internal static class ModHits
    {
        private static readonly (string Method, string Name)[] Sweeps =
        {
            ("EliteCreaturesPack.Headsman.HeadsmanCut:Sweep", "Crypt Executioner axe head"),
            ("EliteCreaturesPack.Gorehide.GoreStrikes:Sweep", "Old Gorehide moves"),
        };
        private static readonly Color Edge = new Color(1f, 0.15f, 0.1f, 0.9f);
        private static readonly Color Ground = new Color(1f, 0.6f, 0.1f, 0.8f);
        private static readonly int Floors = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
        private static bool tried;

        internal static string Hooked { get; private set; } = "not looked for yet";

        internal static void Hook()
        {
            if (tried) return;
            tried = true;
            var harmony = new Harmony(DevBridgePlugin.PluginGuid + ".hitbox");
            var hooked = new System.Collections.Generic.List<string>();
            foreach ((string method, string name) in Sweeps)
            {
                MethodInfo sweep = AccessTools.Method(method);
                if (sweep == null) continue;
                harmony.Patch(sweep, prefix: new HarmonyMethod(typeof(ModHits), nameof(Sweep)));
                hooked.Add(name);
            }
            Hooked = hooked.Count > 0 ? string.Join(", ", hooked) : "no mod hit shapes found (Elite Creatures Pack, AssetLab not loaded, or renamed)";
        }

        private static void Sweep(Vector3 from, Vector3 to, float radius)
        {
            if (!HitboxView.On) return;
            try
            {
                Draw(from, to, radius);
            }
            catch (System.Exception error)
            {
                Publish.WarnOnce("hitbox (mod hit shapes)", error);
            }
        }

        private static void Draw(Vector3 from, Vector3 to, float radius)
        {
            Lines.Draw(Lines.Ring(to, radius, 16), Edge, HitboxView.Seconds, 0.03f);
            Vector3 floor = Physics.Raycast(to + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 4f, Floors) ? hit.point : to;
            Lines.Draw(Lines.Ring(floor + Vector3.up * 0.05f, radius, 16), Ground, HitboxView.Seconds, 0.03f);
            if ((to - from).sqrMagnitude > 1e-4f) Lines.Draw(new[] { from, to }, Edge, HitboxView.Seconds, 0.03f);
        }
    }
}
