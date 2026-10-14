using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// A straggler marked for the players (features/arena.md section 3, "Stragglers"), on every machine with a screen and
    /// for its own eyes only: while Hel's recall of the wave's last few runs (<see cref="ArenaRecall"/>), each of them
    /// flares once in cold soul-fire, carries the hall's cold light from then on, so a pale Ghost reads in the dark, and
    /// shows on the map as the game's event marker with its name, moved to it every second. Nothing is sent: every client
    /// reads the recall from the core's ZDO and the creature's wave from its tag, each second as the creature's
    /// <see cref="ArenaFighter"/> ticks.
    /// </summary>
    internal sealed class StragglerMark
    {
        private const float LightIntensity = 2.5f;
        private const float LightRange = 8f;

        /// <summary>Metres from the camera within which the light shines (the game's light LOD; 40 by default): across
        /// the 130 m arena.</summary>
        private const float SeenFrom = 140f;

        private const float FlareSize = 0.4f;

        private static readonly Color ColdFire = new Color(0.5f, 0.92f, 0.88f);

        private readonly GameObject _glow;
        private Minimap.PinData? _pin;

        private StragglerMark(GameObject glow) => _glow = glow;

        /// <summary>Every second, on every machine: the mark put on, moved or taken off. Returns the mark to keep.</summary>
        public static StragglerMark? Step(ArenaFighter fighter, StragglerMark? mark)
        {
            if (!Due(fighter))
            {
                mark?.Remove();
                return null;
            }
            mark ??= Create(fighter.Body);
            mark.Move(fighter.Body);
            return mark;
        }

        /// <summary>The mark off: the light out, the pin gone.</summary>
        public void Remove()
        {
            if (_glow != null)
            {
                Object.Destroy(_glow);
            }
            if (_pin != null && Minimap.instance != null)
            {
                Minimap.instance.RemovePin(_pin);
            }
            _pin = null;
        }

        // A living creature of the running trial, of a wave still fighting, while the recall runs.
        private static bool Due(ArenaFighter fighter)
        {
            ZDO? zdo = fighter.Zdo;
            ArenaCore? core = ArenaCore.Current;
            ZDO? state = core != null ? core.Zdo : null;
            if (zdo == null || state == null || ArenaPortalLook.NoScreen || fighter.Body.IsDead() || fighter.Body.IsTamed())
            {
                return false;
            }
            long trial = ArenaTag.Trial(zdo);
            return trial != 0L && trial == ArenaState.RunningTrial(state) && state.GetLong(ArenaKeys.RecallAtHash) > 0L
                && ArenaTag.Wave(zdo) > state.GetInt(ArenaKeys.SweptHash);
        }

        private static StragglerMark Create(Character body)
        {
            Vector3 center = body.GetCenterPoint();
            ArenaPortalLook.Flare(center, FlareSize);
            var glow = new GameObject("ecr_arena_straggler_light");
            glow.transform.SetParent(body.transform, false);
            glow.transform.position = center;
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = ColdFire;
            light.intensity = LightIntensity;
            light.range = LightRange;
            light.shadows = LightShadows.None;
            LightLod lod = glow.AddComponent<LightLod>();
            lod.m_lightDistance = SeenFrom;
            return new StragglerMark(glow);
        }

        // The pin on the creature, made once the map is up.
        private void Move(Character body)
        {
            Minimap map = Minimap.instance;
            if (map == null)
            {
                return;
            }
            if (_pin == null)
            {
                _pin = Pin(map, body);
            }
            _pin.m_pos = body.transform.position;
        }

        // The game's event marker with the creature's name, unsaved, made as the game's own AddPin makes one (which is
        // not called: its signature needs the platform library this mod does not reference).
        private static Minimap.PinData Pin(Minimap map, Character body)
        {
            var pin = new Minimap.PinData
            {
                m_type = Minimap.PinType.RandomEvent,
                m_name = Localization.instance != null ? Localization.instance.Localize(body.m_name) : "",
                m_pos = body.transform.position,
                m_icon = map.GetSprite(Minimap.PinType.RandomEvent),
                m_save = false,
                m_animate = true,
            };
            pin.m_NamePinData = pin.m_name.Length > 0 ? new Minimap.PinNameData(pin) : null;
            map.m_pins.Add(pin);
            map.m_pinUpdateRequired = true;
            return pin;
        }
    }
}
