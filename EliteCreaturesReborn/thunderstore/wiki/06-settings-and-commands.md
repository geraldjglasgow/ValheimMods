# Settings and Commands

## The rule file

`BepInEx/config/creature_rules.yml` holds every gameplay rule. Saved changes apply at once. If the file has a mistake,
the log (`BepInEx/LogOutput.log`) names the line and the old rules stay in force. Delete the file to get the defaults
back.

Percentages are 0-100. Lists by star count start at 0 stars, and the last entry repeats.

| Block | Sets | Page |
| --- | --- | --- |
| `difficulty:` | Easy to Extreme, or Custom | [Difficulty and World Tiers](wiki:Difficulty and World Tiers) |
| `lock to server:` | true: everyone uses the server's file; false: each player's own | |
| `max mutations:` | Most mutations per creature (1; 0 = no limit). Custom only | |
| `mutations enabled:` | Turn each mutation on or off | [Mutations and Breeding](wiki:Mutations and Breeding) |
| `defaults:` | Values every biome starts from | below |
| `respawning:` | Camps, dungeons and chests refilling | [Loot and Respawning](wiki:Loot and Respawning) |
| `world tiers:` | Which bosses raise the tier | [Difficulty and World Tiers](wiki:Difficulty and World Tiers) |
| `breeding:` | Inheritance | [Mutations and Breeding](wiki:Mutations and Breeding) |
| `bosses:` | Boss stars and aspects | [Boss Aspects](wiki:Boss Aspects) |
| `loot:` | Loot mode and multipliers | [Loot and Respawning](wiki:Loot and Respawning) |
| `creatures:` | Rules for one creature | below |
| `biomes:` | Per-biome changes to `defaults:` | below |

**`defaults:`** holds:

- `large star power` (1) - extra mutation strength on creatures with five or more stars.
- `star power` - the star multipliers ([Difficulty and World Tiers](wiki:Difficulty and World Tiers)).
- `mutation chance` - chance of each mutation by star count. On a difficulty it only picks which mutation.
- `mutation chances` - mutations with their own chance curve (Devouring and Gilded, the rare ones).
- `mutation power` - each mutation's numbers ([Mutations and Breeding](wiki:Mutations and Breeding)).

**`biomes:`** entries (`match: Swamp`) change only what they name. They take the same keys plus `star chances`
(Custom only). Unlisted biomes use the Meadows entry.

**`creatures:`** entries (`match:` a prefab name) take the loot keys ([Loot and Respawning](wiki:Loot and Respawning))
and the three mutation keys, for that creature only. For example, no Cloaked mosquitoes:

```yaml
creatures:
  - match: Deathsquito
    mutation chances:
      Cloaked: [0]
```

The default file already makes trolls and lox reveal Cloaked at 15 m and keeps drakes from being Cloaked.

## Your own settings

`gglasgow.elitecreaturesreborn.cfg` holds per-player display preferences. The server never changes them.

| Setting | Default | Meaning |
| --- | --- | --- |
| Show trait names | true | Mutation name in front of the creature's name |
| Coloured stars | true | Stars in the mutation's colour |
| Nameplate distance | 0 | How close you must be to see mutation names; 0 = the game's distance |
| Effect density | 1 | Strength of mutation effects, 0 to 1; 0 hides them but they still hurt |
| Small star size | 1.3 | Size of a small star |
| Large star size | 2.2 | Size of a large star (worth five) |
| Show stolen items | true | Show what a Thieving creature carries |
| Stolen item icon size | 1.6 | Size of those icons |
| Show devoured creatures | true | Show what a Devouring creature ate |
| Devoured creature icon size | 1.6 | Size of those icons |
| Boss damage board | true | Show the damage board when a boss dies |
| Boss damage board seconds | 60 | How long it stays (5 to 600) |
| Show world tier | true | With PackPanel: tier on the inventory screen |
| Show world tier under minimap | true | With PackPanel: tier under the minimap |
| Star colours | see [Mutations and Breeding](wiki:Mutations and Breeding) | One colour per mutation |
| Log diagnostics | false | Log details of Splintering, Bloated and Devouring deaths for bug reports |

## Console commands

Press F5. `elite tier` and `damage` work for everyone; the rest need admin (your ID in the server's `adminlist.txt`).

| Command | What it does |
| --- | --- |
| `elite spawn <prefab> <stars> [mutation or aspect...]` | Spawns a creature with exactly those stars and mutations, or a boss with that aspect |
| `elite inspect` | Shows the stars, mutations and numbers of the creature you look at |
| `elite purge [radius]` | Removes all loaded starred and mutated creatures, or only those within the radius, without drops |
| `elite effects <text>` | Lists and plays game effects and sounds matching the text |
| `elite reference` | Writes `creature_reference.yml`: every creature's prefab name, health and drops |
| `elite tier` | Shows the world tier, the difficulty and what they do where you stand |
| `damage` | Shows the last boss damage board (`/damage` in chat) |
| `charter` | Shows whether the server's settings apply to you |
| `charter diff` | Shows where the server's settings differ from your file |
| `charter versions` | Shows your and the server's mod versions |
