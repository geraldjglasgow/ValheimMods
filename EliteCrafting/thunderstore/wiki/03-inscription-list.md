# Inscription List

All 162 inscriptions with their default values. A server can change any of them; `ecraft list inscriptions` prints the ones in force, and `ecraft list inscriptions <slot>` (for example `legs`) filters them.

- **Effect**: the tooltip line; `x` is the rolled value.
- **Rolls on**: melee, ranged (bows and crossbows), staff, shield, head, chest, legs, cape, utility (belts and other utility items), tool. "Weapons" means melee, ranged and staff. A condition in brackets limits it further.
- **Values**: the weakest tier's range → the T1 range. Most start at T7 and roll from Meadows on. A tier in front (T6 Black Forest, T5 Swamp, T4 Mountain, T3 Plains, T2 Mistlands) means it first rolls on items of that biome. "on/off" has no number.
- **Wt**: how often it is picked against the others that could roll on the item. Most are 100; lower is rarer.
- **Health-critical** means at or below 30% of maximum health.

## Groups

One item never holds the same inscription twice, nor two of one group:

- the elemental brands (Emberbrand ... Spiritbrand); the physical brands (Bonebreaker, Keen Edge, Needlepoint)
- the skill masteries; Undead / Beast / Sea Slayer; the four bulwarks
- Balanced Grip, Seidr Thrift, Blood Price, Rune-Edged
- Fleetfoot, Cornered Flight, Stride
- pairs: Well-Forged / Everlasting, Lightened / Gossamer, Vigor / Stout Heart, Troll Blood / Cornered Blood, Seidr Flow / Restless Mind, Hardened / Cornered Hide, Soft Landing / Raven's Glide, Reaper / Soul Reaper, Blood Drinker / Cornered Thirst, Mist Veil / Cornered Veil, Oilskin / Sealegs, Reflex Draught / Swift Draught

## Weapons: damage

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Honed Might | +x% blunt, slash and pierce damage | melee, ranged | 2-3 → 13-15 | 100 |
| Primal Fury | +x% fire, frost, lightning and poison damage | weapons | 2-3 → 13-15 | 100 |
| Nightstalker | +x% damage at night | weapons | 2-3 → 13-15 | 100 |
| Emberbrand, Rimebrand, Stormbrand, Venombrand, Spiritbrand | Adds x% of this weapon's damage as fire / frost / lightning / poison / spirit | melee, ranged | 3-5 → 15-18 | 100 (Spiritbrand 70) |
| Bonebreaker, Keen Edge, Needlepoint | Adds x% of this weapon's damage as blunt / slash / pierce | melee, ranged | 3-5 → 15-18 | 100 |
| Undead Slayer, Beast Slayer, Sea Slayer | +x% damage against the undead / beasts / sea creatures | weapons | 4-6 → 22-26 | 100 |
| Godslayer | +x% damage against bosses | weapons | T5: 9-12 → 22-26 | 50 |
| Ambusher | +x% sneak attack multiplier | melee, ranged | 4-6 → 22-26 | 100 |
| Cruel Opening | x% chance that a hit on a staggered enemy is a sneak attack | melee, ranged | 3-5 → 15-18 | 60 |
| Press the Advantage | +x% damage against staggered enemies | melee, ranged | 4-6 → 22-26 | 100 |
| Deathblow | +x% damage on your first hit on an enemy below 20% health | weapons | 10-15 → 60-75 | 100 |
| Berserkergang | While health-critical, +x% damage | weapons | 5-8 → 30-35 | 60 |
| Fafnir's Greed | +x% damage per full 999 coins carried | melee | 1-2 → 7-8 | 40 |

## Weapons: on hit and on kill

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Reaper | Killing an enemy restores x stamina | melee, ranged | 3-5 → 15-18 | 100 |
| Soul Reaper | Killing an enemy restores x eitr | staff | T3: 11-13 → 15-18 | 100 |
| Blood Drinker | Heal x% of the damage your hits deal | melee, ranged | 1-1.5 → 4-5 | 60 |
| Cornered Thirst | While health-critical, heal x% of the damage your hits deal | melee, ranged | 3-4 → 9-10 | 50 |
| Seidr Siphon | Restore eitr equal to x% of the damage your hits deal | melee, staff | T3: 3-3.5 → 4-5 | 60 |
| Evader's Fury | Dodging through a melee attack grants +x% damage for 10 s | melee, ranged | 5-8 → 30-35 | 60 |
| Hamstring | Hit enemies move and attack x% slower for 2 s | melee, ranged | 5-8 → 21-25 | 70 |
| Staggering Blows | +x% stagger on the target | melee | 4-6 → 22-26 | 100 |
| Dazing Blows | Enemies you stagger stay staggered x% longer | melee | 5-10 → 40-50 | 70 |

