# EliteCrafting - specification: Salvage

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers **grinding**: turning a magic item the player does not want into **shards**, and fusing shards back
into stones, so that a dead drop - the wrong slot, the wrong affixes - still has value. What rarities are is
`rarity.md`; the stones shards fuse into are `stones.md`; where the item state lives is `item-data.md`; the confirm
gate reused here is `applying-stones.md` section 4. **OpenKeep's Salvage tab** is a different feature of a different
mod of ours; section 9 draws the line between them.

Numbers are defaults and all of them are configurable in the `salvage:` section of `EliteCrafting_economy*.yml`.
Every number here is a judgement call; they are collected in `../DECISIONS.md` SAL-1 to SAL-16.

**Status: built 2026-09-24, not tested in game.** Ships in **Phase 2** (0.2.0). The schema (section 7) is in
`economy-yaml.md` sections 9 and 11. Code: `Salvage/` (key, checks, grind, fuse), shard prefabs in `Items/`,
`Rules/Parsing/SalvageParser` and `SalvageChecks`. The shard tooltip line is part of the shard's item description
(DECISIONS.md IMP-102).

---

# 1. What the player sees

A Rare bow with two useless affixes sits in the inventory. The player hovers it and presses **End** while holding
**Shift**. The bow is gone; two **Shards of Ascension** are in its place. Five shards of Ascension, right-clicked,
fuse into one Stone of Ascension.

- Grinding works on **magic items only** (Uncommon and up). A Common item has nothing of ours in it; getting
  materials back from any crafted item is OpenKeep's Salvage tab, not this (section 9).
- It returns **part of the ascension stone that made the item that rarity**: an Uncommon grinds into Shards of
  Awakening, a Rare into Shards of Ascension, and so on up to Mythic and the Shards of Apotheosis. Always less than
  one stone, so grinding can never make stones out of nothing.
- It never returns vanilla materials, never returns the other stones spent on the item, and never keeps anything of
  the item.

---

# 2. The gesture

**The Salvage key over a hovered item** (`.cfg` `8 - Salvage / Salvage key`, a per-player keyboard shortcut,
default `End`).

