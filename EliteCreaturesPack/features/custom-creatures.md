# Elite Creatures Pack - specification: Custom creatures

One feature of the mod, specified on its own. The other feature files sit beside it.

This file covers custom creatures: server admins define new creatures and human enemies in YAML, each one built as a
copy of a creature that already exists, with its own stats, behaviour, attacks, gear, drops and look. No new models: a
custom creature wears its base creature's body (or the player's body for a human). Settled with the user on 2026-10-09.

**Scope: the MVP, nothing more for now (the user, 2026-10-09).** This feature brings over everything another mod did
for creatures and humans. The one addition is that a custom creature can use Elite Creatures Reborn's mutations and boss aspects (section 8). Other ideas wait until the user asks for them.

**Status: built, never run in game.** Every part of sections 2 to 8 is built and compiles: the foundation (switch, files,
definition model, build pipeline, saves, `ecp` command), every step (character, combat, look, humans, the ECR link), the
export command and the ready-made creatures; reviewed across the seams and read back offline through the real reader. Not
one of them has been seen in a game yet. See the build checklist at the end.

**Clean room.** This file is written in our own words from a list of what the other mod does for the player. Build it
from this file and the game's code only. Do not open the other mod's DLL outside of this project, decompiled sources, config files or documentation, and take no names, keys, numbers or texts from it. A behaviour this file leaves open is decided with the user.

---

# 1. The rule above all: it works without Elite Creatures Reborn

Custom creatures are complete in Elite Creatures Pack alone. Everything in sections 2 to 7 works the same whether Elite
Creatures Reborn is installed or not, and none of it reads ECR, waits for it, or depends on it.

ECR is an addition (section 8). Without ECR, a definition's ECR lines do nothing: the creature still loads and behaves as
defined, and one warning per load names the creatures whose ECR lines are inactive. Remove ECR from a server and every
custom creature keeps working.

# 2. The switch and the files

- One setting in `com.EliteCreaturesPack.cfg`: a `Custom Creatures` section with `Enabled` (on). Everything else lives in YAML.
- The definitions are YAML files read through the YamlConfig library: a main `EliteCreaturesPack.Creatures.yml` and any
  further `EliteCreaturesPack.Creatures*.yml` files in the config folder and its subfolders, merged into one set. The
  server's files are sent to every player who joins.
- A change to the files applies the next time a world is loaded, and the log says so when a file changes during play.
- A new main file holds the ready-made creatures of section 6.

# 3. Defining a creature

- Each creature has a **name**, which becomes its prefab name, and a **base**: any creature prefab that exists, from the
  game, any mod, Elite Creatures Pack itself or another custom creature.
- Every setting left out keeps the base's value. The base's own settings are never changed.
- A name that is already a prefab is refused. Prefab names are hashed into saved worlds, so renaming a definition makes
  a new creature. The old one is handled as in section 7.

**Character.** Display name, health, movement speed, faction, and whether it is a boss. A boss can name the game's
boss-fight weather and music. Per damage type: normal, weak, very weak, resistant, very resistant, immune or ignored.
Whether water, fire and smoke hurt it. Whether it staggers when its attack is blocked.

**Progress.** A world progress key set when it dies, for raids and other mods. A message shown to nearby players when it
spawns and when it dies.

**Senses and movement.** Sight range and angle, hearing range, alert range. Wandering, circling, and for flyers when to
take off and land and how high to fly.

**Behaviour.** Separate switches for: fleeing at low health, fleeing when it cannot reach its target, avoiding fire,
fearing fire, avoiding water, hunting players, and attacking buildings. Chase distance, time between attacks, and circling
the target before charging. Starting asleep, and waking from noise or from players coming close. Eating items on the
ground to heal, with a search range and a heal amount.

**Drops.** Rows added to the base's table or replacing it, each with an amount range, a chance, one per player, and
more for higher levels.

**Gear.** Items it always carries, one random pick from each of several lists, and one random set from a list of sets.

**Damage.** All its attacks' damage overridden per damage type, or scaled to a fixed total that keeps the original mix.
Every projectile attack swapped for another projectile.

**New attacks.** Built from any weapon or creature attack in the game or a mod. Each sets: animation, projectile,
cooldown, damage, the hit's reach, height and width, the AI's minimum and maximum range and attack angle, whether the AI
prefers it, what it targets, whether it can be blocked or dodged, and a status effect it applies.

**Effects.** Its hit, death, alert and idle effects replaced. An effect may spawn another creature, custom ones
included, even in a loop (A spawns B on death and B spawns A).

