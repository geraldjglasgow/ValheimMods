# DevBridge

A dev-only BepInEx plugin that lets an agent (or a script) see and drive the running game over HTTP on localhost:
screenshots, the live UI tree, clicks, key presses, mouse, console commands, the log, reflection on any object, and
the ZDOs of nearby objects. It exists to test the mods in this workspace in the real game, and to look at new assets
from `ValheimAssets` there: load a freshly built bundle, stand its models beside the game's own under the game's
lighting and shaders, play its effects and sounds, frame it all for a screenshot, then rebuild and reload without
restarting the game (see "A stage for new assets"). Beyond looking, it slows or freezes the game clock, captures
motion on one image, draws what has no look of its own (AI senses, paths, colliders, wards), streams game events,
traces any method's calls, measures what each mod costs per frame, runs scripted tests, compares a client with a
server, tunes values on live creatures, swaps a real prefab's look and hot-reloads a mod's DLL.

It is published on Thunderstore (MilkyTeam/DevBridge) for modders and their agents, and installed in development
profiles only, never a play profile: no mod depends on it. In this workspace it lives in the `LocalTesting` profile.
`README.md` is its short store page; this file is the full reference.

## Install

From Thunderstore with r2modman into a development profile, or build it:

```
dotnet build DevBridge/DevBridge/DevBridge.csproj -c Release
```

