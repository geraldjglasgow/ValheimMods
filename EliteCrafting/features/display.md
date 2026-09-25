# EliteCrafting - specification: Display

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **everything a player sees of a magic item**: rarity-colored names, the tooltip block, the per-player
display preferences, and the ground glow on dropped magic items. The rarity palette itself is data (`rarity.md`,
edited in the economy YAML); what this file owns is where and how it is drawn.

**Status: Phase 1 and Phase 2 built, not tested in game** (2026-09-24). Phase 2 added the crafting panel's upgrade tab and item / armor stand hovers (DECISIONS.md IMP-121-124); slot borders are Phase 3.

---

# 1. The rule

**Display is drawn locally from replicated item data plus local preferences.** Nothing about display is ever sent
over the network. Two players looking at one item may correctly see different amounts of detail; they always see the
same rarity, affixes and values, because those come from the item and the synced YAML.

The **palette** (rarity colors, `glow` flags) is gameplay-adjacent data in the synced rarity definitions, so every
player agrees what "orange" means on a server. How much detail a player wants, and whether their screen glows, is
theirs (section 4).

---

# 2. Rarity-colored names

The game's item name (`m_shared.m_name`) is shared by every copy of an item and cannot carry per-item color, so the
mod colors it **at each place the name is drawn**, wrapping it in `<color=#RRGGBB>...</color>` with the rarity's
color. Common and non-magic items are left exactly as vanilla draws them.

| Where | Hook (verified 2026-09-23) | Phase |
|---|---|---|
| Inventory and container grid tooltip title | prefix on `InventoryGrid.CreateItemTooltip`, **returning false**: one `tooltip.Set(coloredTopic, item.GetTooltip(), anchor)`. Never a postfix that sets a second time: `UITooltip.Set` only skips identical strings, so two Sets per frame would rebuild the text every frame | 1 |
| Dropped item hover text ("Bronze sword [2]\n[E] Pick up") | postfix `ItemDrop.GetHoverText`, first line only | 1 |
| Pickup message ("Bronze sword x1" at the left) | prefix on `Character.ShowPickupMessage(ItemData, int)` (the player calls it on pickup) | 1 |
| Crafting panel, upgrade tab | prefix + postfix on `InventoryGui.UpdateRecipe` (every frame): the selected upgrade target is remembered while it runs, and the tooltip postfix (section 3) appends **the target's** affix block to the recipe prefab's tooltip the game asks for (the vanilla panel describes the prefab, not the player's item). The recipe name label (`m_recipeName`) takes the rarity color through its `color` property, not a tag: the game rewrites the text every frame, and a tag would rebuild the label every frame; its own color is restored for plain recipes. Postfix on `InventoryGui.AddRecipeToList`: an upgrade entry in the list gets the rarity color the same way, dimmed to 66% when the player cannot afford it. (`SetupUpgradeItem` is never called in this game build; not hooked.) IMP-121, IMP-122 | 2 |
| Item stand hover ("Item stand ( Bronze sword )") | postfix `ItemStand.GetHoverText`: the name inside the parentheses colored, the affix block under that first line, above the key hints. The item is decoded from the stand's ZDO (`itemData`) into a private copy, again only when the ZDO's data revision changes. Guardian stones and the no-access text stay vanilla. IMP-123 | 2 |
| Armor stand slot hover | postfix `Switch.GetHoverText` for switches that are an `ArmorStand` slot (found once per switch): the slot's hover names the slot, not the item, so a colored name line and the block go under its first line; the item comes from the slot's `<index>_itemData`. IMP-123 | 2 |
| Drag ghost name, "dropped"/"broke" messages | `InventoryGui.UpdateItemDrag`, the message calls | optional, 3 |

- **Hotbar**: vanilla draws icons and durability there, no names, so there is nothing to color. Phase 3 may add a
  thin rarity-colored slot border in the grid and the hotbar (a display preference, default on; DSP-3).
- The rarity word is not added to the name ("Rare Bronze sword"): the color carries it and the tooltip spells it out
  (section 3), which also serves players who cannot tell the colors apart (`../DECISIONS.md` DSP-2).
- An **unknown rarity id** draws in the default text color (`item-data.md` section 6).
- Color strings are built once per rarity at YAML apply, not per draw.
- Every per-frame surface (grid tooltip, ground hover, crafting panel, stand hovers) remembers its last input and
  output, so a repeated frame is a content compare and no allocation; work is redone only when the vanilla text, the
  item, the rules, the words or a display setting change.
- Stones and shards carry no `ecf_` data and get no block. Their extra lines (the essence family line, the shard fuse
  line) are part of their item description, written by the Items area (IMP-102, kept by IMP-124).

---

# 3. The tooltip block

Appended **after** the vanilla tooltip by a postfix on the static
`ItemData.GetTooltip(ItemData item, int qualityLevel, bool crafting, float worldLevel, int stackOverride, bool
appending)`, only when `appending` is false (the game reuses the method for appended sub-tooltips). The result is an
unlocalized `$token` string that the caller localizes, so the block is built from `$ecf_` tokens too. Skipped
entirely for items with no `ecf_` data. That one postfix covers the grid, containers, the trader and the radial menu,
and (through the remembered upgrade target, section 2) the crafting panel's upgrade tab.

The game asks for the hovered item's tooltip **every frame**, so the built block is cached per item record, keyed on
the record, the detail level and the configuration generation; a hover costs one lookup after the first frame. The
game's shared tooltip `StringBuilder` is never touched.

Layout, top to bottom (lines that do not apply are omitted):

```
<vanilla tooltip: description, stats, durability, crafter...>

Rare                                          ← rarity line, rarity color, bold
You move 6% faster              T3            ← affix lines, one per active affix, in stored order
+35 carrying capacity           T4  [Bound]   ← bound marker on the bound affix (ecf_bound)
You fall slowly and take no fall damage  T7   ← flag affix: no value
Attacks with this weapon cost 9% less stamina  T3
20 storm_ward                   T6  (dormant) ← dormant: grey (#808080), after the active ones
Honed +7%                                     ← $ecf_ui_honed / $ecf_ui_tempered (quality.md)
Sealed: Corrupted                             ← sealed marker, red, reason word from ecf_sealed
Pending: Sigil of War                         ← $ecf_ui_pending_sigil (sigils.md), in the sigil's stone tint
  <the sigil's $ecf_stone_<id>_desc>           ← its description, grey; omitted at Compact detail
```

Details:

- **Value format**: the stored value (already rounded, `item-data.md` section 4), written with the **player's**
  culture for the decimal separator (display only; storage is always invariant). In the generic format it gets a
  sign from the effect's `polarity` (`raise` `+`, `lower` `-`) and its unit (`%` for percent, the affix's `unit`
  for flat). Flag affixes show no value.
