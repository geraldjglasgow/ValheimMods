# EliteCrafting - specification: Applying runes

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **the general flow of using a rune on an item**: the gesture, the order of checks, the refusal
mechanism, the confirm gate, costs, equipped items, and what the player sees. Players see **rune**; the code and this
file's name keep the internal word **stone** (`stones.md`). What each rune *does*, and each rune's own refusals,
feedback texts and edge cases, are `stones.md`. `stones.md` sections 1-3 restate this flow from the runes' side; the
two agree, and where wording differs this file is the mechanism and `stones.md` is the per-rune content.

**Status: built, not tested in game** (2026-09-23; cut to the six runes 2026-10-02).

---

# 1. The gesture

**Pick up a rune stack in the inventory, click it onto the target item.** The vanilla move-item gesture; nothing
new to learn.

- A prefix on `InventoryGui.OnSelectedItem(InventoryGrid grid, ItemData item, Vector2i pos, InventoryGrid.Modifier
  mod)` (verified 2026-09-23, game notes Q5). The game routes click-then-click, drag-and-release, touch and gamepad
  through it, so all of them work.
- The prefix **takes over the click** (runs our pipeline and skips the vanilla method) only when all hold:
  1. something is being carried (`m_dragGo` is set) and the carried item is one of our runes;
  2. the clicked slot holds an item, and it is not the carried rune itself;
  3. the clicked item is **not one of our runes** (so dropping a rune on a stack of the same rune still merges
     and on a different rune still swaps, both vanilla, with no refusal; `../DECISIONS.md` RC-2);
  4. the local player is not teleporting.
- Everything else is vanilla: dropping a rune on an empty slot moves it.
- When the prefix takes over, the vanilla swap never happens, whether the rune applies or is refused. The carried
  stack stays on the cursor afterwards (its carried amount clamped to what is left) so the next click can apply
  again; when the stack is used up the drag is cleared.

---

# 2. The pipeline

Checks run in this order; the **first** failure refuses. Every check reads only; nothing is rolled or written
before step 13. Message ids are `$ecf_msg_<id>`; texts are in `stones.md` section 2 (and `localization.md` for the
two this file adds).

| # | Check | Refusal id |
|---|---|---|
| 1 | Target and rune are both in the **local player's own inventory** (the clicked grid is the player grid, and the carried item's inventory is the player's) | `not_own_inventory` |
| 2 | Target is a **magic base** (`item-data.md` section 2) | `not_magic_base` |
| 3 | Target's format version is not newer than this mod's (`item-data.md` section 8) | `newer_format` |
| 4 | The rune has a live definition in the economy YAML | `stone_disabled` |
| 5 | That definition has `enabled: true` and a verb this build performs | `stone_disabled` |
| 6 | Target is not sealed (`ecf_sealed`) | `sealed` |
| 7 | Target is not equipped, **or** `Modify equipped items` is on | `equipped` |
| 8 | Target's rarity is known (`item-data.md` section 6) | `unknown_rarity` |
| 9 | Target's rarity is in the rune's `applies_to` | `wrong_rarity` |
| 10 | The carried stack holds at least the rune's cost for this rarity | `not_enough_stones` |
| 11 | **verb precondition**: the rune's own check (the item is full, for Shaping and Consecrated) | `stones.md` |
| 12 | **dry run**: the verb runs on a copy of the item's parsed record; an invalid result (no inscription can roll) refuses | `stones.md` |
| 13 | **confirm gate** (section 4), only for runes with `confirm: true` | `confirm_required` (a prompt, not a failure) |
| 14 | **commit** (section 3) | |

Rules around the pipeline:

- **A refusal never consumes anything and never changes the item**.
- **Refusals are for what is known before rolling.** An outcome that changes nothing but the seal (the Serpent's
  `seal_only`) is a success and costs the rune (`stones.md`).
- **The dry run's result is what commits.** Nothing is rolled twice: the record built in step 12 is the one written
  in step 14, so the player gets exactly the outcome the checks approved.
- The rune must come from the player's own inventory too (step 1): a rune in an open chest is moved over first.
  Every write stays inside the one inventory this client owns. (Judgement call, `../DECISIONS.md` APP-2.)
- Cost is paid only from the carried stack (step 10). Other stacks of the same rune are not pooled. (Judgement
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
2. The cost is removed from the carried stack (`Inventory.RemoveItem(item, amount)`, which removes the stack when it
   reaches zero and raises the inventory's change event).
3. If the item is equipped, the aggregate is marked dirty and rebuilds next frame (`effects-runtime.md`).
4. Feedback (section 6).

Costs:

- Each rune declares a per-rarity cost (`cost: {<rarity>: n}`, default 1; `economy-yaml.md`). The cost for the
  target's rarity **before** the rune acts is paid (Ascension on a Magic item pays the Magic cost).
- A cost of 0 is allowed (a free rune for test servers): the rune applies and nothing is consumed.

---

# 4. The confirm gate

Runes with `confirm: true` in the economy YAML ask first. Defaults: the **Cleansing Rune** and the **Serpent Rune**,
the two that cannot be undone. Which runes ask is synced (the rune entry); **how** a player is asked is theirs: the
`.cfg` key `Confirm destructive runes` is per player and unsynced, because it only protects the player from their own
click.

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

- The rune's feedback message in the center (`stones.md` section 3, for example "Bronze sword rises to Rare.").
- The item's name recolors at once in the grid, and the open tooltip shows the new block (`display.md`).
- The vanilla item-move sound.

Phase 3 polish (specified so it slots in without changing the flow):

- A short flash on the inventory slot in the new rarity color.
- A sound per rune, and distinct ones for the Serpent's outcomes.
- A transient visual on the player for the Serpent's corruption and for a promotion to Rare, sent to the peers that
  have the player instantiated (`multiplayer.md` section 4).

---

# 7. Multiplayer

- Everything in this file runs on the client that owns the inventory. Valheim trusts a client with its own inventory;
  the mod follows that model (`multiplayer.md`).
- The rules the dice use (pool, weights, tiers, costs, rune definitions, `confirm`, `Modify equipped items`) are the
  server's synced values while the server locks the configuration, so a client cannot change its own odds by
  editing a file.
- Nothing is sent to the server when a rune is used. The changed item reaches other players when it next leaves
  the inventory (trade, chest, drop).
- Refusals are decided from the item and the synced rules only, so a host-less client and the host refuse
  identically.

---

# 8. Decisions

Every question this file raised is answered in `../DECISIONS.md` (Applying stones: APP-1 to APP-5; a rune dropped
onto another rune is RC-2).