The build copies `DevBridge.dll` into the `LocalTesting` profile's plugins folder. Start the profile from r2modman;
the log shows `DevBridge listening on http://127.0.0.1:7780/`. A rebuilt DevBridge takes effect on the next start:
BepInEx does not reload plugins, and `/reload` refuses DevBridge itself (it reloads other mods: see "Reloading a mod
without a restart").

The port is set in `BepInEx/config/com.DevBridge.cfg` (default 7780). A second game, or a dedicated server with
DevBridge on the same machine, gets the next free port up to 7789. On a dedicated server everything except
screenshots and input works.

## Using it

`curl -s http://127.0.0.1:7780/help` lists every endpoint with its arguments. Arguments go in the query string or a
form body, so text with spaces is easiest as `curl -s -G --data-urlencode "cmd=spawn Troll 1" .../console`. In
PowerShell call `curl.exe`, not the `curl` alias.

| Endpoint | What it does |
| --- | --- |
| `/status` | state (`starting`, `menu`, `loading`, `ingame`, `server`), world, network role, player, open screens, time, plugins |
| `/wait` | `seconds=N`, or `for=menu`, `for=player` (in the world, loading screen gone), `for=log&grep=text` |
| `/screenshot` | writes a PNG or JPG (`out=`, `maxWidth=1600`, `crop=x,y,w,h`), replies with the path and sizes |
| `/ui`, `/find` | indented tree of the live hierarchy with components, text, sprites and screen rectangles; search by text, name or component |
| `/click`, `/hover` | UI pointer events on an element by `path=` or at screen pixel `x=&y=` (top-left origin) |
| `/key`, `/type` | real key presses through the Input System (`tap=E`, `hold=W&seconds=2`, `down=`/`up=`); set an input field's text |
| `/mouse` | the mouse device: move to `x,y`, turn with `dx,dy`, `click=left`, `down`/`up`, `scroll` |
| `/console` | runs a console command and returns what the console printed |
| `/log` | numbered recent log lines (`since=`, `level=warning`, `grep=`), or the console's (`buffer=console`) |
| `/eval` | reflection: `Player.m_localPlayer.m_runSpeed`, `$hover.Character.GetLevel()`, `Terminal.m_cheat = true`, `members=1` |
| `/nearby` | networked objects near the player with creature, item, container and piece details and ZDO ids |
| `/zdo` | every value a ZDO stores (`id=`, `hover=1`, `nearest=Troll`), key names recovered from the game's and plugins' strings; owner id and revisions; `full=1` leaves strings uncut |
| `/bundle` | load an asset bundle from a file (`load=`), list its assets by type (`assets=`), `reload=` it after a rebuild, `unload=` |
| `/prefabs` | the game's prefabs by name text and kind (`filter=sfx_`, `kind=creature`, `piece`, `item`, `sfx`, `vfx`) |
| `/place` | a still, local copy of a bundle prefab in a row in front of the player, dressed in a game material or worn by a game creature |
| `/lineup` | still, local copies of the game's own prefabs beside it (`prefabs=Skeleton,Battleaxe`), creatures with their gear |
| `/placed`, `/dress`, `/clear` | what is placed (size, triangles, materials, shaders); dress one again; remove them |
| `/animate` | a placed object's Animator: its clips and parameters; play a state, triggers, parameters, speed, swap in a bundle's clips |
| `/effect`, `/sound` | a bundle or game particle effect at the row; a game sound, or a bundle clip played through a game sound |
| `/frame` | the game's free camera framing the row (or one placed object, or a point) from a set angle; HUD off |
| `/light` | fixed time of day, weather environment and wind, on this machine only |
| `/hitbox` | `on=1`/`off=1`: flash each melee swing's hit shape and mark each hit on the player; replies with their distances |
| `/time` | this machine's game clock: slow motion `scale=0.25`, `pause=1`, `resume=1`, `step=N` frames or `step_seconds=` game time then frozen again, `reset=1` hands it back |
| `/burst` | a run of frames (`seconds=2&frames=12` or `every=N`) on one labelled contact sheet; `crop=`, `cell=`, `keep=1`, `video=` through ffmpeg |
| `/overlay` | persistent debug lines round the player: colliders, AI senses and targets, AI paths, wards, portal links, edited terrain, spawner ranges, the zone grid (`show=`, `off=`, `radius=30`, `every=0.5`, `max=`, `layer=`) |
| `/events` | game events as they happen (hits, deaths, spawns, the player, bosses, log warnings, console lines, hitbox records, other features' kinds): a long poll (`since=`, `kinds=`, `grep=`, `wait=`) or a live NDJSON stream (`stream=1`) |
| `/trace` | live tracepoints: patch any game or mod method now (`method=Attack.DoMeleeAttack`) and record each call: time, ms, instance, arguments, result or exception; `id=N&last=20` reads them, `off=N` or `off=all` unpatches |
| `/perf` | samples `seconds=` (default 5, max 60): fps, frame ms avg/p50/p95/p99/max, collections, managed heap; per mod the main-thread ms and calls per frame of its Harmony patches and MonoBehaviour updates, with its costliest methods (`mod=`, `top=`, `baseline=1`) |
| `/scenario` | a scripted test from a JSON file (`file=`) or POST body: setup, steps, cleanup; each step calls an endpoint, evals, waits, waits for an event or checks the log, with expectations; pass/fail per step |
| `/sync` | this game against every other DevBridge on the machine (a dedicated server, a host, clients): `peers=1` who runs what, a ZDO (`id=`, `nearest=`, `hover=1`), an `/eval` result (`expr=`), a plugin's config (`config=`); `"same"` and the differences |
| `/config` | a plugin's config entries in memory (`mod=` name or GUID, `section=`): value, default, type, description, Charter status; without `mod=` the plugins |
| `/tune` | change a value on a prefab and every live copy at once (`prefab=Troll&field=Character.m_runSpeed&value=9`, a creature's attack item, a player item); `list=1`, `revert=`, `code=1` |
| `/swap` | a real prefab and every copy of it here take a rebuilt bundle prefab's meshes, textures and clips (`clips=1`); `watch=1` swaps again on each rebuild; `revert=1`, `list=1` |
| `/reload` | hot-reload a mod's rebuilt DLL (`mod=`, `file=`): the old copy torn down, the new one loaded beside it; `list=1`, `watch=<mod>` |

Screen coordinates are always pixels of the game window from the top-left, the same as in a full-size screenshot. When
a screenshot is shrunk (`maxWidth`), scale by `screen / image` from its reply before clicking.

### A test loop

1. Build the mod; ask the user to start (or restart) the `LocalTesting` profile.
2. `wait?for=menu&timeout=300`, then either the user loads a world or the agent clicks through the menu.
3. `wait?for=player&timeout=300`, then `status`.
4. Set up the scene with `console` (`devcommands`, `god`, `spawn`, `pos`) and `eval`.
5. Act with `key`, `mouse`, `click`; look with `screenshot`, `ui`, `nearby`, `zdo`; read errors with
   `log?level=warning&since=N`.

### Hit shapes

`/hitbox?on=1` shows how far attacks reach while a creature is fought. Each melee swing flashes its hit shape for
`seconds=` (default 1.5), worked out the way `Attack.DoMeleeAttack` casts it: red at the swing's height, orange on the
ground under it. A body that crosses the edge is hit (the sweep is spheres of the attack's ray width out to its range,
so the edge is the reach). Every hit on the local player, whatever made it (swing, projectile, a mod's own area damage),
draws a yellow line from the attacker's centre to the player with the player's body outlined. The reply, and the log
(`/log?grep=hitbox`), list the recent swings and hits: the attack's item name, shape, range and ray width, the damage,
and the distance centre to centre and the gap body to body (`you_distance`/`you_gap` on a swing, `distance`/`gap` on a
hit). `players=1` adds the players' own swings; `clear=1` empties the list; `off=1` stops it.

The lines are drawn over everything (no depth test) and only on this machine, and a swing is drawn only where the game
works it out, on the attacker's owner (single player or the host). Hits on the player are seen wherever the player is.
Area and custom damage have no shape to draw; their hits still get the yellow line and the distances.

## Watching and measuring

### Slow motion and frame stepping

`/time?scale=0.25` runs this machine's game clock at a quarter speed, `pause=1` freezes it, `resume=1` runs on at the
scale, and `step=3` (or `step_seconds=0.1`) runs that many frames (that much game time) at the scale, freezes again and
then replies with what ran: frames, game seconds, physics steps. A frozen swing keeps its `/hitbox` lines, and the
camera runs on real time, so `/frame`, `/mouse` and `/screenshot` work on the frozen moment. `/time` alone reports the
scale, frame, game time and fixed time step.

While it holds the clock, DevBridge writes `Time.timeScale` every frame after the game writes its own (the game forces 1
with anyone connected and resets its scale when its menu closes); the game's own pause still wins while its menu is
open. In slow motion physics steps shrink with the scale (`Time.fixedDeltaTime`, never above the game's), so bodies move
every frame instead of in jumps. `reset=1`, or logging out, gives back the game's scale and step. Each change is a
`time` event.

The clock is this machine's alone. Single player, or a host with nobody connected: the whole world. With others
connected (a host or a client) they run on, so what this machine simulates slows or stops for them and the rest drifts;
the reply carries a `warning`, and a paused machine sends no world updates. A dedicated server needs `force=1`. Pausing
also holds respawns and teleports; sounds keep their normal speed.

### Motion on one image

`/burst` captures a run of frames and lays them out left to right, top to bottom on one contact sheet, so a single
image shows an attack, an animation or an effect playing. `frames=` (default 12, at most 64) are spread evenly over
`seconds=` (default 2) of real time, the first at 0 and the last at `seconds`, or taken every N rendered frames with
`every=N`; `delay=` waits before the first. Each frame is captured as `/screenshot` captures it, UI included, cropped
(`crop=x,y,w,h`, screen pixels from the top-left) and at once shrunk to a cell `cell=` pixels wide (default 480);
`cols=` sets the columns (default: the most nearly square sheet). Each cell is labelled with its number and its
seconds since the first frame. When game time ran apart from real time (slow motion through `/time`, a pause, a long
hitch) the labels give game time and end in GAME; the reply lists both for every frame, and `frame` counts rendered
frames. `keep=1` also writes each frame as `frame-NN.png` into a folder named after the sheet, beside it.
`video=<file.mp4|.gif>` (or `video=mp4` beside the sheet) turns the frames into a video with ffmpeg, found on the
PATH or given as `ffmpeg=`, at `fps=` (default: the rate they were taken at, so it plays at real speed); ffmpeg runs
in the background and any error it gives comes back in the reply. Each capture reads back the whole screen, so
`every=1` runs below the game's normal frame rate; the times in the reply are the measured ones. A burst is local to
this machine and is refused on a dedicated server.

### Debug overlay

`/overlay?show=ai,paths` (or `all`) draws what has no look of its own within `radius=` metres (30) of the player, or of
the free camera while it is on, redrawn every `every=` seconds (0.5). `show=` adds categories, `off=<names>` or `off=1`
removes them; `/overlay` alone lists what is on and what each found and drew. A category draws at most `max=` lines
(400, colliders nearest first) and says `capped` when it stops.

| Category | Lines |
| --- | --- |
| `colliders` | boxes, spheres, capsules, mesh colliders as bounds: creatures red, pieces amber, triggers green, the rest blue; `layer=piece,Default` filters |
| `ai` | view range as a wedge of the view angle (all round while alerted), hearing a faint ring; cyan calm, red alerted; magenta line to the target |
| `paths` | the AI's path (`BaseAI.m_path`) orange; the point asked for green if found, red if not |
| `wards` | radius green on, grey off |
| `portals` | trigger cyan, light-up range faint violet, violet line to the game's paired portal, ring at its exit |
| `terrain` | edited ground hatched orange (height), faint yellow (paint); terrain modifiers as level green, smooth blue, paint yellow rings |
| `spawners` | spawn point or radius red, trigger distance orange, nest near radius yellow, spawn group pink |
| `zones` | 64 m zones on the ground: white loaded, grey not, the player's cyan |

Lines show through everything, on this machine only; logging out turns the overlay off; a dedicated server refuses it.
Targets and paths exist only where the AI runs (the creature's owner). Wayfare ignores the game's pairing, so with it
the portal line is not where a portal sends anyone.

### Game events

`/events` reports what happens in the game as numbered events, so a test can wait for "the troll died" instead of
polling. Each event has `seq`, `kind`, `clock`, `time` (the game's `Time.time`) and `data`. A bare `/events` returns
only `next`: pass it back as `since=` after acting. `since=0` reads every event still kept (the last 4000), `since=-N`
the last N. `kinds=death,hit` keeps those kinds (`-hit` leaves one out), `grep=Troll` keeps events whose JSON contains
the text, `wait=30` waits up to 30 s for at least one, and `max=` (default 100) caps the reply, oldest first (`more`
says there are more). `stream=1` sends the same events as NDJSON, one line each as it happens, until the caller hangs up
or `seconds=` pass; after 15 quiet seconds it sends one space, which ends no line. Both are answered on the HTTP thread
and never hold up a frame. `kinds=list` describes every kind.

| Kind | Data |
| --- | --- |
| `hit` | target, prefab, id, level, damage (after block, resistances, armour, difficulty), types, raw, health, max, how, attacker, distance |
| `death` | name, prefab, id, level, how, killer, position, boss |
| `spawn` | name, prefab, id, level, origin (`created` here, or `loaded` for an existing ZDO), owner, position |
| `player` | the local player: spawned, respawned, died, teleport, arrived (blocked), skill |
| `boss` | appeared, alerted, died |
| `log`, `console`, `hitbox` | BepInEx warnings and errors (20 a second at most); console and chat lines; `/hitbox` records |
| `time`, `trace`, `tune`, `swap`, `reload`, `perf`, `scenario` | published by those endpoints |

Hits, deaths and boss alerts are seen on the machine that owns the target: single player sees everything, a client its
own player and the creatures it owns, a dedicated server only what it has loaded. Spawns are seen wherever a character
loads. Fire, poison and spirit arrive as later `hit` ticks (`how=Burning`, `Poisoned`).

### Tracepoints

`/trace?method=Type.Method` puts a Harmony patch on a game or mod method while the game runs and records each call: game
time and frame, duration in ms (other mods' patches included), the instance (a creature by name and level, other objects
by name, type and prefab), each argument as `name=value`, and the result or the exception. `stack=1` adds five calling
methods. The type is found as `/eval` finds it. `Type.Method(int,string)` or `overload=N` picks an overload (an
ambiguous name replies with the list), `Type..ctor` a constructor, `Type.get_X` a property. `where=text` keeps calls
whose instance or arguments contain the text, `sample=N` every Nth. After `max=` kept calls (default 200) the tracepoint
switches itself off, so tracing an Update method is safe. `log=1` writes each call to the log (`[DevBridge] trace N
...`), `events=1` to the event log.

`/trace` lists the tracepoints with their counts and state. `/trace?id=N&last=20` returns the newest calls. `off=N` or
`off=all` unpatches; the calls stay readable until `clear=1`. Every patch is under the Harmony id `DevBridge.trace`, and
unpatching removes only those.

Refused: generic methods and methods of generic types, abstract and extern methods, and methods of the runtime, Harmony
and DevBridge. Not read: a struct's own instance, ref and pointer returns, and out arguments. Arguments are shown as
they were when the call started. A trace sees only this process, so the owner's logic is seen only where this machine
owns the object. Calls from places where the JIT inlined a tiny method are not seen.

### What each mod costs per frame

`/perf?seconds=5` (max 60) samples the running game for that many real seconds. The reply gives the frames (fps;
milliseconds average, p50, p95, p99 and max, from Unity's unscaled frame time), the garbage collections and the managed
heap at start and end. Allocation per frame appears only where something counts it (Unity's development-build recorder);
Valheim's Boehm Mono does not, and the reply says so.

First it times every prefix, postfix and finalizer another mod has on any method, and the `Update`, `LateUpdate`,
`FixedUpdate` and `OnGUI` of every MonoBehaviour in a plugin's assembly (merged libraries count as their mod), by
patching those methods themselves under the Harmony id `DevBridge.perf`. Per mod: main-thread milliseconds and calls
per frame (its own nested timed calls counted once), other threads' calls apart, and its `top=` (default 10) methods
with what they patch, microseconds per call and the slowest call. Transpilers cannot be timed (their code is inside the
patched method) and are listed per mod; MonoMod hooks (Jötunn's) are not seen.

Each timed call costs a little (`timing.extra_us_per_call`, measured every time): compare frames with `baseline=1`,
which times nothing, or time one mod with `mod=OpenKeep`. Probing stutters the game for a few seconds first; the probes
come off over the frames after the reply, as they went on (HarmonyX keeps its rebuilt copies of the timed methods until
a restart, with the same behaviour). One sample runs at a time; its summary is also a `perf` event. It measures this
process only: a client's own cost, or a host's or server's.

## Testing

### Scenarios

`/scenario?file=<path.json>&timeout=120` runs a scripted test and replies `passed`, then for each step `ok`, `ms`, a
clipped `got` and, when it failed, the `reason`. The JSON can also be POSTed with `Content-Type: application/json`. A
file holds `name`, `setup`, `steps` and `cleanup` (lists of steps) and `continue_on_fail`; a bare list is just steps. A
step is one of:

```
{"do": "/console", "cmd": "spawn Greyling 1"}    any endpoint; its other keys are the arguments
{"eval": "Player.m_localPlayer.GetHealth()"}     the value after "Type = ", a bool as true or false
{"wait": 2}
{"wait_event": "death", "grep": "Greyling", "timeout": 30}
{"log_clean": "warning", "grep": "OpenKeep", "ignore": ["..."]}
```

Any step may add `name`; `json`, a JSON path into the reply (`state`, `[0].id`, `[?(@.prefab == 'Troll')]`); checks, all
of which must hold: `expect` (`ingame`, `>0`, `>=5`, `!=0`; a plain value means equal, numbers compare as numbers),
`expect_contains`, `expect_not_contains`, `expect_regex`, `expect_approx` with `tolerance` (default 0.01) and
`expect_count` (a list's items, the values picked, or text lines); `save`, which keeps the value for `${name}` in later
steps; `continue_on_fail` and `optional`. Text matches ignore case. Endpoint arguments whose names clash with step keys
(`/find`'s `name`) go in `"args": {...}`. A `timeout` in a `do` step goes to the endpoint.

A failed step skips the rest to cleanup, and cleanup runs every one of its steps; an optional step's failure is shown
but does not fail the run. `wait_event` looks from the start of the step before (`since`: `run`, `previous`, `now`);
`log_clean` from the start of the run. Each endpoint call runs on the main thread, as an HTTP call would, and one
scenario runs at a time. `timeout=` covers the whole run (default 300 s, max 595); a fifth of it, at most 15 s, is kept
for cleanup. Progress goes to the log as `[DevBridge] scenario <name>: ...`, and the end is a `scenario` event.
Examples are in `DevBridge/scenarios/`.

### Comparing instances

`/sync` puts one question to every DevBridge on this machine and compares the answers, to catch what works on a host
but not on a dedicated server. It finds the others on ports 7780-7789 (`ports=` narrows it) through `/status`, then
asks every instance, this one included, from threads of its own, so no game waits. `peers=1` lists each instance's
role, world, player and plugins, and the plugins whose versions differ or that some lack. `id=`, `nearest=` or
`hover=1` finds a ZDO here and compares its copies: owner, data and owner revisions, metres apart, and every stored
value (only here, only there, different). `expr=` compares an `/eval` result; an assignment runs on every instance.
`config=<plugin>` compares `/config` values. `"same"` is true only when every peer agrees; an unreachable peer is a
`problem` row, not an error.

On a bound player Charter writes the server's values into the config entries without saving the .cfg, so `/config`
there shows what the server pushed, and `charter.serverDiff` lists the player's own values beside them. Synced YAML
files are not config entries and do not show.

A dedicated server beside the game: install Steam's Valheim Dedicated Server tool, copy the `LocalTesting` profile's
`BepInEx` folder, `winhttp.dll` and `doorstop_config.ini` into its folder, and have the user start
`valheim_server.exe -nographics -batchmode -name DevTest -port 2456 -world DevTest -password devtest1 -public 0`.
Join it at `127.0.0.1:2456`; whichever starts second gets port 7781. (Untried on this machine.)

## A stage for new assets

The stage shows an asset from `ValheimAssets` in the running game, next to the game's comparable assets, lit and
shaded the way the game does it, with no mod code and no restart for each change. Everything on it is local to this
machine: nothing is networked, saved or seen by other players, and logging out removes it. It lives under one
`DevBridge_Stage` object in the world scene (`/ui?path=DevBridge_Stage&depth=3` shows it).

**Still copies.** `/place` and `/lineup` make a copy on an inactive bench, strip it there before anything in it wakes,
then wake it with `ZNetView.m_forceDisableInit` set, the way the game makes its own local copies (a network view that
wakes under it removes itself, so no ZDO is ever made; never "ghost init"). A still copy keeps what draws and sounds
(renderers, level of detail, animators, particles, lights, audio, `VisEquipment`, `LightLod`, `LightFlicker`, `ZSFX`)
and loses the rest: creature, AI, piece, item and network scripts, rigidbodies, joints and colliders. It does not
move, think, fall, block, take hits or show a health bar. Its animator plays the controller's default state (no root
motion, no animation events) until `/animate` says otherwise. A game creature is sized as the game sizes it (world
enemy size and world level), and with `gear=1` (the default) carries its default items and the first of its random
weapon, shield, armour and set.

**The row.** The first object stands `distance` metres (default 4) in front of the player, facing them (its +Z, the
workshop's front); later ones go beside it with `gap` (or `spacing`) metres between their drawn meshes: `/place` to the
right, `/lineup` alternating right and left so the new asset stays in the middle (`side=right|left|both`). Each stands
on the ground under it with its lowest drawn point on the ground (`sit=1`, the default; `sit=0` puts the pivot there),
raised by `height`, turned by `yaw`, sized by `scale`. `new=1` starts a new row where the player now looks; `at=x,y,z`
puts one object at an exact point outside the row. Clearing everything resets the row.

**Dressing.** A workshop bundle keeps plain Standard placeholder materials; a mod dresses them in the game's own at
runtime (ValheimModLibs' `GameMaterials`). `dress=` does the same here, always starting from the materials the asset
had when placed:

| `dress=` | Material copied |
| --- | --- |
| `Creature` | the game's creature and item shader (`Custom/Creature`) as the Skeleton's body wears it; plain at gloss 0.1 |
| `Piece` | the game's building shader (`Custom/Piece`) as the wood chest wears it; plain at gloss 0.1 |
| `<game prefab>` | that prefab's body (its largest skinned mesh) or first renderer's material, or the renderer under `child=` |
| `none` | back to the bundle's placeholders |

The copy keeps the game material's shader, lighting, fog, rain and snow response and settings, and takes the
placeholder's baked albedo (`_MainTex`) and normal map (`_BumpMap`), as `GameMaterials.Dress` does. `plain=<gloss>`
then takes off the game maps laid out for the game model's UVs (metal mask, gloss, glow, style variants) and sets the
gloss, as `GameMaterials.Plain` does (plus the building shader's `_MetallicTex`); `plain=off` keeps them. `only=text`
dresses only the placeholder materials whose name contains the text, so one asset can wear several game materials
(one `/dress` per part). A bundle particle effect can be dressed from a game effect (`dress=vfx_...`): its texture
goes onto a copy of that effect's particle material.

**Wearing.** `/place?asset=<kit>&on=Skeleton` puts the asset on a still copy of a game creature, the way the mods
hang the workshop's kits: each top-level child named after a bone (`l_hand`, `RightHand`, `root`) hangs on that bone
at no offset. `bone=RightHand_Attach` hangs the whole asset on one bone at no offset, keeping its world scale, as the
game attaches a held item (so a weapon built in the game's attach frame sits in the fist). `gear=0` leaves the
creature's own weapons off. `/animate?swap=<game clip>:<bundle clip>` puts the workshop's clips in place of the
controller's, as the mods do with an `AnimatorOverrideController`; `/animate` alone lists the controller's clips,
parameters and what each layer plays, and `play=`, `trigger=`, `set=forward_speed:1.5`, `speed=` drive it.

**Effects and sounds.** `/effect?asset=<bundle prefab>` or `game=vfx_...` plays a particle effect at a placed object's
centre (`at=<id>`), a point, or the row, as the LocalEffects library plays them (no area damage, projectile, transform
sync or collider); `scale` resizes every particle system with the root; `life=` removes it after that many seconds.
`/sound?game=sfx_...` plays a game sound; `/sound?clip=<bundle AudioClip>&base=sfx_...` plays a workshop clip through
a copy of a game sound, so it gets that sound's mixer group, 3D rolloff, pitch and volume spread, delay and reverb (the
reply lists them). The camera hears it, the free camera included.

**Framing and light.** `/frame` points the game's own free-fly camera (the `freefly` console command's) at everything
placed (or `id=`, or `at=x,y,z&radius=`), from the side the row faces, turned `yaw` and tilted `pitch` degrees (default
12 down), at a distance that fits it in the view (or `distance=`), with the game's field of view (or `fov=`). It moves
the player 2 m behind the camera, out of the shot (`park=0` leaves them), and `hud=0` hides the HUD. `camera=player`
moves and turns the player instead and leaves the normal camera behind them (`zoom=`); it is approximate.
`/frame?off=1` returns to the normal camera and HUD. `/light?tod=0.5&env=Clear` fixes the time of day (0 midnight,
0.25 dawn, 0.5 noon, 0.75 dusk) and the weather environment at once, and `wind=<angle>,<0-1>` the wind; `list=1` lists
the environments, `reset=1` (or `off` for each) hands them back to the world. These are the fields the console's
`tod`, `env`, `resetenv` and `wind` commands set, set directly so no `devcommands` is needed; `skiptime` exists too but
moves the world's clock for everyone, so it is not used here.

### Looking at a new asset

```
B=http://127.0.0.1:7780
W=C:/Users/gglasgow/projects/ValheimAssets
S=<session scratchpad>
.\build.ps1 -Asset ecp_battleaxe_bone -Bundle ecp_preview          # in ValheimAssets: model, bake, bundle
curl -s "$B/wait?for=player&timeout=300"                           # the user has the LocalTesting profile in a world
curl -s "$B/bundle?load=$W/out/bundles/ecp_preview.windows"        # its assets by type: triangles, textures, clips
curl -s "$B/place?asset=ecp_battleaxe_bone&dress=Creature"         # 4 m ahead, facing the player, in the game's shader
curl -s "$B/lineup?prefabs=Battleaxe,BattleaxeCrystal,AxeBlackMetal,Skeleton&gap=0.4"   # the codex's references
curl -s "$B/light?tod=0.5&env=Clear"
curl -s "$B/frame?pitch=8&hud=0"
curl -s "$B/screenshot?out=$S/axe-1.png"                           # then Read the PNG
# change model.py, run build.ps1 again, then:
curl -s "$B/bundle?reload=ecp_preview"                             # the axe comes back in place, same id and dress
curl -s "$B/screenshot?out=$S/axe-2.png"
curl -s "$B/place?asset=ecp_battleaxe_bone&on=Skeleton&bone=RightHand_Attach&gear=0&dress=Creature"
curl -s "$B/frame?id=<its id>&yaw=30"                              # the Skeleton holding it
curl -s "$B/frame?off=1"; curl -s "$B/light?reset=1"; curl -s "$B/clear"
```

Effects and sounds the same way: `/effect?asset=<vfx prefab>&at=<id>` beside `/effect?game=vfx_ice_destroyed&at=<id>`,
`/sound?clip=<clip>&base=sfx_ice_destroyed` beside `/sound?game=sfx_ice_destroyed`. `/prefabs?kind=sfx&filter=troll`
finds game sounds and effects to compare with or play through.

### Limits of the stage

- **Local and temporary.** Nobody else sees the stage, nothing of it is saved, and it is not a test of the mod: the
  mod's own prefab registration, sync, AI and code still need the mod built and the game restarted.
- **One bundle name at a time.** Unity loads no two bundles of the same name. When a mod in the profile embeds the
  bundle (Elite Creatures Pack loads its bundles as the world loads), build the preview under another name
  (`build.ps1 -Bundle <name>_preview`) or disable that mod. A bundle only loads on the platform it was built for:
  `load=` takes the `.windows` build (a folder, or a path without the extension, finds it). Bundles are read into
  memory, so the file stays free for the next build to overwrite. If a rebuilt bundle fails to load, what was placed
  from it is gone: fix the build and `load=` again.
- **Reload puts assets back at the same pivot** with the same id and dresses, and clip swaps that used the bundle are
  applied again; a new version's size does not move it (place it again to re-sit it). Effects and sounds from the
  bundle are dropped, not replayed.
- **What a dress reproduces:** the game material's shader and everything the shader does with light, fog, rain and
  snow, its settings (gloss, normal strength, culling, cutout), and the asset's own baked albedo and normal map.
  **What it does not:** any other map of the asset's own (a metal mask, a glow map, style variants: the dress uses the
  game's, or with `plain` none); anything the game's scripts set on a live object (creature level colours, a player's
  skin and hair, hit flashes, burning and wet looks, snow on pieces, wear and damage states); and any look a mod builds
  in its own code beyond Dress and Plain (a glow on some parts, a tint, a scale), which only the mod shows. Particle
  effects keep their own colours, sizes and timings; a dress only moves the texture onto a game particle material.
- **Still copies are not live.** No IK, head look, footsteps, level stars or random looks; animation events do not
  fire, so effects and sounds tied to an attack's frames are not played (use `/effect` and `/sound` alongside). Gear is
  what the ObjectDB knows: creature-only attack items without a model are left out (the reply names them).
- **Fixed light is this machine's.** `tod` also changes this machine's own day and night checks, so where it owns the
  zones around (single player, a host) night creatures can spawn under a midnight `tod`.
- **The free camera takes the game's movement keys** (W, A, S, D, jump, crouch) and the mouse, and the player takes
  no input while it is on: keep the real mouse off the window, and `/frame?off=1` before driving the player again.

## Changing the running game

### Tuning values live

`/tune` changes a value on a prefab and on every loaded copy of it at once, so a fight can be adjusted without a
restart. `prefab=Troll&field=Character.m_runSpeed` names a component member (the field starts with the component); it is
written on the prefab and each live instance. `item=` names an item's shared data, the path read from there: a
creature's attack item with `prefab=` the creature, or a player item alone. It is written on the item prefab and every
loaded copy (creatures, players, containers, world drops), each distinct object once; a struct such as `m_damages` is
written back whole. `value=` takes a number, `true`, an enum name, text, `x,y,z` or `r,g,b,a`; `*1.2`, `/2` and `+0.5`
work from each copy's own value. Without `value=` the reply shows the prefab's value and each distinct live one;
`members=1` lists what is there. `list=1` shows the changes with their originals, `revert=all` or `revert=<n>` puts them
back (copies spawned since included), and `code=1` prints them as C# to paste into the mod, since a value a mod writes
at load returns on restart.

    curl -s "$B/tune?prefab=Troll&item=troll_punch&field=m_attack.m_attackRayWidth&value=*1.3"   # hit shape
    curl -s "$B/tune?prefab=Troll&item=troll_punch&field=m_aiAttackInterval&value=4"             # pause between punches
    curl -s "$B/tune?prefab=Troll&field=MonsterAI.m_minAttackInterval&value=2"                   # pause between any attacks
    curl -s "$B/tune?prefab=Troll&field=Character.m_runSpeed&value=*0.8"                         # speed
    curl -s "$B/tune?code=1"     # Troll / troll_punch: shared.m_attack.m_attackRayWidth = 0.39f; // was 0.3f

An item is one prefab however many creatures carry it: `prefab=Troll&item=troll_punch` changes every creature that
carries `troll_punch`. In Git Bash a value starting with `/` is turned into a path; write `value=%2F2` (or set
`MSYS_NO_PATHCONV=1`).

Changes are local to this machine. An attack is worked out on the attacker's owner, so a tuned creature fights
differently only where this machine owns it (single player, or the host for its creatures). A change reaches the next
swing (each attack starts from a copy). The game has no field for attack animation speed: the clips set the animator's
speed themselves through animation events.

### Swapping a real prefab's look

`/swap` puts a freshly built bundle prefab's look on a real prefab (a mod's creature, item or piece, or the game's)
while the game runs: on the prefab, so every new spawn, on its live copies in the world, the stage's still copies of it,
the build ghost and, for an item, the visuals characters, armour stands and item stands show. Each mesh renderer of the
prefab is paired with one of the bundle prefab's by path, then a path that ends the other, then renderer name, then mesh
name (`map=<bundle renderer>:<live renderer>` pairs by hand). A skinned mesh is rebound to the live skeleton by bone
names; one whose bones are missing fails and keeps its look. By default each material stays the live one (the mod's
dressed game shader) as a copy wearing the bundle's textures; `materials=replace` uses the bundle's placeholders,
`materials=keep` leaves them (for a mod that dresses by placeholder name, like the mimic). `clips=1` overrides the
animators' clips of the same names, keeping a fighting creature's state. The reply lists each renderer as swapped
(triangles before and after), kept or failed, the bundle's unused renderers as missing, and the copies reached.
`watch=1` reloads the bundle when its file is rebuilt and swaps again in the same frame, as `/bundle?reload=` does;
`revert=1` puts everything back, copies spawned since included. Only this machine sees it: colliders, AI and networking
are untouched, and swaps last until reverted or the game closes, across logouts (a mod's prefab built anew for each
world takes the swap again within a second). A mod that embeds the bundle holds its name, so build the preview as
`<name>_preview`.

    .\build.ps1 -Asset crypt_mimic -Bundle ecp_mimic_preview          # in ValheimAssets
    curl -s "$B/swap?prefab=ECP_CryptMimic&load=$W/out/bundles/ecp_mimic_preview.windows&asset=crypt_mimic&materials=keep&clips=1&watch=1"
    curl -s -G --data-urlencode "cmd=spawn ECP_CryptMimic 1" "$B/console"
    # change model.py, run build.ps1 again: a second or two after it writes the bundle the mimic wears the new build
    curl -s "$B/swap?list=1"; curl -s "$B/screenshot?out=$S/mimic-2.png"
    curl -s "$B/swap?prefab=ECP_CryptMimic&revert=1"

Not covered: ragdolls (their own prefabs: swap them separately), icons, colliders, the renderer objects' own transforms,
and copies a mod made of a swapped game prefab while the world loaded (prefabs of their own); mod code that sets
materials or controllers again at runtime wins over the swap.

### Reloading a mod without a restart

`/reload?mod=OpenKeep` swaps a mod's code in the running game. Mono cannot unload an assembly, so the old copy stays
loaded but is taken apart: the Harmony patches whose methods live in its assembly come off, its plugin is switched off
at once, its MonoBehaviours in the loaded scenes are destroyed (the plugin component last), its console commands, RPC
handlers and event handlers are removed, and the timers and asset bundles its static fields reach are stopped or
unloaded. The rebuilt DLL is renamed (`OpenKeep-reload-1`), written to `BepInEx/cache/DevBridgeReload` and loaded; the
new plugin starts as at game start, and its `Terminal.InitTerminal` patches run again so its commands come back. A DLL
that does not load changes nothing.

The loop: build the mod, then `/reload?mod=<Mod>`; or `watch=<Mod>` once, and every build reloads by itself.
`list=1` shows which mods run an older build than their DLL.

Patch logic, UI code, commands and settings apply at once. What a mod sets up as a world loads (prefabs on
`ZNetScene.Awake`, items on `ObjectDB.Awake`, RPCs on `ZNet.Awake` or `ZoneSystem.Start`) comes with the next world
load, so reload such mods at the main menu, then load the world; the reply's `takesEffect` says which applies. YAML
files load at the main menu: reloaded there, a mod reads them at once; reloaded in a world, after the next visit to
the menu.

Left behind, and listed in `leftBehind`: components on prefabs and on the inactive holders mods keep prefab copies
in, UI the old copy built without a component of its own, running threads, coroutines it started on game objects.
Each reload keeps one more copy in memory: restart now and then, and always before a final test. Only this machine
changes: it still talks to other players (RPC names stay the same), but a client's server-bound settings are its own
until it reconnects, and SyncedConfig looks for YAML files beside the cache copy, not the plugin DLL.

## Notes and limits

- The game keeps running and accepting injected input while it is in the background (the bridge turns on
  `runInBackground` and the Input System's ignore-focus mode). Keep the window visible, not minimized, or screenshots
  come out blank, and keep the real mouse cursor off the game window, since the OS still sends it mouse moves there.
- `/click` sends pointer events straight to UI elements; it cannot trigger anything that polls the mouse. Use `/mouse`
  for that (drags, camera, world interaction).
- Text typed into input fields is set with `/type`; key presses from `/key` do not type characters into them.
- Release anything held with `/key?up=all` when done.
- The bridge never launches or closes the game. Starting and restarting it stays with the user.
- Listening on 127.0.0.1 only, with no authentication: any local process can drive the game while it runs. Requests
  from a web browser (an `Origin`, `Referer` or `Sec-Fetch-Site` header) or naming another host than 127.0.0.1 or
  localhost are refused with 403, so a web page cannot reach `/eval` or `/reload`.
