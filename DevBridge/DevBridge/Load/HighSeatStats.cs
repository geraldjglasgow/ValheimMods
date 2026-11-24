using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;

namespace DevBridge.Load
{
    /// <summary>
    /// HighSeat's own sync figures while a /load measure runs, when HighSeat is loaded: every public static field (and
    /// plain static property) of its HighSeat.SyncStats class (internal, updated once a second: Active, MsPerSecond,
    /// MaxFrameMs, RoundsPerSecond, BytesPerSecond, MaxPeerBytesPerSecond, ObjectsPerSecond, PendingObjects, Peers,
    /// PeersServed, PeersSkipped when this was written), read by reflection once a second, by name, whatever they are,
    /// so fields added or gone later need no change here. The class is looked up in the assembly of the HighSeat plugin
    /// running now (after a /reload, the new copy). Numbers are summed up (last, min, avg, max over the seconds),
    /// anything else (Active) is kept as its last text. HighSeat not loaded, or a version without the class, reads as
    /// absent with the reason, never as an error. A second reader takes HighSeat.DeltaStats the same way (the delta
    /// sends to players who read them).
    /// </summary>
    internal sealed class HighSeatStats
    {
        internal const string SyncStats = "HighSeat.SyncStats";
        internal const string DeltaStats = "HighSeat.DeltaStats";
        private const string Guid = "com.HighSeat";
        private readonly List<MemberInfo> members = new List<MemberInfo>();
        private readonly Dictionary<string, List<double>> numbers = new Dictionary<string, List<double>>();
        private readonly Dictionary<string, string> texts = new Dictionary<string, string>();
        private readonly string missing;
        private int samples;

        internal HighSeatStats(string typeName = SyncStats)
        {
            Type type = Find(typeName, out missing);
            if (type == null) return;
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static)) members.Add(field);
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.CanRead && property.GetIndexParameters().Length == 0) members.Add(property);
            }
            if (members.Count == 0) missing = typeName + " has no public static fields";
        }

        private static Type Find(string typeName, out string missing)
        {
            missing = null;
            if (!Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) || info.Instance == null)
            {
                missing = "HighSeat is not loaded";
                return null;
            }
            Type type = info.Instance.GetType().Assembly.GetType(typeName);
            if (type == null) missing = $"HighSeat {info.Metadata.Version} has no {typeName}";
            return type;
        }

        /// <summary>One reading of every member.</summary>
        internal void Sample()
        {
            if (missing != null) return;
            samples++;
            foreach (MemberInfo member in members)
            {
                object value = Read(member);
                if (value == null) continue;
                if (IsNumber(value)) Numbers(member.Name).Add(Convert.ToDouble(value));
                else texts[member.Name] = value.ToString();
            }
        }

        internal Dictionary<string, object> Report()
        {
            if (missing != null) return new Dictionary<string, object> { ["loaded"] = false, ["why"] = missing };
            var fields = new Dictionary<string, object>();
            foreach (KeyValuePair<string, List<double>> pair in numbers) fields[pair.Key] = Stat.Of(pair.Value).WithLast(pair.Value);
            foreach (KeyValuePair<string, string> pair in texts) fields[pair.Key] = pair.Value;
            return new Dictionary<string, object> { ["loaded"] = true, ["samples"] = samples, ["fields"] = fields };
        }

        private List<double> Numbers(string name)
        {
            if (!numbers.TryGetValue(name, out List<double> list)) numbers[name] = list = new List<double>();
            return list;
        }

        private static object Read(MemberInfo member)
        {
            try
            {
                return member is FieldInfo field ? field.GetValue(null) : ((PropertyInfo)member).GetValue(null, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsNumber(object value) =>
            value is int || value is long || value is float || value is double || value is short || value is byte ||
            value is uint || value is ulong || value is ushort || value is decimal;
    }
}