- **Affix line text**: `$ecf_affix_<id>_line` when it exists (the catalog's behaviour sentence, "You move $1%
  faster"; `$2` is the second half of a composite value, `affixes.md`), so the sentence carries its own sign and
  unit. Otherwise the generic `$ecf_ui_affix_line` = "$1 $2" (signed value with unit, then the name from
  `$ecf_affix_<id>`), which is what owner-added and dormant affixes use. The tier is `$ecf_ui_tier` = "T$1".
- **Tier and range columns** depend on the detail preference (section 4).
- **Dormant affixes** (`item-data.md` section 6) are listed after the active ones, grey, with `$ecf_ui_dormant`.
  An affix removed from the YAML has no definition, so no polarity or unit is known: it uses its line key if a
  translation still has one, otherwise the bare stored value and the name (or the raw id). Unreadable segments show
  only at `Full` detail, as `$ecf_ui_unreadable` with the raw text.
- **Newer format**: one grey line `$ecf_ui_newer_format` under the rarity line.
- Colors in the block are the rarity color for the rarity line only; affix lines use the game's default tooltip text
  color so the block is readable on every rarity. Bound marker: the game's orange. Sealed: red `#E6262E` is the
  Mythic red, so sealed uses dark red `#B22222` instead to keep them apart. Judgement calls (DSP-5).

Detail levels (preference `Tooltip detail`):

| Level | Shows |
|---|---|
| `Compact` | rarity line; affix lines without tier; honed/tempered, sealed, sigil lines |
| `Standard` (default) | Compact + tier on each affix |
| `Full` | Standard + the tier's roll range `[4-7]` after each value + unreadable segments + the item's tier ceiling (`item-tier.md`) on the rarity line |

---

# 4. Display preferences

All in the `.cfg` section `5 - Display (per player)`, **unsynced and never locked**, on any server
(`configuration.md`). Hot-reloaded; take effect on the next tooltip and on the next glow re-evaluation.

| Key | Default | Meaning |
|---|---|---|
| `Colored item names` | `true` | Section 2 on or off. Off leaves names vanilla everywhere |
| `Tooltip detail` | `Standard` | `Compact`, `Standard`, `Full` (section 3) |
| `Show dormant affixes` | `true` | Off hides dormant lines (the effect is inert either way) |

Ground glow preferences are in section 5's own table.

---

# 5. Ground glow

**A magic item lying in the world glows in its rarity color**, so a loot drop reads from a distance (user decision
2026-09-23, Phase 1).

What glows:

- Any `ItemDrop` world object whose item has a rarity whose definition says `glow: true`. Defaults: every rarity
  except Common; **Common never glows**, whatever the YAML says (`glow: true` on the base rarity is ignored with
  a warning, `economy-yaml.md`). Creature drops, player-dropped items and items flung from a destroyed chest all
  count: the source does not matter, only the item on the ground.
- **Stones do not glow by default**; their tinted models carry them (`prefabs.md`). The preference `Glow stones`
  opts them into the same system, in their stone tint.
- Items on item stands, armor stands and in containers never glow: they are not `ItemDrop` world objects.

How:

- A Unity `Light` component, `LightType.Point`, **shadows off**, on a child object of the item, color from the
  rarity, `range` and `intensity` from the preferences. Render mode left to Unity (`Auto`).
- Attached in a postfix on `ItemDrop.Start` (by then both locally dropped and remote items hold their final data),
  only when the item's `ZNetView` is valid, which excludes the game's temporary inventory instances and our
  inactive prefab clones. The child dies with the object.
- Not the game's `LightLod` component: its light limit is shared with every torch and fire, and the cap here must
  count only our lights.
- Created **locally on each client** from the item's replicated data. No netcode, no ZDO key, nothing on the server.
  A dedicated server never creates one; the test is `SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null`
  (the game's own), because `ZNet.IsDedicated()` always returns false in this build (game notes).
- **Nearest-N cap**: only the `Glow max lights` nearest glowing items to the camera hold a live light (default
  **25**). The rest have their light disabled, not destroyed. A loot explosion must not become a light explosion.
- **Re-evaluated on a timer**, every `Glow refresh seconds` (default **1.0 s**), never per frame: the timer walks the
  game's live `ItemDrop` list (`ItemDrop.s_instances`), asks each one `ItemDrop.Load()` (a revision compare in the game; it picks up data
  that changed while the item lay on the ground), reads the cached record, and enables the nearest
  N. Candidate items are remembered between ticks so the walk touches the record cache only for new objects.
- The light is removed when the item is picked up (the object is destroyed) or its data changes to non-glowing.

| Preference (per player) | Default | Range | Meaning |
|---|---|---|---|
| `Ground glow` | `true` | on/off | Master switch |
| `Glow intensity` | `1.0` | 0-3 | Light intensity multiplier |
| `Glow range` | `2.0` m | 0.5-6 | Light radius. Small on purpose: it marks the item, not the area |
| `Glow max lights` | `25` | 0-100 | Nearest-N cap. 0 is the same as off |
| `Glow refresh seconds` | `1.0` | 0.25-5 | Timer for the nearest-N re-evaluation |
| `Glow stones` | `false` | on/off | Stones glow in their tint too |

All numbers are judgement calls, to be tuned in play (DSP-4; every rarity glows the same size). Phase 3 adds an
optional soft loot-beam variant under the same cap.

---

# 6. Multiplayer

- Every visual in this file is drawn on the viewing client from data it already has: the item's replicated custom
  data and the synced rarity definitions. Nothing is transmitted.
- Pre-rolled drops carry their data from their first moment (`multiplayer.md` section 2); the glow timer's `Load()`
  call covers any item whose data changes while it lies on the ground.
- Two players seeing different detail levels, or one seeing no glow, is correct and not a bug.

---

# 7. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Display: DSP-1 to DSP-5; the glow itself is the
user's, U-3).
