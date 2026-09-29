using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using OpenKeep.Core;
using PatchGuard;

namespace OpenKeep.Stacks
{
    /// <summary>
    /// PackPanel's Key Stack: while PackPanel is installed, every prefab in its <c>3. Key Ring / Key Items</c> stacks to
    /// at least its <c>Key Stack</c>. PackPanel leaves the writing to this module when both are installed, so one mod
    /// writes every stack size; it is applied as the key's starting value (<see cref="StackValues"/>), so an
    /// OpenKeep.Stacks.yml entry or a per item entry for a key still wins, and it holds with this module off too, so no
    /// switch cuts a stack of keys at the next load. A change of either entry, or a server's values arriving, applies
    /// the values again.
    /// </summary>
    public static class PackPanelKeys
    {
        private const string Section = "3. Key Ring";

        private static bool watching;
        private static string parsedFrom;
        private static readonly HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static int Over(string prefab, int stack)
        {
            ConfigEntry<int> size = PackPanelLink.Entry<int>(Section, "Key Stack");
            return size != null && IsKey(prefab) ? Math.Max(stack, size.Value) : stack;
        }

        /// <summary>Called when the item database is ready (every plugin is loaded by then): follows both entries once.</summary>
        public static void Watch()
        {
            if (watching || !PackPanelLink.Present)
                return;
            watching = true;
            EventHandler apply = Guard.Wrap("packpanel key stack", (_, _) => StackValues.ApplyAll());
            ConfigEntry<int> size = PackPanelLink.Entry<int>(Section, "Key Stack");
            ConfigEntry<string> items = PackPanelLink.Entry<string>(Section, "Key Items");
            if (size != null)
                size.SettingChanged += apply;
            if (items != null)
                items.SettingChanged += apply;
        }

        private static bool IsKey(string prefab)
        {
            ConfigEntry<string> items = PackPanelLink.Entry<string>(Section, "Key Items");
            if (items == null)
                return false;
            if (!ReferenceEquals(items.Value, parsedFrom))
            {
                parsedFrom = items.Value;
                keys.Clear();
                foreach (string name in (parsedFrom ?? "").Split(','))
                {
                    if (name.Trim().Length > 0)
                        keys.Add(name.Trim());
                }
            }
            return keys.Contains(prefab);
        }
    }
}
