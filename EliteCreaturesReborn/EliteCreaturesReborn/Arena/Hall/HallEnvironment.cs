using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The hall's own sky and light (features/arena.md section 1, "Light and mood"), clients only: a copy of the game's
    /// <c>Morkhalla</c> environment (cold blue, always dark, its ambience and music) named <see cref="Name"/>, with its
    /// fog thinned from 0.1 to <see cref="Fog"/> so the far wall stays visible from the gate (51 m), and neither cold,
    /// freezing nor wet: the hall has no fire to warm by, and a trial is no place for a hidden debuff. The game's own
    /// <see cref="EnvZone"/> forces it while the local player stands in a trigger box round the hall, on the
    /// <c>character_trigger</c> layer the game's fire areas use (it meets the player, and no hover, attack or
    /// placement ray tests it). The game clears a forced environment when a new local player spawns (a respawn); the hall
    /// clears it when it is removed with the player inside (<see cref="Release"/>), since a destroyed trigger sends no exit.
    /// </summary>
    internal static class HallEnvironment
    {
        public const string Name = "ECR_HelHall";

        private const string Source = "Morkhalla";
        private const float Fog = 0.015f;
        private const string TriggerLayer = "character_trigger";

        // From the floor's foot to above the dome (24 m), across the hall and its gate passage (to z 29.5).
        private static readonly Vector3 ZoneCentre = new Vector3(0f, 12f, 0f);
        private static readonly Vector3 ZoneSize = new Vector3(62f, 34f, 62f);

        /// <summary>Registers the environment once per world and puts the zone round the hall.</summary>
        public static void Add(Transform hall)
        {
            if (!Register())
            {
                return;
            }
            var zone = new GameObject("ecr_helhall_env");
            zone.layer = LayerMask.NameToLayer(TriggerLayer);
            zone.transform.SetParent(hall, false);
            zone.transform.localPosition = ZoneCentre;
            BoxCollider box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = ZoneSize;
            EnvZone env = zone.AddComponent<EnvZone>();
            env.m_environment = Name;
            env.m_force = true;
        }

        /// <summary>The hall is going: give the local player back the world's sky if the hall's was forced.</summary>
        public static void Release()
        {
            EnvMan? man = EnvMan.instance;
            if (man != null && man.m_forceEnv == Name)
            {
                man.SetForceEnvironment("");
            }
        }

        private static bool Register()
        {
            EnvMan? man = EnvMan.instance;
            if (man == null)
            {
                return false;
            }
            if (man.GetEnv(Name) != null)
            {
                return true;
            }
            EnvSetup? source = man.GetEnv(Source);
            if (source == null)
            {
                Log.Warn($"Hel's hall: the game has no {Source} environment; the hall keeps the world's sky.");
                return false;
            }
            man.AppendEnvironment(Thinned(source.Clone()));
            return true;
        }

        private static EnvSetup Thinned(EnvSetup env)
        {
            env.m_name = Name;
            env.m_default = false;
            env.m_fogDensityNight = env.m_fogDensityMorning = env.m_fogDensityDay = env.m_fogDensityEvening = Fog;
            env.m_isCold = env.m_isColdAtNight = false;
            env.m_isFreezing = env.m_isFreezingAtNight = false;
            env.m_isWet = false;
            return env;
        }
    }
}
