using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;

namespace ShipConfig
{
    /// <summary>
    /// GrindstoneSkills' Sailing API (<c>GrindstoneSkills.Api.SailingApi</c>, version 1 or later), found by reflection:
    /// the plugin by its GUID in the chainloader, then the type in its assembly. Nothing of GrindstoneSkills is
    /// referenced. Bound once, on the first call (the ship panel's, long after every plugin loaded). Without
    /// GrindstoneSkills, with an older one, or once a call has thrown, every answer is the neutral one.
    /// </summary>
    public static class GrindstoneLink
    {
        private const string Guid = "com.GrindstoneSkills";
        private const string TypeName = "GrindstoneSkills.Api.SailingApi";
        private const int Required = 1;

        private static bool bound;
        private static bool ready;
        private static Func<Ship, float> speedFactor;
        private static Func<float> exploreFactor;
        private static Func<string[]> abilities;
        private static Func<string, string> abilityName;
        private static Func<string, string> abilityKey;
        private static Func<string, string> abilityDescription;
        private static Func<string, float> abilityCooldown;
        private static Func<string, float> abilityCooldownLength;

        /// <summary>GrindstoneSkills is installed with a Sailing API this mod can use.</summary>
        public static bool Present => Bind();

        public static float SpeedFactor(Ship ship) => Ask(() => speedFactor(ship), 1f);
        public static float ExploreFactor() => Ask(() => exploreFactor(), 1f);
        public static string[] Abilities() => Ask(() => abilities(), Array.Empty<string>());
        public static string AbilityName(string id) => Ask(() => abilityName(id), id);
        public static string AbilityKey(string id) => Ask(() => abilityKey(id), "");
        public static string AbilityDescription(string id) => Ask(() => abilityDescription(id), "");
        public static float AbilityCooldown(string id) => Ask(() => abilityCooldown(id), 0f);
        public static float AbilityCooldownLength(string id) => Ask(() => abilityCooldownLength(id), 0f);

        private static T Ask<T>(Func<T> call, T fallback)
        {
            if (!Bind())
                return fallback;
            try
            {
                T answer = call();
                return answer != null ? answer : fallback;
            }
            catch (Exception e)
            {
                ready = false;
                ShipConfig.Log.LogWarning($"GrindstoneSkills' Sailing API failed; the ship panel stops asking it: {e}");
                return fallback;
            }
        }

        private static bool Bind()
        {
            if (bound)
                return ready;
            bound = true;
            Type api = FindApi();
            if (api != null)
                BindAll(api);
            return ready;
        }

        private static void BindAll(Type api)
        {
            speedFactor = Create<Func<Ship, float>>(api, "GetShipSpeedFactor");
            exploreFactor = Create<Func<float>>(api, "GetExploreRadiusFactor");
            abilities = Create<Func<string[]>>(api, "GetAbilities");
            abilityName = Create<Func<string, string>>(api, "GetAbilityName");
            abilityKey = Create<Func<string, string>>(api, "GetAbilityKey");
            abilityDescription = Create<Func<string, string>>(api, "GetAbilityDescription");
            abilityCooldown = Create<Func<string, float>>(api, "GetAbilityCooldown");
            abilityCooldownLength = Create<Func<string, float>>(api, "GetAbilityCooldownLength");
            ready = speedFactor != null && exploreFactor != null && abilities != null && abilityName != null &&
                abilityKey != null && abilityDescription != null && abilityCooldown != null && abilityCooldownLength != null;
        }

        /// <summary>The API type of an installed GrindstoneSkills whose version is at least <see cref="Required"/>, else null.</summary>
        private static Type FindApi()
        {
            if (!Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) || info.Instance == null)
                return null;
            Type api = info.Instance.GetType().Assembly.GetType(TypeName);
            Func<int> version = api != null ? Create<Func<int>>(api, "GetApiVersion") : null;
            return version != null && version() >= Required ? api : null;
        }

        /// <summary>A typed delegate to a public static method of that name and shape, or null.</summary>
        private static T Create<T>(Type api, string name) where T : Delegate
        {
            MethodInfo invoke = typeof(T).GetMethod("Invoke");
            Type[] parameters = Array.ConvertAll(invoke.GetParameters(), p => p.ParameterType);
            MethodInfo method = api.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
            return method != null && method.ReturnType == invoke.ReturnType
                ? (T)Delegate.CreateDelegate(typeof(T), method)
                : null;
        }
    }
}
