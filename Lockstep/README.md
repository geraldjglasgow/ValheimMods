# Lockstep

The next boss stays sealed until every player on the server has defeated the previous one. Fast players wait for the
group; nobody skips ahead.

## Features
- A boss altar opens only once everyone has defeated the boss before it, and says who the group is waiting for.
- Credit per player: everyone who hit the boss, plus everyone within a set distance when it dies.
- The roster fills itself; players away for a set number of days stop holding the group back.
- New players catch up with everything the world has already cleared (configurable).
- Safe to install mid-playthrough: bosses already defeated count as cleared for everyone.
- The boss chain is a YAML file: add modded bosses or change the order.
- Admins can grant or revoke credit and ignore or forget players from the console.

## Install
Needed on the server and every client; the server refuses clients without it. Install with r2modman or the
Thunderstore app, or put `Lockstep.dll` in `BepInEx/plugins`. Replaces OathBound (up to 0.1.2): remove it, its config
files are not read.

## Configuration
`BepInEx/config/com.Lockstep.cfg` and `LockstepChain.yml`; the server keeps its roster in
`Lockstep.<world>.roster.yml`. Every setting is described in the file and applies without a restart; the server's
values bind every player (`Lock Configuration`). Console: `lockstep status` shows who the group waits for; admin
commands: https://github.com/geraldjglasgow/ValheimMods/blob/main/Lockstep/PLAN.md. `charter status` shows whether
the server binds your settings.

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.
- The co-op group that kept waiting at the Elder altar for the one player who was still at work.
