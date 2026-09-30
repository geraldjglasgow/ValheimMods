# DevBridge

A dev-only BepInEx plugin that lets an agent (or a script) see and drive the running game over HTTP on localhost:
screenshots, the live UI tree, clicks, key presses, mouse, console commands, the log, reflection on any object, and
the ZDOs of nearby objects. It exists to test the mods in this workspace in the real game, and to look at new assets
from `AssetWorkshop` there: load a freshly built bundle, stand its models beside the game's own under the game's
lighting and shaders, play its effects and sounds, frame it all for a screenshot, then rebuild and reload without
restarting the game (see "A stage for new assets").

It is **never packed, uploaded or shipped**. It has no `thunderstore/` folder, so `pack.ps1 -All` skips it, and no
mod depends on it. It belongs in the `LocalTesting` profile only.

## Install

```
dotnet build DevBridge/DevBridge/DevBridge.csproj -c Release
```

The build copies `DevBridge.dll` into the `LocalTesting` profile's plugins folder. Start the profile from r2modman;
the log shows `DevBridge listening on http://127.0.0.1:7780/`. Rebuilding while the game runs takes effect on the
next start: BepInEx does not reload plugins.

The port is set in `BepInEx/config/com.DevBridge.cfg` (default 7780). A second game, or a dedicated server with
DevBridge on the same machine, gets the next free port up to 7789. On a dedicated server everything except
screenshots and input works.

## Using it

`curl -s http://127.0.0.1:7780/help` lists every endpoint with its arguments. Arguments go in the query string or a
form body, so text with spaces is easiest as `curl -s -G --data-urlencode "cmd=spawn Troll 1" .../console`. In
PowerShell call `curl.exe`, not the `curl` alias.

| Endpoint | What it does |
| --- | --- |
| `/status` | state (`starting`, `menu`, `loading`, `ingame`), world, network role, player, open screens, time, plugins |
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
| `/zdo` | every value a ZDO stores (`id=`, `hover=1`, `nearest=Troll`), key names recovered from the game's and plugins' strings |
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
ground under it. A body that crosses the edge is hit (the sweep is spheres of the attack's ray width out to its range, so
the edge is the reach). Every hit on the local player, whatever made it (swing, projectile, a mod's own area damage),
draws a yellow line from the attacker's centre to the player with the player's body outlined. The reply, and the log
(`/log?grep=hitbox`), list the recent swings and hits: the attack's item name, shape, range and ray width, the damage,
and the distance centre to centre and the gap body to body (`you_distance`/`you_gap` on a swing, `distance`/`gap` on a
hit). `players=1` adds the players' own swings; `clear=1` empties the list; `off=1` stops it.

The lines are drawn over everything (no depth test) and only on this machine, and a swing is drawn only where the game
works it out, on the attacker's owner (single player or the host). Hits on the player are seen wherever the player is.
Area and custom damage have no shape to draw; their hits still get the yellow line and the distances.

## A stage for new assets

The stage shows an asset from `AssetWorkshop` in the running game, next to the game's comparable assets, lit and
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
workshop's front); later ones go beside it with `gap` (or `spacing`) metres between their drawn meshes: `/place` to the right,
`/lineup` alternating right and left so the new asset stays in the middle (`side=right|left|both`). Each stands on the
ground under it with its lowest drawn point on the ground (`sit=1`, the default; `sit=0` puts the pivot there), raised
by `height`, turned by `yaw`, sized by `scale`. `new=1` starts a new row where the player now looks; `at=x,y,z` puts
one object at an exact point outside the row. Clearing everything resets the row.

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
W=C:/Users/gglasgow/projects/ValheimMods/AssetWorkshop
S=<session scratchpad>
.\build.ps1 -Asset ecp_battleaxe_bone -Bundle ecp_preview          # in AssetWorkshop: model, bake, bundle
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

## Notes and limits

- The game keeps running and accepting injected input while it is in the background (the bridge turns on
  `runInBackground` and the Input System's ignore-focus mode). Keep the window visible, not minimized, or screenshots
  come out blank, and keep the real mouse cursor off the game window, since the OS still sends it mouse moves there.
- `/click` sends pointer events straight to UI elements; it cannot trigger anything that polls the mouse. Use `/mouse`
  for that (drags, camera, world interaction).
- Text typed into input fields is set with `/type`; key presses from `/key` do not type characters into them.
- Release anything held with `/key?up=all` when done.
- The bridge never launches or closes the game. Starting and restarting it stays with the user.
- Listening on 127.0.0.1 only, with no authentication: any local process can drive the game while it runs.
