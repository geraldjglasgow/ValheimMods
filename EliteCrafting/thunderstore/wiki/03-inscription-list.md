# Inscription List

All 209 inscriptions with their default values. A server can change any of them: `ecraft list inscriptions` prints the ones in force, `ecraft list inscriptions <class>` (for example `legs` or `sword_1h`) those that roll on one item class, and `ecraft inscription <id>` one inscription with every tier.

- **Effect**: the tooltip line; X is the rolled value.
- **P/S**: prefix or suffix. A Magic item holds 1 of each, a Rare item 3 of each.
- **Best on**: item classes that roll all its tiers. **Also on**: classes where its top third of tiers stays closed. "weapons" means every melee class plus bows and crossbows, "melee" the eleven melee classes, "one-handed" swords, axes, maces, knives and spears, "staffs" both staffs, "shields" bucklers, round and tower shields, "armour" helmets, chests, legs and capes; "other" means the rest of that group. The classes are listed on [Runes and Magic Gear](wiki:Runes and Magic Gear).
- **Tiers**: how many, then the weakest tier's range → T1's range. "on" has no number and one tier. ×scale: flat added damage, multiplied by the weapon's damage scale when it rolls.
- **From**: the biome whose gear first rolls it (its item level). The strongest tier of a longer ladder needs Deep North gear.
- **Cap**: the most the total from everything you wear can reach; blank means no cap.
- **Health-critical** means at or below 30% of maximum health.
- Some inscriptions roll less often than others; `ecraft inscription <id>` shows the weight.

## Groups

One item never holds the same inscription twice, nor two of one group:

- Fleetfoot, Cornered Flight, Stride; Raven's Glide, Soft Landing; Undead, Beast and Sea Slayer; the four bulwarks
- Seidr Thrift, Balanced Grip, Blood Price, Rune-Edged; Blood Drinker, Cornered Thirst; Reaper, Soul Reaper
- pairs: Vigor / Stout Heart, Troll Blood / Cornered Blood, Seidr Flow / Restless Mind, Hardened / Cornered Hide, Mist Veil / Cornered Veil, Well-Forged / Everlasting, Lightened / Gossamer, Oilskin / Sealegs, Swift Draught / Reflex Draught
- the eighteen skill masteries

## Weapon damage

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Wrath `wrath` | +X% damage | P | - | weapons, staffs | 13: 0.5-0.7 → 5.6-6 | Meadows |  |
| Honed Might `honed_might` | +X% blunt, slash and pierce damage | P | weapons | - | 13: 2-3 → 19-20 | Meadows |  |
| Primal Fury `primal_fury` | +X% fire, frost, lightning and poison damage | P | staffs | weapons | 13: 2-3 → 24-25 | Meadows |  |
| Bonebreaker `bonebreaker` | +X blunt damage | P | Maces, Sledges | Fists | 13: 1 → 22 ×scale | Meadows |  |
| Keen Edge `keen_edge` | +X slash damage | P | Swords, Axes, Greatswords, Battleaxes, Dual axes | Knives, Fists | 13: 1 → 22 ×scale | Meadows |  |
| Needlepoint `needlepoint` | +X pierce damage | P | Spears, Atgeirs, Bows, Crossbows | Knives | 13: 1 → 22 ×scale | Meadows |  |
| Fafnir's Greed `fafnirs_greed` | +X% damage per full 999 coins carried | P | Trinkets | Utility | 8: 4-5 → 12 | Plains |  |
| Berserkergang `berserkergang` | While health-critical, +X% damage | S | - | weapons, staffs | 8: 4-5 → 22-24 | Meadows |  |
| Deathblow `deathblow` | +X% damage on your first hit on an enemy below 20% health | P | Greatswords, Battleaxes, Sledges | other weapons | 8: 50-65 → 220-250 | Plains |  |
| Godslayer `godslayer` | +X% damage against bosses | P | - | weapons, staffs | 8: 2-3 → 14-15 | Meadows |  |
| Undead Slayer `slayer_undead` | +X% damage against the undead | P | Maces, Sledges | other weapons, staffs | 8: 3-5 → 23-25 | Meadows |  |
| Sundering `sundering` | Hits ignore X% of the target's resistance | P | Knives, Spears, Crossbows | other weapons, staffs | 8: 2-3 → 14-15 | Swamp | 50 |
| Keen Eye `keen_eye` | +X% critical hit chance | S | Knives, Bows, Crossbows | other weapons, staffs | 8: 1-2 → 10 | Meadows | 25 |
| Brutal Strikes `brutal_strikes` | +X% critical hit damage | S | Greatswords, Battleaxes, Sledges, Crossbows | other weapons, staffs | 8: 10-14 → 53-60 | Meadows | 100 |
| Glass Cannon `glass_cannon` | +X% damage, but armour is X% lower | P | Greatswords, Battleaxes, Dual axes, Sledges, Fists | other weapons, Elemental staffs | 8: 3-4 → 18-20 | Swamp | 25 |
| Nightstalker `nightstalker` | +X% damage at night | P | - | weapons, staffs | 8: 2-3 → 14-15 | Meadows |  |
| Beast Slayer `slayer_beasts` | +X% damage against beasts | P | - | weapons, staffs | 8: 4-6 → 24-26 | Meadows |  |
| Sea Slayer `slayer_sea` | +X% damage against sea creatures | P | Spears, Bows | other weapons, staffs | 8: 4-6 → 24-26 | Meadows |  |

