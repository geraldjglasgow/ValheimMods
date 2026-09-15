# ShipConfig

Configure every ship in Valheim: health, sail and paddle force, steering, drag, weather wear, damage taken, build cost and
more, with server sync and live config reloading.

### Features
- One entry per ship and setting in the `Ship` section, named after the prefab (`VikingShip.Health` is the Longship): Raft, Karve,
  Longship, Drakkar, and any modded ship that has both a `Ship` and a `WearNTear` component
- Per ship: `Health`, `SailForce`, `PaddleForce`, `RudderSpeed`, `TurnForceSailing`, `TurnForcePaddling`,
  `ForwardDrag`, `SidewaysDrag`, `AngularDamping`, `WaterImpactDamage`, `UpsideDownDamage`, `WeatherWear`,
  `AshlandsOceanDamage`, `DamageTaken`, `Invulnerable` and `BuildCost`. Every default is the ship's vanilla value
- Global multipliers on top of the per-ship values: health, sail force, paddle force, turning, damage taken, build cost
- Values apply to newly built ships and to ships already loaded in the world
- Health changes only the maximum: a loaded ship keeps its current health, so raising the max shows more damage until
  the ship is repaired, and lowering it leaves the ship above max until it takes damage
- `AshlandsOceanDamage` off lets a ship sail the burning Ashlands ocean unharmed. WARNING: that removes the
  progression gate that makes the Drakkar necessary, and the ocean effects and world key it triggers
- Config changes apply immediately without restarting the game
- Server enforces its settings for all connected players; "Lock Configuration" prevents clients from overriding them

### How to Install
1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install this mod.

For manual install, drag ShipConfig.dll into the BepInEx/plugins folder.

The ship entries are added to the config file when a world is loaded for the first time, because the ship prefabs
only exist in a loaded world.

### Server settings

Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. With it off, each player uses
their own file. In the console (F5), `charter status` shows whether the server binds your settings, `charter diff`
lists where the server's values differ from your own file, and `charter versions` lists the mods on both sides.
Joining with a missing or mismatched version of the mod shows one screen naming the mod and both versions, with a
refusal code that is also written to the server's and your own log.

### Building
Requirements: .NET SDK 8, Valheim installed with BepInEx, and the `ValheimModLibs` repository checked out next to this
one. `pack.ps1` builds the mod and creates a Thunderstore zip in `thunderstore/`.

```
dotnet build ShipConfig/ShipConfig.csproj -c Release
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

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you distribute that is built from it must carry the same freedoms and be released under the same licence, with source. See the `LICENSE` file for the full terms.
