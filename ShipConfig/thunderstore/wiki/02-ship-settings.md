# Ship Settings

A negative number counts as 0 in every setting except `SailForceOffset`.

## General and Multipliers

| Key | Default | Effect |
| --- | --- | --- |
| `Lock Configuration` | `true` | Read on the server only. On: every player uses the server's values and cannot override them. See [Multiplayer and Commands](wiki:Multiplayer and Commands) |
| `Health Multiplier` | `1` | Times every ship's `Health` |
| `Sail Force Multiplier` | `1` | Times every ship's `SailForce` |
| `Paddle Force Multiplier` | `1` | Times every ship's `PaddleForce` |
| `Turning Multiplier` | `1` | Times every ship's `TurnForceSailing` and `TurnForcePaddling` (not `RudderSpeed`) |
| `Damage Taken Multiplier` | `1` | Times every ship's `DamageTaken` |
| `Build Cost Multiplier` | `1` | Times every ship's `BuildCost` |

Ship settings not listed here are used as written.

## The ships

Keys in the `Ship` section are `<Prefab>.<Setting>`. They appear the first time a world loads (on a dedicated server,
when it loads its world), each set to the ship's vanilla value, so a fresh file changes nothing.

| Ship | Prefab |
| --- | --- |
| Raft | `Raft` |
| Karve | `Karve` |
| Longship | `VikingShip` |
| Drakkar | `VikingShip_Ashlands` |
| none (in the game's files, not in the vanilla build menu) | `Trailership` |

Ships from other mods get the same settings under their own prefab names. One added after the world started gets
them the first time one appears in the world.

A change applies at once to new ships and every loaded ship; ships elsewhere get it when they load.

## Per-ship settings

| Setting | Effect |
| --- | --- |
| `Health` | Maximum health (see below) |
| `SailForce` | Thrust from the sail: top speed under sail, scaled by wind and sail size |
| `SailForceOffset` | Height in metres above the ship's centre of mass where the sail pushes. Lower heels less and capsizes less at high sail force; `0` removes heeling from the sail; negative pushes below the centre of mass. Lower it when you raise `SailForce` a lot |
| `PaddleForce` | Force when paddling forward (slow) and backward |
| `RudderSpeed` | How fast the rudder swings to full lock |
| `TurnForceSailing` | Turning force under sail; grows with forward speed |
| `TurnForcePaddling` | Turning force while paddling |
| `ForwardDrag` | Forward water drag; caps the top speed. Lower is faster and coasts longer |
| `SidewaysDrag` | Sideways water drag. Lower lets the ship slide in a crosswind. Tune with care |
| `AngularDamping` | Resistance to rolling and pitching. Higher is a steadier deck; too high and the ship stops turning. Tune with care |
| `WaterImpactDamage` | Damage from slamming into rough seas with players aboard. `0` turns it off |
| `UpsideDownDamage` | Damage per second while capsized. `0` turns it off |
| `WeatherWear` | `true`: loses health in rain and while the hull is under water, down to half health |
| `AshlandsOceanDamage` | `true`: the burning Ashlands ocean damages the ship. `false`: it sails there unharmed (see the warning below) |
| `DamageTaken` | Default `1`. Multiplier on every hit: attacks, collisions, rough seas, capsizing, the Ashlands ocean. `0.5` halves it; it also scales `WaterImpactDamage` and `UpsideDownDamage` |
| `Invulnerable` | Default `false`. `true`: the ship takes no damage at all |
| `BuildCost` | Default `1`. Multiplier on every building material. Only on ships with a building recipe |

- **Health.** New ships get the new maximum. A loaded ship keeps its current health: raising the maximum makes it
  look more damaged until repaired (a repair fills it to the new maximum); lowering it takes no health away (the ship
  stays above the maximum until it takes damage). At world level above 0, ships also get the game's extra building
  health, as in vanilla.
- **No damage.** `Invulnerable = true`, or a `DamageTaken` that is 0 after the multiplier, blocks all damage: hits,
  weather wear and the Ashlands ocean. Ships cannot be dismantled, so an invulnerable ship can only be destroyed after
  you turn this off.
- **Ashlands warning.** `AshlandsOceanDamage = false` on any ship but the Drakkar removes the progression gate that
  makes the Drakkar necessary, along with the Ashlands ocean effects and the world key that ocean damage triggers.
- **Build cost.** Each material is its vanilla amount times the cost, rounded to the nearest whole number and never
  below 1 (so `0` makes every material cost 1). It is always worked out from the vanilla amounts, so changing it again
  never compounds. The build menu shows it at once.

## Vanilla values

Read from the game when the world loads; as of this writing:

| Setting | Raft | Karve | Longship | Drakkar | Trailership |
| --- | --- | --- | --- | --- | --- |
| `Health` | 300 | 500 | 1000 | 3000 | 1000 |
| `SailForce` | 0.05 | 0.03 | 0.05 | 0.085 | 0 |
| `SailForceOffset` | 0.5 | 1 | 2 | 2 | 0 |
| `PaddleForce` | 0.5 | 0.2 | 0.2 | 0.25 | 0.5 |
| `RudderSpeed` | 1 | 1 | 1 | 0.5 | 0.5 |
| `TurnForceSailing` | 0.2 | 0.18 | 0.8 | 1.05 | 0.5 |
| `TurnForcePaddling` | 0.3 | 0.2 | 1 | 1.9 | 1.5 |
| `ForwardDrag` | 0.005 | 0.001 | 0.001 | 0.002 | 0.005 |
| `SidewaysDrag` | 0.1 | 0.15 | 0.15 | 0.5 | 0.05 |
| `AngularDamping` | 0.05 | 0.05 | 0.3 | 0.95 | 0.1 |
| `WaterImpactDamage` | 10 | 10 | 10 | 10 | 10 |
| `UpsideDownDamage` | 20 | 20 | 20 | 20 | 20 |
| `WeatherWear` | false | false | false | false | false |
| `AshlandsOceanDamage` | true | true | true | false | true |

The file keeps what it holds: if a game update changes a ship's vanilla values, your file still applies the old ones.
To take the new values, stop the game or server and delete that ship's lines (or the whole file); the next world load
writes them fresh.
