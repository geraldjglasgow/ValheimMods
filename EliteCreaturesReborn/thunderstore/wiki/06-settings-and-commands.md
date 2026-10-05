# Settings and Commands

## The rule file

`BepInEx/config/creature_rules.yml` holds every gameplay rule. Saved changes apply at once. If the file has a mistake,
the log (`BepInEx/LogOutput.log`) names the line and the old rules stay in force. The file is never rewritten: what it
names stays as written, and anything it leaves out uses the default. Delete it to get a new file with every default.

Percentages are 0-100. Lists by star count start at 0 stars, and the last entry repeats.

| Block | Sets | Page |
| --- | --- | --- |
| `difficulty:` | Easy to Extreme, or Custom | [Difficulty and World Tiers](wiki:Difficulty and World Tiers) |
| `creature stars:` | false: creatures keep the stars the game or another mod gave them, and still mutate (true) | |
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

A new file already makes trolls, lox and Elite Creatures Pack's Rime Giant reveal Cloaked at 15 m, keeps drakes from
being Cloaked and keeps bats from being Mad or Cloaked.

## Your own settings

`gglasgow.elitecreaturesreborn.cfg` holds each player's own display and death recap settings. The server never changes
them.

| Setting | Default | Meaning |
| --- | --- | --- |
| Show trait names | true | Mutation name in front of the creature's name |
| Coloured stars | true | Stars in the mutation's colour |
| Nameplate distance | 0 | How close you must be to see mutation names; 0 = the game's distance |
| Effect density | 1 | Strength of mutation and aspect effects, 0 to 1; 0 hides them but they still hurt. Warnings (Stormbound circles, ice and mud, tornadoes) always show |
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

## Death recap

Every death can be watched again. Dying and respawning are unchanged; two seconds after a death a line at the top left
names the killer and the key ("Killed by Mad Greydwarf 2★. F10: death recap"). F10, `/deaths` in chat or `deaths` in
the F5 console opens the recap window any time in a world, alive or dead; Esc or the key closes it.

- **Deaths**, newest first: a picture of the moment, the killer, the day and the clip's length. Kept until the game
  closes, on your machine only.
- **Killer and summary**: its mutations, aspect and stars; the damage you took, over how long, by type.
- **Video** of your own screen: the 15 seconds before the death and 2 after. Play or pause, speeds 0.25x, 0.5x, 1x and
  2x, and a timeline to drag, marked at every hit and the death; hover it for a preview. Space plays or pauses, the
  arrow keys step one frame.
- **Hits**: time, who or what, damage by type, health left. Click one to play from just before it.

Settings, in the section `10 - Death Recap (per player)`:

| Setting | Default | Meaning |
| --- | --- | --- |
| Record deaths | true | Record the video; off keeps only the hit list |
| Recap key | F10 | Opens and closes the window |
| Death notice | true | The line naming the killer after a death |
| Deaths kept | 5 | Deaths the window keeps (1 to 10) |
| Seconds before death | 15 | Video kept before a death (5 to 30) |
| Video frames per second | 15 | Smoother but more memory (5 to 30) |
| Video height | 360 | Pixels; sharper but more memory, about 10 MB per death at 360 (180 to 720) |

## Console commands

Press F5. `elite tier`, `damage` and `deaths` work for everyone; the rest need admin (your ID in the server's
`adminlist.txt`).

| Command | What it does |
| --- | --- |
| `elite spawn <prefab> <stars> [mutation or aspect...]` | Spawns a creature with exactly those stars and mutations, or a boss with that aspect |
| `elite inspect` | Shows the stars, mutations and numbers of the creature you look at |
| `elite purge [radius]` | Removes all loaded starred and mutated creatures, or only those within the radius, without drops |
| `elite effects <text>` | Lists and plays game effects and sounds matching the text |
| `elite reference` | Writes `creature_reference.yml`: every creature's prefab name, health and drops |
| `elite tier` | Shows the world tier, the difficulty and what they do where you stand |
| `damage` | Shows the last boss damage board (`/damage` in chat) |
| `deaths` | Opens the death recap window (`/deaths` in chat) |
| `charter` | Shows whether the server's settings apply to you |
| `charter diff` | Shows where the server's settings differ from your file |
| `charter versions` | Shows your and the server's mod versions |
