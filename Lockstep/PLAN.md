# Lockstep plan

The next boss stays sealed until every player in the group has defeated the previous one. A player who races
ahead can gather, build and explore, but cannot summon the Elder until everyone has beaten Eikthyr. This document
is the design and the roadmap.

## The vanilla progression path

Valheim progression is a chain of bosses. Each boss drops the item that unlocks the next biome, and the game records
each kill as a **global key** on the world. The keys are what the game (and other mods) use to decide what has
happened, so they are also what Lockstep has to intercept.

| # | Biome | Boss | Global key | Summoned with | What the kill unlocks |
| --- | --- | --- | --- | --- | --- |
| 1 | Meadows | Eikthyr | `defeated_eikthyr` | 2 Deer Trophies at the Meadows altar | Hard Antler pickaxe: tin and copper, so bronze and the Black Forest |
| 2 | Black Forest | The Elder | `defeated_gdking` | 3 Ancient Seeds at the Black Forest altar | Swamp Key: sunken crypts, so iron and the Swamp |
| 3 | Swamp | Bonemass | `defeated_bonemass` | 10 Withered Bones at the Swamp altar | Wishbone: finds silver, so the Mountains |
| 4 | Mountains | Moder | `defeated_dragon` | 3 Dragon Eggs at the Mountain altar | Dragon Tears: artisan table, black metal smelting, so the Plains |
| 5 | Plains | Yagluth | `defeated_goblinking` | 5 Fuling Totems at the Plains altar | Torn Spirit: Mistlands gear, Sealbreaker fragments |
| 6 | Mistlands | The Queen | `defeated_queen` | Sealbreaker opens the Infested Citadel door; the Queen is inside | Queen drop: Ashlands travel and gear |
| 7 | Ashlands | Fader | `defeated_fader` | Bell fragments placed at the altar inside a fortress | Deep North (in development) |

Other keys the game sets that matter to progression but are not bosses: `KilledTroll`, `KilledBear` and similar
creature keys (raids and events), and `defeated_hive` style keys for mini bosses. Lockstep only gates the boss
chain; raids stay vanilla.

Things the chain does **not** stop a fast player from doing: exploring every biome, dying there, dragging enemies
back, or bringing back materials for the group. That is fine. The point is that boss kills, boss powers and the
unlocks they carry are earned by the group.

### How the game records a kill (verified in the decompiled assembly)

- `Character.CheckDeath` runs only on the client that **owns** the boss ZDO, and calls `Character.OnDeath` there.
- `OnDeath` calls `ZoneSystem.SetGlobalKey(m_defeatSetGlobalKey)`, which is a routed RPC to the server. The server
  adds the key to the world and broadcasts the full key list. Nothing records **who** killed it.
- `OnDeath` also queues the key as a per-player "unique key" (`Player.m_addUniqueKeyQueue`), but because it runs
  on the owner only, just one player gets it, and it is saved in that character file, not on the server. Not
  usable for group credit.
- Every player who damages a boss is written into the boss ZDO: `Character.RPC_Damage` sets a bool under
  `ZDOVars.s_attackers + playerName` and increments the `Attackers` count. `OnDeath` then walks
  `ZNet.instance.GetPlayerList()` and sends `RPC_RegisterKill` to each peer whose name is in that list, for the
  stats screen. **This is the credit source**: the boss itself knows who fought it.
- Only five boss keys are in the `GlobalKeys` enum (`defeated_eikthyr`, `defeated_gdking`, `defeated_bonemass`,
  `defeated_dragon`, `defeated_goblinking`). The Queen and Fader keys live in prefab data, which is inside
  compressed asset bundles and cannot be grepped from disk. The community-documented strings are
  `defeated_queen` and `defeated_fader`; confirm with the `listkeys` console command after a kill in the test
  profile before 0.5. The YAML chain makes fixing a wrong key a config edit.

### How the game summons a boss (verified)

All bosses go through `OfferingBowl`, in two flavours selected by `m_useItemStands`:

- **Offering item** (Eikthyr, Elder, Bonemass, Moder, Yagluth): `OfferingBowl.UseItem` with the item in hand,
  checks the count, then `InitiateSpawnBoss`.
- **Item stands** (the bell altar for Fader, and presumably the Queen): `OfferingBowl.Interact` checks that every
  linked `ItemStand` has an attachment, then `InitiateSpawnBoss`.

Both paths send `RPC_SpawnBoss` to the altar owner, which runs `CanSpawnBoss`, then `SpawnBoss`, then a delayed
`DelayedSpawnBoss` that instantiates `m_bossPrefab`. The altar may also set its own key through `m_setGlobalKey`.
The Sealbreaker `Door` in the Infested Citadel is a separate `Door` with `m_keyItem`; it is not the summon and does
not need gating, since the altar behind it is.