**Look.** Size per axis. A tint on the body and a tint on carried items. A coloured smoke or flame overlay on the whole
body.

**Texture.** An image file replacing the body texture, named in the definition and placed in the config folder. Images
are not sent over the network: each player needs the file, and a player without it sees the base's texture.

**Taming.** Tameable, born tame, follows commands when tame, breeds when tame. With the players' faction, a creature or
human fights monsters instead of players.

**Sounds.** Its alert, idle, hurt and death sounds can each be muted.

# 4. Humans

- A definition with the human base is a person on the player's body, not a creature's. Humans show hair, beards and
  skin and hair colours, which the game normally draws only on players.
- A random look per human within set ranges: gender (or random), hair style, beard (with a switch for none on women),
  hair colour and skin colour.
- Gear as for creatures. Humans use the game's weapons with the player's animations, including the player's bows.

# 5. Spawning

Custom creatures spawn the way any creature prefab does: through the game's `spawn` command, other spawner and raid mods
by prefab name, and the death effects of other creatures (section 3). This feature adds no spawn rules of its own.

# 6. Ready-made creatures

The new main file ships these definitions, each with its own on/off line. They use game bodies with size and tint, no new
textures. They should default to off.

- **A troll variant**: bigger, faster and tougher. It throws two boulders at once from range, and its ground slam hits
  harder and comes more often.
- **A greydwarf shaman variant**: bigger, faster and tougher. It fires a poisoned thorn bolt and releases a poison blast
  around itself.
- **A ghost variant**: faster and tougher, and its hit deals fire damage.
- **Hostile humans**: one set for each boss stage, each better equipped than the last.

# 7. Multiplayer, changes and errors

- Everything works on a dedicated server first. The server's files bind every player (Charter), like the rest of the config.
- A joining player receives the definitions and has every custom prefab registered before the world's creatures load, so
  nothing appears as an unknown object.
- Decisions are made on the creature's owner, and state that must survive is kept in its ZDO.
- A definition removed or renamed while its creatures are in the world: they stay in the save untouched, a warning names
  them, and they come back if the definition returns. Nothing is deleted.
- A file with a mistake is reported in the log with the file, the creature and the line. An unknown key, prefab, item,
  effect or status effect, or a number out of range, skips that creature only, and the rest load.
- Nothing may throw while the game sets up its prefabs: a failure there leaves the world stuck loading. Every build step
  is guarded and a failed creature is left out with a message.
- An admin can export any creature (game, modded or custom) as a full definition to start from, and a human definition
  to start a human from.

# 8. The addition: Elite Creatures Reborn's mutations and aspects

An addition on top of sections 2 to 7, never a requirement (section 1).

**What a definition can ask for** (ignored without ECR):

- **Mutations**: one or several of ECR's mutations by name. Mutations apply to any custom entity - creature, boss or
  human. They replace ECR's random mutation roll; stars are still rolled by ECR as usual.
- **Aspect**: a fixed ECR boss aspect, or a list it may roll from. Aspects apply to any custom entity marked as a boss,
  including a human boss. A human or creature not marked as a boss ignores this line.

Mutations and aspects were kept separate in ECR (creatures roll mutations, bosses roll aspects, never both). Custom
creatures may combine them: a custom boss can carry both an aspect and mutations, and a non-boss custom creature can
be given an aspect if the definition marks it as a boss. ECR's architecture supports this through its API; the
separation was a design choice, not a technical limit.

Custom creatures are real prefabs, so ECR's own per-creature entries in its rule file (mutation chances, mutation power,
drops) already reach them by name, with no work here.

## Ability mapping for aspects

Some aspects require specific abilities to function. Portalbound redirects projectile attacks through portals - it
needs a throw or ranged attack to redirect. Aspects that enhance or modify attacks need attacks to enhance. The
definition must map abilities when an aspect needs them:

- **Portalbound**: requires a `portal attack` field naming one or more projectile attacks the creature has. Without it
  the aspect does nothing (no error, it simply never fires). Melee attacks cannot be portaled. The Elder uses its vine
  throw, Bonemass its slime ball; a custom creature names its own.
- **Summoner**: uses the creature's biome to pick what it summons, or the definition can override with `summon`:
  a list of prefab names and optional star counts.
- **Echoing**: replays attacks the boss performed. A creature with no attacks produces a ghost that walks but never
  strikes.
- **Nightfall**, **Stormbound**, **Gravitic**, **Brutal**, **Colossal**: work on any creature with no mapping needed.
  They add effects around the boss, not tied to its own attacks.
