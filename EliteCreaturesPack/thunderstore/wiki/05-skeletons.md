# Skeletons

New Black Forest skeletons: seven **arsenal skeletons** with bone weapons, the **Skeleton Crossbowman**, and the
**Crypt Executioner**. Their weapons can be made: see [Bone Weapons](wiki:Bone Weapons).

## Arsenal skeletons and the crossbowman

- **Where:** whenever the game spawns a Black Forest skeleton (crypts, ruins, bone piles, the wild), it may be one of
  these instead. The plain skeleton and each switched-on arsenal skeleton are equally likely: 1 in 9 each (1 in 7
  where the skeleton could never be an archer, which leaves out the bowman and crossbowman).
- Other skeletons (Meadows, Swamps, Mountains, poison, Hildir, summoned) are never replaced.
- Each has a Black Forest skeleton's 40 health, resistances and drops, plus **one Spine one kill in ten**.

| Skeleton | Weapon | Damage | Reach | At most every |
| --- | --- | --- | --- | --- |
| Cutthroat | Bone Dagger | 10 slash + 10 pierce | 1.5 m | 2 s |
| Swordsman | Bone Sword | 25 slash | 1.8 m | 3 s |
| Axeman | Bone Axe | 27.5 slash | 1.9 m | 3 s |
| Bonebreaker | Bone Mace | 28.75 blunt | 2.2 m | 3.5 s |
| Spearman | Bone Spear | 25 pierce | 2.3 m | 3 s |
| Halberdier | Bone Atgeir, no shield | 30 pierce | 2.8 m | 4 s |
| Bowman | Bone Bow, no shield | 20 pierce | as a skeleton archer | as a skeleton archer |

- Melee ones strike one blow at a time, no combos. The others carry a shield.

**Skeleton Crossbowman:** a crossbow and nothing else.

- It stands and shoots from up to 25 m: a fast, straight bolt for 20 blunt at where you stood. Dodge, block or keep
  moving across its line.
- **Tell:** after every shot it stands still to reload (shot and reload take about 5.5 seconds). Close in then.
- **Loot:** also drops 1-2 Blunted Bone Bolts half the time.

## Crypt Executioner

A skeleton headsman, a quarter taller than the rest, with a bone greataxe. A mini boss (no boss bar).

- **Where:** in about one Black Forest burial chamber in four, in its largest room. It returns 3 game days (90
  minutes) after it dies.
- **Stats:** 900 health; weak to blunt and fire; Undead.
- **Loot:** 20-39 Coins, 4-7 Bone fragments, and a 50% chance of the **Executioner's axehead**. With EliteCrafting,
  every kill also 2 runes, and a 25% chance of an Ascension Rune; its raised skeletons drop nothing.

| Move | When | Damage | Answer |
| --- | --- | --- | --- |
| Slam | Within 3 m | 60 slash | Only the axe head hits: sidestep where it lands. Hard knockback. |
| Ground sweep | Within 4 m | 35 slash | Across its front, right to left. |
| Spin | Within 3 m | 50 slash | Stand close in to its body: only the axe head's ring hits. It staggers after. |
| Axe throw (overhand or spinning) | 7 to 22 m away | 55 slash | Dodge sideways or block. |
| Rear strike | Behind it, with two or more foes near | 55 slash | Mind its back when fighting as a group. |

- **Raised skeletons:** a thrown axe shatters and a sword-and-shield skeleton forms where it lands (no loot, at most
  2 at once). Staying within 7 m denies it the throws.

## Settings

`8 - Skeleton Arsenal` (all default `true`):

| Key | Meaning |
| --- | --- |
| Enabled | Master switch for every arsenal skeleton and the crossbowman |
| Skeleton Cutthroat, Swordsman, Axeman, Bonebreaker, Spearman, Halberdier, Bowman, Crossbowman | Each one can spawn |

`6 - Skeleton Crossbowman`:

| Key | Default | Meaning |
| --- | --- | --- |
| Damage Factor | 1 | Bolt damage, times the skeleton archer's bow (20), as blunt |
| Shot Interval | 6 | Least seconds between shots (the archer: 4) |
| Bolt Speed | 40 | Bolt speed in m/s (the archer's arrow: 30) |
| Range | 25 | Shooting range in metres (the archer: 20) |

`25 - Crypt Executioner`:

| Key | Default | Meaning |
| --- | --- | --- |
| Enabled | true | Executioners appear |
| Chambers | 25 | Percent of burial chambers with one |
| Respawn Days | 3 | Game days before it returns |
| Health | 900 | Its health |
| Axehead Chance | 50 | Percent chance it drops its axehead |
| Slam Damage | 60 | Slash damage of the slam |
| Sweep Damage | 35 | Slash damage of the ground sweep |
| Spin Damage | 50 | Slash damage of the spin |
| Axe Head Radius | 0.5 | Metres round the blade that hit in the slam, sweep and spin |
| Slam Windup Speed | 1.35 | How much faster than animated the slam winds up |
| Throw Damage | 55 | Slash damage of a thrown axe |
| Rear Strike Damage | 55 | Slash damage of the strike behind it |
| Summons | 2 | Raised skeletons at once; 0 raises none |
