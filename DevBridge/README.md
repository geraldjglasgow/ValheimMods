# DevBridge

A test bridge for Valheim modders and their AI agents: it opens the running game to HTTP on 127.0.0.1, so an LLM
or a script can see the game, play it and check a mod unattended.

## Features
- See and drive: screenshots, the UI tree, the log, ZDOs; clicks, keys, mouse and console commands.
- Inspect and measure: reflection, method traces, game events, frame cost per mod, memory hitches.
- Test and iterate: scripted scenarios, client and server compared, DLL hot reload, staged asset bundles.
- `curl -s http://127.0.0.1:7780/help` lists every endpoint with its arguments.

## Endpoints
- `/status`: menu, loading or in a world, the player, open screens and loaded plugins.
- `/screenshot`: the game window with its UI, saved as a PNG or JPG.
- `/ui`, `/find`: the live UI tree, and elements found by text, name or component.
- `/log`: recent lines of the BepInEx log or the game console.
- `/nearby`, `/zdo`: networked objects around the player, and every value a ZDO stores.
- `/prefabs`, `/config`: the game's prefabs by name and kind; a mod's config values in memory.
- `/click`, `/hover`, `/key`, `/mouse`, `/type`: real input on UI elements, keys and the mouse.
- `/console`: runs a console command and replies with what it printed.
- `/wait`: waits for seconds, the menu, a loaded world or a log line.
- `/eval`: reads, sets or calls anything by reflection, private members too.
- `/trace`: records each call of a game or mod method, with arguments and result.
- `/events`: hits, deaths, spawns, boss fights and warnings, polled or streamed.
- `/perf`, `/heap`: frame cost per mod; memory and garbage-collection hitches.
- `/time`, `/burst`: slow motion and frame stepping; a run of frames on one contact sheet.
- `/overlay`, `/hitbox`: colliders, AI senses, paths and wards drawn in the world; melee reach.
- `/scenario`: a scripted test from a JSON file, pass or fail per step.
- `/sync`: one question to every game and server on this machine, with the differences.
- `/tune`: changes a value on a prefab and every live copy, printable as C#.
- `/reload`: hot-reloads a mod's rebuilt DLL without restarting the game.
- `/studio` (F3): a page in your browser to pick an object near you, move its parts, attach effects and play them, find
  the game's items by name, kind or biome, grouped, with their tier, and turn each one in a viewer, or open the asset
  workshop's built bundles and turn their models and effects in the game's own look.
- `/swap`, `/bundle`, `/place`, `/lineup`: asset bundles staged beside the game's own prefabs.
- `/animate`, `/effect`, `/sound`, `/frame`, `/light`: animations, effects, sounds, camera and lighting for staged models.

## Install
For development profiles only, never a profile you play in: any program on your computer can control the game while
it runs. It is an internal tool, never released to a store: build it (the build copies `DevBridge.dll` into the
`LocalTesting` profile) or put the DLL in a development profile's `BepInEx/plugins`. It listens on
127.0.0.1 only and refuses requests from web browsers. On a dedicated server most endpoints work; screenshots,
overlays, look swaps and input do not.

## Configuration
`BepInEx/config/com.DevBridge.cfg`: the port (default 7780; a second game takes the next free one, up to 7789).

## Links
Discord: https://discord.gg/DrFUyfuXzT

Full reference: https://github.com/geraldjglasgow/ValheimMods/blob/main/DevBridge/REFERENCE.md. Bugs and ideas:
https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.