- Active only while the inventory panel is open, on the item under the pointer **in the player's own inventory
  grid** (not a container's grid, not the hotbar HUD). Read in a postfix on `InventoryGui.Update`, only while the
  panel is visible.
- Ignored while an item is being dragged, while a dialog or split popup is open, while the console or chat has text
  focus, and while the player is teleporting. Ignored on an empty slot.
- **Confirm gate**: grinding cannot be undone, so it asks first when `salvage.confirm` is true (default), in the
  player's own mode (`.cfg` `2 - Stones / Confirm destructive stones`, `applying-stones.md` section 4):
  - **HoldShift** (default): the key alone refuses with `salvage_confirm`; Shift + key grinds.
  - **Dialog**: the key opens a yes/no dialog (`$ecf_ui_salvage_title` "Grind $1?", `$ecf_ui_salvage_body` "It
    becomes $2. This cannot be undone."); yes re-runs every check (section 3) and grinds; no or closing does nothing.
  - **Off**: grinds at once.
- **Default key `End`**: the game binds nothing to it, and no mod in this workspace does either (checked
  2026-09-24). `Delete` was the first choice but OpenKeep binds it (Trash Key; Shift + Delete is its Destroy Junk
  Key), so Shift + Delete over a magic item would have ground it *and* destroyed every junk stack (IMP-111).
- **Gamepad**: no binding in v1 (SAL-13). A keyboard shortcut is what the configuration library gives; a gamepad
  path would need its own input design.

**Why not a tab, a station or a stone** (SAL-1):

- A **crafting-panel tab** is where OpenKeep already adds its third tab; two mods fighting over the tab row is the
  collision to avoid, and a tab needs a station open.
- A **station interaction** (a grinding wheel) does not exist in vanilla - the only grindstone in the game is part of
  the windmill - and a new build piece would be a prefab and a recipe for a menu action.
- A **"grinding stone"** would make salvage cost currency, which defeats "dead drops have value".

A **station requirement** is available as a knob (`salvage.stations`, section 7) for a server that wants grinding done
at home; **empty by default**, so grinding works anywhere, including in a dungeon with a full bag (SAL-2).

---

# 3. Checks and refusals

In this order; the first failure refuses; a refusal changes nothing.

| # | Check | Refusal id |
|---|---|---|
| 1 | The `.cfg` switch `8 - Salvage / Salvage` is on (synced) | `salvage_disabled` |
| 2 | The hovered item is in the **local player's own inventory** | `salvage_not_own` |
| 3 | It is a **magic base** with our data, rarity above the base rarity (`item-data.md` section 2): not Common, not a stone, essence or shard, not stackable | `salvage_not_magic` |
| 4 | Its format version is not newer than this build's | `newer_format` |
| 5 | Its rarity is known (`item-data.md` section 6) | `unknown_rarity` |
| 6 | It is **not equipped** - always, whatever `Modify equipped items` says (SAL-8) | `equipped` |
| 7 | No sigil is pending on it (SAL-7) | `salvage_sigil_pending` |
| 8 | The rarity has at least one yield row (section 4) | `salvage_no_yield` |
| 9 | A station named in `salvage.stations` is in range, if the list is not empty | `salvage_station` |
| 10 | Every yield row's shards fit in the inventory **after the item is removed**, counting every chance row as if it succeeds | `salvage_no_room` |
| 11 | Confirm gate, if `salvage.confirm` | `salvage_confirm` (a prompt, not a failure) |

| Message id | English text |
|---|---|
| `$ecf_msg_salvage_disabled` | Grinding is disabled on this server. |
| `$ecf_msg_salvage_not_own` | Only items in your own inventory can be ground. |
| `$ecf_msg_salvage_not_magic` | Only magic items can be ground into shards. |
| `$ecf_msg_salvage_sigil_pending` | Spend the pending sigil before grinding this item. |
| `$ecf_msg_salvage_no_yield` | $1 items grind into nothing on this server. |
| `$ecf_msg_salvage_station` | You need to be near a $1 to grind items. |
| `$ecf_msg_salvage_no_room` | No room for the shards. |
| `$ecf_msg_salvage_confirm` | Hold Shift to grind $1. |

`newer_format`, `unknown_rarity` and `equipped` are the existing ids with their existing texts. In
`salvage_no_yield`, `$1` is the rarity's name; in `salvage_station`, the first listed station's name.

**Success**: `$ecf_msg_ground` "$1 is ground into $2.", `$1` the item's name, `$2` the shards gained ("2 Shards of
Ascension"; several kinds joined by the game's list comma). A short vanilla sound; Phase 3 may give it its own.

**Sealed items grind** (SAL-6). Sealed means "no stone can change it"; grinding does not change the item, it ends it.
A Serpent-sealed item the player regrets is exactly the dead weight this feature is for. A Reflection copy grinds
like any item of its rarity.

**Pending sigil refuses** rather than vanishing with the item, the same choice as the Serpent's `sigil_would_strand`
(`../DECISIONS.md` SIG-2): the player spends the sigil first, or keeps the item.

---

# 4. Yields

Per rarity, a list of `{fragment, amount, chance}` rows. Each row is rolled on its own at the moment of grinding
(`chance` percent, default 100) and adds `amount` shards.

| Rarity | Default yield | One stone needs | Share of a stone back |
|---|---|---|---|
| Uncommon | 2 Shards of Awakening | 5 | 40% |
| Rare | 2 Shards of Ascension | 5 | 40% |
| Epic | 2 Shards of Exaltation | 5 | 40% |
| Legendary | 2 Shards of Transcendence | 5 | 40% |
| Mythic | 2 Shards of Apotheosis | 10 | 20% |

- **The loop always loses.** Making a Rare took at least one Stone of Ascension; grinding it returns 40% of one. No
  sequence of stones and grinding ends with more stones than it started with, whatever the item. Mythic returns a
  fifth of an Apotheosis, because the chase stone should not come back cheaply.
- **Tier is not an input** (SAL-5). The ascension stones work on items of every tier, so a tier-7 Rare and a tier-1
  Rare were made with the same stone and return the same shards. Tier already makes a high-tier item more valuable
  to *keep*; grinding is the floor, not the reward.
- **Affixes are not an input.** Their count, tiers, a bound affix, dormant affixes: none change the yield. Grinding is
  not appraisal.
- **Quality is lost** (SAL-9). A honed or tempered item's Honing or Tempering is not refunded; the tooltip already
  shows it, and the player chose to grind.
- **What it means in play**, with the default drop tables: a Black Forest player finds a magic item about every two
  hours (`drops.md` section 4): in ten hours about five, of which three or four Uncommon and one Rare (tier-2
  rarity weights 70/24/5/1). Grinding all of them adds roughly one Stone of Awakening and half a Stone of Ascension
  per ten hours of play - a trickle, not a second drop table.

---

# 5. Shards

Five built-in shard items, one per ascension stone.

| id | Display name (English) | Prefab | Fuses into | Fuse count | Tint |
|---|---|---|---|---|---|
| `shard_awakening` | Shard of Awakening | `ECF_ShardAwakening` | `awakening` | 5 | its stone's tint |
| `shard_ascension` | Shard of Ascension | `ECF_ShardAscension` | `ascension` | 5 | its stone's tint |
| `shard_exaltation` | Shard of Exaltation | `ECF_ShardExaltation` | `exaltation` | 5 | its stone's tint |
| `shard_transcendence` | Shard of Transcendence | `ECF_ShardTranscendence` | `transcendence` | 5 | its stone's tint |
| `shard_apotheosis` | Shard of Apotheosis | `ECF_ShardApotheosis` | `apotheosis` | 10 | its stone's tint |

- **A fixed built-in list of five prefabs**, registered from code on every peer exactly like the stones
  (`prefabs.md` section 2) - including when salvage is disabled, so stacks survive a server toggling it. An owner
  can repoint a shard to another stone (`stone:`) or change its fuse count, but cannot add a sixth shard: there is no
  reserved shard pool (SAL-4). An owner who wants a shard for their own stone repoints one of the five.
- **Base**: the ascension group's base (`Ruby`, verify in game), at **scale x0.55**, tinted like the stone it fuses
  into - which for the ascension stones is the rarity palette (IMP-14), so a shard reads as a chip of its stone.
- Stackable 50, weight 0.1, value 0, teleportable, item type Material.
- **A shard is not a stone.** It has no stone definition and no verb. Clicking a carried shard onto an item is a
  vanilla swap, never a stone use or refusal. (Contract note for the Stones area: `ItemSlots.IsStone` is true for
  every `ECF_` prefab, which is right for the magic-base rule - a shard can never carry affixes - but the click
  take-over must also require a stone *definition* prefab, so a shard falls through to vanilla. Today the take-over
  keys on `IsStone` alone.)
- Shards glow on the ground only with the per-player `Glow stones` preference, like stones.
- Localization: `$ecf_fragment_<id>` and `$ecf_fragment_<id>_desc` (for example "Right-click with enough of them to
  fuse a stone."). The fuse count is not in the description (it is YAML); the tooltip line below says it.

---

# 6. Fusing

**Right-click a shard stack in the player's own inventory.** If it holds at least the fuse count, that many shards are
consumed and one stone of the target kind is added.

- The right-click is taken over only for shard prefabs in the player's own inventory grid (a prefix on
  `InventoryGui.OnRightClickItem`); in vanilla, right-clicking a material from the inventory does nothing (verified
  in the decompile: `Humanoid.UseItem` finds nothing to equip and, from the inventory panel, says nothing).
- **Shift + right-click fuses every full set** in that stack that fits (SAL-10).
- The added stone joins an existing stack of it, or the slot the shards emptied, or any free slot; the game's own add.
- Fusing is instant: no confirm (nothing is lost - the stone is worth exactly the shards).

| Situation | Result | Message |
|---|---|---|
| Fewer shards than the fuse count | nothing happens | `fuse_short` |
| No room for the stone (full inventory, shards not used up) | nothing happens | `fuse_no_room` |
| Target stone disabled or undefined | nothing happens (SAL-11) | `stone_disabled` |
| Salvage switched off on the server | fusing still works: existing shards are the players' | - |
| Shard stack in a container, on the hotbar HUD | vanilla (nothing) | - |
| Shard dropped onto another shard stack | vanilla merge or swap | - |

| Message id | English text |
|---|---|
| `$ecf_msg_fuse_short` | You need $1 $2 to fuse a $3. |
| `$ecf_msg_fuse_no_room` | No room for the $1. |
| `$ecf_msg_fused` | $1 $2 fuse into a $3. |

**Tooltip of a shard** (Display area): one line `$ecf_ui_fuse_line` "Right-click with $1 to fuse a $2.", built from
the synced fuse count and the target stone's name, cached like every tooltip block.

---

# 7. YAML

A new top-level section `salvage:` in `EliteCrafting_economy*.yml`. The on/off switch is in the `.cfg` (section 8),
the rules here, as for drops (`../DECISIONS.md` CFG-6).

| Field | Type | Default | Meaning |
|---|---|---|---|
| `confirm` | bool | true | grinding goes through the player's confirm gate |
| `stations` | station prefab names | `[]` | empty: grind anywhere; otherwise one of these stations must be in its own build range (the game's in-range test, `CraftingStation.HaveBuildStationInRange`, by the station's name) |
| `yields` | map rarity id -> list of rows | table in section 4 | row: `fragment` (shard id, required), `amount` (int 1-999, default 1), `chance` (percent 0-100, default 100) |
| `fragments` | list, by `id` | the five shards | entry fields below |

Fragment entry:

| Field | Type | Required | Default | Meaning |
|---|---|---|---|---|
| `id` | one of the five shard ids | yes | - | which built-in shard this entry configures |
| `stone` | stone id | yes | - | what the shards fuse into; any defined stone, owner stones included |
| `fuse` | int 1-999 | no | 5 | shards per stone |
| `name`, `description` | loc key or literal text | no | `$ecf_fragment_<id>`, `..._desc` | as for stones |
| `stack` | int | no | 50 | max stack |
| `item_weight` | number | no | 0.1 | item weight |
| `tint` | `#RRGGBB` | no | the target stone's tint | world model and icon |

Merging (`configuration.md` section 3): `yields` merges by rarity key, each rarity's list replaced whole;
`fragments` merges by `id`, field by field; `stations` is replaced whole.

## Validation (additions to `economy-yaml.md` section 9)

| Check | Level |
|---|---|
| A fragment `id` that is not one of the five | error (there is no prefab for it) |
| `stone` naming no defined stone | error |
| `fuse`, `amount` outside 1-999; `chance` outside 0-100 | error |
| A `yields` key naming no rarity; a row naming an undefined fragment | error |
| A yield row on the base rarity (Common) | warning, ignored (Common never grinds) |
| `amount` above the fragment's `stack` | warning (the fit check will refuse it whenever the inventory has no partial stack to absorb the rest) |
| A `stations` name matching no piece prefab | warning (modpacks) |
| A rarity above Common with no yield row | allowed: that rarity refuses with `salvage_no_yield` |

## The defaults

```yaml
salvage:
  confirm: true
  stations: []
  yields:
    uncommon:  [ { fragment: shard_awakening,     amount: 2 } ]
    rare:      [ { fragment: shard_ascension,     amount: 2 } ]
    epic:      [ { fragment: shard_exaltation,    amount: 2 } ]
    legendary: [ { fragment: shard_transcendence, amount: 2 } ]
    mythic:    [ { fragment: shard_apotheosis,    amount: 2 } ]
  fragments:
    - { id: shard_awakening,     stone: awakening,     fuse: 5 }
    - { id: shard_ascension,     stone: ascension,     fuse: 5 }
    - { id: shard_exaltation,    stone: exaltation,    fuse: 5 }
    - { id: shard_transcendence, stone: transcendence, fuse: 5 }
    - { id: shard_apotheosis,    stone: apotheosis,    fuse: 10 }
```

## Worked example

A server that wants grinding at the forge, a sweeter Legendary with a chance of something extra, and cheaper
Awakening shards, in `EliteCrafting_economy_server.yml`:

```yaml
salvage:
  stations: [forge]
  yields:
    legendary:
      - { fragment: shard_transcendence, amount: 2 }
      - { fragment: shard_exaltation,    amount: 1, chance: 25 }
  fragments:
    - { id: shard_awakening, fuse: 4 }
```

---

# 8. `.cfg`

Additions to `configuration.md` section 2:

| Section | Key | Type | Default | Synced | Meaning |
|---|---|---|---|---|---|
| `8 - Salvage` | `Salvage` | bool | `true` | synced | Grinding magic items into shards. Fusing shards works either way |
| `8 - Salvage` | `Salvage key` | keyboard shortcut | `End` | local | The key that grinds the hovered item (section 2) |

The confirm mode is the existing `2 - Stones / Confirm destructive stones` (local), so a player sets "ask me first"
once for stones and grinding alike.

---

# 9. OpenKeep, and who handles what

OpenKeep (ours, `../OpenKeep/CLAUDE.md` and its README "Salvage") has a **Salvage tab** in the crafting panel: every
stack in the inventory that has a recipe, turned back into a fraction of its **recipe materials** (Return Fraction
0.75), with a `Salvage Key` (Backspace) for the hovered stack and a confirmation.

The line between the two:

| | OpenKeep Salvage | EliteCrafting grinding |
|---|---|---|
| Works on | any item with a recipe, magic or not, stacks too | magic items only (Uncommon and up), never stackable |
| Returns | the recipe's vanilla materials | shards of an ascension stone, never materials |
| Where | the crafting panel's third tab, or its key | the inventory, the Salvage key |
| Key | Backspace | End |
| Reads the other mod | no | no |

**Neither mod reads the other's code, and there is no code path between them.** EliteCrafting never looks at a recipe
or returns a material. OpenKeep only compares item-data key prefixes: its `3. Salvage / Skip Items With Mod Data`
(default on, prefixes `ecf_`) leaves magic items out of its Salvage tab (SAL-14, implemented 2026-09-24). With both
installed:

- A **Common** item (vanilla, or unmade by the Stone of Unmaking): only OpenKeep can salvage it. EliteCrafting refuses
  (`salvage_not_magic`).
- A **magic** item: by default only EliteCrafting grinds it (shards). OpenKeep's tab doesn't list it and its key
  refuses it; an owner who turns OpenKeep's skip setting off lets both act, and whichever runs first takes the item -
  there is no state in which both pay out for one item.
- A **stone, essence or shard**: EliteCrafting refuses; OpenKeep does not list it (none has a recipe - stones are
  drop and trade only, PLAN.md, and shards fuse by right-click rather than by recipe, which is one reason fusing is
  not a crafting recipe (SAL-12): a recipe would put stones into OpenKeep's list).

**The risk this closes** is a player salvaging a Legendary through OpenKeep's tab for its materials, not realising
the affixes go with it. Decided by the user 2026-09-24 (SAL-14) and implemented in OpenKeep: it skips items carrying
`ecf_` data by default. Each README says in one line which mod grinds what.

---

# 10. Edge cases

| Situation | Result | Message |
|---|---|---|
| Common item, or a stone, essence, shard | refused | `salvage_not_magic` |
| Magic item in an open chest | refused (move it over first) | `salvage_not_own` |
| Equipped magic item, `Modify equipped items` on | refused anyway: grinding a worn item mid-fight is a misclick, not a plan | `equipped` |
| Sealed (Serpent, Reflection copy) | grinds | `ground` |
| Pending sigil | refused | `salvage_sigil_pending` |
| Bound affix | irrelevant; grinds | `ground` |
| Only dormant affixes, or a rarity whose count is now out of range | grinds by its rarity | `ground` |
| Rarity removed from the YAML | refused | `unknown_rarity` |
| Owner's custom ladder with a rarity that has no yield row | refused | `salvage_no_yield` |
| Honed / tempered | quality lost, no refund | `ground` |
| Broken item (durability 0) | grinds | `ground` |
| Inventory full, shards have a partial stack with room | grinds (the fit check counts partial stacks and the freed slot) | `ground` |
| A chance row fails its roll | no shards from that row; the others still pay | `ground` |
| Key held down | one grind per key press, never a repeat | - |
| Shift held but mode is Dialog | the dialog opens (Shift has no meaning in Dialog mode) | - |
| Server switches salvage off | the key refuses; shards already owned still fuse | `salvage_disabled` |
| Player on a server without the mod's Charter binding yet (joining) | the local rules apply until the server's arrive, like every stone | - |

---

# 11. Multiplayer

- **Everything happens in the player's own inventory, on that player's client**, which Valheim trusts with its own
  inventory - the same trust model as stones (`multiplayer.md` section 2). No container is edited, no RPC is sent,
  the server is not asked.
- The rules - the switch, yields, fuse counts, stations, confirm - are server-synced and lockable, so every player
  grinds under the server's numbers; the chance rolls are local dice under those odds, like every stone.
- The result (item gone, shards added, stones added) is ordinary inventory state, saved with the character and
  carried by the game's own serialization.
- The five shard prefabs are registered on every peer from code before any world data, so shards in chests, on the
  ground and in tombstones load everywhere, and a join-in-progress client knows them.
- Station range is the game's own local test on the client, as vanilla crafting uses.

---

# 12. Performance

- One key check per frame while the inventory panel is open (an `InventoryGui.Update` postfix that returns at once
  when the panel is hidden or the key is not down).
- Yields and fuse tables resolved at every rules apply; a grind is a handful of dictionary lookups and inventory
  calls. The right-click prefix returns at once for anything that is not a shard.

---

# Build checklist

- [x] Salvage key over the hovered own-inventory item; guards (drag, popup, console, teleport)
- [x] Checks 1-11 in order; the confirm gate in the player's mode; the fit simulation counts partial stacks and the
      freed slot
- [x] Yields per rarity with chance rows; item removed, shards added in one frame
- [x] Five shard prefabs, code-registered, tinted from their stone, scale x0.55
- [x] Right-click fuse, Shift fuses all; refusals; shard click onto an item stays vanilla
- [x] `salvage:` section read, merged, validated; `.cfg` `8 - Salvage`
- [x] Shard tooltip fuse line (in the item description, IMP-102)
- [x] `ecraft give` accepts shard ids (`console-commands.md`)
- [ ] Seen working on a dedicated server with two clients: grind, fuse, trade shards, shards in a tombstone

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified (Phase 2 spec pass). | pending |
| 2026-09-24 | Built: `Salvage/`, shard prefabs, `salvage:` parsing with `fragments` merged by id, `.cfg` `8 - Salvage`, `ecraft give`/`list`. Yield, fit and fuse arithmetic checked with a scratch harness. Not tested in game. | pending |

---

# Decisions

Every judgement call in this file is in `../DECISIONS.md` (Salvage: SAL-1 to SAL-16). SAL-14 (an OpenKeep-side change
for the both-installed case) needs the user; it blocks nothing in EliteCrafting.
