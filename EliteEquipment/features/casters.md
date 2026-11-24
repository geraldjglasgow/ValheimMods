# Casters: wands, robes and spells

A healer, buffer and caster line for EliteEquipment, from the Meadows to the Ashlands. Designed with the user on
2026-10-09; the Meadows tier was built as an AssetLab bench and moved into EliteEquipment on 2026-10-12 (`EliteEquipment/src/Casters/CASTERS.md`), the
rest is not. The shape and names are decided; the numbers are to be tuned in game.

## The idea in one paragraph

Valheim has no classes, so this adds none: anyone who wields a wand is a caster. A wand is one-handed (the left hand
stays free for a focus, a shield or a torch), it fires a weak bolt of its own, and it casts spells chosen from the
ones the character has learned. Spells are learned once, from items crafted with each biome's materials or dropped
by its boss. A wand holds only 3 of them ready at once, so the choice works like choosing armour: what do I bring to
Bonemass? A spell's strength comes from the wand, not from the spell, so the
Meadows heal is still worth a slot in the Ashlands. Robes give the eitr that the game only gives from Mistlands food.

## Resource: eitr from the start

- The game gives no eitr before the Mistlands: `Player.GetTotalFoodValue` (private) starts eitr at 0 and adds only
  food eitr. A postfix there adds the eitr of the worn caster gear and the wielded wand; the game recalculates it on
  every food update, so taking a robe off lowers the bar.
- Gear eitr grows until the Plains and then stops; from the Mistlands, eitr food takes over and caster gear moves to
  eitr regen and power. The game's own staves arrive in the Mistlands too: they stay the damage line, wands the
  support line, and a caster can carry both.

| Tier | Wand | Each robe piece (3) | Total from gear | Mend costs |
| --- | --- | --- | --- | --- |
| 1 Meadows | +10 | +5 | 25 | 8 |
| 2 Black Forest | +15 | +8 | 39 | 8 |
| 3 Swamp | +20 | +10 | 50 | 8 |
| 4 Mountains | +25 | +12 | 61 | 8 |
| 5 Plains | +30 | +15 | 75 | 8 |
| 6 Mistlands | +30, regen +25% | +15, regen +15% | 75 + food | 8 |
| 7 Ashlands | +30, regen +40% | +15, regen +20% | 75 + food | 8 |

Spell costs stay the same at every tier; more gear eitr means more casts, and the wand's heal power means bigger ones.

## Eitr food before the Mistlands (agreed 2026-10-10)

The game has eitr food only from the Mistlands. The user asked for eitr food in the earlier biomes too: 1 in the Meadows,
3 in the Black Forest, 3 in the Swamp, 2 in the Mountains, 3 in the Plains, following the game's food curve.

**The curve, read from the game (2026-10-10).** Every biome has a top food number: 35 Meadows, 45 Black Forest, 55 Swamp,
65 Mountains, 75 Plains, 85 Mistlands, 95 Ashlands, 105 Deep North (its best health dish gives it in health, its best
stamina dish in stamina). The game's eitr dishes give that number in eitr, a third of it in health and a sixth in
stamina (Seeker Aspic 28 / 14 / 85, Marinated Greens 32 / 16 / 95, Fish Soup 37 / 18 / 105), about 12% more stats in
all than the biome's best normal dish, and cost more: 6 to 9 ingredients a serving. A biome's one raw eitr ingredient
gives an even third (Magecap 25 / 25 / 25, Vineberry 30 / 30 / 30, Poteitr 35 / 35 / 35); here a cooked snack takes its
place, so no game food changes (user: candied cloudberries "instead of just cloudberries").

| Biome | Food | Made from | Where | Health / stamina / eitr | Lasts | Regen |
| --- | --- | --- | --- | --- | --- | --- |
| Meadows | Dandelion Mash | Dandelion 3, Honey 1, Mushroom 1 | Workbench (no cauldron yet) | 12 / 6 / 35 | 25 min | 1 |
| Black Forest | Candied Thistle | Thistle 3, Honey 1 (makes 2) | Cauldron 1 | 15 / 15 / 15 | 15 min | 1 |
| Black Forest | Glowcap Broth | Yellow mushroom 2, Thistle 2, Carrot 1 | Cauldron 1 | 15 / 8 / 40 | 25 min | 2 |
| Black Forest | Seer's Porridge | Blueberries 4, Thistle 2, Honey 1 | Cauldron 1 | 15 / 8 / 45 | 30 min | 2 |
| Swamp | Turnip Crisps | Turnip 3, Thistle 1 (makes 2) | Cauldron 2 | 18 / 18 / 18 | 15 min | 1 |
| Swamp | Blood Draught | Bloodbag 2, Thistle 2, Turnip 1 | Cauldron 2 | 18 / 9 / 50 | 25 min | 2 |
| Swamp | Mire Stew | Turnip 3, Bloodbag 1, Yellow mushroom 1 | Cauldron 2 | 18 / 9 / 55 | 30 min | 2 |
| Mountains | Glacier Tea | Onion 2, Freeze gland 1, Honey 1, Thistle 1 | Cauldron 3 | 22 / 11 / 60 | 25 min | 2 |
| Mountains | Wolfheart Stew | Wolf meat 1, Onion 3, Freeze gland 2 | Cauldron 3 | 22 / 11 / 65 | 30 min | 2 |
| Plains | Candied Cloudberries | Cloudberries 4, Honey 1 (makes 2) | Cauldron 4 | 25 / 25 / 25 | 15 min | 1 |
| Plains | Lox Marrow Broth | Lox meat 1, Cloudberries 2, Barley flour 3 | Cauldron 4 | 25 / 12 / 70 | 25 min | 3 |
| Plains | Sunbarley Porridge | Barley flour 4, Cloudberries 3, Honey 1 | Cauldron 4 | 25 / 12 / 75 | 30 min | 3 |

