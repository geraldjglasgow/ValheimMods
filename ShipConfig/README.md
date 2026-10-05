# ShipConfig

Configure every ship in Valheim: health, speed, steering, damage and build cost, for vanilla and modded ships.

## Features
- Every ship gets its own settings, defaulting to its vanilla values: `Health`, `SailForce`, `PaddleForce`, steering,
  drag, damage taken, weather wear, build cost and more.
- Global multipliers on top of every ship's values.
- Modded ships get settings too (any ship with a `Ship` and a `WearNTear` component).
- Changes apply at once, to new ships and to ships already in the world.
- `AshlandsOceanDamage` off lets ships sail the Ashlands ocean unharmed (this skips the Drakkar's progression gate).
- A small ship panel under the minimap while aboard: speed, speed multiplier, map explore radius and, with
  GrindstoneSkills, your Sailing abilities and their cooldowns (hover for details).

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `ShipConfig.dll` in
`BepInEx/plugins`.

## Configuration
`BepInEx/config/com.ShipConfig.cfg`; the ship entries appear after the first world load. Every setting is described
in the file and applies without a restart; the server's values bind every player (`Lock Configuration`). Console:
`charter status` shows whether the server binds your settings.

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.
