# Lockstep

The next boss stays sealed until every player on the server has defeated the previous one. Fast players wait for the
group; nobody skips ahead.

## Features
- Altar gate: a boss altar opens only once everyone has defeated the boss before it.
- Altar hover: every gated altar says whether it is open or which players the group is waiting for; the offering
  is kept.
- Credit per player: everyone who hit the boss, plus everyone within a set distance when it dies.
- Roster: fills itself as players join; players away for a set number of days stop holding the group back.
- Late joiners: new players catch up with everything the world has already cleared, or earn each boss themselves.
- Safe mid-playthrough: bosses already defeated count as cleared for everyone.
- Spawn guard: a sealed boss cannot spawn even if another mod gets past the altar.
- Open world: biomes, portals, dungeons and crafting stay open, and the game's own boss progress is untouched.
- Boss chain: Eikthyr, The Elder, Bonemass, Moder, Yagluth, The Queen and Fader, in a YAML file; add modded bosses or
  change the order.

## Console commands
- `lockstep status`: each altar, open or who it waits for, and every player's credit.
- `lockstep grant <player> <boss>`: gives a player credit for a boss (admins).
- `lockstep revoke <player> <boss>`: takes that credit away (admins).
- `lockstep ignore <player>` / `unignore`: the player never holds the group back, or counts again (admins).
- `lockstep forget <player>`: removes a character from the roster; joining again starts fresh (admins).
- `charter status`: whether the server's settings bind yours.

## Install
Needed on the server and every client; the server refuses clients without it, and a player on a server without it is told so. Install with r2modman or the
Thunderstore app, or put `Lockstep.dll` in `BepInEx/plugins`. Replaces OathBound (up to 0.1.2): remove it, its config
files are not read.

## Configuration
`BepInEx/config/com.Lockstep.cfg` and `LockstepChain.yml`; the server keeps its roster in
`Lockstep.<world>.roster.yml`. Every setting is described in the file and applies without a restart; the server's
values bind every player (`Lock Configuration`). The Wiki tab has the details.

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