Eitr a caster can carry with these (gear plus the three best eitr foods so far): 60 Meadows (its one food), 139 Black
Forest, 200 Swamp, 241 Mountains, 285 Plains, then the game's own 315 Mistlands and 340 Ashlands. A caster on three eitr
foods has little health (about 100 in the Plains against about 220 on normal food), as in the game.

The foods live in EliteEquipment, behind the casters switch (user, 2026-10-11). Still open: whether gear eitr (25 to
75) goes now that food carries eitr (the game's mage sets give none, only regen: see "Armour"). Models and icons for the twelve foods are workshop work: the Meadows' and the Black Forest's are made (`ee_food_meadows`, `ee_food_blackforest`, 2026-10-11) and benched in AssetLab.

## Skills

The game's own two magic skills, no new skill: heals, wards and chants use **Blood Magic** (the game's support school:
the Staff of Protection is Blood Magic), strikes and the wand's bolt use **Elemental Magic**. They raise and scale
with the skill the way the game's staves do. Skill books, skill mods and EliteCrafting's `staff_blood` /
`staff_elemental` item classes then work with wands without extra code. Enhancing the skills themselves is for another
mod, later (user, 2026-10-09), not part of this feature.

## Wands

| Tier | Wand | Station | Materials | Heal power |
| --- | --- | --- | --- | --- |
| 1 | Gnarled Wand | Workbench | Wood 5, Resin 3, Feathers 2, Dandelion 3 | 15 |
| 2 | Greywood Wand | Forge | Fine wood 4, Bronze 2, Greydwarf eye 6 | 25 |
| 3 | Rootbound Wand | Forge | Ancient bark 6, Iron 3, Root 2 | 35 |
| 4 | Frostsilver Wand | Forge | Fine wood 4, Silver 4, Freeze gland 3 (user, 2026-11-02: "finewood, silver, ice gland"; a new clean ice wand) | 45 |
| 5 | Blackmetal Wand | Forge | Black metal 4, Fine wood 4, Needle 5 | 60 |
| 6 | Yggdrasil Wand | Galdr table | Yggdrasil wood 6, Refined eitr 4, Royal jelly 3 | 75 |
| 7 | Ashwood Wand | Galdr table | Blackwood 6, Flametal 3, Celestial feather 2 | 90 |

Every wand holds 2 spells beside its bolt; a better wand makes them stronger, never more.

- **Bolt** (left mouse, charged while held, for eitr; see "Controls"): pierce plus spirit, about 40% of the tier's one-handed sword.
  Spirit makes it good against skeletons, ghosts and draugr; living creatures ignore the spirit part. A healer can
  defend themselves but is not the damage dealer.
