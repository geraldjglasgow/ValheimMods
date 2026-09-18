using System;

namespace HaloMenu.API
{
    /// <summary>
    /// The public face of HaloMenu. Soft-dependency by design: every method here is safe to call whether or not
    /// the HaloMenu plugin is loaded. Check <see cref="IsAvailable"/> if your mod wants to know, but you do not
    /// have to - <see cref="Register"/> on a missing HaloMenu is a silent no-op, not an exception.
    /// </summary>
    public static class HaloMenuAPI
    {
        /// <summary>The frozen 1.x API surface this assembly implements. Additive changes only within 1.x; a
        /// breaking change ships as a separate HaloMenu.API 2.0 assembly, never in place of this one.</summary>
        public static readonly Version Version = new Version(1, 0, 0);

        private static HaloMenuService service;

        /// <summary>True once the HaloMenu plugin has loaded and called <see cref="Provide"/>.</summary>
        public static bool IsAvailable => service != null;

        /// <summary>Called by the HaloMenu plugin's own Awake. Never call this from a dependent mod.</summary>
        public static void Provide(HaloMenuService implementation) => service = implementation;

        /// <summary>Adds Id to the shared default ring. A no-op when HaloMenu is not loaded.</summary>
        public static void Register(RingEntry entry) => service?.Register(entry);

        /// <summary>Creates (or returns, if ringId was already created) a ring your mod owns. Null when HaloMenu is
        /// not loaded - check <see cref="IsAvailable"/> first if you need your own ring rather than the default one.</summary>
        public static Ring CreateRing(string ringId) => service?.CreateRing(ringId);
    }
}
