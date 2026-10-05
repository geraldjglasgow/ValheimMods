# Endpoints

Every endpoint at `http://127.0.0.1:7780`. `/help` lists each one's arguments and defaults; the
[full reference](https://github.com/geraldjglasgow/ValheimMods/blob/main/DevBridge/REFERENCE.md) explains them. Screen
coordinates are game-window pixels from the top-left. [Getting Started](wiki:Getting Started) has the first calls.

## See

| Endpoint | What it does |
| --- | --- |
| `/help` | Every endpoint with its arguments |
| `/status` | State (`starting`, `menu`, `loading`, `ingame`, `server`), world, network role, player, open screens, time, plugins |
| `/screenshot` | The game window with its UI to a PNG or JPG (`out=`, `maxWidth=1600`, `crop=x,y,w,h`); replies with the path and sizes |
| `/ui` | Indented tree of the live UI: components, text, sprites, screen rectangles (`path=`, `depth=`, `all=1` for inactive) |
| `/find` | Paths of objects whose text, name or component type contains the words (`text=`, `name=`, `component=`) |
| `/log` | Numbered recent lines of the BepInEx log, Unity's included, or of the console (`buffer=console`); `since=`, `level=`, `grep=` |
| `/nearby` | Networked objects near the player, nearest first, with creature, item, container and piece details and ZDO ids |
| `/zdo` | Every value a ZDO stores, keys named where known (`id=`, `hover=1`, `nearest=Troll`) |
| `/prefabs` | The game's prefabs by name text and kind (`filter=`, `kind=creature`, `piece`, `item`, `sfx`, `vfx`) |
| `/config` | A plugin's config entries in memory: value, default, type, description (`mod=` name or GUID); without `mod=` the plugins |

## Drive

| Endpoint | What it does |
| --- | --- |
| `/wait` | `seconds=N`, or until `for=menu`, `for=player` (in a world, loading screen gone) or `for=log&grep=text` |
| `/click` | Pointer down, up and click on a UI element by `path=` or at `x=&y=` |
| `/hover` | Pointer-enter on an element and its parents, for tooltips |
| `/key` | Real key presses: `tap=E`, `tap=LeftCtrl+F3`, `hold=W,LeftShift&seconds=2`, `down=`, `up=`, `up=all` |
| `/type` | Sets an input field's text (`text=`, `path=`); `submit=1` submits it |
| `/mouse` | Moves the pointer to `x,y`, turns the camera with `dx,dy`, `click=left`, `down=`, `up=`, `scroll=` |
| `/console` | Runs a console command as if typed (F5) and replies with what the console printed (`cmd=`) |

## Inspect

| Endpoint | What it does |
| --- | --- |
| `/eval` | Reflection on any object, private members too: `Player.m_localPlayer.m_runSpeed`, `$hover.Character.GetLevel()`, method calls, assignments (`Terminal.m_cheat = true`); `members=1`, `methods=1` list them |
| `/trace` | Patches a game or mod method now and records each call: time, ms, instance, arguments, result or exception (`method=Type.Method`, `where=`, `stack=1`); `id=N&last=20` reads them, `off=N` or `off=all` unpatches |
| `/events` | Game events (`hit`, `death`, `spawn`, `player`, `boss`, log warnings, console lines and more): a long poll (`since=`, `kinds=`, `grep=`, `wait=`) or a live NDJSON stream (`stream=1`); `kinds=list` explains each kind |

## Measure

| Endpoint | What it does |
| --- | --- |
| `/perf` | Samples `seconds=` (default 5, max 60): fps, frame ms avg/p50/p95/p99/max, collections, heap; per mod the main-thread ms and calls per frame of its Harmony patches and MonoBehaviour updates, with its costliest methods (`mod=`, `top=`, `baseline=1`) |
| `/time` | This machine's game clock: slow motion (`scale=0.25`), `pause=1`, `resume=1`, `step=N` frames or `step_seconds=`, `reset=1` hands it back |
| `/burst` | A run of frames (`seconds=2&frames=12`, or `every=N`; at most 64) on one labelled contact sheet; `crop=`, `cell=`, `keep=1`, `video=` through ffmpeg |
| `/overlay` | Persistent debug lines round the player: colliders, AI senses and targets, AI paths, wards, portals, edited terrain, spawners, zones (`show=`, `off=`, `radius=30`) |
| `/hitbox` | `on=1`: flashes each melee swing's reach and draws a line for each hit on the player; replies with ranges and distances |

## Test

| Endpoint | What it does |
| --- | --- |
| `/scenario` | Runs a scripted test from a JSON file (`file=`) or a POST body; pass or fail per step. See [Scenarios](wiki:Scenarios) |
| `/sync` | Puts one question to every DevBridge on this machine (host, client, dedicated server, ports 7780-7789): `peers=1`, a ZDO (`id=`, `nearest=`, `hover=1`), an `/eval` (`expr=`), a plugin's config (`config=`); `same` and the differences |
| `/tune` | Changes a value on a prefab and every live copy (`prefab=Troll&field=Character.m_runSpeed&value=9`, `*1.2` scales), or an item's shared data (`item=`); `list=1`, `revert=all`, `code=1` prints the changes as C# |

## Iterate

| Endpoint | What it does |
| --- | --- |
| `/reload` | Hot-reloads a mod's rebuilt DLL (`mod=`, `file=`): the old copy's patches, components, commands and RPCs come off, the new one starts; `list=1` shows stale builds, `watch=<mod>` reloads on every build. Not DevBridge itself |
| `/swap` | A real prefab and its live copies take a rebuilt bundle prefab's look (meshes, textures, `clips=1` animations); `watch=1` swaps again on each rebuild, `revert=1`, `list=1` |
| `/bundle` | Loads an asset bundle file (`load=`), lists its assets (`assets=`), `reload=` after a rebuild, `unload=` |
| `/place` | A still, local copy of a bundle prefab in a row in front of the player, dressed in a game material (`dress=`) or worn by a game creature (`on=`) |
| `/lineup` | Still copies of the game's own prefabs beside it (`prefabs=Skeleton,Battleaxe`), creatures with their gear |
| `/placed`, `/dress`, `/clear` | What is placed (size, triangles, materials, shaders); dress one again; remove them |
| `/animate` | A placed object's animator: clips and parameters; `play=`, `trigger=`, `set=`, `speed=`, `swap=` in a bundle's clips |
| `/effect`, `/sound` | A bundle or game particle effect at the row or a point; a game sound, or a bundle clip played through a game sound |
| `/frame` | The free camera framing what is placed (or `id=`, or a point) from a set angle, `hud=0`; `off=1` returns |
| `/light` | Fixed time of day (`tod=0.5`), weather (`env=`) and wind on this machine; `reset=1` hands them back |

Everything `/place`, `/lineup`, `/effect` and `/sound` make is local to this machine, never networked or saved, and
goes when you log out.
