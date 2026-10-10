# Elite Creatures Pack - specification: Custom creatures

One feature of the mod, specified on its own. The other feature files sit beside it.

This file covers custom creatures: server admins define new creatures and human enemies in YAML, each one built as a
copy of a creature that already exists, with its own stats, behaviour, attacks, gear, drops and look. No new models: a
custom creature wears its base creature's body (or the player's body for a human). Settled with the user on 2026-10-09.

**Scope: the MVP, nothing more for now (the user, 2026-10-09).** This feature brings over everything another mod did
for creatures and humans, except its raids, which go to Elite Creatures Reborn (section 9). The one addition is that a
custom creature can use Elite Creatures Reborn's mutations and boss aspects (section 8). Other ideas wait until the user
asks for them.

**Status: specification only, nothing built.**

**Clean room.** This file is written in our own words from a list of what the other mod does for the player. Build it
from this file and the game's code only. Do not open the other mod's DLL, decompiled sources, config files or
documentation, and take no names, keys, numbers or texts from it. A behaviour this file leaves open is decided with the
user.

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
textures.

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

- **Mutations**: one or several of ECR's mutations by name. They replace ECR's random mutation roll for that creature;
  its stars are still rolled by ECR as usual.
- **Aspect** (bosses): a fixed ECR boss aspect, or a list it may roll from.

Custom creatures are real prefabs, so ECR's own per-creature entries in its rule file (mutation chances, mutation power,
drops) already reach them by name, with no work here.

**What ECR needs:** a public API class, read by reflection the way GrindstoneSkills' APIs are read:

- the mutation and aspect names, so Elite Creatures Pack can check a definition's names when it loads;
- registering fixed mutations or aspects per prefab at load. ECR then applies them whenever that prefab spawns, from any
  source, so nothing races ECR's own roll;
- ECR's own limits stay in force (no Gilded on large creatures, no Cloaked on deathsquitos...). A refused mutation is
  skipped with the reason in the log, as `elite spawn` already reports it.

**What Elite Creatures Pack needs:** a small library in ValheimModLibs, `EliteCreaturesLink` (like `EliteCraftingLink`),
with typed wrappers that do nothing when ECR is missing or too old (its version read from the chainloader). Renaming the
API's methods later needs a matching change in both mods.

Several mutations on one creature is new ground for ECR's balance and needs testing.

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