## Elemental damage

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Emberbrand `emberbrand` | +X fire damage | P | weapons | Lights | 13: 1 → 22 ×scale | Meadows |  |
| Rimebrand `rimebrand` | +X frost damage | P | weapons | - | 13: 1 → 22 ×scale | Meadows |  |
| Stormbrand `stormbrand` | +X lightning damage | P | weapons | - | 13: 1 → 22 ×scale | Meadows |  |
| Venombrand `venombrand` | +X poison damage | P | weapons | - | 13: 1 → 22 ×scale | Meadows |  |
| Spiritbrand `spiritbrand` | +X spirit damage | P | weapons | - | 13: 1 → 22 ×scale | Meadows |  |
| Lingering Wounds `lingering_wounds` | Burning, poison and frost you inflict last X% longer | S | Elemental staffs | weapons, Blood staffs | 8: 5-8 → 36-40 | Meadows | 100 |

## On hit

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Thor's Chain `thors_chain` | Hits may call chain lightning for X% of the damage | P | Elemental staffs | weapons | 8: 5-9 → 57-65 | Meadows | 100 |
| Ambusher `ambusher` | +X% sneak attack multiplier | S | Knives | Bows, Crossbows | 8: 1-2 → 15-16 | Meadows |  |
| Numbing Blow `numbing_blow` | Hits paralyse for X s (not bosses) | S | Maces, Sledges | Fists, Crossbows | 8: 0.5-0.9 → 5.3-6 | Swamp | 3 |
| Dazing Blows `dazing_blows` | Enemies you stagger stay staggered X% longer | S | Maces, Sledges | other melee, shields | 8: 1-2 → 16-18 | Meadows |  |
| Staggering Blows `staggering_blows` | +X% stagger on the target | S | Maces, Sledges | other melee, shields | 8: 1-2 → 11-12 | Meadows |  |
| Cruel Opening `cruel_opening` | X% chance that a hit on a staggered enemy is a sneak attack | S | Knives | Fists | 8: 4-5 → 22-24 | Swamp |  |
| Press the Advantage `press_the_advantage` | +X% damage against staggered enemies | S | Maces, Sledges, Fists | other weapons | 8: 2-3 → 19-21 | Meadows |  |
| Hamstring `hamstring` | Hit enemies move and attack X% slower for 2 s | S | Spears, Atgeirs | other weapons | 8: 4-5 → 22-24 | Meadows |  |
| Mighty Blows `mighty_blows` | +X% knockback with this weapon | S | Maces, Sledges | other melee, Crossbows | 8: 5-8 → 36-40 | Meadows | 100 |
| Heavy Hand `heavy_hand` | +X% stagger with this weapon, but it swings a third as much slower | S | Maces, Sledges | Axes, Greatswords, Battleaxes, Atgeirs, Fists | 8: 10-13 → 41-45 | Black Forest | 60 |

## Attack speed and cost

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Seidr Thrift `seidr_thrift` | Attacks with this staff cost X% less eitr | S | staffs | - | 8: 1-2 → 16-18 | Meadows | 30 |
| Blood Thrift `blood_thrift` | Attacks with this staff cost X% less health | S | Blood staffs | - | 8: 1-2 → 16-18 | Meadows | 30 |
| Balanced Grip `balanced_grip` | Attacks with this weapon cost X% less stamina | S | melee | Pickaxes | 8: 2-4 → 22-24 | Meadows | 30 |
| Blood Price `blood_price` | Attacks cost health instead of stamina | S | - | melee | on | Meadows |  |
| Quickened `quickened` | +X% attack speed with this weapon | S | melee | Pickaxes | 8: 1-2 → 11-12 | Meadows | 15 |
| Steel Rhythm `steel_rhythm` | A combo finisher gives 2 s of stagger immunity and X% less damage taken | S | - | melee | 8: 15-17 → 41-45 | Meadows |  |
| Desperate Haste `quickened_hc` | +X% attack speed with this weapon while health is critical | S | - | melee | 8: 4-5 → 22-24 | Meadows | 15 |
| Rune-Edged `rune_edge` | Half the stamina cost is paid in eitr; then +X% damage | P | - | melee | 8: 4-6 → 27-30 | Swamp |  |
| Long Reach `long_reach` | +X% melee range | S | Spears, Atgeirs | other melee | 8: 2-3 → 10 | Meadows |  |
| Sweeping Arc `sweeping_arc` | Swing arc X° wider | S | Greatswords, Battleaxes, Sledges, Atgeirs | Swords, Axes, Maces | 8: 2-3 → 15-16 | Meadows |  |

