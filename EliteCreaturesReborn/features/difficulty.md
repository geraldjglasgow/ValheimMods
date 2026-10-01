# Elite Creatures Reborn - specification: Difficulty presets

Designed with the user 2026-09-30 to 2026-10-01 after a player found the Queen's Mistlands and the Ashlands
unplayable on the default rules (the newest biome ~95% starred and nearly all mutated by then). Every number below was
signed off by the user; the player view is in `../CLAUDE.md`, "Difficulty". Code: `Rules/Difficulty.cs`,
`Rules/PresetTables.cs`, `Rules/PresetCell.cs`, `Traits/PresetRoller.cs`. Built for 3.14.0.

## 1. The setting

`difficulty:` at the top of `creature_rules.yml`: `Easy`, `Medium`, `Hard`, `Very Hard`, `Extreme` or `Custom` (case,
spaces, `_` and `-` ignored). No line = Custom, so an existing server keeps playing as before; a new file is written
with Medium. Custom is the rule file's own per-biome `star chances` and `mutation chance(s)` with the `world tiers`
boost lines. An unknown word is an error (the file is not adopted, like any bad value).

Terms: world tier t = bosses defeated (`world tiers:`, clamped to 0-7 for these tables). Biome rank b: Meadows 0,
Black Forest 1, Swamp 2, Mountain 3, Plains 4, Mistlands 5, Ashlands 6, Deep North 7; the Ocean has no rank; a biome
outside the list counts as the biome being entered (b = t). behind = past = max(0, t - b), 0 on the Ocean.

## 2. Caps

| Preset | Ranked biome | Ocean by tier 0-7 |
|---|---|---|
| Easy | min(4, 1 + behind) | 2 |
| Medium | min(5, 2 + behind) | 2,2,2,2,3,3,3,3 |
| Hard | min(5, 3 + behind) | 3,3,3,3,4,4,4,4 |
| Very Hard | min(5, 4 + behind) | 4,4,4,4,5,5,5,5 |
| Extreme | b <= t: 5,5,6,6,7,7,8,8 by tier | b > t and Ocean: that minus one, never below 5 |

## 3. Star odds

Shared rows by cap (weight of 0, 1, 2 ... stars): 1 [90,10]; 2 [85,12,3]; 3 [75,15,7,3]; 4 [65,17,10,5,3];
5 [55,18,12,8,5,2]; 6 [45,18,13,10,7,5,2]; 7 [35,18,14,11,9,7,4,2]; 8 [28,17,14,12,10,8,6,3,2].
Each star count's weight is multiplied by lean^stars, lean = preset lean x (1 + 0.05 x past):

- Easy 0.9, Medium 1, Hard 1.25, Very Hard 1.1 x (1 + 0.15 t), Extreme 1.3 x (1 + 0.05 t).
- Extreme, biomes reached (b <= t) at tiers 1-3: 1.53833, 1.44709, 1.63712, solved so the biome being entered climbs
  60.1, 70.3, 78.5, 85.0 to 90.3% starred at tier 4 (each rise four-fifths of the last) instead of stepping with the
  5,5,6,6 caps. Biomes not reached and the Ocean keep 1.3 x (1 + 0.05 t).

## 4. Mutations

Rate m for a creature with no stars; a creature with s stars: min(100, m x (1 + 0.25 s)). Ranked biome:
m = ceiling - (ceiling - start) x 0.8^past, start = base + per tier x t.

| Preset | Base | Per tier | Ceiling | Ocean |
|---|---|---|---|---|
| Easy | 10 | 0 | 35 | 10 |
| Medium | 25 | 0 | 55 | 25 below tier 4, then 30 |
| Hard | 30 | 0 | 65 | 30 below tier 4, then 35 |
| Extreme | 45 | 7 | 98 | its start |
| Very Hard | the mean of Hard's and Extreme's rate in every cell, Ocean included | | | |

A mutated creature takes exactly one mutation, drawn by weight from every enabled mutation's chance at its star count
in its biome's rules (its `creatures:` entry on top). No weight anywhere = plain. `max mutations` and the boost lines
are Custom only.

## 5. Checks the numbers pass

1. Each preset beats the one below it in every cell: starred %, average stars and mutated % strictly; the cap never lower.
2. In every preset, a biome cleared earlier beats every later biome at the same tier; biomes not reached never beat the
   one being entered.
3. Every biome's starred % rises tier over tier on Extreme.

The design script (`presets_data.py` in the 2026-10-01 session scratchpad) asserted 1 and 2; the C# port was compared
cell by cell against it (5 presets x 8 tiers x 9 biomes, 0 mismatches) with an offline harness calling `PresetCell.For`.

## 6. Star power for 6-8 stars (Extreme only)

The built-in lines have nine entries; 6-8 continue each line by its 5th star's step, the speeds by +0.03: growth
0.35, 0.40, 0.45; hp 4.7, 5.4, 6.1; attack 2.9, 3.3, 3.7; swing speed 1.19, 1.22, 1.25; speed 1.18, 1.21, 1.24; drops
3.5, 4, 4.5. On Extreme a file's shorter line is padded by the built-in steps on top of its own last value
(`StarPower.PadFrom`); other difficulties never roll above 5 and keep "past the end the last entry repeats".

## 7. Multiplayer

The roll is the creature owner's, as before (`EliteController.RollFresh`), from the synced rule file and the world's
global keys, then stored in the ZDO; nothing new is sent. `elite tier` reads the same rules on the asking machine.
