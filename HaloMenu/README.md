# HaloMenu

A radial menu that other mods fill. Hold a hotkey and a ring of icons opens at the centre of the screen; move the
mouse or a gamepad stick toward one and let go to pick it.

HaloMenu adds no gameplay of its own: with no mod registering entries, it opens an empty ring.

## Features
- Hold or Toggle: hold the key and release to pick, or press to open and click to pick.
- Aim, not hover: the direction picks the segment, even past the ring's outer edge.
- Gamepad: either stick picks the same way the mouse does.
- Cancel: let go in the centre or press Escape (right-click too in Toggle mode).
- 2 to 16 segments, with the hovered entry's name in the middle.
- Hover feedback: the segment grows, brightens and gets a rim.
- Disabled entries dim and shake when picked; hidden entries leave no gap.
- No stray swings: attacks and hotbar keys are held back while a ring is open; walking still works.
- Several rings: a mod can own a ring with its own hotkey; only one is open at a time.
- Client only: no network traffic, works on a vanilla server and when only some players have it.

## For mod authors
Reference `HaloMenu.API.dll` only: your mod loads whether or not HaloMenu is installed.
- `HaloMenuAPI.Register`: adds an entry to the shared default ring.
- `HaloMenuAPI.CreateRing`: a ring of your own, with its own hotkey, layout and config sections.
- `RingEntry`: id, label, icon, order, visible and enabled checks, and what picking it does.
- Ring events: opening (can block), opened, highlight changed, selecting (can cancel), selected, cancelled.
- `HaloMenuAPI.IsAvailable`: whether HaloMenu is loaded; without it every call does nothing.
- API 1.0: only additions within 1.x; a breaking change would ship as a separate 2.0 assembly.
- [Sample.HaloMenuDemo](https://github.com/geraldjglasgow/ValheimMods/tree/main/HaloMenu/Sample.HaloMenuDemo): two
  entries on the default ring and a ring of its own.

## Install
Needed on each client only. Install with r2modman or the Thunderstore app, or put `HaloMenu.dll` and
`HaloMenu.API.dll` (both needed) in `BepInEx/plugins`.

## Configuration
`BepInEx/config/com.HaloMenu.cfg`: each ring's hotkey, activation, layout and look; a mod's own ring gets its own
sections. Every setting is described in the file and applies the next time a ring opens, no restart.

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.
