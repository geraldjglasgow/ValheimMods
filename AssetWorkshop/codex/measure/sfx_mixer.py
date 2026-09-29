"""The game's audio mixer (Audio/MasterMixer.mixer in the export): its groups, their effect chains with every parameter
named and valued per snapshot, and which group an AudioSource's OutputAudioMixerGroup reference means.

Unity stores an effect's parameters as Param_0.. and their values per snapshot by GUID; the names below are the order
Unity's built-in effects declare them in (the mixer window lists them in the same order).
"""
import re

import game  # noqa: I100  (first: it puts the workshop's helpers on the path)
from workshop import unity

MIXER = "Audio/MasterMixer.mixer"
PARAMS = {
    "ParamEQ": ["centre_hz", "octaves", "gain"],
    "Compressor": ["threshold_db", "attack_ms", "release_ms", "makeup_db"],
    "Highpass": ["cutoff_hz", "resonance"],
    "Lowpass": ["cutoff_hz", "resonance"],
    "Lowpass Simple": ["cutoff_hz"],
    "Highpass Simple": ["cutoff_hz"],
    "Duck Volume": ["threshold_db", "ratio", "attack", "release", "makeup_db", "knee_db", "sidechain_mix"],
    "SFX Reverb": ["dry_mb", "room_mb", "room_hf_mb", "decay_s", "decay_hf_ratio", "reflections_mb",
                   "reflect_delay_s", "reverb_mb", "reverb_delay_s", "diffusion_pct", "density_pct", "hf_reference_hz",
                   "room_lf_mb", "lf_reference_hz"],
}


def documents():
    return unity.documents(MIXER)


def snapshots(docs):
    """{snapshot name: {parameter GUID: value}}."""
    found = {}
    for cls, body in docs.values():
        if cls == 245:
            values = dict(re.findall(r"^    ([0-9a-f]{32}): (\S+)$", body, re.MULTILINE))
            found[unity.field(body, "m_Name")] = {k: float(v) for k, v in values.items()}
    return found


def group_paths(docs):
    """{group fileID: 'Master/Effects/SFX'} for every mixer group."""
    parents = {}
    for file_id, (cls, body) in docs.items():
        if cls == 243:
            for child in unity.items(body, "m_Children"):
                parents[unity.ref(child)[0]] = file_id
    paths = {}
    for file_id, (cls, body) in docs.items():
        if cls == 243:
            names, at = [], file_id
            while at:
                names.append(unity.field(docs[at][1], "m_Name"))
                at = parents.get(at)
            paths[file_id] = "/".join(reversed(names))
    return paths


def effect(body, values):
    """{'effect': name, 'params': {name: value}} of one effect controller in one snapshot."""
    name = unity.field(body, "m_EffectName")
    guids = re.findall(r"m_GUID: ([0-9a-f]{32})", body)
    labels = PARAMS.get(name, [f"param_{i}" for i in range(len(guids))])
    params = {label: values.get(guid) for label, guid in zip(labels, guids)}
    level = unity.field(body, "m_MixLevel")
    if name == "Send":
        params["send_db"] = values.get(level)
    return {"effect": name, "params": params}


def groups():
    """[{path, volume_db (per snapshot), pitch, effects (per snapshot)}] for every group, parents first."""
    docs, result = documents(), []
    shots, paths = snapshots(docs), group_paths(docs)
    for file_id, path in sorted(paths.items(), key=lambda item: item[1]):
        body = docs[file_id][1]
        volume, pitch = unity.field(body, "m_Volume"), unity.field(body, "m_Pitch")
        chain = [docs[unity.ref(e)[0]][1] for e in unity.items(body, "m_Effects")]
        result.append({"path": path, "file_id": file_id,
                       "volume_db": {s: v.get(volume) for s, v in shots.items()},
                       "pitch": shots.get("Default", {}).get(pitch),
                       "effects": {s: [effect(c, v) for c in chain if unity.field(c, "m_EffectName") != "Attenuation"]
                                   for s, v in shots.items()}})
    return result


def exposed():
    """{exposed name: group path or parameter GUID}: the parameters the game's code sets (volume sliders)."""
    docs = documents()
    body = next(b for c, b in docs.values() if c == 241)
    pairs = re.findall(r"- guid: ([0-9a-f]{32})\n    name: (\S+)", body)
    volumes = {unity.field(b, "m_Volume"): p for i, p in group_paths(docs).items() for b in [docs[i][1]]}
    return {name: volumes.get(guid, guid) for guid, name in pairs}


def group_of(reference):
    """The mixer group path an OutputAudioMixerGroup reference names ('Master/Effects/SFX'), '' when none."""
    file_id, guid = unity.ref(reference)
    if file_id == "0":
        return ""
    if game.path_of(reference) != MIXER:
        return f"other:{guid}"
    return _paths().get(file_id, f"unknown:{file_id}")


_cache = {}


def _paths():
    if "paths" not in _cache:
        _cache["paths"] = group_paths(documents())
    return _cache["paths"]
