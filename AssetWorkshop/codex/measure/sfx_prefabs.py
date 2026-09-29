"""Every sound the game's prefabs play, read from the reference export: each ZSFX (the game's sound component: clips,
random pitch and volume, concurrency, fades, captions) with the AudioSource beside it (mixer group, 3D settings,
roll-off) and the prefab's TimedDestruction, and every AudioSource that holds a clip of its own (looping emitters,
location music).

    records = sfx_prefabs.survey(sfx_scan.load())
"""
import re

import game  # noqa: I100  (first: it puts the workshop's helpers on the path)
import sfx_mixer
import sfx_rolloff
import sfx_scan
from workshop import unity

ZSFX_FLOATS = ("m_minPitch", "m_maxPitch", "m_minVol", "m_maxVol", "m_fadeInDuration", "m_fadeOutDuration",
               "m_fadeOutDelay", "m_minPan", "m_maxPan", "m_minDelay", "m_maxDelay", "m_customReverbDistance",
               "m_minimumCaptionVolume", "m_vibrationModifier")
ZSFX_INTS = ("m_playOnAwake", "m_maxConcurrentSources", "m_ignoreConcurrencyDistance", "m_fadeOutOnAwake",
             "m_randomPan", "m_distanceReverb", "m_useCustomReverbDistance", "m_useVibration", "m_vibrateAllAudible",
             "m_captionType", "m_hash")
SOURCE_FLOATS = (("m_Volume", "volume"), ("m_Pitch", "pitch"), ("DopplerLevel", "doppler"),
                 ("MinDistance", "min_distance"), ("MaxDistance", "max_distance"), ("Pan2D", "pan"))
SOURCE_INTS = (("m_PlayOnAwake", "play_on_awake"), ("Loop", "loop"), ("Priority", "priority"),
               ("rolloffMode", "rolloff_mode"), ("BypassEffects", "bypass_effects"),
               ("BypassReverbZones", "bypass_reverb_zones"), ("Mute", "mute"))


def number(body, name, kind=float):
    value = unity.field(body, name)
    try:
        return kind(float(value)) if value else None
    except ValueError:
        return None


def clips(body, field="m_audioClips"):
    """The clip paths a component lists (ZSFX m_audioClips) or holds (AudioSource m_Resource)."""
    if field == "m_Resource":
        path = game.path_of(unity.field(body, field))
        return [path] if path else []
    return [p for p in (game.path_of(item) for item in unity.items(body, field)) if p]


def curve(body, name):
    """[(time, value)] keys of an AudioSource curve (rolloffCustomCurve, panLevelCustomCurve...)."""
    match = re.search(rf"^  {name}:\n(?:    .*\n)*?    m_Curve:\n((?:    [- ].*\n)*)", body, re.MULTILINE)
    if not match:
        return []
    times = re.findall(r"time: (\S+)", match.group(1))
    values = re.findall(r"value: (\S+)", match.group(1))
    return [(float(t), float(v)) for t, v in zip(times, values)]


def source(body):
    """An AudioSource's settings, with its mixer group's path and the distances its roll-off implies."""
    found = {key: number(body, field) for field, key in SOURCE_FLOATS}
    found.update({key: number(body, field, int) for field, key in SOURCE_INTS})
    found["mixer"] = sfx_mixer.group_of(unity.field(body, "OutputAudioMixerGroup"))
    found["rolloff"] = sfx_rolloff.MODES.get(found["rolloff_mode"], "?")
    found["curve"] = curve(body, "rolloffCustomCurve")
    blend, spread = curve(body, "panLevelCustomCurve"), curve(body, "spreadCustomCurve")
    found["spatial"] = blend[0][1] if len(blend) == 1 else blend
    found["spread_deg"] = round(spread[0][1] * 360, 1) if len(spread) == 1 else spread
    found.update(sfx_rolloff.distances(found))
    return found


def zsfx(body):
    """A ZSFX's settings and clips."""
    found = {name[2:]: number(body, name) for name in ZSFX_FLOATS}
    found.update({name[2:]: number(body, name, int) for name in ZSFX_INTS})
    found["caption"] = unity.field(body, "m_closedCaptionToken")
    found["caption_action"] = unity.field(body, "m_secondaryCaptionToken")
    found["clips"] = clips(body)
    return found


def prefab_facts(prefab):
    """What the whole prefab carries besides its sounds: the timer, a network view, particles, lights."""
    kinds = [kind for _, kind, _ in prefab.all_components()]
    timers = [body for _, kind, body in prefab.all_components() if kind == "TimedDestruction"]
    return {"timeout_s": number(timers[0], "m_timeout") if timers else None,
            "networked": "ZNetView" in kinds, "particles": kinds.count("ParticleSystem"),
            "lights": kinds.count("Light"), "components": sorted(set(kinds) - {"Transform"})}


def records_of(path):
    """One record per sound node in a prefab: its ZSFX and/or its AudioSource, plus the prefab's facts."""
    prefab = game.prefab(path)
    facts, found = prefab_facts(prefab), []
    for node in prefab.nodes():
        parts = dict((kind, body) for kind, body in prefab.components(node) if kind in ("ZSFX", "AudioSource"))
        if "ZSFX" not in parts and not ("AudioSource" in parts and clips(parts["AudioSource"], "m_Resource")):
            continue
        record = {"prefab": path, "name": prefab.name(prefab.root()), "node": prefab.path_to(node),
                  "active": prefab.active(node), **facts}
        record["zsfx"] = zsfx(parts["ZSFX"]) if "ZSFX" in parts else None
        record["source"] = source(parts["AudioSource"]) if "AudioSource" in parts else None
        if record["zsfx"] is None:
            record["clips"] = clips(parts["AudioSource"], "m_Resource")
        else:
            record["clips"] = record["zsfx"]["clips"]
        found.append(record)
    return found


def survey(scan):
    """Every sound record in every prefab of the export (scenes and assets hold sounds through other managers)."""
    records = []
    for path in sfx_scan.sound_files(scan):
        if path.endswith(".prefab"):
            try:
                records.extend(records_of(path))
            except (StopIteration, KeyError) as error:
                print(f"sfx_prefabs: skipped {path}: {error!r}")
    return records
