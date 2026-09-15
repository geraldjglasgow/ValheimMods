# Lockstep

The next boss stays sealed until every player on the server has defeated the previous one. Fast players wait for
the group; nobody skips ahead. Server synced, live config reload.

### Features
- Each boss altar only works once everyone in the group has defeated the boss before it. The altar says who the
  group is still waiting for.
- Credit is per player: everyone who hit the boss is credited, plus everyone within a configurable distance when
  it dies. Admins can grant or revoke credit.
- The roster fills itself as players log in. Players who have been away for a configurable number of days stop
  holding the group back, and admins can ignore or forget players.
- New players catch up automatically with everything the world has already cleared (configurable).
- Installing mid-playthrough is safe: bosses the world had already defeated count as cleared for everyone.
- The chain is a YAML file, so modded bosses can be added and the order changed.
- Server enforces its settings for all connected players; "Lock Configuration" prevents clients from overriding them

### How to Install
1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install this mod on the server and on every client. Clients without it are rejected by the server.
3. Published as OathBound up to 0.1.2; that package is deprecated and its config files are not read.

For manual install, drag Lockstep.dll into the BepInEx/plugins folder.

### Server settings

Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. With it off, each player uses
their own file. In the console (F5), `charter status` shows whether the server binds your settings, `charter diff`
lists where the server's values differ from your own file, and `charter versions` lists the mods on both sides.
Joining with a missing or mismatched version of the mod shows one screen naming the mod and both versions, with a
refusal code that is also written to the server's and your own log.

### Console commands
Run from the in-game console (F5). The changes are made on the server and answered there.

```
lockstep status                    every stage: open or waiting for whom, and the roster
lockstep grant <player> <stage>    credit a player with a boss           (admin)
lockstep revoke <player> <stage>   take that credit away                 (admin)
lockstep ignore <player>           the player never holds the group back (admin)
lockstep unignore <player>         count the player again                (admin)
lockstep forget <player>           remove the player from the roster     (admin)
```

`<player>` is a name or a player ID, `<stage>` is a stage name from the chain (Eikthyr, TheElder, Bonemass,
Moder, Yagluth, TheQueen, Fader) or its global key.

### Files
- `com.Lockstep.cfg`: credit radius, inactive days, late joiner mode, gate options. Hot reloaded, synced.
- `LockstepChain.yml`: the chain of stages, one entry per boss with its order, global key and boss prefab.
  Hot reloaded, synced.
- `Lockstep.<world>.roster.yml`: written by the server, one entry per player with last seen, ignored flag and
  cleared stages. Hot reloaded when an admin edits it.

### Building
Requirements: .NET SDK 8, Valheim installed with BepInEx, and the `ValheimModLibs` folder of the same repository next
to this one. `pack.ps1` builds the mod and creates a Thunderstore zip in `thunderstore/`.

```
dotnet build Lockstep/Lockstep.csproj -c Release
```

### Bugs and feature requests
The source lives on [GitHub](https://github.com/geraldjglasgow/ValheimMods). Found a bug or want a feature? Open an issue at
https://github.com/geraldjglasgow/ValheimMods/issues and name the mod, its version and what happened.

### Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.
- The co-op group that kept waiting at the Elder altar for the one player who was still at work.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you distribute that is built from it must carry the same freedoms and be released under the same licence, with source. See the `LICENSE` file for the full terms.
