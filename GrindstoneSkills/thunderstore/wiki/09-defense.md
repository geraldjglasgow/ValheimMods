# Defense Skill

A new skill for the body (the game's Blocking skill is unchanged). **Trained by** blocking creatures' hits, mostly with a shield, and a little by hits that hurt you. Bigger hits train more; falls, fire, poison and dodged hits give nothing.

Perks grow evenly from 0 at level 0 to the value at 100. All damage reductions multiply, each capped at 90%.

## Core perks (19 - Defense)

| Setting | Default | Meaning |
| --- | --- | --- |
| Defense Enabled | true | Turns Defense on or off; levels are kept. |
| Show Callouts | true | Floating words like Riposte! and Bash!. Per player. |
| Max Health At 100 | 25 | Health added to your base health. |
| Food Health At 100 | 10 | % more health from food. |
| Damage Reduction At 100 | 10 | % less damage from every source. |
| Regeneration At 100 | 1 | % of max health healed per interval out of combat. |
| Regeneration Interval | 10 | Seconds between those heals. |
| Out Of Combat Delay | 10 | Seconds without fighting before you count as out of combat. |
| Poise At 100 | 25 | % more stagger needed to stagger you or break your guard. |
| Parry Window At 100 | 0.1 | Seconds added to the parry window (game: 0.25). |
| Block Stamina Reduction At 100 | 10 | % less stamina to block. |
| Dodge Stamina Reduction At 100 | 10 | % less stamina to dodge. |

## Guard perks (22 - Defense Guard)

| Setting | Default | Meaning |
| --- | --- | --- |
| Reflex Chance At 100 | 10 | **Reflex:** % chance your shield blocks a frontal hit although you were not blocking (needs a shield). |
| Shield Bash Chance At 100 | 15 | **Bash:** % chance a shield block staggers the attacker. |
| Thorns At 100 | 10 | **Thorns:** % of blocked damage sent back to a melee attacker. |
| Adrenaline Bonus At 100 | 25 | % more adrenaline from blocks and parries. |
| Shield Wear Reduction At 100 | 50 | % less wear on what you block with. |
| Knockback Reduction At 100 | 50 | % less knockback from blocked hits. |
| Desperation Health | 25 | **Desperation:** below this % health, Damage Reduction is multiplied (0 off). |
| Desperation Multiplier | 2 | How much Desperation multiplies it. |

## Milestones (21 - Defense Milestones)

Status icons show active milestones. A relog resets Last Stand's cooldown and Hardened's stacks.

| Setting | Default | Meaning |
| --- | --- | --- |
| Riposte Level | 25 | **Riposte:** after a parry, your next melee attack deals more damage. |
| Riposte Window | 2 | Seconds to start it. |
| Riposte Damage | 25 | % more damage. |
| Shield Wall Level | 50 | **Shield Wall:** while you block with a shield, players behind you take less damage. |
| Shield Wall Radius | 4 | Metres behind you. |
| Shield Wall Reduction | 10 | % less damage for them. |
| Hardened Level | 75 | **Hardened:** each unblocked hit that hurts you adds a stack of damage reduction. |
| Hardened Per Stack | 3 | % less damage per stack. |
| Hardened Max Stacks | 5 | Most stacks. |
| Hardened Duration | 8 | Seconds stacks last. |
| Last Stand Level | 100 | **Last Stand:** a killing blow leaves you at 1 health instead. |
| Last Stand Cooldown | 600 | Seconds before it can save you again. |
| Last Stand Invulnerability | 2 | Seconds of no damage after it saves you. |

## Experience (20 - Defense Experience)

A hit's **size** is √(damage ÷ 10), from 0.5 to 4.

| Setting | Default | Meaning |
| --- | --- | --- |
| Experience Multiplier | 1 | Multiplies all Defense experience. |
| Block Experience | 1 | Shield block of a size 1 hit. |
| Parry Multiplier | 2 | A parry earns this many blocks. |
| Weapon Block Share | 50 | % for blocking with a weapon instead of a shield. |
| Hit Taken Experience | 0.5 | Unblocked size 1 hit that hurts you. |
| Hit Size Damage | 10 | Damage of a size 1 hit. |
| Min Hit Size | 0.5 | Smallest size counted. |
| Max Hit Size | 4 | Largest size counted. |
| Experience Cooldown | 0.5 | Seconds between hits that earn experience. |
| First Block Bonus | 3 | Multiplier for your first block against each kind of creature. |
| Player Hits Train | false | PvP hits train Defense too. |