## Leech

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Blood Drinker `blood_drinker` | Heal X% of the damage your hits deal | P | melee | - | 8: 0.5-0.8 → 3.6-4 | Swamp | 5 |
| Seidr Siphon `seidr_siphon` | Restore eitr equal to X% of the damage your hits deal | P | - | melee | 8: 0.5-0.8 → 3.6-4 | Swamp | 5 |
| Cornered Thirst `blood_drinker_hc` | While health-critical, heal X% of the damage your hits deal | S | - | melee | 8: 1-1.3 → 4.5-5 | Swamp | 5 |
| Wind Siphon `wind_siphon` | Hits restore X% of their damage as stamina | P | melee | - | 8: 0.5-0.8 → 3.6-4 | Meadows | 5 |
| Reaper `reaper` | Killing an enemy restores X stamina | S | - | weapons | 8: 3 → 18 | Meadows |  |
| Soul Reaper `soul_reaper` | Killing an enemy restores X eitr | S | staffs | - | 8: 11 → 18 | Meadows |  |

## Ranged and magic

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Thrifty Quiver `thrifty_quiver` | X% chance that a shot uses no ammunition | S | Bows, Crossbows | - | 8: 5-8 → 44-50 | Meadows | 50 |
| Quick Windlass `quick_windlass` | This crossbow reloads X% faster | S | Crossbows | - | 8: 1-2 → 17-19 | Meadows | 50 |
| Swift Casting `swift_casting` | +X% casting speed with this staff | S | Elemental staffs | Blood staffs | 8: 1-3 → 27-30 | Meadows | 25 |
| True Flight `true_flight` | Projectiles fly X% faster (only items that shoot) | S | Bows, Crossbows | Spears, Elemental staffs | 8: 5-8 → 36-40 | Meadows |  |
| Easy Draw `easy_draw` | Drawing this bow drains X% less stamina | S | Bows | - | 8: 2-3 → 17-18 | Meadows | 30 |
| Bursting Shot `bursting_shot` | Shots burst for X% of their damage around the impact | P | Bows, Crossbows | - | 8: 2-3 → 17-18 | Meadows | 100 |
| Swift String `swift_string` | This bow draws X% faster | S | Bows | - | 8: 2-4 → 22-24 | Meadows | 50 |
| Grave Vigor `grave_vigor` | Creatures summoned with this staff have +X% health | P | Blood staffs | - | 8: 1-3 → 27-30 | Meadows |  |
| Grave-Lord's Command `grave_command` | Creatures summoned with this staff deal +X% damage | P | Blood staffs | - | 8: 1-3 → 27-30 | Meadows |  |
| Twincast `twincast` | Casts every projectile twice, for twice the eitr | S | Elemental staffs | - | on | Plains |  |
| Volley `volley` | Looses three arrows in a spread, using three | S | Bows | - | on | Plains |  |
| Skirmisher `skirmisher` | Attacking with this weapon slows you X% less | S | Bows | - | 8: 5-8 → 44-50 | Meadows |  |

## Throwing

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Throwing Grip `throwing_grip` | Secondary attack throws this weapon | S | Axes, Knives | Swords, Maces | on | Meadows |  |
| Returning `returning` | Thrown, this weapon flies back to you | S | Spears | - | on | Swamp |  |
| Bifrost Step `bifrost_step` | A thrown hit carries you to your weapon | S | Spears | - | on | Swamp |  |