## Melee weapons

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Balanced Grip | Attacks with this weapon cost x% less stamina | melee | 3-5 → 15-18 | 100 |
| Long Reach | +x% melee range | melee | 2-3 → 8-10 | 100 |
| Sweeping Arc | Swing arc x° wider | melee | 2-4 → 14-16 | 100 |
| Blood Price | Attacks cost health instead of stamina | melee | on/off, T4 | 30 |
| Rune-Edged | Half the stamina cost is paid in eitr; then +x% damage | melee | T3: 9-11 → 13-15 | 30 |
| Lone Blade | Off-hand empty: blocks with +x% of its attack power | melee (one-handed) | 5-8 → 30-35 | 40 |
| Steel Rhythm | A combo finisher gives 2 s of stagger immunity and x% less damage taken | melee | 5-8 → 30-35 | 40 |
| Heartwood | Trees and logs you fell drop x extra wood | melee (chops wood) | 1 → 2-3 | 70 |

## Bows and crossbows

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Easy Draw | Drawing this bow drains x% less stamina | ranged (bows) | 3-5 → 15-18 | 100 |
| Quick Windlass | This crossbow reloads x% faster | ranged (crossbows) | 3-5 → 15-18 | 100 |
| Swift String | This bow draws x% faster | ranged (bows) | 2-3 → 8-10 | 60 |
| True Flight | Projectiles fly x% faster | ranged, staff (shoots projectiles) | 5-10 → 40-50 | 100 |
| Volley | Looses three arrows in a spread, using three | ranged (bows) | on/off, T4 | 20 |
| Thrifty Quiver | x% chance that a shot uses no ammunition | ranged | 3-5 → 15-18 | 70 |
| Skirmisher | Attacking with this weapon slows you x% less | ranged | 5-10 → 40-50 | 70 |

## Staves

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Seidr Thrift | Attacks with this staff cost x% less eitr | staff | T3: 11-13 → 15-18 | 100 |
| Blood Thrift | Attacks with this staff cost x% less health | staff (Blood Magic) | T3: 11-13 → 15-18 | 100 |
| Twincast | Casts every projectile twice, for twice the eitr | staff (Elemental Magic) | on/off, T2 | 20 |
| Grave-Lord's Command | Creatures summoned with this staff deal +x% damage | staff (Blood Magic) | T3: 15-18 → 22-26 | 100 |
| Grave Vigor | Creatures summoned with this staff have +x% health | staff (Blood Magic) | T3: 15-18 → 22-26 | 100 |

## Skill masteries

Each gives +x levels to its skill: 2-3 → 13-15, weight 100 (Green Thumb 60).

| Inscription | Skill | Rolls on |
|---|---|---|
| Blade Mastery | Swords | melee (swords) |
| Axe Mastery | Axes | melee (axes) |
| Club Mastery | Clubs | melee (clubs) |
| Knife Mastery | Knives | melee (knives) |
| Spear Mastery | Spears | melee (spears) |
| Polearm Mastery | Polearms | melee (polearms) |
| Fist Mastery | Unarmed | melee (fists) |
| Bow Mastery | Bows | ranged (bows) |
| Crossbow Mastery | Crossbows | ranged (crossbows) |
| Elemental Mastery | Elemental Magic | staff (Elemental Magic) |
| Blood Mastery | Blood Magic | staff (Blood Magic) |
| Shield Mastery | Blocking | shield |
| Woodcutter's Mastery | Woodcutting | melee (chops wood) |
| Miner's Mastery | Pickaxes | tool (pickaxes) |
| Angler's Mastery | Fishing | tool (fishing rods) |
| Green Thumb | Farming | tool (Farming tools) |
| Wanderer's Mastery | Run, Jump, Swim and Sneak | legs, cape |
| Artisan's Mastery | Crafting and Cooking | head, utility |

## Shields

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Stalwart | +x% block armor | shield | 4-6 → 25-30 | 100 |
| Perfect Guard | +x% perfect block bonus | shield (can parry) | 5-8 → 30-35 | 100 |
| Repelling Guard | +x% block knockback | shield | 5-8 → 30-35 | 100 |
| Tireless Guard | Blocking costs x% less stamina | shield | 3-5 → 15-18 | 100 |
| Keen Guard | Perfect-block window x ms longer | shield (can parry) | 10-15 → 55-60 | 60 |
| Anchored Guard | Blocking with this shield: no knockback or stagger | shield | on/off, T5 | 30 |
| Seidr Riposte | A perfect block restores x eitr | shield (can parry) | T3: 11-13 → 15-18 | 60 |

