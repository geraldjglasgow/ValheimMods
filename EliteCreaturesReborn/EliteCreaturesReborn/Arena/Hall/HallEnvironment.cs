using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The arena's own light (features/arena.md section 1, "Light and mood"), clients only: Hel's realm in a cave, under a
    /// rock roof 400 m up with no sky. A copy of the game's <c>Twilight_Clear</c> (the Deep North's clear weather: a weak
    /// cold sun) named <see cref="Name"/>, made the same at every hour - a dim cold blue-green light and fill, as if from
    /// the cave's glows, and a fog of #263439 at 0.0018 (half gone at 385 m: the far wall clear, the mountains and the
    /// roof fading into it), always dark, no clouds, no aurora, no weather particles, and neither cold, freezing nor wet
    /// (a trial is no place for a hidden debuff). It carries <c>Morkhalla</c>'s ambience and music. The stars, moon and
    /// sun disc are the sky camera's: <see cref="HallView"/> turns it off in the zone. The game's own
    /// <see cref="EnvZone"/> forces the environment while the local player is in a 160 x 60 x 160 m trigger round the
    /// arena, on the <c>character_trigger</c> layer the game's fire areas use (it meets the player, and no hover, attack
    /// or placement ray tests it). The game clears a forced environment when a new local player spawns (a respawn); the
    /// arena clears it when it is removed with the player inside (<see cref="Release"/>), since a destroyed trigger sends
    /// no exit.
    /// </summary>
    internal static class HallEnvironment
    {
        public const string Name = "ECR_HelArena";

        private const string Sky = "Twilight_Clear";
        private const string Sound = "Morkhalla";
        private const string TriggerLayer = "character_trigger";

        private const float Fog = 0.0018f;
        private const float EveryCheck = 0.5f;
        private const float SunIntensity = 0.55f;
        private static readonly Color SunColour = new Color(0.58f, 0.8f, 0.82f);
        private static readonly Color Ambient = new Color(0.33f, 0.41f, 0.44f);
        private static readonly Color FogColour = new Color(0.149f, 0.204f, 0.224f);
        private static readonly Color SunFogColour = new Color(0.18f, 0.25f, 0.27f);

        // The arena and what stands round it: the gatehouse (r 71), the ground outside, up past the sky cap (40 m).
        private static readonly Vector3 ZoneCentre = new Vector3(0f, 20f, 0f);
        private static readonly Vector3 ZoneSize = new Vector3(160f, 60f, 160f);

        private static float _checkedAt = float.NegativeInfinity;
        private static bool _playerInZone;

        /// <summary>The local player is in the zone round the arena: tested at most twice a second, shared by the view,
        /// the river's flow and the bubbles.</summary>
        public static bool PlayerInZone
        {
            get
            {
                if (Time.time - _checkedAt < EveryCheck)
                {
                    return _playerInZone;
                }
                _checkedAt = Time.time;
                Player? player = Player.m_localPlayer;
                _playerInZone = player != null && InZone(player.transform.position);
                return _playerInZone;
            }
        }

        /// <summary>Registers the environment once per world and puts the zone round the arena.</summary>
        public static void Add(Transform hall)
        {
            if (!Register())
            {
                return;
            }
            var zone = new GameObject("ecr_helarena_env");
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

        /// <summary>True for a world position inside the zone round the arena (the view's far clip follows it).</summary>
        public static bool InZone(Vector3 position)
        {
            Vector3 local = position - ArenaHall.Origin - ZoneCentre;
            Vector3 half = ZoneSize * 0.5f;
            return Mathf.Abs(local.x) < half.x && Mathf.Abs(local.y) < half.y && Mathf.Abs(local.z) < half.z;
        }

        /// <summary>The arena is going: give the local player back the world's sky if the arena's was forced.</summary>
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
            EnvSetup? source = man.GetEnv(Sky);
            if (source == null)
            {
                Log.Warn($"Hel's arena: the game has no {Sky} environment; the arena keeps the world's sky.");
                return false;
            }
            EnvSetup env = source.Clone();
            Dim(env);
            Calm(env, man.GetEnv(Sound));
            man.AppendEnvironment(env);
            return true;
        }

        // The same dim cold light at every hour of the day; a cave: no clouds, no aurora.
        private static void Dim(EnvSetup env)
        {
            env.m_name = Name;
            env.m_default = false;
            env.m_alwaysDark = true;
            env.m_lightIntensityDay = env.m_lightIntensityNight = SunIntensity;
            env.m_sunColorDay = env.m_sunColorNight = env.m_sunColorMorning = env.m_sunColorEvening = SunColour;
            env.m_ambColorDay = env.m_ambColorNight = Ambient;
            env.m_fogColorDay = env.m_fogColorNight = env.m_fogColorMorning = env.m_fogColorEvening = FogColour;
            env.m_fogColorSunDay = env.m_fogColorSunNight = SunFogColour;
            env.m_fogColorSunMorning = env.m_fogColorSunEvening = SunFogColour;
            env.m_fogDensityDay = env.m_fogDensityNight = env.m_fogDensityMorning = env.m_fogDensityEvening = Fog;
            env.m_cloudOpacityDay = env.m_cloudOpacityMorning = env.m_cloudOpacityEvening = env.m_cloudOpacityNight = 0f;
            env.m_auroraIntensityDay = env.m_auroraIntensityMorning = env.m_auroraIntensityEvening = 0f;
            env.m_auroraIntensityNight = 0f;
        }

        // No weather, no debuffs, Morkhalla's sound. The game walks the particle array without a null test: keep one.
        private static void Calm(EnvSetup env, EnvSetup? sound)
        {
            env.m_psystems = new GameObject[0];
            env.m_psystemsOutsideOnly = false;
            env.m_envObject = null;
            env.m_rainCloudAlpha = 0f;
            env.m_snowBuildup = 0f;
            env.m_isWet = env.m_isCold = env.m_isColdAtNight = env.m_isFreezing = env.m_isFreezingAtNight = false;
            if (sound != null)
            {
                env.m_ambientLoop = sound.m_ambientLoop;
                env.m_ambientVol = sound.m_ambientVol;
                env.m_ambientList = sound.m_ambientList;
                (env.m_musicMorning, env.m_musicDay) = (sound.m_musicMorning, sound.m_musicDay);
                (env.m_musicEvening, env.m_musicNight) = (sound.m_musicEvening, sound.m_musicNight);
            }
        }
    }
}