## Blocking and parry

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Stalwart `stalwart` | +X% block armor | P | shields | melee | 13: 2-3 → 23-24 | Meadows |  |
| Repelling Guard `repelling_guard` | +X% block knockback | S | Round shields, Tower shields | Bucklers | 8: 5-6 → 22-24 | Meadows |  |
| Perfect Guard `perfect_guard` | +X% perfect block bonus (only items that can parry) | P | Bucklers, Round shields | melee | 13: 2-3 → 23-24 | Meadows |  |
| Tireless Guard `tireless_guard` | Blocking costs X% less stamina | S | shields | melee | 8: 2-3 → 19-21 | Meadows | 30 |
| Bramblehide `bramblehide` | Melee attackers take X% of the damage they deal to you | P | Round shields, Tower shields, Chests | Bucklers, Legs | 13: 2-2.5 → 13-14 | Meadows |  |
| Mist Veil `mist_veil` | X% chance to avoid a hit | S | Bucklers, Chests | Round shields, Capes | 8: 2-3 → 13-14 | Meadows | 15 |
| Last Stand `stalwart_hc` | +X% block armour while health is critical | S | shields | melee | 8: 4-5 → 22-24 | Meadows |  |
| Desperate Guard `perfect_guard_hc` | +X% parry bonus while health is critical | S | Bucklers, Round shields | melee | 8: 4-5 → 22-24 | Meadows |  |
| Cornered Veil `mist_veil_hc` | While health-critical, X% chance to avoid a hit | S | Bucklers | Chests, Capes | 8: 4-6 → 24-27 | Meadows | 15 |
| Lone Blade `lone_blade` | Off-hand empty: blocks with +X% of its attack power | P | Swords | Axes, Maces, Knives, Spears | 8: 4-6 → 25-28 | Meadows |  |
| Anchored Guard `anchored_guard` | Blocking with this shield: no knockback or stagger | S | Tower shields | Round shields | on | Meadows |  |
| Keen Guard `keen_guard` | Perfect-block window X ms longer (only items that can parry) | S | Bucklers | one-handed, Round shields | 8: 40-44 → 92-100 | Meadows | 150 |
| Seidr Riposte `seidr_riposte` | A perfect block restores X eitr (only items that can parry) | S | Bucklers | one-handed, Round shields | 8: 15-20 → 71-80 | Swamp |  |
| Shield Mend `shield_mend` | Blocking or parrying a hit heals X | S | Bucklers, Round shields | Tower shields | 8: 1 → 12 | Meadows |  |

## Health and regeneration

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Vigor `vigor` | +X maximum health | P | Chests | shields, Helmets, Legs, Capes, Trinkets | 13: 3-4 → 37-40 | Meadows |  |
| Troll Blood `troll_blood` | Health regenerates X% faster | S | Helmets | Chests, Legs, Capes, Trinkets | 13: 2-3 → 23-24 | Meadows | 150 |
| Valhalla's Edge `valhallas_edge` | Health-critical starts X percentage points higher | S | Trinkets | Helmets | 8: 2-4 → 22-24 | Meadows | 20 |
| Mending `mending` | Heal X every 10 seconds | S | Chests | Helmets, Legs, Capes | 13: 0.2-0.8 → 12.8-14 | Meadows |  |
| Cornered Blood `troll_blood_hc` | While health-critical, health regenerates X% faster | S | Helmets | Chests, Capes | 8: 4-6 → 30-33 | Meadows | 150 |
| Stout Heart `stout_heart` | +X maximum health, but health regenerates X% slower | P | Chests | Legs | 8: 4-8 → 51-58 | Swamp |  |
| Purity `purity` | Burning, poison and frost wear off X% faster | S | Helmets, Trinkets | Chests | 8: 5-8 → 44-50 | Meadows | 75 |

## Stamina and eitr

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Endurance `endurance` | +X maximum stamina | P | Legs | Chests, Capes, Trinkets | 13: 3-4 → 37-40 | Meadows |  |
| Wellspring `wellspring` | +X maximum eitr | P | Helmets | staffs, Chests, Capes, Trinkets | 13: 4-6 → 56-60 | Meadows |  |
| Second Wind `second_wind` | Stamina regenerates X% faster | S | Legs | Chests, Capes, Trinkets | 13: 2-3 → 23-24 | Meadows | 100 |
| Seidr Flow `seidr_flow` | Eitr regenerates X% faster | S | staffs, Helmets | Chests, Legs, Capes, Trinkets | 13: 3-4 → 34-36 | Meadows | 100 |
| Last Breath `second_wind_hc` | +X% stamina regeneration while health is critical | S | Legs | Trinkets | 8: 4-6 → 30-33 | Meadows | 100 |
| Seidr Surge `seidr_flow_hc` | +X% eitr regeneration while health is critical | S | Helmets | staffs, Capes | 8: 4-7 → 44-50 | Meadows | 100 |
| Restless Mind `restless_mind` | Eitr regenerates X% faster, but maximum eitr is lower by half as much | S | Helmets | Trinkets | 8: 10-17 → 87-100 | Swamp |  |