- **Other aspects** (Mending, Reflective, Shielded, Adaptive, etc.): passive modifiers that work on any creature.

A definition that gives an aspect to a creature without the abilities it needs gets a warning at load. The creature
still spawns; that aspect simply has no effect.

## What ECR needs

A public API class, read by reflection the way GrindstoneSkills' APIs are read:

- the mutation and aspect names, so Elite Creatures Pack can check a definition's names when it loads;
- registering fixed mutations per prefab at load - ECR applies them whenever that prefab spawns, from any source;
- registering fixed aspects per prefab at load, including for non-bosses when the definition asks (ECR's `IsBoss()`
  gate is bypassed for registered prefabs);
- registering ability mappings per prefab (portal attacks, summon lists) so aspects that need them can find them;
- ECR's own limits stay in force (no Gilded on large creatures, no Cloaked on deathsquitos...). A refused mutation is
  skipped with the reason in the log, as `elite spawn` already reports it.

## What Elite Creatures Pack needs

A small library in ValheimModLibs, `EliteCreaturesLink` (like `EliteCraftingLink`), with typed wrappers that do nothing
when ECR is missing or too old (its version read from the chainloader). Renaming the API's methods later needs a
matching change in both mods.

Several mutations on one creature, and aspects on non-bosses, are new ground for ECR's balance and need testing.

# 9. Not in this feature

- **Raids** go to Elite Creatures Reborn, specified separately. They can use custom creatures by prefab name.
- **New models**: a creature with a body of its own is made in `../ValheimAssets` and added as a hand-built creature,
  like the other creatures in this mod.

# 10. Test checklist

- Without ECR: every ready-made creature loads, spawns by command, fights with its attacks, drops loot and looks right,
  in single player and on a dedicated server with two players.
- A player joining late sees every custom creature correctly.
- A file change applies on the next world load. A broken entry skips only itself.
- Removing a definition leaves its creatures in the save, and restoring it brings them back.
- With ECR: fixed mutations and a custom boss's aspect apply. Uninstalling ECR leaves every creature working, with the
  single warning.

# Build checklist

`[ ]` not started, `[~]` partly (built, never run in game), `[x]` built and seen working on a dedicated server. Tick
from observed behaviour, never from the `Status:` line. Code in `EliteCreaturesPack/Custom/`.

## Foundation

- [~] `29 - Custom Creatures` / `Enabled`, synced and lockable (`CustomSettings`)
- [~] Files: main `EliteCreaturesPack.Creatures.yml` + `EliteCreaturesPack.Creatures*.yml` in the config folder and its
  subfolders, one set through YamlConfig; default written from `Custom/Ready/creatures.yml`
- [~] Definition model for every field of sections 3, 4 and 8 (`Custom/Definitions`), read and checked
  (`Custom/Files`); a mistake leaves out that creature only, logged with file, creature and line (read offline)
- [~] Build pipeline (`Custom/Build`): name refused if already a prefab, base chains (custom on custom, loops refused),
  every shell made before any step, steps per chain definition, guarded, dependents of a failed creature left out
- [~] Timing: server/host/single player build at `ZoneSystem.Start` from local files; a joining client builds from the
  files the server built from (standing Charter article, first push); file change during play logged, applies next load
- [~] Removed definitions: `ecp_custom` ZDO mark, unknown custom ZDOs parked (never destroyed), one warning per name
- [~] ECR absent: one warning per build naming creatures with elite lines (`EliteNotice`)
- [~] `ecp` console command, admin-gated by the server (`Custom/Commands`)

## Steps (one per folder)

- [~] Character, progress, senses, movement, behaviour, taming, sounds (`Custom/Nature`)
- [~] Drops, gear, damage, projectile swap, new attacks (`Custom/Combat`)
- [~] Effects (creature spawns, loops), look, texture (`Custom/Look`)
- [~] Humans: shell from `HumanBody.Build` each world, `HumanStep` lays each definition's look on it (later keys win,
  hair and beard names checked) and drops the bare kit when the chain gives gear; the body itself (`Custom/Humans`)