- **Frostsilver Wand: ice shard volley** (user, 2026-11-02: a fan of sharp ice shards in place of its bolt, the Staff
  of Frost's own shard with its look and hit effect): a tap fires 1 shard, a full charge (3 s) 5 in a spread, 1 + round(4 x charge)
  between, like the Rootbound's roots; each shard the wand's bolt damage (frost 15 + spirit 15), frost slows.
- **Greywood Wand: thrown rock** (user, 2026-11-02: "black forest left click spell wand should be like a rock"; then
  "the charged version should be like a large fragmented rock fires and the pieces start coming apart into an attack
  cone, but it's a tight cone"): in place of its bolt a copy of the stone a Greydwarf throws, blunt 7 + spirit 7 (the
  pierce made blunt), 4 eitr a cast as the bolt; the charge's +30% and up to +30 push (a little knockback).
  2026-11-02, second version (user: "more greenish aspects ... thrown like a spell and shoot straight ahead ... shoot the
  large rock for 5 meters, then break up ... into like 15 pieces and keep going forward ... keep a tight cone"): the
  charge is a mossy green glow with stone chips (`ee_fx_charge_rock`) with a real stone in it growing with the charge to
  the size it will throw; the rock flies straight at the crosshair (no gravity) with a dark green trail on it and on every
  piece (`ee_fx_rock_trail`, on every machine); a tap throws one stone; from 0.15 of a charge the rock (up to 2.5 times)
  breaks 5 m out in a burst (`ee_fx_rock_break`, the game's rock crack) into 1 + round(14 x charge) pieces (3 to 15),
  0.45 times the stone, flying on in a 4 degree cone, the first dead on the crosshair, each 1.5 / pieces of the rock's hit
  (all 15 landing 1.95 times a tap). 2026-11-03 (user: "a little more proportional to how long it took to charge the
  shot ... shouldn't look like some uniform pre planned distribution"): round(15 x charge) pieces, at least 2 (five a
  second held), breaking 4.5 to 5.5 m out, each with a random heading in a 4.5 degree cone, a random speed (give or take
  15%) and a random size (0.3 to 0.6 times the stone), its share of the hit by its size.
- **Ashwood Wand: firebolt** (user, 2026-11-02: "need ashlands wand with firebolt as main lmb"): in place of its bolt a
  copy of the Staff of Embers' fireball, fire 27 + spirit 27 (40% of the Nidhogg sword's 135; fire burns by the game's
  rule), 4 eitr a cast as the bolt. A tap throws one half the staff's size, a single-target bolt; a charge makes it
  bigger, up to the staff's own size at full charge (3 s), with the charge's +30%, and from 90% of a full charge it
  explodes where it lands (2 m, the same hit, the caster's enemies only, the staff's own explosion). +30 eitr and +40%
  eitr regen while wielded; heal power 90.
- Upgrades like a game weapon (4 qualities); each quality adds a tenth of the heal power.
- **Spellbook** (left hand, a shield-slot item; replaced the focus on 2026-11-02, see "The spellbook"): little block,
  + eitr regen and + heal power, and the spell pages. Three so far: the Meadows' Leatherbound Spellbook (2 pages), the
  Swamp's Barkbound Grimoire (4) and the Ashlands' Cinderbound Tome (8).
  The foci (Raven Fetish to Ember Heart) are dropped; the Raven Fetish, built and released, was removed.

## Armour: the Seer's robes

Three pieces per tier (hood, robe, trousers), plus boots when `1. Boots / Separate Boots` is on (split 80/20 like the
game's leggings). Armour follows the game's own mage sets (user, 2026-10-11: "follow pattern in late game that is
established"), read from the game: the Eitr-weave has half the Carapace's armour (16 to 32), the Embla half the
Flametal's (19 to 38), both +2 per level like every set, and -2% movement on the robe and trousers where the heavy set
has -5% (hoods none). So each piece has half the biome's heavy armour, +2 per level:

| Tier | Heavy set (a piece) | Seer's piece | Movement (robe, trousers) |
| --- | --- | --- | --- |
| 1 Meadows | Leather 2 | 1 | none (leather has none) |
| 2 Black Forest | Bronze 8 | 4 | -2% |
| 3 Swamp | Iron 14 | 7 | -2% |
| 4 Mountains | Wolf 20 | 10 | -2% |
| 5 Plains | Padded 26 | 13 | -2% |
| 6 Mistlands | Carapace 32 | 16 | -2% |
| 7 Ashlands | Flametal 38 | 19 | -2% |
| 8 Deep North | Deep North heavy 44 | 22 | -2% |

The game's mage sets give no max eitr at all, only eitr regen (Eitr-weave hood +20%, robe +40%, trousers +40%; Embla
+30%, +50%, +50%); every point of max eitr comes from food. Eitr as in the table above until the user decides.

| Tier | Set | Materials | Set bonus (all pieces) |
| --- | --- | --- | --- |
| 1 | Deerhide Vestments | Deer hide, leather scraps, dandelion | Mend costs 25% less |
| 2 | Mossweave | Troll hide, thistle, greydwarf eye | Chants last 50% longer |
| 3 | Rootweave | Root, ancient bark, bloodbag | Poison resistant |
| 4 | Wolfwool | Wolf pelt, silver, wolf fang | Frost resistant |
| 5 | Sunweave | Linen, lox pelt, black metal | Your heals also restore 10 stamina |
| 6 | Mistweave | Linen, jotun puffs, carapace | Blood Magic +15 |
| 7 | Emberweave | Asksvin hide, flametal, celestial feather | Fire resistant, Blood Magic +20 |
| 8 | Rimegild | Norn thread, seal hide, moose sinew | Frost resistant, Blood Magic +25 |

Rimegild (2026-11-02): the user asked for "a brand new set created for the deep north", "really cool", in the colours of
a reference picture (a RuneScape mage outfit; colours and style only): an ice-white robe with cobalt side panels, a
pearlescent silver breastplate and pauldrons, plum leather straps and belt, a tabard of silver links, a cobalt-and-white
wizard hat; no gold. Workshop asset `ee_seer_deepnorth` (ValheimAssets `Assets/Gear/SeerRobes`). Its numbers are a step
above the Emberweave: half the Deep North heavy set's 44, +15 eitr a piece (the table above stays at +15 from the
Plains), frost resistant for the cold biome and Blood Magic +25; crafted beside the game's Deep North mage set (the
Caller's), at its station and level.

## Spells

Kinds: **heal**, **chant** (a 60 s group buff on everyone within 10 m, yourself included), **ward** (protects one
friend), **strike** (damage or control), **utility**. A caster keeps at most two chants running; a third ends the
oldest. The same chant from two casters refreshes instead of stacking (the game's rule for one status effect).

Since 2026-11-02 every spell is a page found in the world, never crafted (see "The spellbook"): the boss spell drops
from the boss, one per player present; the others from elite creatures, dungeon chests and rare drops. The "Learned
from" column below says "crafted" for those (the plan before pages).

| # | Spell | Tier | Kind | What it does | Eitr | Learned from |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Mend | 1 | heal | Heals the highlighted friend, or yourself with none, by the wand's heal power | 8 | the first wand you craft |
| 2 | Hearthsong | 1 | chant | Health regen x1.5 | 15 | crafted |
| 3 | Stag's Swiftness | 1 | chant | +10% movement, running costs 20% less stamina | 15 | Eikthyr |
| 4 | Wisplight | 2 | utility | A wisp follows you for 5 min and lights crypts and caves | 10 | crafted |
| 5 | Spirit Lash | 2 | strike | Fast spirit bolt, strong against the undead | 10 | crafted |
| 6 | Bark Skin | 2 | chant | +armour | 20 | crafted |
| 7 | Elder's Volley | 2 | strike | 6 fast seed projectiles from the wand toward the aim over ~0.6 s, the Elder's own shot (user, 2026-11-02: "the elder's attack where things shoot fast out of his hand"); each about 60% of the wand's bolt | 20 | The Elder (replaced Rootbind) |
| 8 | Cleanse | 3 | heal | Removes every harmful status effect (poison, burning, frost, wet, smoke, tar and any other the game marks harmful) from the marked friend or yourself, and a small heal (user, 2026-11-02: "removes all debuffs from ally or self"); Mend's reach motion, its own pale water-blue charge and a washing splash. Built 2026-11-02 (`Cleanse`, `CleanseEffect`, `CleanseList`; src/Casters/CASTERS.md, "Cleanse"): the game's harmful effects by name (Burning, Spirit, Poison, Frost, Lightning, Smoked, Tared, Wet, Slimed, Cold, Freezing, Harpooned, the three Immobilized), our mods' published ones and any `SE_Burning`/`SE_Poison`/`SE_Frost`/`SE_Smoke`/`SE_Harpooned`; nothing good touched; heal 0.2 of the wand's heal power; lands at once (no flight), carried to the target's owner by a status effect the game routes there | 12 | loot page (Swamp) |
| 9 | Eir's Spring | 3 | heal | A 4 m circle that heals everyone inside every second for 8 s | 25 | crafted |
| 10 | Purity Chant | 3 | chant | Poison resistant | 20 | Bonemass |
| 11 | Hearthfire Chant | 4 | chant | Frost resistant, no Freezing, for 120 s | 20 | crafted |
| 12 | Ward | 4 | ward | Absorbs damage on one friend for 20 s (2x heal power) | 20 | crafted |
| 13 | Moder's Breath | 4 | strike | Frost cone that damages and slows | 25 | Moder |
| 14 | War Chant | 5 | chant | +10% damage | 25 | crafted |
| 15 | Second Wind | 5 | heal | Restores 50 stamina to everyone within 10 m | 20 | crafted |
| 16 | Valkyrie's Grace | 5 | ward | The next lethal blow within 60 s leaves the friend at 1 health; once per 5 min per friend | 40 | Yagluth |
| 17 | Chain Mend | 6 | heal | Mend that jumps to up to 4 friends, 20% less each jump | 20 | crafted |
| 18 | Eitr Font | 6 | chant | Eitr regen +50%, and 20 eitr at once to each friend | 20 | crafted |
| 19 | Sanctuary | 6 | ward | A 6 m dome for 10 s; friends inside take 30% less damage; 2 min cooldown | 40 | The Queen |
| 20 | Ashward | 7 | chant | Fire resistant; puts out Burning | 20 | crafted |
| 21 | Renewal | 7 | heal | Heals everyone within 15 m over 10 s (3x heal power each) | 35 | crafted |
| 22 | Allfather's Hymn | 7 | heal | Everyone within 20 m: half their health back, cleansed, chants refreshed; 5 min cooldown | 50 | Fader |

Built so far (2026-11-02): Mend, Hearthsong, Stag's Swiftness and Elder's Volley (src/Casters/CASTERS.md, "Elder's
Volley": the Elder's `gd_king_shoot` seed `gdking_root_projectile` copied as `EE_VolleySeed`; 6 seeds 0.1 s apart from
the wand's tip, up to 1.5 degrees of spread, 30 m/s, each 60% of the bolt's damage in the wand's types; a strike's cost
is cut by Elemental Magic, the skill its hits raise).

Every spell's power reads the wielded wand's heal power and the caster's skill. Fixed numbers (the chant percentages)
never scale, so stacking casters cannot run away with them.

## The spellbook (user, 2026-11-02)

The user: "a left handed item that can be equipped when you have a wand in the main hand ... a book ... start with
meadows, leatherbound spellbook ... spells obtained from bosses, elite creatures, or dungeons, or even random drops.
Spells will be 'spell pages' that you add to the book." Then picked: the book replaces the focus; without a book a
wand fires its bolt only; pages can be taken out again; pages drop from all four sources.

- **The book.** Leatherbound Spellbook, `EE_Book_Meadows` (bundle `ee_book_meadows`), a copy of `ShieldWood` like the
  focus was: left hand, beside any one-handed weapon. Workbench 1: Deer hide 3 (+1), Leather scraps 4 (+2), Resin 2
  (+1), Feathers 2 (+1). The focus's stats: block 2 (+1 a level), parry bonus 1.5x, eitr regen +10%, wand heal power +3;
  weight 1, durability 100. Holds 2 pages (4 until the user's tiers below; quality adds none).
- **More books, 2, 4, 8** (user, 2026-11-02: "give me two more books, one for swamp and another for ashlands, first book
  holds 2 spells, then 4, then 8"). Each a copy of `ShieldWood` with its own model (`ee_book_<tier>` closed,
  `ee_book_<tier>_open` open; the wood shield's look until its bundle is in):

  | Book | Pages | Block (+1 a level) | Eitr regen | Wand heal power | Crafted | Cost (per level) |
  | --- | --- | --- | --- | --- | --- | --- |
  | Leatherbound Spellbook, `EE_Book_Meadows` | 2 | 2 | +10% | +3 | workbench 1, beside the wood shield | Deer hide 3 (+1), Leather scraps 4 (+2), Resin 2 (+1), Feathers 2 (+1) |
  | Barkbound Grimoire, `EE_Book_Swamp` | 4 | 4 | +15% | +6 | forge 2, beside the banded shield | Ancient bark 4 (+2), Root 3 (+1), Iron 2 (+1) |
  | Cinderbound Tome, `EE_Book_Ashlands` | 8 | 8 | +25% | +12 | galdr table 2, beside Dundr | Asksvin hide 4 (+2), Flametal 3 (+1), Charred bone 4 (+2) |

  Names and stations are my calls. A book holding more pages than its room (a Leatherbound filled to 3 or 4 before
  2026-11-02) keeps them all, all castable, and takes no new page until it is under its room; the Spellbook window
  shows "4 / 2 pages". No page is ever removed for it.
- **Held open** (user, 2026-11-02: "the character hand open with the book binder in the palm of his hand and the book is
  open"): while a spellbook is in the left hand, the left forearm comes forward, the hand open and palm up, the book
  lying OPEN on the palm with its spine in it, at about waist-to-chest height in front-left, like a seer reading while
  casting. Every player sees it on every other player. On the ground, on the back, on an item stand and as the icon the
  book stays closed. The shield hold (the focus's) is not used for the book any more, except where the pose gives the
  arm back to the game (blended over 0.2 s): dead, swimming, in bed, a chair or throne, a ship's controls, a saddle, an
  emote, the dodge roll, building, and (my call) blocking; there, and with `Wands and Spells` off, the book is closed in
  the shield hold. It holds through walking, running, jumping and every wand attack, charge and cast (the left arm keeps
  the book; the right arm the wand's moves). Code and numbers: `src/Casters/CASTERS.md`, "Held open".
- **Pages in the book.** Saved on the book item (custom data `EliteEquipment.pages`, spell keys), so a book dropped,
  traded or left in a chest carries its pages. A new book is empty: every spell, Mend included, is a page found in the world (user, 2026-11-02: "spellbook should not automatically come with the heal"); Mend is a loot page like Hearthsong. Each spell once per book.
- **Without a book** in the left hand the wand fires its bolt and casts nothing, Mend included. A wand's last picked
  spell stays saved on the wand; while its page is not in the held book it is shown unavailable on the spell bar.
- **Adding and taking out.** Using a page item (right click) while holding a book puts it in the book (refused when the
  book is full or already has it, with a message). The Spellbook window (a book in hand, inventory open; since
  2026-11-02 no wand needed and nothing prepared there) lists the held book's pages; each page has a button that takes it out into the inventory (refused when the inventory is full).
- **Page items.** `EE_Spell_<Key>` (the scroll prefabs kept, so scrolls already found become pages), named
  "Page: <spell>", one page model for every spell (bundle `ee_spells_meadows`, prefab `ee_spell_page`), an icon per
  spell. Mend gets a page item too (`EE_Spell_Mend`).
- **Where pages come from** (only while `Wands and Spells` is on; each source decided where the game decides its own
  drops, so every player sees the same items):
  - Bosses: each boss drops its own page, one per player present (Eikthyr: Stag's Swiftness, as before). Bosses whose
    spells are not built yet drop none.
  - Elite creatures: a creature with two stars or more, 5% to drop one page.
  - Dungeon chests: a chest in a dungeon, 10% when its loot is first made.
  - Random: any other creature a player kills, 0.5%.
  - The page is picked from the spells of the creature's or chest's biome and the biomes before it (weighted toward its
    own biome), excluding boss pages. So far the Meadows' Mend and Hearthsong drop this way; Black Forest
    dungeons give Meadows pages until the Black Forest's spells exist.
- **Gone.** The Raven Fetish (`EE_Focus_Meadows`, bundle `ee_focus_meadows`), the Hearthsong recipe, learning into the
  character. A character who had learned spells gets one page of each (Mend excepted) into the inventory once, and the
  old record is removed.

## Spell animations (user, 2026-11-02)

"These spells shouldn't just have that base white animation when charging/casting, they need their own animations that
make sense." Each spell gets its own body motion (a clip in ee_casteranim) and its own charge and release effects (in
ee_casterfx_meadows), replacing the shared white charge for spells:

| Spell | Motion | Charge | Release |
| --- | --- | --- | --- |
| Mend | the wand reached out toward the friend (toward one's own chest when healing oneself) | warm gold motes gather at the wand tip | the gold ribbon flies to them (ee_fx_mendbolt), gold motes rise on them |
| Hearthsong | the wand lifted high overhead | embers rise from the ground round the caster, a warm glow builds | wand swept down; a ring of warm firelight rolls out to 10 m with drifting embers |
| Stag's Swiftness | a quick sweeping arc, low to high | pale green leaves and wind swirl round the feet | a gust ring rolls out; a faint green stag-antler wisp dashes forward |
| Elder's Volley | the wand thrust forward and held, a small recoil per seed | green seeds spiral into the wand tip | the seeds fire (the Elder's shot) with a green burst at the tip |

Each wand's own bolt charge is tinted to its stone (Gnarled amber, Greywood mossy green with stone chips since
2026-11-02, Frostsilver icy white-blue with
frost motes, Rootbound earthy green); the white stays only as the fallback.

**Built (2026-11-02; code and patches in src/Casters/CASTERS.md, "Spell motions and effects").** A cast plays its spell's
clip on the upper body in the charged staff's cast state, as the bolt's moves do, and the spell lands at the clip's
`Hit`: Mend at 0.40 s, Hearthsong at 0.92 s, Stag's Swiftness at 0.42 s, Elder's Volley one seed per `Hit` from 0.30 to
0.80 s (the six recoils; the 0.1 s timer only without the clip). The charge shows from the cast's start to that `Hit`
(Mend and the volley at the wand's tip, the chants on the ground under the caster), the release where it lands (the
chants' rings at the caster's feet, Swiftness's wisp dashing the way the caster faces). Every player near sees the motion
and the effects: the motion goes on the bolt's own move message, the effects are drawn locally on each machine.

Judgement calls (not asked):

- Each player plays their own copy of the swapped controller, so a spell in one player's cast slot never changes the
  motion of another player casting beside them.
- The spells play at the animator's own speed; the bolt's casts keep their 0.8.
- A spell with its own effects drops the shared cast flash but keeps its cast sound; the chants' own rings keep the chant
  ring's sound; the volley's burst at the tip has no sound of its own.
- Mend has no release effect of its own: its gold bolt and motes stay its release.
- The Blackmetal Wand keeps the white bolt charge: the workshop has no tint for it and none of the four stone colours
  fits black metal. The full charge's glow and flash and the hand's stream stay white for every wand.
- With a spellbook held the stream leaves the middle of the open book's pages, not the palm under the book.

## Nine more spells (user, 2026-11-02)

The user picked these from the plan and the new ideas: "make these spells/pages". Each is a page (icons `ee_spellicon_<key>`
and `ee_spell_<key>_icon` in ee_spells_meadows), cast from the ring with its motion (a clip of ee_casteranim), a charge at
the tip or the feet and a release effect (ee_casterfx_meadows). The main session's calls are marked (mine).

| Key | Spell | Tier, source | Kind | Eitr | What it does | Motion | Charge | Release |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| spring | Eir's Spring | 3, loot | heal | 25 | a 4 m circle on the ground where the caster aims (within 20 m; at the feet with no ground) heals everyone friendly inside by a tenth of the heal power every second for 8 s | raise | ee_fx_charge_cleanse | ee_fx_cast_spring (ground ring, loops; the code ends it at 8 s) |
| ward | Ward | 4, loot | ward | 20 | the marked friend (or the caster) gets a shield absorbing 2x the heal power for 20 s (the game's own shield status, as the Staff of Protection's) | reach | ee_fx_charge_ward | ee_fx_cast_ward (bubble on the target while it lasts) |
| breath | Moder's Breath | 4, boss: Moder (Dragon) | strike | 25 | a frost cone from the wand toward the aim for ~1.3 s (one burst per Hit of the thrust clip), frost + spirit at the wand's bolt damage per burst x 0.5, slows (the game's frost); the game's own Moder breath look | thrust | ee_fx_charge_frost | the game's breath effect |
| warchant | War Chant | 5, loot | chant | 25 | everyone within 10 m, the caster too: +10% damage for 60 s | raise | ee_fx_charge_war | ee_fx_cast_warchant (ring to 10 m) |
| secondwind | Second Wind | 5, loot | heal (instant, area) | 20 | everyone within 10 m, the caster too: 50 stamina back at once | sweep | ee_fx_charge_swift | ee_fx_cast_secondwind (ring to 10 m) |
| chainmend | Chain Mend | 6, loot | heal | 20 | Mend on the marked friend (or the caster), then jumps to the nearest hurt friend within 8 m not yet healed, up to 4 in all, each 20% less | reach | ee_fx_charge_mend | ee_fx_chainmend_arc between each pair (a beam along +Z over 1 m, stretched) + ee_fx_mend on each |
| sanctuary | Sanctuary | 6, boss: The Queen (SeekerQueen) | ward | 40 | a 6 m dome at the caster's feet for 10 s; friends inside take 30% less damage; 2 min cooldown per caster | raise | ee_fx_charge_ward | ee_fx_cast_sanctuary (dome, loops; the code ends it at 10 s) |
| revive | Revive | dungeon chests only, rare (2%) | heal | 50 | aimed at a friend's tombstone whose owner died within the last 3 min and is online: that player is brought back to the tombstone with half health (the game's own teleport on their machine); 5 min cooldown per caster (mine: no prompt, a message to both) | reach | ee_fx_charge_mend | ee_fx_cast_revive (golden pillar at the grave, and on the revived player) |
| featherfall | Feather Fall | 4, loot | chant | 15 | everyone within 10 m, the caster too: no fall damage and slow falling for 30 s (the game's feather cape effect) | sweep | ee_fx_charge_feather | ee_fx_cast_featherfall (feathers swirling round each body, one shot) |

## How spells are chosen

1. **Collect.** A spell is a page in the spellbook held in the left hand (see "The spellbook"; until 2026-11-02 a scroll
   that taught the character for good).
2. **Pick on the ring** (user, 2026-11-02: "right clicking and holding brings up the halo menu and you drag the
   mouse to the spell you want (more the direction, the angle from the center selects the spell)"; "the spellbook has
   up to 4 spells right now, so you select the one to use in the moment"). Holding right mouse (past about 0.2 s)
   opens a ring at the screen centre with every page in the held book: its icon, name and eitr cost, a page you cannot
   afford shown dim. The direction of the mouse from the centre picks; letting go casts it at once. Letting go in the
   centre, or Esc, casts nothing. The ring is the HaloRing library, HaloMenu's ring taken out into ValheimModLibs and
   merged into EliteEquipment, so players need nothing extra. This reverses the 2026-10-09 "no HaloMenu" and the
   2026-10-11 two slots per wand (and the Spellbook's preparing).
3. **Again with a tap.** "You can click RMB quick to recast the last spell you selected": a quick right mouse tap
   casts the spell last picked on the ring again. It is saved on the wand itself (item custom data
   `EliteEquipment.lastSpell`), so two wands remember their own. While a wand is drawn a small spell bar sits under
   the hotbar (the game's hotbar look): one slot labelled RMB with that spell's icon and eitr cost ("Keep the spell
   icon for RMB, but get rid of RMB+LMB"). A new wand has none. (Until 2026-11-02: two slots per wand, RMB and RMB+LMB,
   set in the Spellbook; until 2026-10-11 three slots and Left Alt + mouse wheel picking one.)
4. **Cast.** Heals and wards go to the friend highlighted within 30 m (aiming
   inside a bubble round them is enough; a marker shows the healer who), locked on as the cast starts and flying to
   them; the cast turns your character to them, wherever the camera looks; with no friend highlighted a heal goes to yourself; chants hit everyone within 10 m; strikes fly where you
   aim. Left mouse fires the wand's bolt.

### Controls (user, 2026-10-11; right mouse 2026-11-02)

With a wand drawn:

| Input | Does |
| --- | --- |
| Left mouse | the bolt, for eitr, cast on the click; clicks chain a three-move combo like a sword's swings; held, the cast turns into a charge (wand forward, the other hand, or the open spellbook on it, streaming white energy into it until full charge at 3 s, the spell at the tip growing in the wand's stone colour) until let go: up to +30% damage and up to 2.5 times the size of a tapped one |
| Right mouse held | the spell ring of the held book's pages; the mouse direction picks, letting go casts it (the centre or Esc: nothing); the camera stays still, walking works, attacks wait; without a book no ring and a message |
| Right mouse tap | casts the last spell picked on the ring again |
| Right mouse held + jump | dodges (a blink with a wand), casts nothing |
| Middle mouse | casts the last spell (the game's secondary attack) |
| Dodge | a blink: up to 5 m, a little farther than the roll; costs the roll's stamina and half that in eitr; nothing a roll dodges hits you from the blink until 0.5 s after |

Casters do not block: the block button never blocks with a wand drawn.

Friends are other players and your tames, so a healer can keep the wolves and the lox alive. With Party installed,
the crosshair prefers party members.

## Multiplayer

- Heals and buffs are the game's own status effects (`SE_Stats` health, stamina, eitr, regen and resistances;
  `SE_Shield` for Ward). `SEMan.AddStatusEffect` on a character another peer owns is sent to that owner, so they work
  on a dedicated server for players and tames alike. The status effects are registered in `ObjectDB` on every peer.
- A cast is the game's `Attack` with the selected spell's settings put in for that swing: its animation, eitr cost
  and projectile or area are replicated the way a game staff's are.
- Max eitr is computed by the player's own client, as the game does for food.
- Spells: item custom data `EliteEquipment.pages` on each spellbook (until 2026-11-02 player custom data
  `EliteEquipment.spells`, now read once and turned into pages). The last picked spell: item custom data
  `EliteEquipment.lastSpell` on each wand (until 2026-11-02 two slots, `EliteEquipment.loadout`; a wand that has only the
  old key takes its first slot). The spell ring is local UI, never sent. No RPC of our own for casting.
- The mod is needed on every client (shared prefabs and status effects) and on the server (synced switch).

## Works with

- **Party**: party members first for the crosshair.
- **EliteCrafting**: wands fall into its staff classes today through their skill; later a `wand` class with healing
  inscriptions (+heal power, -spell cost, +chant duration).
- **Boots**: the robes' boots, when Separate Boots is on.

The Spellbook window and the spell bar use the game's own UI look (only PackPanel restyles UI).

## Settings

One switch, `4. Casters / Wands and Spells` (off by default, user 2026-10-09; synced, locked by `Lock Configuration`). All numbers above
are constants in code, named in the setting's description where a player would ask.

## Names

Prefabs `EE_Wand_<Tier>`, `EE_Focus_<Tier>`, `EE_Seer_<Tier>_<Piece>`, spell items `EE_Spell_<Key>`, status effects
`EE_SE_<Key>`, words `$ee_wand_*`, `$ee_spell_*`, `$ee_seer_*`.

## Assets (ValheimAssets, until each part ships)

7 wands, 7 foci, 7 robe sets (hood, robe, trousers, boots), one spell item model in 7 tier colours, 22 spell icons, and
effects: heal glow, chant ring, ward bubble, wisp, roots, frost cone, sanctuary dome. Game effects first (the healing
mead, the Staff of Protection bubble, the wisp, the frost staff) through LocalEffects; sounds from the game's clips.
Built in AssetLab, moved into EliteEquipment on 2026-10-12.

## Phases

1. Meadows to Swamp: gear eitr, three wands, three foci, three robe sets, spells 1-10, the Spellbook window and the spell
   bar. Play it through before going on.
2. Mountains and Plains: spells 11-16, two wands, Silver Talisman, Sun Disc, two sets.
3. Mistlands and Ashlands: spells 17-22, the last wands, Eitr Lantern, Ember Heart and sets, tuned beside the game's
   staves and eitr food.

## Open decisions

None.

Decided (user, 2026-11-02): the spell is picked in the moment on a HaloMenu-style ring (right mouse held, from a shared
library, every page of the held book), a right mouse tap casts the last one again, no second slot and no right mouse +
left mouse; this reverses 2026-10-09's "no HaloMenu" and 2026-10-11's two slots. Decided (user, 2026-10-09): the game's Blood Magic and Elemental Magic, skill enhancements left to another
mod; one focus per biome; the names stand, the foci's included.

## Meadows: the build contract (2026-10-09)

The user: "everything will be custom made, I want to start with medows first completely". Built as an AssetLab bench
(namespace `EliteEquipment.Casters`), moved into EliteEquipment on 2026-10-12 (`src/Casters/`, section `4. Casters`). Models, icons and effects are made in
`..\ValheimAssets`; sounds are the game's own clips. The names below must not move: the code is written from them
at the same time as the models.

### Bundles and what each holds

Every model: metres, Y up, front +Z in Unity; children named as below; a 64 x 64 icon sprite named `<prefab>_icon`
unless listed otherwise; `.windows` and `.linux` bundles in the asset's `out/bundles`.

| Bundle | Workshop folder | Prefabs and sprites |
| --- | --- | --- |
| `ee_wand_meadows` | `Assets/Weapons/Wands/ee_wand_meadows` | `ee_wand_meadows` (the Gnarled Wand: `model`, `glow_tip`, points `grip` and `tip`), `ee_wand_meadows_icon` |
| `ee_book_meadows`, `ee_book_swamp`, `ee_book_ashlands` | the workshop's books | `ee_book_<tier>` (closed: `model`, `grip` at the middle of the back), `ee_book_<tier>_open` (open: pages up +Y, spine along Z, a `palm` point), `ee_book_<tier>_icon` |
| `ee_focus_meadows` | `Assets/Gear/Foci/ee_focus_meadows` | `ee_focus_meadows` (the Raven Fetish: `model`, optional `glow*`, point `grip`), `ee_focus_meadows_icon` |
| `ee_seer_meadows` | `Assets/Gear/SeerRobes/ee_seer_meadows` | Deerhide Vestments: `ee_seer_meadows_hood`, `ee_seer_meadows_robe`, `ee_seer_meadows_trousers` (worn parts and dropped model per piece, see below), their `_icon` sprites |
| `ee_spells_meadows` | `Assets/Items/SpellScrolls/ee_spells_meadows` | `ee_spell_scroll` (the dropped spell item: `model`), item icons `ee_spell_hearthsong_icon`, `ee_spell_swiftness_icon`; spell icons `ee_spellicon_mend`, `ee_spellicon_hearthsong`, `ee_spellicon_swiftness` |
| `ee_casterfx_meadows` | `Assets/Effects/ee_casterfx_meadows` | effects `ee_fx_cast`, `ee_fx_mend`, `ee_fx_chant`, `ee_fx_bolt`, `ee_fx_bolt_hit` |

- **Gnarled Wand**: about 0.55 m, a crooked branch of pale wood, leather wrapped at the grip, a dandelion-yellow
  feather or two bound below the head, a bead of amber resin set in the head (`glow_tip`, faint warm glow). `grip`
  where the right hand closes, `tip` at the head where spells leave. Held like the game's club, one-handed.
- **Raven Fetish** (redesigned 2026-10-10, user: "a wooden base almost shield looking thing that is a little bigger than
  your hand ... attach the current focus item to the outside of it ... not circular, cut some interesting shape out of a
  circle"): a wooden hand-ward about 0.30 m across, a circle with a shape cut out of it, the raven fetish (handle, black
  feathers, flint shard, thongs) lashed to its face; held on the left hand like the game's bucklers. Front +Z, up +Y,
  `grip` at the back centre.
- **Deerhide Vestments** (v2, 2026-10-11, user: "robes of a wizard, look at deerhide to get an idea of the color
  scheme"): a deep cowl, a long robe to the lower calf split at the front with bell sleeves, a cream front panel, a black
  sash and stitched runes, built like the game's Eitr-weave robe (skirt skinned to the legs); bases `HelmetMage`,
  `ArmorMageChest`, `ArmorLeatherLegs`, recipes at the workbench beside the leather set's. Trousers end in wrapped feet. Body paint in
  the body's layout (`_ChestTex`, `_LegsTex`, codex `models/armour.md`) plus `attach_skin` meshes on the Player armature;
  the hood an `attach` on `Helmet_attach` or an `attach_skin`. The foot wraps are separate meshes named `boots*`, so they
  can become their own boots later. Male and female bodies. A folded dropped model per piece.
  Since 2026-11-02 every seer's robe skirt is cloth on the players who wear it (the game's MagicaCloth, fixed above the
  hips line at 0.92 m, pushed by capsules round the thighs, shins and feet; `src/Casters/CASTERS.md`, "The robes' skirts
  as cloth"): skinned to the shins the skirt creased at the knee, skinned to the thighs the shins and feet came through
  its back (user: "the back foot glitches through the robe bottom, also when running the legs and feet completely glitch
  through the robe bottom"), and no skin weights avoid both. The bundles are unchanged.
- **Spell scroll**: about 0.25 m, a strip of hide rolled and tied, runes burnt into it. One model for every spell; the item
  icons show the scroll with the spell's sign.
- **Effects**: `ee_fx_cast` a short flash at the wand tip; `ee_fx_mend` warm gold motes rising round the healed friend
  (about 1 s, 1 m wide); `ee_fx_chant` a ring on the ground growing to 10 m (about 1.5 s; tinted per chant in code:
  Hearthsong warm gold, Stag's Swiftness pale green); `ee_fx_bolt` the bolt in flight (a small pale mote with a short
  trail, child of the projectile); `ee_fx_bolt_hit` a small burst. Local particle effects (`BundleEffects`, game
  particle shaders).

### Items (Meadows numbers)

| Item | Prefab | Copy of | Recipe (Workbench 1) | Stats |
| --- | --- | --- | --- | --- |
| Gnarled Wand | `EE_Wand_Meadows` | `Club` | Wood 5, Resin 3, Feathers 2, Dandelion 3 | heal power 15 (+1.5 a quality), +10 eitr while wielded; bolt pierce 6 + spirit 6, 4 eitr (charged up to +30%) |
| Raven Fetish | `EE_Focus_Meadows` | `ShieldWood` | Feathers 4, Leather scraps 3, Flint 2 | block 2, eitr regen +10%, heal power +3 |
| Deerhide Hood | `EE_Seer_Meadows_Hood` | `HelmetLeather` | Deer hide 2, Leather scraps 2, Dandelion 2 | armour 1 (+2 a level), +5 eitr |
| Deerhide Robe | `EE_Seer_Meadows_Robe` | `ArmorLeatherChest` | Deer hide 4, Leather scraps 3, Dandelion 3 | armour 1 (+2 a level), +5 eitr |
| Deerhide Trousers | `EE_Seer_Meadows_Trousers` | `ArmorLeatherLegs` | Deer hide 3, Leather scraps 3, Dandelion 2 | armour 1 (+2 a level), +5 eitr |
| Spell: Hearthsong | `EE_Spell_Hearthsong` | `Raspberry` | Raspberries 5, Honey 2, Dandelion 3 | teaches Hearthsong |
| Spell: Stag's Swiftness | `EE_Spell_Swiftness` | `Raspberry` | none: Eikthyr drops one per player | teaches Stag's Swiftness |

Set bonus (all three pieces): Mend costs 25% less. (Mend was known by every character from the start until 2026-11-02; now a page.)

| Spell | Eitr | Effect | Status effect |
| --- | --- | --- | --- |
| Mend | 8 | heals the highlighted friend within 30 m, or yourself with none, by the heal power | none |
| Hearthsong | 15 | everyone within 10 m: health regen x1.5 for 60 s | `EE_SE_Hearthsong` |
| Stag's Swiftness | 15 | everyone within 10 m: +10% movement, running costs 20% less stamina, 60 s | `EE_SE_Swiftness` |

The two-chant limit has nothing to limit in the Meadows (two chants exist) and is left for the Black Forest.
