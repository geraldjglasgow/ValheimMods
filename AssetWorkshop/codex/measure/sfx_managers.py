"""The game's audio managers, which play clips without a sound prefab: AudioMan (wind, ocean, the random ambient
one-shots per biome, lava noises, concurrency limits), MusicMan (every named music track: clips, volume, fade, loop) and the
environments' ambient loops (EnvMan in the main scene and the Ashlands/Deep North location lists).
"""
import re

import game  # noqa: I100  (first: it puts the workshop's helpers on the path)
from workshop import unity

AUDIO_MANAGER = "Systems/_AudioManager.prefab"
AUDIO_NUMBERS = ("m_snapshotTransitionTime", "m_windMinVol", "m_windMaxVol", "m_windMinPitch", "m_windMaxPitch",
                 "m_windVariation", "m_windIntensityPower", "m_oceanVolumeMax", "m_oceanVolumeMin", "m_oceanFadeSpeed",
                 "m_ambientFadeTime", "m_randomAmbientInterval", "m_randomAmbientChance", "m_randomMinDistance",
                 "m_randomMaxDistance", "m_lavaNoiseInterval", "m_lavaNoiseChance", "m_maxLavaLoops",
                 "m_concurrencyThreshold", "m_forcedMaxConcurrentLoops")
CLIP = re.compile(r"\{fileID: 8300000, guid: ([0-9a-f]{32})")


def _component(path, cls_name):
    return next(body for _, kind, body in game.prefab(path).all_components() if kind == cls_name)


def _clips(block):
    return [unity.asset_path(g) for g in CLIP.findall(block) if unity.asset_path(g)]


def _items(body, field):
    """The '  - m_name: …' entries of a top-level list, each as its text."""
    match = re.search(rf"^  {field}:\n((?:  [- ] .*\n|    .*\n)*)", body, re.MULTILINE)
    if not match:
        return []
    return re.split(r"^  - ", match.group(1), flags=re.MULTILINE)[1:]


def audio_man():
    """AudioMan's settings, its wind and ocean loops, the random ambient lists per biome and the lava noises."""
    body = _component(AUDIO_MANAGER, "AudioMan")
    found = {name[2:]: float(unity.field(body, name) or 0) for name in AUDIO_NUMBERS}
    found["wind"] = game.path_of(unity.field(body, "m_windAudio"))
    found["ocean"] = game.path_of(unity.field(body, "m_oceanAudio"))
    found["random_prefab"] = game.path_of(unity.field(body, "m_randomAmbientPrefab"))
    found["random_ambients"] = [{"name": re.match(r"m_name: (.*)", item).group(1).strip(),
                                 "biome": int(re.search(r"m_biome: (\d+)", item).group(1)),
                                 "clips": _clips(item)} for item in _items(body, "m_randomAmbients")]
    lava = re.search(r"^  m_randomLavaNoises:\n((?:  - .*\n)*)", body, re.MULTILINE)
    found["lava_noises"] = _clips(lava.group(1)) if lava else []
    return found


def music():
    """[{name, clips, volume, fade_in_s, loop, resume, enabled, ambient}] of every MusicMan track."""
    body, tracks = _component(AUDIO_MANAGER, "MusicMan"), []
    for item in _items(body, "m_music"):
        def value(name):
            match = re.search(rf"^    {name}: (.*)$", item, re.MULTILINE)
            return match.group(1).strip() if match else ""
        tracks.append({"name": re.match(r"m_name: (.*)", item).group(1).strip(), "clips": _clips(item),
                       "volume": float(value("m_volume") or 1), "fade_in_s": float(value("m_fadeInTime") or 0),
                       "loop": value("m_loop") == "1", "resume": value("m_resume") == "1",
                       "enabled": value("m_enabled") == "1", "ambient": value("m_ambientMusic") == "1"})
    return tracks


LOOP = re.compile(r"^( +)m_ambientLoop: (\{[^}]*\})\n +m_ambientVol: (\S+)", re.MULTILINE)


def environments(scan):
    """[{name, file, loop, volume}] for every environment (EnvSetup) that sets an ambient loop: each m_ambientLoop,
    named by the '- m_name:' that opens its list entry."""
    found = []
    for path, entry in sorted(scan["files"].items()):
        if not entry["clips"]:
            continue
        text = unity.read(path)
        for match in LOOP.finditer(text):
            opener = "\n" + match.group(1)[:-2] + "- m_name: "
            start = text.rfind(opener, 0, match.start())
            name = text[start + len(opener):text.find("\n", start + 1)].strip() if start >= 0 else "?"
            loop = game.path_of(match.group(2))
            if loop:
                found.append({"name": name, "file": path, "loop": loop, "volume": float(match.group(3))})
    return found