## Design

### The rule

A boss at stage N can be summoned only if every **counted** player has credit for stage N-1. Stage 1 (Eikthyr) is
always open. The rule is checked on the server, and the result is synced to clients so the altar can refuse with a
readable message: "The Elder will not answer. Waiting for: Bjorn, Sigrid (Eikthyr)."

### Who is counted

"Everyone" has to be defined, otherwise one player who joined once and never returned blocks the server forever.

- **Roster, automatic.** Every player the server has seen is added on login, keyed by player ID (the number in
  the character save, stable across renames and name collisions), with name and last-seen time. Nobody maintains
  it by hand.
- **Ignore list, manual.** Players who never count, matched by player ID or by name so an admin can type a name.
  Ignored players still play and still earn credit; they just hold nobody back. Edited two ways that stay in
  sync: the roster file on the server (hot reload; the roster itself is never sent to clients) or admin console
  commands `lockstep ignore <player>` and `lockstep unignore <player>`.
- **Inactivity, automatic.** A player drops out of the count after `Inactive Days` without logging in (default
  14, 0 disables). Covers the player who drifted away without anyone thinking to ignore them.
- **Forget, for cleanup.** `lockstep forget <player>` deletes the roster entry. A returning player is re-added
  fresh on login.
- **Late joiners.** Config switch `Late Joiners`: `catchup` (default) credits a new player with every stage the
  group has already cleared, `earn` makes them fight each boss. Without catch-up, inviting a new friend freezes
  the server at the next boss.
- Optional mode: count only players who are online right now. Simpler, but lets a group summon while a member is
  at work. Off by default.

The roster is one YAML file in the server's `BepInEx/config` folder, one entry per player: id, name, last seen, ignored flag,
cleared stages. Admins can read and edit it directly, hot reload applies edits, and the console commands are
conveniences over the same file. This replaces the separate JSON credit table described below.

### How credit is given

- Primary rule, **attackers**: a prefix on `Character.OnDeath` (owner client, boss with a defeat key; a prefix
  because OnDeath destroys the object at its end, see `Client/KillReporter.cs`) reads the `Attackers` names from
  the boss ZDO and sends them with the key to the server in one custom RPC. The server maps
  names to player IDs through the online player list and credits them. A player who hit the boss once and then
  died and respawned still has their name in the ZDO, so death does not lose credit.
- Secondary rule, **radius**: also credit online players within `Credit Radius` of the boss (default 200 m) who
  never landed a hit, such as a dedicated healer or the one who kited adds. Config toggle, on by default.
- Config toggle: credit everyone online at kill time, regardless of distance. For groups who trust each other.
- Player identity: the name in the ZDO is the display name, so the server resolves it to the persistent
  `playerID` read from the player ZDO (`ZDOVars.s_playerID`) and stores that. Two online players with the same
  name is a real edge case; both get credit.
- Admin console commands: `lockstep grant <player> <boss>`, `lockstep revoke <player> <boss>`,
  `lockstep status`. Grant is the escape hatch for every edge case.
- Existing worlds: on first run, if a world key is already set, credit every player already on the roster, so
  installing the mod mid-playthrough does not lock a group out of content it has already cleared.

### How the gate is enforced

1. Prefix `OfferingBowl.UseItem` and `OfferingBowl.Interact`: if `m_bossPrefab` belongs to a gated stage and the
   server-synced state says the stage is closed, show the waiting message and return false. Both altar flavours
   are covered, the offering item is not consumed and item stands are untouched. This is the visible gate.
2. Prefix `OfferingBowl.RPC_SpawnBoss` on the altar owner as a safety net: another client with a stale state, or
   a mod calling the altar directly, still cannot spawn. Log when the net catches anything. A boss spawned with
   the `spawn` console command bypasses the altar entirely; that is an admin action and is left alone.
3. Stages are matched by the altar's `m_bossPrefab` name, not the altar prefab, so a modded altar for a vanilla
   boss is still gated.
4. Do **not** touch global keys. When the boss dies, the vanilla key is set as usual, so other mods that read
   keys (world level in EliteCreaturesReborn, raids, vendor stock) behave normally. Lockstep only decides whether the
   fight can start.

### Sync and storage

- Server state: the roster YAML `BepInEx/config/Lockstep.<world>.roster.yml` on the server, one file per world
  name (copy it along when the world moves to another server). Written on every change; polled for admin edits so
  hot reload works like every other file in this workspace.
