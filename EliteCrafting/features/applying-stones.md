# EliteCrafting - specification: Applying stones

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **the general flow of using a stone on an item**: the gesture, the order of checks, the refusal
mechanism, the confirm gate, costs, equipped items, and what the player sees. What each stone *does*, and each
stone's own refusals, feedback texts and edge cases, are `stones.md` (sigils: `sigils.md`, Honing and Tempering:
`quality.md`). `stones.md` section 1-3 restates this flow from the stones' side; the two agree, and where wording
differs this file is the mechanism and `stones.md` is the per-stone content.

**Status: Phase 1 built, not tested in game** (2026-09-23).

---

# 1. The gesture

**Pick up a stone stack in the inventory, click it onto the target item.** The vanilla move-item gesture; nothing
new to learn.

- A prefix on `InventoryGui.OnSelectedItem(InventoryGrid grid, ItemData item, Vector2i pos, InventoryGrid.Modifier
  mod)` (verified 2026-09-23, game notes Q5). The game routes click-then-click, drag-and-release, touch and gamepad
  through it, so all of them work.
- The prefix **takes over the click** (runs our pipeline and skips the vanilla method) only when all hold:
  1. something is being carried (`m_dragGo` is set) and the carried item is one of our stones;
  2. the clicked slot holds an item, and it is not the carried stone itself;
  3. the clicked item is **not one of our stones** (so dropping a stone on a stack of the same stone still merges
     and on a different stone still swaps, both vanilla, with no refusal; `../DECISIONS.md` RC-2);
  4. the local player is not teleporting.
- Everything else is vanilla: dropping a stone on an empty slot moves it.
- When the prefix takes over, the vanilla swap never happens, whether the stone applies or is refused. The carried
  stack stays on the cursor afterwards (its carried amount clamped to what is left) so the next click can apply
  again; when the stack is used up the drag is cleared.

---

# 2. The pipeline

Checks run in this order; the **first** failure refuses. Every check reads only; nothing is rolled or written
before step 13. Message ids are `$ecf_msg_<id>`; texts are in `stones.md` section 2 (and `localization.md` for the
two this file adds).

| # | Check | Refusal id |
|---|---|---|
| 1 | Target and stone are both in the **local player's own inventory** (the clicked grid is the player grid, and the carried item's inventory is the player's) | `not_own_inventory` |
| 2 | Target is a **magic base** (`item-data.md` section 2) | `not_magic_base` |
| 3 | Target's format version is not newer than this mod's (`item-data.md` section 8) | `newer_format` |
| 4 | The stone has a live definition in the economy YAML with `enabled: true` | `stone_disabled` |
| 5 | Target's slot is in the stone's `slots` filter, if it has one, and the stone's base precondition holds (Honing needs damage, Tempering armor or block) | `wrong_item_type` |
| 6 | Target is not sealed (`ecf_sealed`) | `sealed` |
| 7 | Target is not equipped, **or** `Modify equipped items` is on | `equipped` |
| 8 | Target's rarity is known (`item-data.md` section 6) | `unknown_rarity` |
| 9 | Target's rarity is in the stone's `applies_to` | `wrong_rarity` |
| 10 | The carried stack holds at least the stone's cost for this rarity | `not_enough_stones` |
| 11 | **verb precondition**: the stone's own checks (full, at minimum, already bound, capped, sigil pending, free slot for Reflection...) | `stones.md` |
| 12 | **dry run**: the verb runs on a copy of the item's parsed record; an invalid result (no affix can roll, nothing to change, a pending sigil finds nothing to steer) refuses | `stones.md` |
| 13 | **confirm gate** (section 4), only for stones with `confirm: true` | `confirm_required` (a prompt, not a failure) |
| 14 | **commit** (section 3) | |

Rules around the pipeline:

- **A refusal never consumes anything and never changes the item**, not the stone, not a pending sigil.
- **Refusals are for what is known before rolling.** A roll that happens to reproduce the old value (Perfection
  rolling the same number) is a success and costs the stone (`stones.md`).
- **The dry run's result is what commits.** Nothing is rolled twice: the record built in step 12 is the one written
  in step 14, so the player gets exactly the outcome the checks approved.
- The stone must come from the player's own inventory too (step 1): a stone in an open chest is moved over first.
  Every write stays inside the one inventory this client owns. (Judgement call, `../DECISIONS.md` APP-2.)
- Cost is paid only from the carried stack (step 10). Other stacks of the same stone are not pooled. (Judgement
  call, APP-3, shared with `stones.md`: the player sees which stack pays.)

