using System.Collections.Generic;
using EliteCrafting.Items;

namespace EliteCrafting.Sockets
{
    /// <summary>Which of a gem's stats an item's base takes (sockets.md section 3); None takes no socket.</summary>
    internal enum SocketBase
    {
        None,
        Weapon,
        Staff,
        Armour,
    }

    /// <summary>
    /// What each gem gives, by the base it is socketed into (sockets.md section 3, user decision 2026-10-07): an inscription
    /// id per base, null where the gem does not fit. Fixed in code like the runes themselves; the inscription's own ladder,
    /// caps and on/off switch still come from the YAML.
    /// </summary>
    internal static class GemCatalog
    {
        private sealed class GemStats
        {
            public GemStats(string? weapon, string? staff, string? armour)
            {
                Weapon = weapon;
                Staff = staff;
                Armour = armour;
            }

            public string? Weapon { get; }
            public string? Staff { get; }
            public string? Armour { get; }
        }

        private static readonly Dictionary<string, GemStats> Stats = new Dictionary<string, GemStats>
        {
            ["gem_surtr"] = new GemStats("emberbrand", "primal_fury", "flameward"),
            ["gem_ymir"] = new GemStats("rimebrand", "seidr_thrift", "frostward"),
            ["gem_thor"] = new GemStats("stormbrand", "thors_chain", "stormward"),
            ["gem_nidhogg"] = new GemStats("venombrand", "lingering_wounds", "venomward"),
            ["gem_hel"] = new GemStats("spiritbrand", "soul_reaper", "spiritward"),
            ["gem_tyr"] = new GemStats("honed_might", "grave_command", "resolute"),
            ["gem_freyja"] = new GemStats("blood_drinker", "blood_thrift", "vigor"),
            ["gem_odin"] = new GemStats("seidr_siphon", "seidr_flow", "wellspring"),
            ["gem_skadi"] = new GemStats("wind_siphon", "grave_vigor", "endurance"),
            ["gem_heimdall"] = new GemStats("keen_eye", "swift_casting", "mist_veil"),
            ["gem_sleipnir"] = new GemStats(null, null, "fleetfoot"),
        };

        /// <summary>
        /// The base an item class is for sockets: one- and two-handed and ranged weapons, staves, armour, and shields
        /// (the offhand group without torches). Everything else (tools, trinkets, utility items, torches) takes none.
        /// </summary>
        public static SocketBase BaseOf(ClassInfo info)
        {
            switch (info.Class?.Group)
            {
                case "onehand":
                case "twohand":
                case "ranged":
                    return SocketBase.Weapon;
                case "magic":
                    return SocketBase.Staff;
                case "armour":
                    return SocketBase.Armour;
                case "offhand":
                    return info.ClassId == "light" ? SocketBase.None : SocketBase.Armour;
                default:
                    return SocketBase.None;
            }
        }

        /// <summary>The inscription a gem gives on this base; null when the gem does not fit it or is unknown.</summary>
        public static string? StatFor(string gemId, SocketBase socketBase)
        {
            if (!Stats.TryGetValue(gemId, out GemStats stats))
            {
                return null;
            }
            switch (socketBase)
            {
                case SocketBase.Weapon:
                    return stats.Weapon;
                case SocketBase.Staff:
                    return stats.Staff;
                case SocketBase.Armour:
                    return stats.Armour;
                default:
                    return null;
            }
        }
    }
}
