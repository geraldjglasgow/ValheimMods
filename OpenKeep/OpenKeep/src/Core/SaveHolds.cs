using System;
using System.Collections.Generic;
using HarmonyLib;

namespace OpenKeep.Core
{
    /// <summary>
    /// One save for many changes to one container. The game saves a container after every change of its inventory (the
    /// change callback <c>Container.OnContainerChanged</c> on the owner: the whole inventory serialised into the ZDO), and
    /// <see cref="ContainerScan.Save"/> saves again, so a quick stack of thirty stacks into one chest wrote it sixty
    /// times in one frame. While a container is held (<see cref="Hold"/> in a <c>using</c>, so a throw still ends it) the
    /// game's save is skipped and remembered, <see cref="ContainerScan.Claim(Container)"/> reads the ZDO only on the first claim
    /// (the held inventory is newer than the ZDO from then on), and the end of the outermost hold saves once through the
    /// game's own path. Holds nest. Only this client's own synchronous work is held: nothing else runs between the start
    /// and the end of a hold, so no other client's change can arrive in between.
    /// </summary>
    public static class SaveHolds
    {
        private sealed class Held
        {
            public int Depth;
            public bool Dirty;
            public bool Loaded;
        }

        private static readonly Dictionary<Container, Held> held = new Dictionary<Container, Held>();

        /// <summary>Holds the container's saves until the returned scope ends. A null container gives a scope that does nothing.</summary>
        public static Scope Hold(Container container)
        {
            if (container == null)
                return default;
            if (!held.TryGetValue(container, out Held entry))
                held[container] = entry = new Held();
            entry.Depth++;
            return new Scope(container);
        }

        /// <summary>Claim's question: read the inventory from the ZDO now? Once per hold; outside a hold, every time.</summary>
        internal static bool NeedsLoad(Container container)
        {
            if (held.Count == 0 || !held.TryGetValue(container, out Held entry))
                return true;
            bool first = !entry.Loaded;
            entry.Loaded = true;
            return first;
        }

        /// <summary>The change callback's question: skip the game's save now? A skipped save is made when the hold ends.</summary>
        internal static bool SkipSave(Container container)
        {
            if (held.Count == 0 || !held.TryGetValue(container, out Held entry))
                return false;
            entry.Dirty = true;
            return true;
        }

        private static void End(Container container)
        {
            if (!held.TryGetValue(container, out Held entry) || --entry.Depth > 0)
                return;
            held.Remove(container);
            if (entry.Dirty)
                ContainerScan.Save(container);
        }

        /// <summary>The end of a hold, for <c>using</c>; a struct, so holding allocates nothing per call.</summary>
        public readonly struct Scope : IDisposable
        {
            private readonly Container container;

            internal Scope(Container container) => this.container = container;

            public void Dispose()
            {
                if (!ReferenceEquals(container, null))
                    End(container);
            }
        }
    }

    /// <summary>
    /// Container.OnContainerChanged prefix: on a held container the game's save after a change is skipped and made once
    /// when the hold ends. Only where the game would save (owner, not loading); the postfixes on the callback still run.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.OnContainerChanged))]
    public static class HeldSavePatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Container __instance)
        {
            if (__instance.m_loading || __instance.m_nview == null || !__instance.IsOwner())
                return true;
            return !SaveHolds.SkipSave(__instance);
        }
    }
}
