using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;

namespace HighSeatPlus.Crowd
{
    /// <summary>
    /// The Party mod's public API (<c>Party.Api.PartyApi.AreInSameParty(long, long)</c>, see Party/PLAN.md), found by
    /// reflection: the plugin by its GUID in the chainloader, then the type in its assembly. Nothing of Party is
    /// referenced. Bound once, on the first question (the crowd's first decision, long after every plugin loaded), and
    /// called through a typed delegate. On a player's PC Party knows the local player's own party, which is all this asks;
    /// on a hosting PC it knows every party. Without Party, or once a call has thrown (logged once), nobody counts as a
    /// party member and far party members are drawn like everyone else: simpler, never hidden.
    /// </summary>
    internal static class PartyLink
    {
        private const string Guid = "com.Party";
        private const string TypeName = "Party.Api.PartyApi";
        private const string MethodName = "AreInSameParty";

        private static bool _bound;
        private static Func<long, long, bool> _sameParty;

        /// <summary>Whether the two players share a party, as far as this PC knows; false without Party.</summary>
        public static bool SameParty(long a, long b)
        {
            if (a == 0L || b == 0L || !Bind())
            {
                return false;
            }
            try
            {
                return _sameParty(a, b);
            }
            catch (Exception e)
            {
                _sameParty = null;
                HighSeatPlus.Log.LogWarning($"Party's API failed; far party members are now drawn like everyone else: {e.Message}");
                return false;
            }
        }

        private static bool Bind()
        {
            if (!_bound)
            {
                _bound = true;
                _sameParty = Find();
            }
            return _sameParty != null;
        }

        private static Func<long, long, bool> Find()
        {
            if (!Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) || info.Instance == null)
            {
                return null;
            }
            Type api = info.Instance.GetType().Assembly.GetType(TypeName);
            MethodInfo method = api?.GetMethod(MethodName, BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(long), typeof(long) }, null);
            if (method == null || method.ReturnType != typeof(bool))
            {
                return null;
            }
            return (Func<long, long, bool>)Delegate.CreateDelegate(typeof(Func<long, long, bool>), method);
        }
    }
}