- [~] ECR mutations, aspects, portal attacks, summons through EliteCreaturesLink (`Custom/Elite`)
- [~] `ecp export <prefab>`, `ecp export human` (`Custom/Export`)
- [~] The ready-made creatures of section 6 (`Custom/Ready/creatures.yml`, all switched off; read offline, every name
  found among the game's prefabs)

## Docs at release

- [~] `CLAUDE.md` section (written 2026-10-10)
- [ ] README line, changelog, wiki page

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-10-10 | Foundation built (never run in game): switch, files, definition model and reader, build pipeline and timing, ZDO mark and parking, ECR notice, `ecp` command with export stub, step stubs. YamlConfig gained line numbers, error scopes, unknown keys as errors and subfolder search. | - |
| 2026-10-10 | cc-elite: the elite step (never run in game): `elite:` lines merged over the chain, checked on the creature's own pass against ECR's names and the finished creature (unknown names, aspect on a non-boss, portal attacks by item, copy origin or name and thrown only, summons; Portalbound/Echoing warnings), registered with ECR after the build (last world's registrations cleared first), ECR's answers logged at the line; the notice uses EliteLink (too old said so); soft dependency on ECR; `CreatureBuild.OriginOf`. | - |
| 2026-10-10 | cc-humans: humans fitted to the pipeline (built per world, nothing kept per process); `human:` keys left out keep the base's look (nullable `HumanLook`, overlaid per chain definition), hair/beard names checked at build, one `[r, g, b]` hair colour reads as one; bare kit dropped when the chain gives gear; AI fitting keeps a definition's angle; no-hair/no-beard marks, no random body model, never a distant object, other mods' Player parts left off. | - |
| 2026-10-10 | cc-character: the character step (`Custom/Nature`, never run in game): character, speeds, resistances, boss fight; key on death (the game's defeat key); spawn/death messages to players within 100 m (ZDO `ecp_spawn_told`); senses, movement, behaviour, eating (`EatHeal`); taming from the Wolf's values, born tame, breeding with its own `<name>_young`; muted sounds. | - |
| 2026-10-10 | cc-look: effects (hit/death/alert lists replaced; saved objects in the idle list spawned on the owner by `IdleSpawns`), size at the root (stacking along the chain, sized corpse copies), body tint and texture (shared material copies, client only; image from the config folder, missing = warning), item tint (`VisEquipment` attach patches), smoke or flame overlay (the Burning/Smoked look, local, recoloured). Never run in game. | - |
| 2026-10-10 | cc-combat: the combat step built (never run in game): drop rows (amount range inclusive), gear (picks and sets rolled as the game's random sets; a human's stand-in kit goes with any gear), creature-wide damage and projectile, new attacks; every changed item is the creature's own copy with its own shared name, put in the world's ObjectDB once built; attacks from a bone the body lacks come from the creature itself. Attack keys added: `replace` (change the attack it already has), `projectiles`, `spread`; `angle` now 1-360. | - |
| 2026-10-10 | cc-export: `ecp export <prefab>` writes any creature (game, mod, custom) as a full definition and `ecp export human` a human to start from, to `EliteCreaturesPack.Export.<name>.yml` in the config folder of the machine that ran it (never loaded); a value a definition cannot say is a comment with the reason; YAML read back through the readers offline. Custom parts traced through the build's part origins (`CustomCreature.PartOrigins`); `FieldReader.ColourOf` no longer errors on a missing colour. | - |
| 2026-10-10 | cc-ready: the ready-made creatures (never run in game), all `enabled: false`: Troll Brute (always bare-handed, two boulders per throw, slam 100 blunt every 10 s), Greydwarf Blightcaller (poison thorn bolt from the greydwarf's throw with the poison arrow's dart on the shaman's spray move; the Blob's poison cloud on its heal move), Ember Ghost (slash and fire, flame overlay, immune to fire), and eight hostile humans, one per boss stage from Meadows to the Deep North (`ECP_Outlaw` ... `ECP_FrostRaider`): that stage's armour as picks, weapon sets with shields or bow, arrows and knife, bows' own damage cut so a shot stays near the stage's melee, health 60 to 800, coins and a little of the stage's materials. Header comment rewritten. | - |
| 2026-10-10 | cc-review: the seams reviewed and fixed (never run in game): one record of a copy's origin (the build's part origins; `OwnItems`' own table and the unused `CarriedAs` gone, the elite step reads the combat step's `CarriedItems`); the human kit taken off once, by the human step (the combat step no longer empties a human's gear); gear combinations counted without overflow; `ecp export` reads back as the same creature (drop amounts one below the game's top, attack `angle` from 1, projectile/projectiles/spread on projectile attacks only, every damage type of a copy, changed set copies as `replace: true`, a status effect on some hits only and flyer/eating/command lines a creature cannot use left as notes); ready-made file and error cases read through the real reader offline; `CLAUDE.md` section, YamlConfig docs. | - |