## Armour and resistances

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Hardened `hardened` | +X% armor | P | Helmets, Chests, Legs | Capes | 13: 2-3 → 17-18 | Meadows |  |
| Flameward `flameward` | X% less fire damage taken | S | Chests, Capes | shields, Helmets, Legs, Trinkets | 13: 1-2 → 17-18 | Meadows | 50 |
| Frostward `frostward` | X% less frost damage taken | S | Chests, Capes | shields, Helmets, Legs, Trinkets | 13: 1-2 → 17-18 | Meadows | 50 |
| Stormward `stormward` | X% less lightning damage taken | S | Chests, Capes | shields, Helmets, Legs, Trinkets | 13: 1-2 → 17-18 | Meadows | 50 |
| Venomward `venomward` | X% less poison damage taken | S | Chests, Capes | shields, Helmets, Legs, Trinkets | 13: 1-2 → 17-18 | Meadows | 50 |
| Spiritward `spiritward` | -X% spirit damage taken | S | Chests, Capes | shields, Helmets, Legs, Trinkets | 13: 1-2 → 17-18 | Meadows | 50 |
| Elemental Ward `elemental_ward` | X% less fire, frost, lightning and poison damage taken | S | Capes | Chests | 4: 2-4 → 11-14 | Plains | 50 |
| Padded `padded` | X% less blunt damage taken | S | Chests | shields, Helmets, Legs | 13: 1-2 → 17-18 | Meadows | 50 |
| Mailed `mailed` | X% less slash damage taken | S | Chests | shields, Helmets, Legs | 13: 1-2 → 17-18 | Meadows | 50 |
| Riveted `riveted` | X% less pierce damage taken | S | Chests | shields, Helmets, Legs | 13: 1-2 → 17-18 | Meadows | 50 |
| Ironclad `ironclad` | X% less blunt, slash and pierce damage taken | S | Chests | Tower shields | 4: 2-4 → 11-14 | Plains | 50 |
| Cornered Hide `hardened_hc` | While health-critical, +X% armor | S | Chests | Legs, Trinkets | 8: 4-5 → 22-24 | Meadows |  |
| Forsaken Ward `forsaken_ward` | -X% damage taken from bosses | S | Round shields, Tower shields, Chests | Bucklers, Helmets, Legs, Capes | 8: 2-3 → 11-12 | Meadows | 40 |
| Ember Skin `ember_skin` | -X% burning and lava damage taken | S | Legs, Capes | Helmets, Chests | 8: 3-5 → 23-25 | Meadows | 75 |
| Quench `quench` | Burning on you ends X% sooner | S | Capes | Chests, Legs | 8: 5-8 → 36-40 | Meadows | 75 |
| Arrowward `arrowward` | X% less damage from projectiles | S | Round shields, Tower shields, Chests | Bucklers, Helmets, Legs, Capes | 8: 2-3 → 15-16 | Meadows | 60 |
| Runic Ward `runic_ward` | After 10 s without damage, a ward absorbs the next X damage | P | Chests | Round shields | 8: 5-8 → 40-45 | Black Forest |  |
| Resolute `resolute` | You build up X% less stagger | S | Chests, Legs | Tower shields | 8: 5-8 → 44-50 | Meadows | 75 |
| Ironroot `ironroot` | You are knocked back X% less | S | Legs | Tower shields | 8: 5-8 → 44-50 | Meadows | 75 |
| Quick Recovery `quick_recovery` | You recover from stagger X% sooner | S | Helmets | Legs | 8: 5-8 → 44-50 | Meadows | 75 |
| Coldblood `coldblood` | Frost slows you X% less | S | Legs, Capes | - | 8: 10-17 → 87-100 | Meadows | 100 |
| Fire Bulwark `bulwark_fire` | Resistant to fire | S | Capes | - | on | Mistlands |  |
| Frost Bulwark `bulwark_frost` | Resistant to frost | S | Capes | - | on | Mistlands |  |
| Lightning Bulwark `bulwark_lightning` | Resistant to lightning | S | Capes | - | on | Mistlands |  |
| Poison Bulwark `bulwark_poison` | Resistant to poison | S | Capes | - | on | Mistlands |  |

