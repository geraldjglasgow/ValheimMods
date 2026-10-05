# Rime Giant

A very rare forest troll of the Mountains, 1.4 times the size and armoured in ice plates. By day it sleeps on the
slope like a snowy outcrop. Weapons barely hurt it until fire breaks its plates.

## Where

- **At most one per mountain, ever.** 60% of mountains (larger than about 150 m across) hold one, fixed by the world
  seed. Once it has come, that mountain never gets another.
- Roaming such a mountain, you will likely meet it within the first few zones. It comes alone and asleep, 40-80 m
  away.

## Asleep and awake

- **Asleep:** no name, health bar or hover text. Walking past does not wake it.
- **Wakes** at night, in a snowstorm, or when hit. **Sleeps again** by day once it has had nothing to fight for 20
  seconds.
- Tip: found asleep by day, a party can prepare and choose when to start.

## The ice plates

It wears 8 plates. They block most **physical** damage (blunt, slash, pierce, chop, pickaxe). Fire, frost,
lightning, poison and spirit go through in full.

| Plates left | 8 | 7 | 6 | 5 | 4 | 3 | 2 | 1 | 0 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Physical damage that gets through | 15% | 26% | 36% | 47% | 58% | 68% | 79% | 89% | 100% |

- **Fire breaks plates:** one per 30 fire damage, burning included; a fire arrow breaks about one. Torches and fire
  staffs work too.
- **Campfire trap:** within 2 m of a fire it loses a plate every 3 seconds. Trolls do not avoid fire.
- **At 40% health** all plates left shatter, and it staggers when the last one falls.
- Plates never regrow during a fight, only after it gives up, 12 seconds unhurt, and above 40% health.

## Attacks

| Attack | Damage | Tell and answer |
| --- | --- | --- |
| Sweep (punch) | 75 blunt | Huge knockback; up close it throws you. |
| Slam (two fists) | 60 blunt | From up to 12 m, at most every 12 s. Sends an avalanche. |
| Avalanche | 45 blunt + 22.5 frost | Rolls about 24 m from its feet towards you, much further downhill, only a few metres uphill. Stops at water. Dodge it, or fight from **above** the giant or along the ridge. |
| Ice boulder (throw) | 20 blunt + 40 frost | From up to 22 m; hits everyone within 3.5 m of where it lands. Frost chills and slows. |

## Stats and loot

- 1500 health (a forest troll: 600). Troll resistances, **immune to frost**, **weak to fire**.
- Wolves, drakes, golems and fenrings leave it alone.
- **Loot:** 4-7 Crystal, 3-5 Freeze gland, 3-5 Silver ore. No troll hide.
- **With EliteCrafting:** every kill also 3 runes and 1 magic item, and half the time a Consecrated Rune.

## Settings: `4 - Rime Giant`

| Key | Default | Meaning |
| --- | --- | --- |
| Enabled | true | Giants can appear |
| Mountains | 60 | Percent of mountains that hold a giant |
| Chance | 25 | Percent chance per zone roll on such a mountain |
| Interval | 3600 | Seconds between a zone's rolls |
| Biomes | Mountain | Biomes that count as mountains |
| Health | 1500 | Its health |
| Plates | 8 | Ice plates it wears (0-8) |
| Armoured Damage | 15 | Percent of physical damage that gets through all plates |
| Shatter At | 40 | Health percent at which all plates break |
| Fire Per Plate | 30 | Fire damage that breaks one plate |
| Regrow Delay | 12 | Seconds unhurt, out of a fight, before plates regrow |
| Regrow Interval | 5 | Seconds per plate as they regrow |
| Sweep Damage | 75 | Blunt damage of its punch |
| Slam Damage | 60 | Blunt damage of its slam |
| Avalanche Damage | 45 | Blunt damage of the avalanche, plus half as much frost |
| Avalanche Length | 24 | Metres the avalanche rolls on flat ground; 0 turns it off |
| Boulder Damage | 40 | Frost damage of the ice boulder, plus half as much blunt |