## Armour: health, stamina and eitr

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Vigor | +x maximum health | head, chest | 3-5 → 22-26 | 100 |
| Stout Heart | +x maximum health, but health regenerates x% slower | chest | 6-10 → 44-52 | 60 |
| Endurance | +x maximum stamina | chest, legs | 4-6 → 23-28 | 100 |
| Wellspring | +x maximum eitr | head, cape | T3: 8-12 → 18-25 | 100 |
| Troll Blood | Health regenerates x% faster | chest, legs | 3-5 → 20-25 | 100 |
| Cornered Blood | While health-critical, health regenerates x% faster | chest | 10-15 → 52-60 | 60 |
| Second Wind | Stamina regenerates x% faster | head, legs | 3-5 → 20-25 | 100 |
| Seidr Flow | Eitr regenerates x% faster | head, cape | T3: 14-17 → 20-25 | 100 |
| Restless Mind | Eitr regenerates x% faster, but maximum eitr is lower by half as much | head | T3: 28-34 → 40-50 | 60 |
| Mending | Heal x every 10 seconds | head, chest | 1 → 4-5 | 100 |
| Valhalla's Edge | Health-critical starts x percentage points higher | head | 1-2 → 7-8 | 50 |
| Reflex Draught | Becoming health-critical drinks your best healing mead | chest, utility | on/off, T6 | 30 |
| Swift Draught | While health-critical, healing meads heal at once | utility | on/off, T5 | 30 |

## Armour: protection

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Hardened | +x% armor | head, chest, legs, cape | 4-6 → 25-30 | 100 |
| Cornered Hide | While health-critical, +x% armor | chest, legs | 10-15 → 54-62 | 60 |
| Padded, Mailed, Riveted | x% less blunt / slash / pierce damage taken | chest, legs | 2-4 → 14-16 | 100 |
| Ironclad | x% less blunt, slash and pierce damage taken | chest | 1-2 → 8-10 | 60 |
| Arrowward | x% less damage from projectiles | chest | 2-4 → 14-16 | 70 |
| Flameward | x% less fire damage taken | chest, cape | 2-4 → 14-16 | 100 |
| Frostward | x% less frost damage taken | head, cape | 2-4 → 14-16 | 100 |
| Stormward | x% less lightning damage taken | head, cape | 2-4 → 14-16 | 100 |
| Venomward | x% less poison damage taken | legs, cape | 2-4 → 14-16 | 100 |
| Elemental Ward | x% less fire, frost, lightning and poison damage taken | cape | 1-2 → 8-10 | 60 |
| Fire, Frost, Lightning, Poison Bulwark | Resistant to fire / frost / lightning / poison | cape | on/off, T5 | 30 |
| Mist Veil | x% chance to avoid a hit | legs, cape | 1-2 → 7-8 | 60 |
| Cornered Veil | While health-critical, x% chance to avoid a hit | cape | 3-5 → 15-18 | 50 |
| Bramblehide | Melee attackers take x% of the damage they deal to you | chest | 5-8 → 30-35 | 70 |
| Runic Ward | After 10 s without damage, a ward absorbs the next x damage | chest | 5-8 → 35-45 | 70 |
| Resolute | You build up x% less stagger | chest, legs | 5-10 → 40-50 | 100 |
| Ironroot | You are knocked back x% less | legs | 5-10 → 40-50 | 100 |
| Quick Recovery | You recover from stagger x% sooner | head | 5-10 → 40-50 | 70 |
| Purity | Burning, poison and frost wear off x% faster | head, chest | 5-10 → 40-50 | 70 |
| Coldblood | Frost slows you x% less | legs, cape | 10-20 → 80-100 | 70 |
| Ashen Skin | Heat builds up x% slower | chest, cape | T3: 20-25 → 30-35 | 100 |