- Client state: a standing Charter article carrying only the per-stage "open / waiting for" summary (one line per
  stage), so the altar message and a status command work without a round trip. The roster stays on the server. Charter also refuses clients without the mod,
  which is required because the gate runs on the interacting client.
- The chain itself is YAML through `YamlConfig`: a list of stages with the boss key and the altar prefab names,
  defaulting to the vanilla seven. Modded bosses and reordered chains are then a config edit, not a code change.

### Mod interaction

- EliteCreaturesReborn reads global keys for world level; unaffected, keys are vanilla.
- Boss power altars (the Sacrificial Stones) only work with a trophy, which needs a kill, so they gate themselves.
- Portals and Vegvisir are not gated. A group can find the next altar early, they just cannot use it.
- Mods that add bosses register more `OfferingBowl` prefabs; add them to the YAML chain.

## Status (2026-09-12)

Milestones 0.1 to 0.5 are implemented in one pass and compile; nothing has been tested in game yet. The roster
is a plain YAML file written with YamlDotNet and watched directly, not a `YamlConfig` source, because it is
server data rather than synced config. Untested items: Queen and Fader keys, the item-stand altar path, remote
admin commands on a dedicated server, roster hot reload while players are online.

## Milestones

| Version | Deliverable | Proves |
| --- | --- | --- |
| 0.1.0 | First working version, published as OathBound: roster, credit by attackers and radius, altar gate with spawn guard, console commands, YAML chain, late-joiner catch-up, inactive days, mid-playthrough backfill (done) | Everything in one pass |
| 0.1.1 to 0.1.4 | Fixes: poll-based hot reload, rewritten YAML library, license, store page (done) | Stability on dedicated servers |
| 0.2.0 | Renamed from OathBound to Lockstep: new GUID, config, chain, roster file and command names (done) | Own identity |
| 0.3.0 | Charter swap: our own server-to-player binding replaces the third-party sync library; one-screen join check, `charter` command (done, untested in game) | Clean-room sync |
| 0.4.0 | Two-client test: one races ahead, one lags, credit through death and respawn, server restart; Queen and Fader keys confirmed with `listkeys` | Multiplayer correctness |
| 1.0.0 | Config and file formats promised stable | Release |

## Risks

- **Player identity**: player ID comes from the character file, so a player with two characters has two IDs.
  Treat each character as its own roster entry; that is what "the group" sees anyway. Document it.
- **Credit fairness**: a player who arrives late to the arena or never lands a hit. The attackers rule plus the
  radius rule plus the grant command cover it. Never make the rule stricter than the group wants.
- **Bypasses**: other mods summoning through the altar are caught by the `RPC_SpawnBoss` net; the `spawn`
  console command is not and is treated as an admin decision. Admins can turn the net off in config.
- **Queen and Fader keys** are not in the `GlobalKeys` enum and could not be read from disk. Confirm the strings
  in game with `listkeys` before 0.5; the YAML chain makes a wrong guess a config edit.
- **Game updates** can rename the ZDO attacker keys or the altar RPC. Both hooks are in one file each and log
  loudly when a member is missing, so a break shows up in the first test run.
- **Server-only worlds versus hosted worlds**: a player hosting from their client is both server and client. The
  roster file lives in that instance's `BepInEx/config` folder in both cases; the RPC path is the same.

## Open questions

- Should a player who never logs in during a boss window be auto-credited after the rest of the group wins, with a
  delay? (Softer alternative to the inactive-days timeout.)
- Should the altar message name the missing players, or just say the group is not ready? Naming is more useful,
  and this is a co-op mod, so lean towards naming.
- Gate the biome itself (damage or a warning when entering a locked biome)? Out of scope for 1.0. The boss gate
  already stops the unlocks that matter.

## Layout

```
Lockstep/
  Lockstep.cs                  plugin entry, Awake
  LockstepConfiguration.cs     synced .cfg entries
  Hooks.cs                     world start and end, players joining and leaving
  Commands.cs                  the lockstep console command, every verb sent to the server
  Chain/
    Chain.cs                    one boss stage: name, global key, boss prefab
    ChainDocument.cs            the chain YAML, one stage per top level key
  Server/
    Roster.cs                   YAML roster: player ID, name, last seen, ignored flag, cleared stages
    ProgressServer.cs           login tracking, credit, the gating rule, the summary pushed to clients
    ServerCommands.cs           server side of the console command
  Client/
    ProgressState.cs            the per-stage summary as every client sees it
    AltarGate.cs                OfferingBowl patches and message, plus the spawn RPC safety net
    KillReporter.cs             Character.OnDeath prefix reporting the attackers to the server
```
