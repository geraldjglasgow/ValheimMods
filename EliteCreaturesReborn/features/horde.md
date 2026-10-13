# Elite Creatures Reborn - specification: the Horde

Status: built, not tested in game.

More creatures in the wild, each weaker: a different kind of hard, crowds instead of a few very strong creatures, and
more stars and mutations in every fight because each creature rolls its own. Requested on Discord on 2026-10-12 ("More
Monsters, Lower Stats"). The user chose its home (ECR), its shape (an on/off and three multipliers in the .cfg, not part
of the rule file's `difficulty:`) and the loot rule (1 / the spawn multiplier).

---

# 1. What it does

While `Horde` is on:

- **More creatures.** The world's own spawning (the game's `SpawnSystem`, outside its random events) makes each group
  the spawn multiplier larger and allows that many more of each kind around the players: the group size range and the
  most of the kind, times the multiplier, each rounded up or down at random so 1.5 averages one and a half. How often
  the world spawns is unchanged.
- **Each one weaker.** A wild creature (section 2) has the health multiplier on its maximum health (after its stars,
  mutations and aspects) and the attack multiplier on the damage it deals (with its stars and mutations, before any
  aspect).
- **No extra loot.** A wild creature drops 1 / the spawn multiplier of its own loot (half at 2): the rows of its own
  drop table and those its `creatures:` entry names, trophies only when the trophy switch says so, rounded at random
  as the raiders' drops are. Rows another mod adds are left alone.

Left as they are: bosses, the game's raids (event spawns), this mod's raids and arena, creature spawners (camps,
dungeons, nests), creatures spawned by command, and tamed creatures (a tame is the player's own and keeps its
strength).

# 2. Wild creatures

Every creature the world's own spawning makes is tagged wild in its ZDO (`ecr_wild`), horde on or off, and a wild
Splintering creature's copies are wild too. The horde weakens tagged creatures only while it is on, so turning it on
reaches the wild creatures already out there, and turning it off gives them back their strength. Creatures spawned
before this version carry no tag and are never weakened. Health follows a change the next time the creature loads;
attack and loot follow at once.

# 3. Multiplayer

- The spawning runs on the client that owns each zone with a player in it; the groups are grown there, from the
  synced settings, and the tag is written there in the frame the creature wakes, so it replicates with the creature.
- Health is set on the creature's owner (`s_maxHealth`, replicated); the attack multiplier is applied on the victim's
  owner as the hit lands; the loot on the dying creature's owner. All three read the tag from the ZDO and the synced
  settings, so every machine agrees.
- The four settings are Charter clauses: while the server binds its players (the rule file's `lock to server`), the
  server's values hold for everyone.

# 4. Configuration

`13 - Horde` in the .cfg, synced and locked as above:

| Setting | Default | Range | What |
| --- | --- | --- | --- |
| `Horde` | false | | the switch |
| `Spawn Multiplier` | 2 | 1 to 3 | group sizes and the most of each kind |
| `Creature Health Multiplier` | 0.65 | 0.1 to 2 | a wild creature's health |
| `Creature Attack Multiplier` | 0.75 | 0.1 to 2 | the damage a wild creature deals |

The cap of 3 is for frame rate: the zone owner runs every extra creature's AI.

# 5. Code

`Horde/`: `WildTag` (the ZDO tag), `WildSpawn` (tags creatures waking inside a wild spawn), `HordeSpawnPatch` (the
game's `UpdateSpawnList` and `Spawn`), `HordeGroups` (grows a spawn list for one pass and restores it), `HordeFactors`
(the three multipliers for one creature), `HordeLoot`. Shared loot scaling with the raids: `Loot/OwnDrops`. Wired in at
`Scaling/StatApplier` (health), `Patches/DamageScalingPatch` (attack), `Patches/LootPatch` (loot),
`Patches/CharacterLifecyclePatch` (tag on waking) and `Patches/DeathPatch` (Splintering copies).

# 6. Build checklist

- [ ] Wild spawns come in larger groups with the horde on, at 1.5 and at 2; event spawns do not
- [ ] A wild creature has the health and attack multipliers; a camp spawner's, a tamed and a commanded one do not
- [ ] Wild loot is 1 / the spawn multiplier; raiders' loot unchanged by the move to `OwnDrops`
- [ ] Turning the horde off restores health on the next load, attack and loot at once
- [ ] A dedicated server's values hold for a bound client

| Date | What moved |
| --- | --- |
| 2026-10-12 | Specified and built from the Discord request and the user's choices; builds, not run in game |
