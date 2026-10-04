# Sailing Skill

A new skill. **Trained by** steering: experience per km your ship travels while you hold the helm, and a share for everyone aboard. Teleports, bobbing in place and building ships earn nothing.

## Perks (8 - Sailing)

Perks grow evenly from 0 at level 0 to the value at 100.

| Setting | Default | Meaning |
| --- | --- | --- |
| Sailing Enabled | true | Turns Sailing's perks, milestones and experience on or off; levels are kept. |
| Ship Health At 100 | 50 | % more health for ships you build, fixed when placed. |
| Ship Speed At 100 | 20 | % more top speed, by the helmsman's level. |
| Exploration Radius At 100 | 100 | % larger map reveal while aboard (game: 100 m). |
| Helm Experience Per Kilometre | 40 | Experience per km steered. |
| Crew Experience Share | 25 | % of that earned by everyone else aboard. |

## Wind Call, level 25 (35 - Wind Call)

**K** while steering a ship turns the wind to blow where you look, for everyone on that ship. It overrides Moder's tailwind there; a new call replaces the old one.

| Setting | Default | Meaning |
| --- | --- | --- |
| Wind Call Level | 25 | Level that unlocks it (0 everyone, 101 off). |
| Wind Call Duration | 60 | Seconds it blows. |
| Wind Call Cooldown | 180 | Seconds between calls, per player. |
| Wind Call Key | K | The key. Per player. |

## Lookout, level 50 (9 - Lookout)

**O** aboard sends a pulse from the ship. Everyone aboard sees the name tags of the enemies it reached (not bosses or tamed creatures), wherever they go, for the duration.

| Setting | Default | Meaning |
| --- | --- | --- |
| Lookout Level | 50 | Level that unlocks it (0 everyone, 101 off). |
| Lookout Radius | 100 | Pulse reach, in metres. |
| Lookout Duration | 30 | Seconds the tags show. |
| Lookout Cooldown | 60 | Seconds between pulses, per player. |
| Lookout Key | O | The key. Per player. |

Keys take modifiers (`K + LeftShift`), work while you walk, never while you type, and do nothing below the level, ashore or on cooldown. A relog resets cooldowns. With ShipConfig, its ship health replaces Sailing's bonus.
