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
player agrees what "blue" means on a server. How much detail a player wants, and whether their screen glows, is
theirs (section 4).

---

# 2. Rarity-colored names

The game's item name (`m_shared.m_name`) is shared by every copy of an item and cannot carry per-item color, so the
mod colors it **at each place the name is drawn**, wrapping it in `<color=#RRGGBB>...</color>` with the rarity's
color. Normal and non-magic items are left exactly as vanilla draws them.

| Where | Hook (verified 2026-09-23) | Phase |
|---|---|---|
| Inventory and container grid tooltip title | prefix on `InventoryGrid.CreateItemTooltip`, **returning false**: one `tooltip.Set(coloredTopic, item.GetTooltip(), anchor)`. Never a postfix that sets a second time: `UITooltip.Set` only skips identical strings, so two Sets per frame would rebuild the text every frame | 1 |
| Dropped item hover text ("Bronze sword [2]\n[E] Pick up") | postfix `ItemDrop.GetHoverText`, first line only | 1 |
| Pickup message ("Bronze sword x1" at the left) | prefix on `Character.ShowPickupMessage(ItemData, int)` (the player calls it on pickup) | 1 |
| Crafting panel, upgrade tab | prefix + postfix on `InventoryGui.UpdateRecipe` (every frame): the selected upgrade target is remembered while it runs, and the tooltip postfix (section 3) appends **the target's** affix block to the recipe prefab's tooltip the game asks for (the vanilla panel describes the prefab, not the player's item). The recipe name label (`m_recipeName`) takes the rarity color through its `color` property, not a tag: the game rewrites the text every frame, and a tag would rebuild the label every frame; its own color is restored for plain recipes. Postfix on `InventoryGui.AddRecipeToList`: an upgrade entry in the list gets the rarity color the same way, dimmed to 66% when the player cannot afford it. (`SetupUpgradeItem` is never called in this game build; not hooked.) IMP-121, IMP-122 | 2 |
| Item stand hover ("Item stand ( Bronze sword )") | postfix `ItemStand.GetHoverText`: the name inside the parentheses colored, the affix block under that first line, above the key hints. The item is decoded from the stand's ZDO (`itemData`) into a private copy, again only when the ZDO's data revision changes. Guardian stones and the no-access text stay vanilla. IMP-123 | 2 |
| Armor stand slot hover | postfix `Switch.GetHoverText` for switches that are an `ArmorStand` slot (found once per switch): the slot's hover names the slot, not the item, so a colored name line and the block go under its first line; the item comes from the slot's `<index>_itemData`. IMP-123 | 2 |
| Drag ghost name, "dropped"/"broke" messages | `InventoryGui.UpdateItemDrag`, the message calls | optional, 3 |

- **Hotbar**: vanilla draws icons and durability there, no names, so there is nothing to color; the icon backdrop
  (below) marks the rarity there instead.
- The rarity word is not added to the name ("Rare Bronze sword"): the color carries it and the tooltip spells it out
  (section 3), which also serves players who cannot tell the colors apart (`../DECISIONS.md` DSP-2).
- An **unknown rarity id** draws in the default text color (`item-data.md` section 6).
- Color strings are built once per rarity at YAML apply, not per draw.
- Every per-frame surface (grid tooltip, ground hover, crafting panel, stand hovers) remembers its last input and
  output, so a repeated frame is a content compare and no allocation; work is redone only when the vanilla text, the
  item, the rules, the words or a display setting change.
- Runes carry no `ecf_` data and get no block; what a rune does is its item description (`$ecf_stone_<id>_desc`),
  written by the Items area.

**Icon backdrop** (user decision 2026-10-05: "when you show the icon for a magic/rare item its always the elite
crafting icon with the background"; moved from PackPanel the same day, so it shows with or without PackPanel, and
PackPanel draws none). Behind **every** icon of a Magic or Rare item, a rounded square with a bright rim, a faint inlaid
line and a soft centre glow (the user's pick "H2" of the mockups, nothing animated; `Display/Backdrops/BackdropArt`,
painted in code) in a light tone of the rarity colour (its hue at full value, saturation x0.6: Magic `#78FF66`, Rare
`#66B4FF`). Normal, plain, unknown-rarity items and runes get none. No setting: it always shows.

| Where | Hook |
|---|---|
| Every inventory grid (player, its slots, containers, other mods' grids) | postfix `InventoryGrid.UpdateGui` |
| Hotbar | postfix `HotkeyBar.UpdateIcons` |
| Dragged item at the cursor | postfix `InventoryGui.UpdateItemDrag` (`m_dragGo`'s `icon`) |
| Crafting panel: upgrade entry in the list, the selected upgrade's icon | postfix `InventoryGui.AddRecipeToList`, postfix `InventoryGui.UpdateRecipe` (`m_recipeIcon`) |
| Radial menu items | postfix `Valheim.UI.ItemElement.Init` |
| Top-left message (picked up, removed, dropped, broke) | prefix (first) / postfix on `Character.ShowPickupMessage`, `ShowRemovedMessage`, `Humanoid.DropItem`, `DrainEquipedItemDurability` name the item; postfix `MessageHud.ShowMessage` tags the message it queued with that item's icon; postfix `UpdateMessage` shows the tagged one's backdrop and fades it with the icon over 4 s |

- One Image per icon (`ecf_backdrop`, `IconBackdrop`), a sibling of the icon just above its cell's own `bkg` (or first),
  so the equipped and queued marks and the icon lie over it; it covers the icon's rect less 3 of every 64 units a side,
  follows the icon's rect and scale, ignores layout groups, and shows only while the icon shows (enabled, active, a
  sprite, not transparent: an upgrade the player cannot afford hides its icon, and the backdrop with it).
- Not drawn: the split dialog (a magic item never stacks), food icons, discovery pop-ups (they name the item kind, not
  the item). An item Epic Loot also calls magic gets ours too (EliteCrafting ignores Epic Loot).

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
+35 carrying capacity           T4
You fall slowly and take no fall damage  T7   ← flag affix: no value
Attacks with this weapon cost 9% less stamina  T3
20 storm_ward                   T6  (dormant) ← dormant: grey (#808080), after the active ones
Sealed                                        ← sealed marker, dark red, the same word whatever sealed it
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
  color so the block is readable on every rarity. Sealed: dark red `#B22222`. Judgement calls (DSP-5).

Detail levels (preference `Tooltip detail`):

| Level | Shows |
|---|---|
| `Compact` | rarity line; affix lines without tier; sealed line |
| `Standard` (default) | Compact + tier on each affix |
| `Full` | Standard + the tier's roll range `[4-7]` after each value + unreadable segments + the item's tier ceiling (`item-tier.md`) on the rarity line |

**Long tooltips** (user request 2026-10-05: "allow a scrollbar on a tooltip if its not showing everything";
`Display/Tooltips/TooltipScroll`). A component on every game tooltip as it is made (postfix `UITooltip.OnHoverStart`)
does nothing until the text would take the tooltip past the screen. Then the text moves into a clipped viewport
(`ecf_tooltip_scroll`, the tooltip's full width, in the text's place in the layout) sized so the whole tooltip fits the
screen less 24 units above and below (at least 80 units of text); a slim bar on its right (`ecf_tooltip_bar`, track
white at 12%, thumb at 55%) shows the view, and the mouse wheel (60 units a notch) or the right stick (700 units a
second) scrolls it while it shows. A new text starts at the top. Only the item tooltip's shape (a
`VerticalLayoutGroup` holding `Topic` and `Text`) is handled. OpenKeep's `Cycle With Wheel` leaves the wheel alone
while `ecf_tooltip_bar` shows (found by name).

---

# 4. Display preferences

All in the `.cfg` section `5 - Display (per player)`, **unsynced and never locked**, on any server
(`configuration.md`). Hot-reloaded; take effect on the next tooltip and on the next glow re-evaluation.

| Key | Default | Meaning |
|---|---|---|
| `Colored item names` | `true` | Section 2 on or off. Off leaves names vanilla everywhere |
| `Tooltip detail` | `Standard` | `Compact`, `Standard`, `Full` (section 3) |
| `Show dormant inscriptions` | `true` | Off hides dormant lines (the effect is inert either way) |

Ground glow preferences are in section 5's own table.

---

# 5. Ground glow

**A magic item lying in the world glows in its rarity color**, so a loot drop reads from a distance (user decision
2026-09-23, Phase 1).

What glows:

- Any `ItemDrop` world object whose item has a rarity whose definition says `glow: true`. Defaults: Magic and Rare;
  **Normal never glows**, whatever the YAML says (`glow: true` on the base rarity is ignored with
  a warning, `economy-yaml.md`). Creature drops, player-dropped items and items flung from a destroyed chest all
  count: the source does not matter, only the item on the ground.
- **Runes do not glow by default**; their tinted models carry them (`prefabs.md`). The preference `Glow runes`
  opts them into the same system, in their tint (white for a rune without one).
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

**The loot beam** (user decision 2026-10-05: option "C" of five previewed, `../PLAN.md` Decisions log). With `Loot
beam` on, each lit item also shows a soft shaft of light about 3.7 m tall, brightest at its foot and breathing a
little, with thin wisps rising through it, a faint halo at the item and a few sparks drifting up about a metre. Our own
effect: ValheimAssets `Assets/Effects/ecf_lootglow_beam_motes` (built from `ecf_lootglow_beam` and
`ecf_lootglow_motes`), embedded as the bundle `ecf_lootglow`, dressed in the game's particle shaders at runtime
(`BundleEffects`). `Display/GlowBeams`, `GlowItem`:

- The same nearest-N cap as the light: an item over the cap has its beam switched off, not destroyed.
- A child of the item, stood upright and at normal size on every tick (a dropped item rolls; the sparks simulate in
  world space). Destroyed with the item, when it stops glowing, and when the glow or the beam is switched off.
- Colour: the effect's five systems are authored in Magic green with their own saturation and alpha (shaft and halo
  0.6 and 0.22, wisps 0.45 and 0.4, sparks between 0.2 and 0.45 at 1, pixels 0.6 at 1). The glow colour replaces the
  hue, multiplies the saturation, sets full value and keeps the alpha, so Magic gives the light tone `#78FF66` and Rare
  `#66B4FF` (the inventory backdrop's), exact for any palette colour. One recoloured copy is kept per colour, so an
  instance starts in its own colour.
- About 13 particles alive per beam in 5 systems (at most 26); 25 beams are about 125 systems and 325 particles. The
  beam's cost is its additive overdraw (three 0.45-0.6 x 3.7 m billboards). The game's own gold twinkle on the item
  stays.
- Local only, like the light: no ZNetView, nothing sent, never loaded on a headless server. A bundle that cannot load
  leaves the light alone (warning).

| Preference (per player) | Default | Range | Meaning |
|---|---|---|---|
| `Ground glow` | `true` | on/off | Master switch |
| `Glow intensity` | `1.0` | 0-3 | Light intensity multiplier |
| `Glow range` | `2.0` m | 0.5-6 | Light radius. Small on purpose: it marks the item, not the area |
| `Glow max lights` | `25` | 0-100 | Nearest-N cap. 0 is the same as off |
| `Glow refresh seconds` | `1.0` | 0.25-5 | Timer for the nearest-N re-evaluation |
| `Glow runes` | `false` | on/off | Runes glow in their tint too |
| `Loot beam` | `true` | on/off | Lit items also show the loot beam |

All numbers are judgement calls, to be tuned in play (DSP-4; every rarity glows the same size). The loot beam above
is the soft loot-beam variant this paragraph used to plan for Phase 3.

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