## Refusal mechanism

- Each check, the verbs' included, returns either *ok* or a **refusal**: a message id plus up to three arguments
  (numbers, or names already localized).
- Shown with the local player's center message (`Player.Message(MessageHud.MessageType.Center, ...)`), text
  `$ecf_msg_<id>` filled through the game's own `Localization.Localize(text, words)`.
- **Placeholders are the game's `$1`, `$2`, `$3`**, in every file and in the shipped English text (RC-1). (Never
  `string.Format` on translated text: a stray brace in a translation would throw.)
- Refusal ids are snake_case and unique across all feature files. Each file lists its own; this file owns the
  mechanism and `newer_format`, `unknown_rarity`.
- A short vanilla "cannot" sound plays with the message. Phase 3 may give it its own sound.

---

# 3. Commit and cost

On success, in one frame, on the owning client (order from `stones.md` section 1):

1. The dry-run record is written to the item (`item-data.md` section 5; one write, the cache entry replaced).
2. A pending sigil that steered this stone is cleared (`sigils.md`).
3. The cost is removed from the carried stack (`Inventory.RemoveItem(item, amount)`, which removes the stack when it
   reaches zero and raises the inventory's change event).
4. If the item is equipped, the aggregate is marked dirty and rebuilds next frame (`effects-runtime.md`).
5. Feedback (section 6).

Costs:

- Each stone declares a per-rarity cost (`cost: {<rarity>: n}`, default 1; `economy-yaml.md`). The cost for the
  target's rarity **before** the stone acts is paid (a promotion on a Rare pays the Rare cost).
- A cost of 0 is allowed (a free stone for test servers): the stone applies and nothing is consumed.

---

# 4. The confirm gate

Stones with `confirm: true` in the economy YAML ask first. Defaults: **Serpent Stone** and **Stone of Unmaking**
(PLAN.md). Reflection has no downside and does not ask. Which stones ask is synced (the stone entry); **how** a
player is asked is theirs: the `.cfg` key `Confirm destructive stones` is per player and unsynced, because it only
protects the player from their own click.

| Mode | Behaviour |
|---|---|
| `HoldShift` (default) | The click applies only while Shift (gamepad: left trigger) is held. Without it: `$ecf_msg_confirm_required` ("Hold Shift to use the $1.") and nothing else. |
| `Dialog` | The click opens the game's yes/no popup (`UnifiedPopup` with a `YesNoPopup`), title `$ecf_ui_confirm_title`, body `$ecf_ui_confirm_body`. Yes re-runs steps 1-12 (the inventory may have changed while the popup was open) and commits; No does nothing. |
| `Off` | No gate. |

"Shift held" is read from the `mod` argument the game already passes: the grid reports `Modifier.Split` when Shift
or the gamepad left trigger is down at the click. With an item carried, vanilla ignores `mod`, so the gesture
collides with nothing.

The gate is the last check, so the prompt only appears for a use that would succeed.

---

# 5. Equipped items

- **Allowed by default** (`Modify equipped items`, synced and lockable, default on).
- The change takes effect at once through the aggregate rebuild (section 3 step 4).
- With the setting off, step 7 refuses with `equipped`.

---

# 6. What the player sees

Phase 1, on success:

- The stone's feedback message in the center (`stones.md` section 3, for example "Bronze sword rises to Rare.").
- The item's name recolors at once in the grid, and the open tooltip shows the new block (`display.md`).
- The vanilla item-move sound.

Phase 3 polish (specified so it slots in without changing the flow):

- A short flash on the inventory slot in the new rarity color.
- A sound per stone family, and distinct ones for the Serpent's outcomes.
- A transient visual on the player for the Serpent's corruption and for a promotion to Legendary or Mythic, sent to
  the peers that have the player instantiated (`multiplayer.md` section 4).

---

# 7. Multiplayer

- Everything in this file runs on the client that owns the inventory. Valheim trusts a client with its own inventory;
  the mod follows that model (`multiplayer.md`).
- The rules the dice use (pool, weights, tiers, costs, stone definitions, `confirm`, `Modify equipped items`) are the
  server's synced values while the server locks the configuration, so a client cannot change its own odds by
  editing a file.
- Nothing is sent to the server when a stone is used. The changed item reaches other players when it next leaves
  the inventory (trade, chest, drop).
- Refusals are decided from the item and the synced rules only, so a host-less client and the host refuse
  identically.

---

# 8. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Applying stones: APP-1 to APP-5; a stone dropped
onto another stone is RC-2).
