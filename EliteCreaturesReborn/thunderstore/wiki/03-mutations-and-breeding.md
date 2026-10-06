# Mutations and Breeding

A mutated creature shows the mutation in its name ("Mad Greydwarf") and its stars in the mutation's colour. Bosses
never mutate (see [Boss Aspects](wiki:Boss Aspects)).

## The nineteen

Settings are under `mutation power:` in `creature_rules.yml`. **E** marks those that `large star power` boosts on
creatures with five or more stars.

| Mutation | Colour | What it does | Settings (defaults) |
| --- | --- | --- | --- |
| Mad | Red `#E23030` | Much faster, with half health; never on bats. | `move` 1.6 E, `attack speed` 1.5 E, `health` 0.5 |
| Bloated | Brown `#8B5A2B` | Double health; explodes 1.5 s after death, dealing blunt damage times (1 + stars). | `health` 2 E, `delay` 1.5, `damage` 40 E, `radius` 4 m E |
| Cloaked | Blue `#4AA6FF` | Invisible until you are within 10 m (15 m for trolls, lox and the Rime Giant); never on drakes, bats or deathsquitos. | `reveal distance` 10 E, `fade time` 0.5 s, `fade margin` 1 m |
| Splintering | White `#FFFFFF` | Deals less damage and splits into two weaker copies on death. | `damage` 0.6, `max generations` 0, `max descendants` 0 (0 = no limit) |
| Leeching | Green `#33CC33` | Heals from damage it deals, and regenerates after 5 s unhit. | `lifesteal` 10% E, `regen` 0.5%/s, `regen cap` 20/s, `combat cooldown` 5 s |
| Warding | Navy `#101C50` | Reflects part of each hit back at you (at most 7.5% of your max health per second) and knocks you back. | `reflect` 30% E, `max reflect` 7.5, `knockback` 4 E |
| Plated | Yellow `#F2C40C` | Tough at full health, hits harder as it is hurt. | `armour` 40% E, `max reduction` 55%, `damage` +60% E |
| Miasmic | Dark green `#1E6B2E` | Leaves poison clouds that hurt players, never creatures. | `cloud life` 6 s, `cloud damage` 5 E, `clouds per second` 1 E, `cloud radius` 4 m |
| Devouring | Dark red `#7A0F0F` | Eats weaker creatures to grow, then hunts players. | see below |
| Thieving | Violet `#A64BE0` | Each melee hit steals one unequipped item (never the Wishbone); kill it to get everything back. | `max items` 1 E (at least one per star, at most 8) |
| Gilded | Pale gold `#FFE066` | Never attacks, runs from players; pays 3x loot plus coins. The rarest. | `loot` 3 E, `bonus item` Coins, `bonus amount` 20 per star+1 E (max 100), `flee distance` 30 m |
| Blinking | Cyan `#29E0E0` | Every 30 s of a fight, teleports behind its target after a flash and chime; less health. | `health` 0.75, `every` 30 s (0 = off), `distance` 4 m, `tell time` 0.5 s |
| Relentless | Orange `#FF7F24` | Chases you up to 150 m, seen or not; never faster than normal. | `chase distance` 150 m (0 = off) |
| Juggernaut | Grey `#6E6E6E` | Never staggers or gets knocked back. | none |
| Screecher | Pink `#FF5CC8` | A heavy hit makes it shriek, deafening nearby players and blocking Elemental and Blood Magic. | `threshold` 15% of its health, `radius` 20 m E, `mute time` 4 s E, `cooldown` 15 s |
| Frostbound | Pale ice `#BFEFFF` | Players within 6 m regain stamina half as fast ("Chilled"). Leaves slick ice where it walks: on it you slide and move 15% slower. Frost heals it instead of hurting it, and never slows it. | `aura radius` 6 m E, `stamina regen` 50% less E, `frost heal` 100% E, `trail life` 10 s (0 = no trail), `patch radius` 1.5 m, `patch spacing` 1.5 m, `grip` 5% (100 = no slide), `slow` 15% E |
| Mudbound | Mud `#5E4A1E` | Leaves thick mud where it walks: 40% slower in it ("Deep mud") and for a second after. | `trail life` 10 s (0 = no trail), `patch radius` 1.5 m, `patch spacing` 1.5 m, `slow` 40% E |
| Corrodent | Rust `#B7410E` | Your armour wears three times as fast under its hits, melee or ranged; a line names the piece and what is left. Shields wear as usual. | `durability` 3 E (1 = normal, 0 = no wear) |
| Cloning | Lavender `#B4A8FF` | Once in its life, fighting you within 12 m, it leaves a harmless decoy in its place and fights on unseen. Its first blow that lands (a block counts, a dodge does not) shows it and the decoy vanishes; killing the decoy, or 20 s, shows it too. | `times` 1 E (0 = off), `range` 12 m, `decoy life` 20 s (0 = no limit), `cooldown` 30 s (between tricks) |

Ice and mud patches slow players only, never creatures, and always show whatever your effect density.

Effect and sound settings (`blast effect`, `cloud effect`, `tell sound`, `trail effect` and the like) name game
prefabs; `elite effects <text>` lists them.

**Devouring.** Ignores players while feeding (unless hit) and kills other creatures in one bite, keeping part of their
health and damage. It eats only creatures with no more health than itself, never bosses or large creatures, one per
star (at least one), then acts normally. Once its gained damage passes a third of the health of the toughest player nearby, it hunts players.
Its meals show on its nameplate. Settings: `absorb health` 50% E, `absorb damage` 25% E, `slow per 100 health` 2%,
`move` 1, `player threshold` 0.333, `devour cooldown` 60 s, `max prey health` 100% (0 = no limit), `min meals` 1.

## Where they appear

Any biome can roll any mutation, but biomes lean: Black Forest Thieving; Swamp Miasmic, Leeching; Mountain Plated;
Plains Mad, Thieving, Devouring; Mistlands Cloaked, Thieving, Devouring; Ashlands Bloated, Splintering; Deep North
Plated.

**Large creatures** (trolls, bears, lox, golems, Fuling berserkers, Seeker soldiers and the like) never get Gilded or
Relentless and are never eaten.

## Tamed creatures

Tamed creatures keep their stars and mutation. Differences once tamed:

| Mutation | Tamed |
| --- | --- |
| Thieving | Never steals |
| Gilded | Does not flee; normal loot |
| Blinking | Blinks behind enemies only, never while ridden |
| Relentless | Hunts enemies up to 150 m; cannot be recalled mid-hunt |
| Splintering | Splits into tamed copies that follow you |
| Screecher | Deafens no one |
| Frostbound | Chills no one and lays no ice; frost still heals it |
| Mudbound | Lays no mud |
| Cloning | Never hides behind a decoy |

Miasmic poison and Bloated blasts still hurt players.

## Breeding

- A newborn gets 0 stars up to the stronger parent's, all equally likely.
- If either parent has a mutation, the newborn gets one of the parents' mutations.
- Two plain parents always have plain young.
- Eggs show what they will hatch, and young keep their traits when grown.
- With GrindstoneSkills, Husbandry's "better offspring" can add one star above the stronger parent.

```yaml
breeding:
  enabled: true        # false: newborns roll like wild creatures
  mutation chance: 100 # % chance to inherit a parent's mutation
```

## Settings

- `mutations enabled:` - set a mutation to `false` to turn it off everywhere.
- `mutation chance` / `mutation chances` - how often each rolls (see [Settings and Commands](wiki:Settings and Commands)).
- `large star power` (in `defaults:`, 1) - multiplier for the E settings on five-star-plus creatures.

Each player can change the star colours in their own settings.
