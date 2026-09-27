using System;

namespace EarthWright.Menu
{
    /// <summary>
    /// Values the Menu module reads from other modules without a compile-time dependency on them. The main session
    /// wires them when the modules are merged.
    /// </summary>
    public static class MenuHooks
    {
        /// <summary>
        /// Whether clearing is switched on in section 5 (the Clearing module's <c>ClearingSettings.Enabled</c>). The
        /// Clear objects entry is listed only while this is true, so it stays hidden until it is wired.
        /// </summary>
        public static Func<bool> ClearingEnabled = () => false;
    }
}