## Movement

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Evader's Fury `evaders_fury` | Dodging through a melee attack grants +X% damage for 10 s | S | Legs | Capes, Trinkets | 8: 5-7 → 31-35 | Meadows |  |
| Supple Fit `supple_fit` | No longer slows your movement (only items that slow you) | S | Tower shields, Chests, Legs | weapons, staffs, Bucklers, Round shields, Helmets | on | Meadows |  |
| Soft Tread `soft_tread` | You make X% less noise | S | Legs | Capes, Trinkets | 8: 2-6 → 44-50 | Meadows | 75 |
| Fleetfoot `fleetfoot` | You move X% faster | S | Legs | Capes, Trinkets | 8: 1-2 → 11-12 | Meadows | 15 |
| Long Wind `long_wind` | Sprinting costs X% less stamina | S | Legs | Capes, Trinkets | 8: 4-5 → 22-24 | Meadows | 30 |
| Nimble `nimble` | Dodge rolls cost X% less stamina | S | Legs | Trinkets | 8: 1-2 → 18-20 | Meadows | 30 |
| Light Leap `light_leap` | Jumping costs X% less stamina | S | Legs | Trinkets | 8: 2-3 → 19-21 | Meadows | 30 |
| Windstep `windstep` | Jump once more in the air | S | Legs | - | on | Plains |  |
| Raven's Glide `ravens_glide` | You fall slowly and take no fall damage | S | Legs, Capes | - | on | Plains |  |
| Cornered Flight `fleetfoot_hc` | While health-critical, you move X% faster | S | Legs | Trinkets | 8: 1-2 → 19-21 | Meadows | 15 |
| Sea Ward `sea_ward` | The ship you steer takes X% less damage | S | Capes | Trinkets, Utility | 8: 5-7 → 31-35 | Meadows | 75 |
| Ghostwalk `ghostwalk` | You move X% faster while sneaking | S | Legs | Capes, Trinkets | 8: 3-4 → 17-18 | Meadows | 50 |
| Spring-Heeled `spring_heeled` | You jump X% higher | S | Legs | - | 8: 5-8 → 44-50 | Meadows | 50 |
| Soft Landing `soft_landing` | You take X% less fall damage | S | Legs, Capes | - | 8: 5-8 → 36-40 | Meadows | 80 |
| Stride `stride` | You sprint X% faster | S | Legs | Trinkets | 8: 3-4 → 17-18 | Meadows | 30 |
| Pack Mule `pack_mule` | You move X% faster while encumbered | S | Legs, Utility | Capes | 8: 6-8 → 32-36 | Meadows | 50 |
| Momentum `momentum` | You move X% faster for 5 s after a dodge roll | S | Legs | - | 8: 3-4 → 17-18 | Meadows | 40 |
| Pathfinder `pathfinder` | You move X% faster on roads and paths | S | Legs | - | 8: 3-4 → 17-18 | Meadows | 30 |
| Mountain Goat `mountain_goat` | Steep slopes slow you X% less | S | Legs | - | 8: 10-17 → 87-100 | Meadows | 75 |
| Marshstrider `marshstrider` | Tar and shallow water slow you X% less | S | Legs | - | 8: 10-17 → 87-100 | Meadows | 75 |
| Strong Swimmer `strong_swimmer` | You swim X% faster for X% less stamina | S | Legs, Capes | Trinkets | 8: 5-8 → 44-50 | Meadows | 50 |
| Shadowmeld `shadowmeld` | While sneaking you are X% harder to see | S | Helmets, Capes | Legs | 8: 5-8 → 44-50 | Meadows | 75 |
| Fair Winds `fair_winds` | A ship you steer sails X% faster | S | Capes | Trinkets, Utility | 8: 5-8 → 44-50 | Meadows | 50 |

## Weather and comfort

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Ashen Skin `ashen_skin` | Heat builds up X% slower | S | Capes | Chests | 8: 15-16 → 33-35 | Plains |  |
| Oilskin `oilskin` | Rain never makes you Wet | S | Capes | Helmets | on | Plains |  |
| Emberheart `emberheart` | You never become Cold | S | Capes | Chests | on | Plains |  |
| Hearthbound `hearthbound` | +X comfort | S | Capes | Trinkets, Utility | 2: 1 → 2 | Plains | 3 |
| Hearthlight `hearthlight` | You give off a soft light | S | Helmets | Chests, Capes, Trinkets | on | Meadows |  |
| Winterborn `winterborn` | You never become Freezing | S | Capes | Chests | on | Mountain |  |
| Sealegs `sealegs` | Being Wet does not slow your regeneration | S | Chests | Capes | on | Swamp |  |