## Movement

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Fleetfoot | You move x% faster | legs | 1-2 → 7-8 | 100 |
| Cornered Flight | While health-critical, you move x% faster | legs | 4-6 → 16-20 | 60 |
| Stride | You sprint x% faster | legs | 3-5 → 15-18 | 100 |
| Ghostwalk | You move x% faster while sneaking | cape | 3-5 → 15-18 | 100 |
| Pack Mule | You move x% faster while encumbered | legs, cape | 6-10 → 30-36 | 70 |
| Momentum | You move x% faster for 5 s after a dodge roll | legs | 3-5 → 15-18 | 70 |
| Pathfinder | You move x% faster on roads and paths | legs | 3-5 → 15-18 | 60 |
| Mountain Goat | Steep slopes slow you x% less | legs | 10-20 → 80-100 | 60 |
| Marshstrider | Tar and shallow water slow you x% less | legs | 10-20 → 80-100 | 60 |
| Spring-Heeled | You jump x% higher | legs | 5-10 → 40-50 | 100 |
| Light Leap | Jumping costs x% less stamina | legs | 3-5 → 15-18 | 100 |
| Long Wind | Sprinting costs x% less stamina | legs | 3-5 → 15-18 | 100 |
| Nimble | Dodge rolls cost x% less stamina | legs, cape | 3-5 → 15-18 | 100 |
| Soft Landing | You take x% less fall damage | legs, cape | 5-10 → 35-40 | 100 |
| Raven's Glide | You fall slowly and take no fall damage | cape | on/off, T4 | 30 |
| Strong Swimmer | You swim x% faster for x% less stamina | cape | 5-10 → 40-50 | 70 |
| Soft Tread | You make x% less noise | legs, cape | 5-10 → 40-50 | 100 |
| Shadowmeld | While sneaking you are x% harder to see | head, cape | 5-10 → 40-50 | 100 |

## Weather and comfort

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Emberheart | You never become Cold | chest, cape | on/off, T6 | 40 |
| Winterborn | You never become Freezing | chest, cape | on/off, T3 | 25 |
| Oilskin | Rain never makes you Wet | head, cape | on/off, T6 | 40 |
| Sealegs | Being Wet does not slow your regeneration | chest | on/off, T5 | 40 |
| Hearthlight | You give off a soft light | head | on/off | 40 |
| Mistbane | Your mist-clearing light clears x% farther | head, utility | T2: 30-40 → 40-50 | 70 |
| Hearthbound | +x comfort | chest, cape, utility | T5: 1 → 2 | 60 |
| Gourmand | Food lasts x% longer | chest, utility | 5-10 → 40-50 | 100 |

## Utility

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Broad Back | +x carrying capacity | chest, utility | 10-15 → 60-75 | 100 |
| Magpie | Picks up items from x% farther away | cape, utility | 10-20 → 80-100 | 100 |
| Huginn's Eye | The map reveals x% farther around you | head, utility | 5-10 → 40-50 | 100 |
| Mimir's Insight | All skills level up x% faster | head, utility | 5-10 → 40-50 | 100 |
| Soulbound | Skills lose x% less on death | head, utility | 10-20 → 80-100 | 70 |
| Brewer's Haste | Mead cooldowns are x% shorter | head, utility | 5-10 → 40-50 | 70 |
| Forsaken Favour | Your forsaken power recharges x% faster | head, utility | 5-10 → 40-50 | 70 |
| Harvester | x% chance for picked plants to yield double | utility | 5-10 → 40-50 | 100 |
| Beast Whisperer | Creatures you tame tame x% faster | cape, utility | 10-20 → 80-100 | 70 |
| Fair Winds | A ship you steer sails x% faster | cape, utility | 5-10 → 40-50 | 70 |

## Loot find

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Norns' Favour | Magic gear dropped by enemies you kill rolls a higher rarity x% more often | head, utility | 5-8 → 30-35 | 50 |
| Fateweaver | Enemies you kill are x% more likely to drop an extra rune | head, utility | 5-8 → 30-35 | 50 |
| Trophy Taker | Enemies you kill drop their trophy x% more often | utility | 5-8 → 30-35 | 70 |
| Hoardfinder | Enemies you kill drop coins and treasure x% more often | utility | 5-8 → 30-35 | 70 |

## Tools

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Builder's Reach | Build and repair x% farther away | tool (hammer, hoe, cultivator) | 10-20 → 80-100 | 100 |
| Tireless Hands | Building, repairing, tilling and planting cost x% less stamina | tool (hammer, hoe, cultivator) | 3-5 → 20-25 | 100 |
| Deep Vein | Rock and ore you break drop x extra | tool (pickaxes) | 1 → 2-3 | 70 |

## Any gear

| Inscription | Effect | Rolls on | Values | Wt |
|---|---|---|---|---|
| Well-Forged | +x% maximum durability | gear with durability | 10-15 → 65-80 | 100 |
| Everlasting | Never loses durability | gear with durability | on/off, T4 | 25 |
| Lightened | Weighs x% less | all gear | 5-10 → 40-50 | 100 |
| Gossamer | Weighs nothing | all gear | on/off, T4 | 20 |
| Supple Fit | No longer slows your movement | melee, shield, armour, cape (items that slow you) | on/off, T6 | 40 |

