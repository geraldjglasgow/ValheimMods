# Runes and Magic Gear

## Rarities

| Rarity | Inscriptions | Prefixes | Suffixes | Colour |
|---|---|---|---|---|
| Normal | none | - | - | white |
| Magic | 1-2 | at most 1 | at most 1 | green |
| Rare | 3-6 | at most 3 | at most 3 | blue |

- A **prefix** is the item's core power (damage, health, armour, blocking, leech); a **suffix** is everything else. The [Inscription List](wiki:Inscription List) marks each.
- Magic gear drops ready-made or is made with runes; crafted items start Normal. Magic items never stack.

## Item classes

Each kind of item is a class, and each inscription rolls on the classes it lists. `ecraft classes` lists every class with its items, their item levels and how many inscriptions it can roll.

| Group | Classes (id) |
|---|---|
| One-handed | Swords `sword_1h`, Axes `axe_1h`, Maces `mace_1h` (clubs and maces), Knives `knife`, Spears `spear` |
| Two-handed | Greatswords `greatsword`, Battleaxes `battleaxe`, Dual axes `dual_axe`, Sledges `sledge`, Atgeirs `atgeir`, Fists `fist` (Unarmed skill) |
| Ranged | Bows `bow`, Crossbows `crossbow` |
| Magic | Elemental staffs `staff_elemental`, Blood staffs `staff_blood` |
| Shields and lights | Bucklers `buckler`, Round shields `shield`, Tower shields `tower`, Lights `light` (torches and lanterns) |
| Armour | Helmets `helmet`, Chests `chest`, Legs `legs`, Capes `cape` |
| Trinkets and utility | Trinkets `trinket`, Utility `utility` (belts, the wishbone...) |
| Tools | Pickaxes `pickaxe`, Tools `tool` (hammer, hoe, cultivator, scythe, shovel, butcher knife, grappling hook), Fishing rod `fishing` |
| Never magic | Practice weapons, cosmetic clothing (dresses, tunics, hats), tankards, bombs and throwables, anything that stacks, runes |

PackPanel adds a Backpacks class.

**Damage scale.** Flat added damage (the brands: +X fire, +X slash...) is multiplied by the weapon's damage scale when it rolls, and the tooltip shows the result: Lights 0.5, Bows 0.6, Knives and Fists 0.7, Dual axes 0.8, Greatswords, Battleaxes, Sledges and Atgeirs 1.3, Crossbows 1.4, every other class 1.

## The runes

| Rune | Works on | Does |
|---|---|---|
| Awakening | Normal | Makes it Magic with one inscription |
| Shaping | Magic | Adds one inscription, up to two: the kind (prefix or suffix) the item lacks |
| Recasting | Magic | Replaces every inscription with one or two new ones; stays Magic |
| Ascension | Magic | Makes it Rare, keeping its inscriptions and adding enough to reach three |
| Consecrated | Rare | Adds one inscription, up to six (three prefixes and three suffixes) |
| Cleansing | Magic, Rare | Back to Normal; every inscription is lost |
| Serpent | Magic, Rare | A gamble, then the item is **sealed**: no rune works on it again |

- Click the rune stack onto an item in your own inventory. The rune can come straight from the chest you have open; the item must be in your inventory. Equipped items work too unless the server forbids it.
- A success uses one rune. If the rune cannot be used, it is kept and a message says why, for example "No inscription can roll on this item".
- Cleansing and Serpent ask first: hold **Shift** while clicking (left trigger on a gamepad). The `Confirm destructive runes` setting changes this. Recasting does not ask, though the inscriptions it replaces are gone for good.
- Runes stack to 50, weigh 0.2 and go through portals. Each is a stone tablet with its own glyph; with `Glow runes` on, it glows on the ground in its colour: Awakening green, Shaping teal, Recasting violet, Ascension blue, Consecrated gold, Cleansing silver, Serpent dark green.

**Serpent outcomes**: 35% nothing more; 35% one inscription past the limit (a 3rd on Magic, a 7th on Rare, even past the prefix or suffix limit); 30% every inscription rerolled at any of its tiers, equally likely, whatever the item level. The tooltip then shows `Sealed`.

## Item level and tiers

**Item level** is the biome of the item's materials, from the latest material in its recipe: Meadows 1, Black Forest 2, Swamp 3, Mountain 4, Plains 5, Mistlands 6, Ashlands 7, Deep North 8. A bronze sword is level 2 wherever you find it. `ecraft tiers` lists every item's class and level.

**Tiers** count down: **T1 is the strongest.** Each inscription has its own ladder of 1 to 16 tiers (most have 8 or 13), and each tier unlocks at an item level, the weakest at the inscription's first biome, T1 on Deep North gear. Vigor (+X maximum health, 13 tiers):

| Item level | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| Unlocks | T13 (3-4) | T12-T11 (5-9) | T10-T9 (10-14) | T8-T7 (15-20) | T6-T5 (21-26) | T4-T3 (27-33) | T2 (34-36) | T1 (37-40) |

- An item rolls any tier its level has unlocked, weaker tiers more often: a tier's weight is its number, so T13 is 13 times as likely as T1. Vigor rolled on Deep North chest armour is T1 once in 91.
- Each inscription is **best** on some classes and **allowed** on others. On an allowed class its top third of tiers stays closed: of 13 tiers it stops at T5, of 8 at T3, of 4 or 3 at T2; ladders of 1 or 2 tiers lose none. Vigor is best on chests (all 13 tiers) and allowed on helmets (up to T5).
- Some inscriptions start in a later biome, such as Deathblow on Plains gear or Masterbuilder only on Deep North gear; the [Inscription List](wiki:Inscription List) gives each.

## How inscriptions work

- A value is rolled once and never changes. Upgrading the item keeps everything.
- Only equipped gear counts, not a weapon on your back.
- The same effect on several pieces adds up, to a cap on the total across everything you wear: for example move speed 15%, attack speed 15%, chance to avoid a hit 15%, leech 5%, critical hit chance 25%, damage taken 50% less per damage type, stamina, eitr and health costs 30% less, cooldowns 30% shorter. The [Inscription List](wiki:Inscription List) gives every cap; `ecraft stats` shows your totals.
- **Health-critical** means at or below 30% health; "while health-critical" inscriptions work only then. Valhalla's Edge raises the line, up to 50%.
- One item never has the same inscription twice or two of one group (listed on the [Inscription List](wiki:Inscription List)).
- An inscription the server switches off stays on the item greyed as `(dormant)`. It does nothing but still takes a place until Recasting rerolls it, or Cleansing or a Serpent reroll clears it.

## How magic items look

- Names in the rarity colour: tooltips, the ground, pickup messages, item and armour stands.
- The tooltip lists prefixes first, then suffixes, each with its tier, for example `+14% armor  T4`. With `Tooltip detail` on Full it also shows value ranges and the item's class and level (`Swords · item level 4`).
- Magic items on the ground glow in their rarity colour, with a soft beam of light and rising sparks.
- The workbench upgrade tab shows the item's inscriptions.
