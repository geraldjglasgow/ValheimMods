# DevBridge

A test bridge for Valheim modders and their AI agents: it opens the running game to HTTP on 127.0.0.1, so an LLM
or a script can see the game, play it and check a mod unattended.

## Features
- See: screenshots, the live UI tree, the log, game state, nearby objects and their ZDOs.
- Drive: clicks, keys, mouse, typed text, console commands, waits for a menu, a loaded world or a log line.
- Inspect: reflection and expression eval on any object, method traces, a stream of game events.
- Measure: frame cost per mod, memory and garbage-collection hitches (`/heap`), slow motion and frame stepping, motion contact sheets, debug overlays (colliders, AI
  senses, paths, wards).
- Test: scripted scenarios, a client compared with a dedicated server on one machine, live value tuning.
- Iterate: hot reload a mod's DLL, swap a prefab's look, stage an asset bundle beside the game's own models.
- `curl -s http://127.0.0.1:7780/help` lists every endpoint.

## Install
For development profiles only, never a profile you play in: any program on your computer can control the game while
it runs. Install with r2modman or the Thunderstore app, or put `DevBridge.dll` in `BepInEx/plugins`. It listens on
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
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.
