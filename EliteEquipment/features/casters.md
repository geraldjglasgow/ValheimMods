# Casters: wands, robes and spells

A healer, buffer and caster line for EliteEquipment, from the Meadows to the Ashlands. Designed with the user on
2026-10-09, not built. The shape and names are decided; the numbers are to be tuned in game.

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
| 4 | Frostsilver Wand | Forge | Silver 4, Crystal 2, Freeze gland 3 | 45 |
| 5 | Blackmetal Wand | Forge | Black metal 4, Fine wood 4, Needle 5 | 60 |
| 6 | Yggdrasil Wand | Galdr table | Yggdrasil wood 6, Refined eitr 4, Royal jelly 3 | 75 |
| 7 | Ashwood Wand | Galdr table | Ashwood 6, Flametal 3, Celestial feather 2 | 90 |

Every wand holds 3 spells; a better wand makes them stronger, never more.

- **Bolt** (middle mouse, the game's secondary attack): pierce plus spirit, about 40% of the tier's one-handed sword.
  Spirit makes it good against skeletons, ghosts and draugr; living creatures ignore the spirit part. A healer can
  defend themselves but is not the damage dealer.
- Upgrades like a game weapon (4 qualities); each quality adds a tenth of the heal power.
- **Focus** (left hand, a shield-slot item): little block, + eitr regen and + heal power. One per biome. The hand can
  still take a shield or a torch instead.

| Tier | Focus | Station | Materials |
| --- | --- | --- | --- |
| 1 | Raven Fetish | Workbench | Feathers 4, Leather scraps 3, Flint 2 |
| 2 | Bone Charm | Forge | Bone fragments 8, Bronze 1, Greydwarf eye 4 |
| 3 | Root Totem | Forge | Root 3, Ancient bark 4, Iron 1 |
| 4 | Silver Talisman | Forge | Silver 3, Crystal 2, Wolf fang 2 |
| 5 | Sun Disc | Forge | Black metal 2, Linen 4, Needle 3 |
| 6 | Eitr Lantern | Galdr table | Refined eitr 4, Yggdrasil wood 3, Black metal 2 |
| 7 | Ember Heart | Galdr table | Flametal 3, Charred bone 6, Sulfur 4 |

## Armour: the Seer's robes

Three pieces per tier (hood, robe, trousers), plus boots when `1. Boots / Separate Boots` is on (split 80/20 like the
game's leggings). Armour about 40% of the tier's heavy set; eitr as in the table above.

| Tier | Set | Materials | Set bonus (all pieces) |
| --- | --- | --- | --- |
| 1 | Deerhide Vestments | Deer hide, leather scraps, dandelion | Mend costs 25% less |
| 2 | Mossweave | Troll hide, thistle, greydwarf eye | Chants last 50% longer |
| 3 | Rootweave | Root, ancient bark, bloodbag | Poison resistant |
| 4 | Wolfwool | Wolf pelt, silver, wolf fang | Frost resistant |
| 5 | Sunweave | Linen, lox pelt, black metal | Your heals also restore 10 stamina |
| 6 | Mistweave | Linen, jotun puffs, carapace | Blood Magic +15 |
| 7 | Emberweave | Asksvin hide, flametal, celestial feather | Fire resistant, Blood Magic +20 |

## Spells

Kinds: **heal**, **chant** (a 60 s group buff on everyone within 10 m, yourself included), **ward** (protects one
friend), **strike** (damage or control), **utility**. A caster keeps at most two chants running; a third ends the
oldest. The same chant from two casters refreshes instead of stacking (the game's rule for one status effect).

Each tier has crafted spells and one boss spell. Crafted spells are made at the tier's station; the boss spell drops
from the boss, one per player present (the game's `m_onePerPlayer` drop).

| # | Spell | Tier | Kind | What it does | Eitr | Learned from |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Mend | 1 | heal | Heals one friend (never yourself) by the wand's heal power | 8 | the first wand you craft |
| 2 | Hearthsong | 1 | chant | Health regen x1.5 | 15 | crafted |
| 3 | Stag's Swiftness | 1 | chant | +10% movement, running costs 20% less stamina | 15 | Eikthyr |
| 4 | Wisplight | 2 | utility | A wisp follows you for 5 min and lights crypts and caves | 10 | crafted |
| 5 | Spirit Lash | 2 | strike | Fast spirit bolt, strong against the undead | 10 | crafted |
| 6 | Bark Skin | 2 | chant | +armour | 20 | crafted |
| 7 | Rootbind | 2 | strike | Roots hold enemies in 4 m for 5 s (bosses only slowed) | 20 | The Elder |
| 8 | Cleanse | 3 | heal | Removes poison, burning, frost, wet and smoke from one friend; small heal | 12 | crafted |
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

Every spell's power reads the wielded wand's heal power and the caster's skill. Fixed numbers (the chant percentages)
never scale, so stacking casters cannot run away with them.

## How spells are chosen

1. **Learn.** A spell is an item: a strip of hide with burnt runes (not a stone, so it never looks like
   EliteCrafting's rune tablets). Using it from the inventory teaches the spell to the character for good; a spell
   already known cannot be used, so the item can be handed to a friend. Known spells are saved on the character.
2. **Prepare.** A wand holds 3 spells ready, at every tier: the Meadows teach exactly three, and from the Black Forest
   on there are always more known spells than slots. With the inventory open and a wand in hand, a Spellbook button (a copy of the game's button) opens the Spellbook
   window, anywhere: the player clicks a slot, then a known spell (built 2026-10-09: click, not drag). The loadout is saved on the wand itself, so
   two wands on the hotbar are two loadouts: swap the wand, swap the spells. A new wand starts with Mend in its first
   slot.
3. **Pick.** While a wand is drawn, a small spell bar sits under the hotbar (the game's hotbar look): the wand's 3
   spells, the selected one framed. **Left Alt + mouse wheel** cycles through them, wrapping round. Left Alt because the
   game binds nothing to it (Shift is its run key, so Shift + wheel would start a run while moving); not Alt + 1-5,
   which many mods take, PackPanel among them. While Left Alt is held with a wand drawn, the camera does not zoom: the
   wheel reads 0 to the rest of the game (`GameCamera.UpdateCamera` zooms by `ZInput.GetMouseScrollWheel`), as
   EarthWright's `Brush/ScrollInput` already does for its brush; that gate moves into a library so both mods share it.
   The selected spell's icon also shows on the wand's hotbar slot. No HaloMenu.
4. **Cast.** Left mouse casts the selected spell. Heals and wards go to the friend highlighted within 30 m (aiming
   inside a bubble round them is enough; a marker shows the healer who), locked on as the cast starts and flying to
   them; the cast turns your character to them, wherever the camera looks; a heal never targets yourself (with no friend highlighted it does not cast); chants hit everyone within 10 m; strikes fly where you
   aim. Middle mouse fires the wand's bolt.

Friends are other players and your tames, so a healer can keep the wolves and the lox alive. With Party installed,
the crosshair prefers party members.

## Multiplayer

- Heals and buffs are the game's own status effects (`SE_Stats` health, stamina, eitr, regen and resistances;
  `SE_Shield` for Ward). `SEMan.AddStatusEffect` on a character another peer owns is sent to that owner, so they work
  on a dedicated server for players and tames alike. The status effects are registered in `ObjectDB` on every peer.
- A cast is the game's `Attack` with the selected spell's settings put in for that swing: its animation, eitr cost
  and projectile or area are replicated the way a game staff's are.
- Max eitr is computed by the player's own client, as the game does for food.
- Known spells: player custom data `EliteEquipment.spells`. Loadout and selected spell: item custom data
  `EliteEquipment.loadout` on each wand. No RPC of our own for casting.
- The mod is needed on every client (shared prefabs and status effects) and on the server (synced switch).

## Works with

- **Party**: party members first for the crosshair.
- **EliteCrafting**: wands fall into its staff classes today through their skill; later a `wand` class with healing
  inscriptions (+heal power, -spell cost, +chant duration).
- **Boots**: the robes' boots, when Separate Boots is on.

The Spellbook window and the spell bar use the game's own UI look (only PackPanel restyles UI).

## Settings

One switch, `3. Casters / Wands and Spells` (off by default, user 2026-10-09; synced, locked by `Lock Configuration`). All numbers above
are constants in code, named in the setting's description where a player would ask.

## Names

Prefabs `EE_Wand_<Tier>`, `EE_Focus_<Tier>`, `EE_Seer_<Tier>_<Piece>`, spell items `EE_Spell_<Key>`, status effects
`EE_SE_<Key>`, words `$ee_wand_*`, `$ee_spell_*`, `$ee_seer_*`.

## Assets (ValheimAssets, until each part ships)

7 wands, 7 foci, 7 robe sets (hood, robe, trousers, boots), one spell item model in 7 tier colours, 22 spell icons, and
effects: heal glow, chant ring, ward bubble, wisp, roots, frost cone, sanctuary dome. Game effects first (the healing
mead, the Staff of Protection bubble, the wisp, the frost staff) through LocalEffects; sounds from the game's clips.
Benches in AssetLab until release.

## Phases

1. Meadows to Swamp: gear eitr, three wands, three foci, three robe sets, spells 1-10, the Spellbook window, the spell
   bar and the wheel gate library. Play it through before going on.
2. Mountains and Plains: spells 11-16, two wands, Silver Talisman, Sun Disc, two sets.
3. Mistlands and Ashlands: spells 17-22, the last wands, Eitr Lantern, Ember Heart and sets, tuned beside the game's
   staves and eitr food.

## Open decisions

None.

Decided (user, 2026-10-09): spells are prepared anywhere; no HaloMenu; a wand holds 3 spells at every tier; Left Alt +
wheel picks one (not Alt + 1-5, which PackPanel and other mods use) and the camera does not zoom meanwhile; the game's Blood Magic and Elemental Magic, skill enhancements left to another
mod; one focus per biome; the names stand, the foci's included.

## Meadows: the build contract (2026-10-09)

The user: "everything will be custom made, I want to start with medows first completely". Built as an AssetLab bench
(`AssetLab/AssetLab/Casters/`, namespace `EliteEquipment.Casters`, EliteEquipment's section `3. Casters`; the bucklers
bench is section 2) until the user releases it in EliteEquipment. Models, icons and effects are made in
`..\ValheimAssets`; sounds are the game's own clips. The names below must not move: the bench code is written from them
at the same time as the models.

### Bundles and what each holds

Every model: metres, Y up, front +Z in Unity; children named as below; a 64 x 64 icon sprite named `<prefab>_icon`
unless listed otherwise; `.windows` and `.linux` bundles in the asset's `out/bundles`.

| Bundle | Workshop folder | Prefabs and sprites |
| --- | --- | --- |
| `ee_wand_meadows` | `Assets/Weapons/Wands/ee_wand_meadows` | `ee_wand_meadows` (the Gnarled Wand: `model`, `glow_tip`, points `grip` and `tip`), `ee_wand_meadows_icon` |
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
- **Deerhide Vestments**: the game's leather set is the base (`HelmetLeather`, `ArmorLeatherChest`, `ArmorLeatherLegs`):
  a deer-hide hood, a hip-length robe-tunic with long sleeves and a belt, trousers ending in wrapped feet. Body paint in
  the body's layout (`_ChestTex`, `_LegsTex`, codex `models/armour.md`) plus `attach_skin` meshes on the Player armature;
  the hood an `attach` on `Helmet_attach` or an `attach_skin`. The foot wraps are separate meshes named `boots*`, so they
  can become their own boots later. Male and female bodies. A folded dropped model per piece.
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
| Gnarled Wand | `EE_Wand_Meadows` | `Club` | Wood 5, Resin 3, Feathers 2, Dandelion 3 | heal power 15 (+1.5 a quality), +10 eitr while wielded; bolt pierce 6 + spirit 6, 10 stamina |
| Raven Fetish | `EE_Focus_Meadows` | `ShieldWood` | Feathers 4, Leather scraps 3, Flint 2 | block 2, eitr regen +10%, heal power +3 |
| Deerhide Hood | `EE_Seer_Meadows_Hood` | `HelmetLeather` | Deer hide 2, Leather scraps 2, Dandelion 2 | armour 1, +5 eitr |
| Deerhide Robe | `EE_Seer_Meadows_Robe` | `ArmorLeatherChest` | Deer hide 4, Leather scraps 3, Dandelion 3 | armour 1, +5 eitr |
| Deerhide Trousers | `EE_Seer_Meadows_Trousers` | `ArmorLeatherLegs` | Deer hide 3, Leather scraps 3, Dandelion 2 | armour 1, +5 eitr |
| Spell: Hearthsong | `EE_Spell_Hearthsong` | `Raspberry` | Raspberries 5, Honey 2, Dandelion 3 | teaches Hearthsong |
| Spell: Stag's Swiftness | `EE_Spell_Swiftness` | `Raspberry` | none: Eikthyr drops one per player | teaches Stag's Swiftness |

Set bonus (all three pieces): Mend costs 25% less. Mend is known by every character from the start.

| Spell | Eitr | Effect | Status effect |
| --- | --- | --- | --- |
| Mend | 8 | heals the highlighted friend within 30 m (never yourself) by the heal power | none |
| Hearthsong | 15 | everyone within 10 m: health regen x1.5 for 60 s | `EE_SE_Hearthsong` |
| Stag's Swiftness | 15 | everyone within 10 m: +10% movement, running costs 20% less stamina, 60 s | `EE_SE_Swiftness` |

The two-chant limit has nothing to limit in the Meadows (two chants exist) and is left for the Black Forest.