## Meads and food

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Long Brew `long_brew` | Mead effects last X% longer | S | Trinkets | Utility | 8: 5-8 → 36-40 | Meadows | 100 |
| Potent Brew `potent_brew` | Restoring meads restore X% more | S | Trinkets | Utility | 8: 5-7 → 27-30 | Meadows | 50 |
| Bottomless Flask `bottomless_flask` | X% chance a mead is not used up | S | Utility | Trinkets | 8: 3-4 → 18-20 | Swamp | 50 |
| Hearty Appetite `hearty_appetite` | Food gives X% more health, stamina and eitr | S | Chests | Trinkets, Utility | 8: 2-3 → 14-15 | Meadows | 30 |
| Well Fed `well_fed` | +X% food health regeneration | S | Chests | Helmets, Trinkets | 8: 5-7 → 27-30 | Meadows | 50 |
| Deep Rest `deep_rest` | Rested lasts X% longer | S | Capes | Helmets, Trinkets, Utility | 8: 5-8 → 44-50 | Meadows | 100 |
| Gourmand `gourmand` | Food lasts X% longer | S | Chests, Trinkets | Utility | 8: 5-8 → 44-50 | Meadows | 100 |

## Adrenaline

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Battle Rush `battle_rush` | Trinket effects last X% longer | P | Trinkets | shields | 8: 5-8 → 36-40 | Meadows | 100 |
| Blood Up `blood_up` | Blocks and parries give X% more adrenaline | S | shields | Trinkets | 8: 5-8 → 36-40 | Meadows | 100 |

## Charms and fortune

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Forsaken Favour `forsaken_favour` | Your forsaken power recharges X% faster | S | Trinkets | Helmets, Utility | 8: 2-4 → 22-24 | Meadows | 30 |
| Brewer's Haste `brewers_haste` | Mead cooldowns are X% shorter | S | Trinkets | Utility | 8: 5-7 → 27-30 | Meadows | 30 |
| Swift Draught `swift_draught` | While health-critical, healing meads heal at once | S | Trinkets | Utility | on | Swamp |  |
| Reflex Draught `reflex_draught` | Becoming health-critical drinks your best healing mead | S | Trinkets | Utility | on | Swamp |  |
| Magpie `magpie` | Picks up items from X% farther away | S | Utility | Trinkets | 8: 10-17 → 87-100 | Meadows | 100 |
| Broad Back `broad_back` | +X carrying capacity | P | Utility | Capes, Trinkets | 13: 10-16 → 138-150 | Meadows |  |
| Hoardfinder `hoardfinder` | Enemies you kill drop coins and treasure X% more often | S | Trinkets | Utility | 8: 1-2 → 11-12 | Meadows | 200 |
| Norns' Favour `norns_favour` | Magic gear dropped by enemies you kill rolls a higher rarity X% more often | S | Trinkets | Helmets, Utility | 8: 1-2 → 16-18 | Plains | 200 |
| Silver Tongue `silver_tongue` | Traders charge you X% less | S | Trinkets | Utility | 8: 2-3 → 14-15 | Meadows | 30 |
| Fateweaver `fateweaver` | Enemies you kill are X% more likely to drop an extra rune | S | Trinkets | Helmets, Utility | 8: 5-7 → 31-35 | Meadows | 200 |
| Beast Whisperer `beast_whisperer` | Creatures you tame tame X% faster | S | Trinkets | Capes, Utility | 8: 10-17 → 87-100 | Meadows | 100 |

## Perception and learning

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Mimir's Insight `mimirs_insight` | All skills level up X% faster | S | Helmets | Trinkets, Utility | 8: 1-2 → 18-20 | Meadows | 100 |
| Huginn's Eye `huginns_eye` | The map reveals X% farther around you | S | Helmets | Lights, Trinkets | 8: 1-3 → 27-30 | Meadows | 100 |
| Trophy Taker `trophy_taker` | Enemies you kill drop their trophy X% more often | S | Helmets | Trinkets | 8: 1-2 → 16-18 | Meadows | 200 |
| Soulbound `soulbound` | Skills lose X% less on death | S | Helmets, Trinkets | Utility | 8: 10-17 → 87-100 | Meadows | 75 |

