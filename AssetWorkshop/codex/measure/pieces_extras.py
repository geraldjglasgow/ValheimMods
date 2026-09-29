"""The scripts that give stations and furniture their behaviour on screen: what a crafting station switches on while
in use, how an extension draws its connection, a fire's fuel objects, a door's hinge animation, a bed's spawn point, a
chest's lid, a production station's switches. Reads a pieces_scan.Scan; every function returns None when the piece
has no such script.

    pieces_extras.all_of(scan)       # {"station": {...}, "door": {...}, ...} for the scripts present
"""
import os
import re

import game
import pieces_fields
from workshop import unity

DOOR_CLIPS = os.path.join("GameElements", "Pieces", "_res", "door")


def _node_name(scan, value):
    node = scan.node_of(value)
    return scan.prefab.path_to(node).split("/", 1)[-1] if node else None


def station(scan):
    body = scan.first("CraftingStation")
    if body is None:
        return None
    fields = ("rangeBuild", "extraRangePerLevel", "discoverRange", "useDistance", "craftRequireRoof",
              "craftRequireFire", "useAnimation", "showBasicRecipies")
    found = {k: pieces_fields.value(body, "m_" + k) for k in fields}
    found.update({k: _node_name(scan, unity.field(body, "m_" + k)) for k in ("inUseObject", "haveFireObject",
                                                                             "areaMarker")})
    found["effects"] = {k: pieces_fields.effects(body, "m_" + k) for k in ("craftItemEffects", "craftItemDoneEffects",
                                                                        "repairItemDoneEffects")}
    return found


def extension(scan):
    body = scan.first("StationExtension")
    if body is None:
        return None
    target = game.path_of(unity.field(body, "m_craftingStation"))
    connection = game.path_of(unity.field(body, "m_connectionPrefab"))
    return {"station": os.path.basename(target)[:-7] if target else None,
            "max_distance": pieces_fields.value(body, "m_maxStationDistance"),
            "stack": pieces_fields.value(body, "m_stack"),
            "connection": os.path.basename(connection)[:-7] if connection else None,
            "connection_offset": list(unity.numbers(unity.field(body, "m_connectionOffset"))),
            "continuous": pieces_fields.value(body, "m_continousConnection")}


def fireplace(scan):
    body = scan.first("Fireplace")
    if body is None:
        return None
    found = {k: pieces_fields.value(body, "m_" + k) for k in ("startFuel", "maxFuel", "secPerFuel", "infiniteFuel",
                                                              "canTurnOff")}
    for key in ("enabledObject", "enabledObjectLow", "enabledObjectHigh", "fullObject", "halfObject", "emptyObject"):
        found[key] = _node_name(scan, unity.field(body, "m_" + key))
    fuel = game.path_of(unity.field(body, "m_fuelItem"))
    found["fuel"] = os.path.basename(fuel)[:-7] if fuel else None
    found["effects"] = {k: pieces_fields.effects(body, "m_" + k) for k in ("fuelAddedEffects", "toggleOnEffects")}
    return found


def door(scan):
    body, animator = scan.first("Door"), scan.first("Animator")
    if body is None:
        return None
    controller = game.path_of(unity.field(animator, "m_Controller")) if animator else None
    clips = _clips(controller) if controller else []
    return {"controller": os.path.basename(controller)[:-11] if controller else None, "clips": clips,
            "moves": sorted({c["path"] for clip in clips for c in clip["curves"]}),
            "key": game.path_of(unity.field(body, "m_keyItem")) is not None,
            "effects": {k: pieces_fields.effects(body, "m_" + k) for k in ("openEffects", "closeEffects")}}


def _clips(controller):
    """[{name, seconds, curves}] of the clips a door controller plays: which child moves, how long, to where."""
    rows = []
    for guid in sorted(set(re.findall(r"m_Motion: \{fileID: 7400000, guid: (\w+)", unity.read(controller)))):
        path = unity.asset_path(guid)
        if path:
            rows.append(_clip(path))
    return sorted(rows, key=lambda r: r["name"])


def _clip(path):
    """One clip: its length and, per animated child, the curve kind and its last key (euler degrees or metres)."""
    text = unity.read(path)
    curves = []
    for kind, start, stop in (("euler", "m_EulerCurves:", "m_PositionCurves:"),
                              ("position", "m_PositionCurves:", "m_ScaleCurves:")):
        section = text[text.index(start):text.index(stop)] if start in text and stop in text else ""
        for chunk in section.split("  - curve:")[1:]:
            values = re.findall(r"value: (\{[^}]*\})", chunk)
            target = re.search(r"^    path: (.*)$", chunk, re.MULTILINE)
            curves.append({"curve": kind, "path": target.group(1) if target else "",
                           "end": [round(v, 3) for v in unity.numbers(values[-1])] if values else None})
    stop = re.search(r"m_StopTime: ([\d.]+)", text)
    return {"name": os.path.basename(path)[:-5], "seconds": float(stop.group(1)) if stop else None,
            "curves": curves}


def bed(scan):
    body = scan.first("Bed")
    if body is None:
        return None
    node = scan.by_object.get(unity.ref(unity.field(body, "m_spawnPoint"))[0]) or \
        (unity.ref(unity.field(body, "m_spawnPoint"))[0] in scan.prefab.docs and
         unity.ref(unity.field(body, "m_spawnPoint"))[0])
    return {"spawn_point": pieces_fields.point(scan, node) if node else None}


def chair(scan):
    """Where the sitter is attached (the seat point in the piece's frame) and the sitting animation."""
    body = scan.first("Chair")
    if body is None:
        return None
    node = unity.ref(unity.field(body, "m_attachPoint"))[0]
    return {"attach_point": pieces_fields.point(scan, node) if scan.prefab.is_local(node) else None,
            "animation": unity.field(body, "m_attachAnimation"), "use_distance": pieces_fields.value(body,
                                                                                                  "m_useDistance")}


def container(scan):
    body = scan.first("Container")
    if body is None:
        return None
    return {"grid": [pieces_fields.value(body, "m_width"), pieces_fields.value(body, "m_height")],
            "open": _node_name(scan, unity.field(body, "m_open")),
            "closed": _node_name(scan, unity.field(body, "m_closed")),
            "effects": {k: pieces_fields.effects(body, "m_" + k) for k in ("openEffects", "closeEffects")}}


def production(scan):
    """Smelters, kilns, the fermenter, cooking stations: the objects they switch and the effects they play."""
    for kind in ("Smelter", "Fermenter", "CookingStation", "Beehive", "SapCollector", "Incinerator"):
        body = scan.first(kind)
        if body is not None:
            effects = re.findall(r"^  (m_\w+(?:Effects?|Effector)):\n", body, re.MULTILINE)
            objects = re.findall(r"^  (m_\w+Object): \{fileID: (-?\d+)\}", body, re.MULTILINE)
            return {"script": kind,
                    "objects": {k[2:]: _node_name(scan, f"{{fileID: {v}}}") for k, v in objects
                                if v != "0" and k != "m_GameObject"},
                    "effects": {k[2:]: pieces_fields.effects(body, k) for k in effects}}
    return None


def all_of(scan):
    """Every script summary this piece has, keyed by kind."""
    found = {"station": station(scan), "extension": extension(scan), "fire": fireplace(scan), "door": door(scan),
             "bed": bed(scan), "container": container(scan), "production": production(scan), "chair": chair(scan)}
    return {k: v for k, v in found.items() if v is not None}
