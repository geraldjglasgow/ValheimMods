# Getting Started

Lockstep keeps a Valheim group together on the boss path. A boss altar stays sealed until every player on the server
has defeated the boss before it, and its hover says who the group is waiting for. Fast players can still explore, gather and
build anywhere; they just cannot summon the next boss ahead of the group. Installing on a world you already play is
safe: bosses already defeated count as cleared for everyone.

This wiki describes version 0.4.0.

Lockstep was published as OathBound up to 0.1.2. It does not read OathBound's files, so remove OathBound. It is not
related to LionAndOtter's Oathbound.

## Install

- On the server **and** every client, the same version everywhere: r2modman, the Thunderstore app, or `Lockstep.dll`
  in `BepInEx/plugins` (needs BepInExPack for Valheim). Hosted worlds work; the host is an admin.
- A player without Lockstep is disconnected; a player with another version is refused with a message naming both
  versions.
- A player with Lockstep is refused by a server that runs other mods with the same join check (see
  `charter versions`) but not Lockstep. On a server that runs none of them, nothing is sealed and the player is told
  so a few seconds after spawning: on screen, in chat, in the altar hover and in `lockstep status`.

## Quick start

1. Install, start the server once. The seven vanilla bosses need nothing else; the settings and the boss chain are
   in `BepInEx/config`, and the server's copy applies to everyone.
2. `lockstep status` in the console (F5) shows which altars are open and who each one waits for.
3. Admins unstick the group with `lockstep grant <player> <stage>` or `lockstep ignore <player>`.
4. Add modded bosses to `LockstepChain.yml`.

## Pages

- [Credit and the Altar](wiki:Credit and the Altar) - who gets credit, who counts, when an altar opens, other mods.
- [Configuration](wiki:Configuration) - every setting, the boss chain, the roster file.
- [Console Commands](wiki:Console Commands) - `lockstep` and `charter`.

## Links

- Source: https://github.com/geraldjglasgow/ValheimMods
- Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues - name the mod and version, say what
  happened and attach `BepInEx/LogOutput.log`.