## Gathering and building

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Deep Vein `deep_vein` | Rock and ore you break drop X extra | S | Pickaxes | - | 2: 1 → 2 | Plains |  |
| Heartwood `heartwood` | Trees and logs you fell drop X extra wood | S | Axes, Battleaxes | Dual axes | 4: 1 → 4 | Meadows |  |
| Builder's Reach `builders_reach` | Build and repair X% farther away (only building tools) | S | Tools | - | 8: 5-10 → 61-70 | Meadows | 100 |
| Mistbane `mistbane` | Your mist-clearing light clears X% farther | S | Utility | - | 4: 25-65 → 183-250 | Meadows | 100 |
| Masterbuilder `masterbuilder` | Build without a crafting station nearby | S | Tools | - | on | Deep North |  |
| Timber Bite `timber_bite` | +X% damage to trees and logs | S | Axes, Battleaxes | Dual axes | 8: 5-8 → 36-40 | Meadows | 100 |
| Stone Bite `stone_bite` | +X% damage to rock and ore | S | Pickaxes | - | 8: 5-8 → 36-40 | Meadows | 100 |
| Butcher's Cut `butchers_cut` | Animals you kill drop X more meat and hide | S | Knives | Bows, Tools | 3: 1 → 3 | Meadows |  |
| Patient Line `patient_line` | X% chance a cast keeps its bait | S | Fishing rod | - | 8: 5-8 → 36-40 | Meadows | 75 |
| Big Catch `big_catch` | X% better odds of a bigger fish | S | Fishing rod | Helmets | 8: 5-8 → 36-40 | Meadows | 100 |
| Steady Reel `steady_reel` | -X% reeling stamina | S | Fishing rod | - | 8: 5-7 → 31-35 | Meadows | 60 |
| Thrifty Hands `thrifty_hands` | X% chance crafting uses no materials | S | Tools | Trinkets, Utility | 8: 2-3 → 14-15 | Meadows | 30 |
| Bountiful Forge `bountiful_forge` | X% chance to craft one more | S | Tools | Trinkets, Utility | 8: 2-3 → 11-12 | Swamp | 30 |
| Tireless Hands `tireless_hands` | Building, repairing, tilling and planting cost X% less stamina (only building tools) | S | Tools | - | 8: 3-5 → 23-25 | Meadows | 30 |
| Harvester `harvester` | X% chance for picked plants to yield double | S | Trinkets, Utility | - | 8: 5-8 → 44-50 | Meadows | 100 |

## Skills

Each adds X levels to its skill: 8 tiers, +1-2 → +16-18 from Meadows gear (Woodcutter's Mastery and Green Thumb +2 → +15). All are suffixes, with no cap, and one item holds only one of them.

| Inscription | Gives | Best on | Also on |
|---|---|---|---|
| Blade Mastery `blade_mastery` | +X Swords skill | Swords, Greatswords | - |
| Knife Mastery `knife_mastery` | +X Knives skill | Knives | - |
| Club Mastery `club_mastery` | +X Clubs skill | Maces, Sledges | - |
| Polearm Mastery `polearm_mastery` | +X Polearms skill | Atgeirs | - |
| Spear Mastery `spear_mastery` | +X Spears skill | Spears | - |
| Shield Mastery `shield_mastery` | +X Blocking skill | shields | - |
| Axe Mastery `axe_mastery` | +X Axes skill | Axes, Battleaxes, Dual axes | - |
| Bow Mastery `bow_mastery` | +X Bows skill | Bows | - |
| Crossbow Mastery `crossbow_mastery` | +X Crossbows skill | Crossbows | - |
| Blood Mastery `blood_mastery` | +X Blood Magic skill | Blood staffs | - |
| Artisan's Mastery `artisan_mastery` | +X Crafting and Cooking skills | Tools | Trinkets, Utility |
| Elemental Mastery `elemental_mastery` | +X Elemental Magic skill | Elemental staffs | - |
| Fist Mastery `fist_mastery` | +X Unarmed skill | Fists | - |
| Miner's Mastery `pick_mastery` | +X Pickaxes skill | Pickaxes | - |
| Angler's Mastery `fishing_mastery` | +X Fishing skill | Fishing rod | - |
| Wanderer's Mastery `wanderer_mastery` | +X Run, Jump, Swim and Sneak skills | Legs | Trinkets |
| Woodcutter's Mastery `woodcutting_mastery` | +X Woodcutting skill | Axes, Battleaxes, Dual axes | - |
| Green Thumb `farming_mastery` | +X Farming skill (only farming tools) | Tools | - |

## Item properties

| Inscription | Effect | P/S | Best on | Also on | Tiers | From | Cap |
|---|---|---|---|---|---|---|---|
| Well-Forged `well_forged` | +X% maximum durability (only items with durability) | S | - | weapons, staffs, shields, armour, Lights, Pickaxes, Tools | 8: 10-24 → 172-200 | Meadows |  |
| Lightened `lightened` | Weighs X% less | S | - | weapons, staffs, shields, armour, Lights, Pickaxes, Tools, Fishing rod | 4: 10-17 → 39-50 | Meadows |  |
| Everlasting `everlasting` | Never loses durability (only items with durability) | S | - | weapons, staffs, shields, armour, Lights, Pickaxes, Tools | on | Ashlands |  |
| Gossamer `gossamer` | Weighs nothing | S | - | weapons, staffs, shields, armour, Lights, Pickaxes, Tools, Fishing rod | on | Plains |  |
