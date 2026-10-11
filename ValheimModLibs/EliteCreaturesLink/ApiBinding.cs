using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;

namespace EliteCreaturesLink
{
    /// <summary>
    /// Finds Elite Creatures Reborn's API: the plugin by its GUID in BepInEx's chainloader, then the type
    /// <c>EliteCreaturesReborn.Api.EliteCreaturesApi</c> in its assembly, then <c>GetApiVersion()</c>. While BepInEx is
    /// still loading plugins the search runs again on every call (one dictionary lookup), so a caller that asks too early
    /// still binds later; once it is found, or found too old, or the plugins have finished loading without it (the
    /// game's start screen or network exists, <see cref="PluginsLoaded"/>), the answer is kept for the process, so an
    /// absent Elite Creatures Reborn costs one flag test per call. Endpoints become typed delegates
    /// (<see cref="Create{T}"/>), so a call costs what a direct call costs. Nothing of Elite Creatures Reborn is
    /// referenced. Main thread only.
    /// </summary>
    internal static class ApiBinding
    {
        public const string Guid = "gglasgow.elitecreaturesreborn";
        public const string TypeName = "EliteCreaturesReborn.Api.EliteCreaturesApi";

        /// <summary>The API version this library is written against; an older Elite Creatures Reborn counts as absent.</summary>
        public const int Required = 1;

        private static bool _settled;
        private static Type? _api;
        private static int _version;
        private static ManualLogSource? _log;

        /// <summary>The API type, or null: Elite Creatures Reborn absent (so far), or older than <see cref="Required"/>.</summary>
        public static Type? Api
        {
            get
            {
                Settle();
                return _api;
            }
        }

        /// <summary>Elite Creatures Reborn's API version; 0 while it is absent or has no API.</summary>
        public static int Version
        {
            get
            {
                Settle();
                return _version;
            }
        }

        /// <summary>A typed delegate to a public static endpoint, or null when the API is absent or lacks it (logged once per name).</summary>
        public static T? Create<T>(string name) where T : Delegate
        {
            Type? api = Api;
            MethodInfo invoke = typeof(T).GetMethod("Invoke")!;
            Type[] parameters = Array.ConvertAll(invoke.GetParameters(), p => p.ParameterType);
            MethodInfo? method = api?.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
            if (api == null || method == null || method.ReturnType != invoke.ReturnType)
            {
                if (api != null)
                {
                    Warn($"Elite Creatures Reborn's API has no {name} of the expected shape; that call does nothing");
                }
                return null;
            }
            return (T)Delegate.CreateDelegate(typeof(T), method);
        }

        /// <summary>A call into Elite Creatures Reborn threw (its endpoints should not): logged, the caller gets the empty answer.</summary>
        public static void Failed(Exception e) => Warn($"a call into Elite Creatures Reborn's API failed: {e}");

        private static void Settle()
        {
            if (_settled)
            {
                return;
            }
            if (!Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) || info.Instance == null)
            {
                _settled = PluginsLoaded();
                return;
            }
            _settled = true;
            Type? type = info.Instance.GetType().Assembly.GetType(TypeName);
            _version = ReadVersion(type);
            if (_version < Required)
            {
                Warn($"Elite Creatures Reborn {info.Metadata.Version} has no API version {Required} or later (found {_version}); its integration is off");
                return;
            }
            _api = type;
        }

        /// <summary>
        /// BepInEx has loaded every plugin: it does so in one go before the game's first scene, so once the start
        /// screen (<c>FejdStartup</c>, a dedicated server's too) or the network (in a world) exists, a plugin that is
        /// not in the chainloader will not come.
        /// </summary>
        private static bool PluginsLoaded() => FejdStartup.instance != null || ZNet.instance != null;

        private static int ReadVersion(Type? type)
        {
            MethodInfo? method = type?.GetMethod("GetApiVersion", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            try
            {
                return method != null && method.ReturnType == typeof(int) ? (int)method.Invoke(null, null) : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static void Warn(string message)
        {
            _log ??= Logger.CreateLogSource("EliteCreaturesLink");
            _log.LogWarning(message);
        }
    }
}
