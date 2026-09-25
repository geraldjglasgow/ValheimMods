# EliteCrafting - specification: Multiplayer

One feature of the mod, specified on its own. `../SPEC.md` is the whole-mod document and index.

This file covers **where everything happens** on a dedicated server with several clients, what is replicated by the
game and what by the mod, prefab registration and join-in-progress, the mod-required rule, and the **full
multiplayer test checklist** every phase must pass. The workspace rule applies without exception: every feature
works on a dedicated server the first time it is built.

**Status: Phase 1 and Phase 2 built, not tested in game** (2026-09-24): the checklist in section 6 is the consolidated
test plan, steps 1-21 for the Phase 1 core and 22-38 for Phase 2 (0.2.0).

---

# 1. Principles

1. **Item state rides the game's own item serialization** (`item-data.md`). The mod has no netcode for items.
2. **Rules are server-owned**: both YAML families and the gameplay `.cfg` keys are synced and locked by Charter
   (`configuration.md`), so every machine that rolls uses the same odds.
3. **Each thing is decided on the machine the game already trusts with it**, and drawn on every client from
   replicated data.
4. **Transient feedback is scoped** to the peers who could see it.

---

# 2. Where everything happens

| What | Decided on | Replicated by | Notes |
|---|---|---|---|
| Creature stone drops and pre-rolled gear drops | the **dying creature's ZDO owner** (often a nearby client) | the game (the dropped `ItemDrop`'s ZDO) | See the note below the table |
| Chest / world drops (Phase 2) | the ZDO owner of the container, when the game fills it; `ecf_filled` marks it rolled | the game | `drops.md` section 11 |
| Essences, sigils, quality, every stone (Phase 2) | the client whose inventory holds the item, like every stone | the game | `stones.md`, `essences.md` |
| Grinding and fusing (Phase 2) | the client whose inventory holds the item or the shards | the game | `salvage.md` |
| Kill restore, Evader's Fury trigger (Phase 2) | seen on the creature's owner / the attacker's peer, applied on the killer's / dodger's client | one routed RPC to that peer only (`ECF_KillRestore`, `ECF_MeleeDodged`) | `effects-runtime.md` section 7 |
| Values other peers apply (Phase 2: Hearthlight, Mistbane, stagger, taming, sailing, gathering) | the player's own client publishes them on its player ZDO; the owner of the creature, ship or resource (or every client, for lights) applies them | the game (player ZDO) | `effects-runtime.md` section 7 |
| Elite Creatures Reborn terms (Phase 2) | the dying creature's owner, from ECR's keys on the creature ZDO | nothing new: ECR's keys ride the creature | `ecr-integration.md` |
| Stone application | the client whose inventory holds the item | the game, when the item next leaves that inventory | `applying-stones.md` |
| Workbench upgrade carry-over | the client crafting | the game | `item-data.md` section 1 |
| Aggregate stats (speed, regen, carry, skills) | the item holder's client | not replicated; the game replicates their *results* (movement, health) as it always does | `effects-runtime.md` |
| Weapon damage affixes, Honing | the attacker's client, inside the `HitData` it sends | the game (the hit) | |
| Damage-taken affixes, Tempering | the victim player's own client | the game (health) | |
| Killer-side loot-find totals (Phase 2) | the killer's client writes them to its own player ZDO | the game (player ZDO) | read by the creature's owner at the roll |
| Tooltips, colored names, ground glow | every client, locally | nothing to replicate | `display.md` |
| Stone prefabs | registered identically on every peer at startup | not replicated: deterministic | section 3 |
| Transient feedback (Phase 3) | the acting client | an object RPC to the peers that have the player instantiated | section 4 |

**The dying creature's owner is usually a client, not the server.** Verified in the decompile
(`ZDOMan.ReleaseNearbyZDOS`): on a dedicated server, persistent ZDOs in a player's active area are owned by that
player's peer; the server owns them only when no player is near. So on a dedicated server the drop roll typically
runs on the client of whichever player's area the creature was in, which is not necessarily the killer. PLAN.md's
"the server on dedicated" is corrected here. It changes nothing in the design (the roll uses the server's synced
rules wherever it runs) but it matters for testing (checklist item 3) and for the loot-find effects, which must read
the killer's totals from the killer's ZDO rather than from local state. Rolls stay on the owner; forcing them onto
the server would add netcode and a round trip for no gain in honesty (`../DECISIONS.md` RC-7).

**Drop write order** (game notes Q15): pre-rolled gear is built on the owner as an `ItemData` (a clone of the
prefab's, with `m_dropPrefab`, durability, world level and our `ecf_` keys set) and spawned with the game's own
`ItemDrop.DropItem`, which saves the data into the new ZDO before it returns, so no peer can ever see the item
without its data. Stones carry no data and are spawned by the same death hook at the same moment; our rows are never
put in the creature's vanilla drop list, so the ragdoll path and other loot mods never see them
(`../DECISIONS.md` RC-12). Details are `drops.md`'s.

**Trust**: Valheim trusts each client with its own inventory. A modified client can write any item it holds, with or
without this mod. The mod does not pretend otherwise: server locking guarantees honest clients all roll with the
same odds; it is not anti-cheat.

---

# 3. Prefabs, registration and join-in-progress

- The 27 stone prefabs (and the reserved custom-stone pool, `prefabs.md`) are **cloned and registered on every peer
  from code alone**, never from YAML or network data, so every peer has the same prefab names and hashes before any
  world data arrives.
- Registered in postfixes on `ObjectDB.Awake` and `ObjectDB.CopyOtherDB` and a prefix on `ZNetScene.Awake`, each
  guarded against duplicates (both registries use `Dictionary.Add` and throw on a repeat; `prefabs.md`). A client that joins mid-session therefore already knows every stone prefab when the zone's
  ZDOs arrive, and its inventory load (which looks items up by prefab name) finds them.
- YAML (names, stack sizes, weights, tint overrides) is applied to the registered prefabs and every live copy later,
  whenever the economy family applies. A join-in-progress client gets the server's economy YAML through Charter's
  first push and applies it; until then its stones show the built-in defaults, which is harmless.
- Existing magic items need nothing: their state is inside the item data the client receives with the container,
  the tombstone or the dropped item.

---

# 4. Transient feedback RPCs (Phase 3)

- Registered on the player's `ZNetView` and invoked by the acting client with `InvokeRPC(ZNetView.Everybody, ...)`.
  A ZDO-targeted RPC executes only on peers where that player object is currently instantiated (game notes Q22),
  which is exactly "the peers who can see the player", with no radius to choose.
- Carry only what to show: an effect id and a rarity id. Each receiving client draws it locally, respecting its own
  display preferences.
- Nothing gameplay-relevant ever travels this way; losing one changes nothing.

---

# 5. The mod is required on the server and every client

- Charter `mandatory: true` with the mod's version (`configuration.md`). A client without the mod, or with a
  different version, is refused at the join screen with the family check's refusal code in both logs. A server
  without the mod refuses our clients the same way.
- Why: stone prefabs must exist on every peer (an unknown prefab in an inventory is dropped on load, and a server
  or host **destroys** a world ZDO whose prefab it does not know, verified in `ZNetScene.CreateObjectsSorted`), and
  rolls on client-owned creatures must use the server's rules.
- The manifest description and README say "Required on the server and on every client" in the first paragraph.

---

# 6. Multiplayer test checklist

Run on a **dedicated server with two clients, A and B**, at the end of every phase, with the server's
`Lock Configuration` on unless a step says otherwise. Tick in the feature files and `SPEC.md` sections 10 and 11 from
observed behaviour only. Steps 1-21 are the **Phase 1 core**; steps 22-38 are **Phase 2 (0.2.0)**. On a 0.2.0 build
every stone is enabled, so where a Phase 1 step names 0.1.0 counts or a disabled Phase 2 stone, use the 0.2.0 values
given in brackets.

**Setup:** dedicated server with the mod; clients A and B with the mod, same version; both admins for the command
steps, then B removed from the admin list for step 17. `Log rolls` on at the server and both clients. A test file
`EliteCrafting_economy_test.yml` on the server holds the temporary overrides the steps name; delete it afterwards.

1. **Startup.** Server and both clients log `43 stone prefabs built` and `rules applied (generation 1): 59 affixes,
   6 rarities, 27 stones` [0.2.0: `64 stone prefabs built` (43 stones and essences, 16 reserved, 5 shards) and `162
   affixes, 6 rarities, 43 stones`], no errors; the server writes `com.EliteCrafting.cfg` and both main YAML files on
   first run.
2. **Item survey and stone bases.** On A: `ecraft dump items` and `ecraft tiers`. The stone base prefabs of every
   group exist (`prefabs.md` section 4; otherwise the coded fallback was used and logged); torches and trinkets are
   not magic bases; fishing rods are `tool`; the unmapped-materials list is short. Record the result in
   `prefabs.md` and `item-tier.md`.
3. **Stone prefabs on clients.** `ecraft give all 5` on A (the eight enabled stones), plus `ecraft give` of a few
   Phase 2 stones such as `growth_greater`, `serpent` and `sigil_war` (given with a note): every stone has its name,
   description and a tinted icon; dropped on the ground each shows a tinted model, Lesser smaller and Greater larger
   than the base. The server logs
   no graphics errors (it tints nothing).
4. **The Phase 1 stones** (`stones.md` 7-9, 11), on A: Awakening on a Common item → Uncommon with one affix;
   Ascension → Rare keeping the affix; Exaltation, Transcendence, Apotheosis → Epic, Legendary, Mythic (6 affixes)
   keeping every affix; Lesser Growth adds one affix to an Uncommon with room; Lesser Turmoil swaps one affix for a
   different id; Lesser Perfection changes values, never ids or tiers. After each success one stone is gone and the
   rest of the stack stays on the cursor. A Phase 2 stone (`ecraft give growth_greater`) refuses with "disabled"
   [0.2.0: it works; disable one in the test file, `stones: [{ id: growth_greater, enabled: false }]`, to see the refusal].
   A stone dropped on a stone stacks or swaps as vanilla.
5. **Stone applied on A is visible to B.** A puts the Rare item from step 4 in a chest. B opens it: the name is
   Rare-blue, the tooltip lists the same affixes, tiers and values; B takes it: same again. A's and B's
   `ecraft inspect` raw lines are identical.
6. **Round trips.** The same item and a Mythic survive unchanged (compare `inspect` raw lines) through: chest in and
   out; dropped on the ground and picked up by the other player; A dies carrying it and loots the tombstone; a
   portal; logout and login; server restart; item stand place and take; a workbench upgrade (every `ecf_` key kept,
   `item-data.md` section 1).
7. **Pre-rolled drops, both owner cases.** Test file: `drops.chances.stone` and `drops.chances.gear` all 100. B
   reaches a creature's area first (B's peer owns it), then A kills it: the roll's log line is on B's log; the
   stones and the magic item appear on both screens, the item glows on both; A picks it up and `inspect` shows a
   complete record. Repeat with A alone near the creature, so the roll runs on A. Both cases look alike.
8. **Who drops.** A kill no player took part in (a creature killed by another creature or by falling) drops none of
   our loot; a kill made by A's tamed wolf does (`ecf_ally_hit`); a tamed creature dying drops nothing; Eikthyr drops
   its two guaranteed Awakening stones plus its rolls; with `Stone drops` or `Magic item drops` off on the server,
   that part stops for everyone.
9. **Join in progress and stack sizes.** Test file: `stones: [{ id: awakening, stack: 200 }]`. While connected B
   receives 150 Awakening stones in one stack and puts 120 in a chest. B disconnects; A drops stones and magic items
   on the ground and in another chest. B rejoins: the stone models, names, icons, the glow on magic items and the
   chest tooltips are right; B's stack is still 150 and the chest's 120 (nothing cut to 50, `StoneStackGuard`); no
   "missing prefab" errors in B's log.
10. **YAML hot reload.** On the server, edit a rarity color, disable one affix A has on an item, and rename a stone
    in the test file. Within ten seconds, without relog: both clients show the new color and the new stone name on
    stacks they already hold; A's affix is dormant (grey) on both and `ecraft stats` on A no longer counts it.
    Re-enable: it revives with its stored value.
11. **Locked settings and the server's texts.** B edits their local `Modify equipped items` to false and their local
    affix YAML: nothing changes for B while connected (`charter diff` shows the difference). A server-side change to
    `Modify equipped items` reaches both. On B, `ecraft list stones` shows `server:` file names and
    `ecraft dump economy` writes the server's configuration; `ecraft reload` on B is refused as author-side. After B
    disconnects, B's own values are back.
12. **Invalid YAML on the server.** Save a file with an error: the server log names file and line, clients keep the
    previous rules, nothing is published; `ecraft reload` on the server reports "errors, previous configuration
    kept". Restart the server with the error still in place: it runs and publishes the built-in defaults.
13. **Refusals identical on A and B** (neither is the host): a Rare item with a Stone of Awakening (wrong rarity), an
    Uncommon holding two affixes with Lesser Growth (full), an equipped item with `Modify equipped items` off, a
    stone clicked onto an item in an open chest, a stack of arrows (not a magic base), a disabled stone. Same
    message, nothing consumed, item unchanged, on both.
14. **Confirm gate.** Test file: `stones: [{ id: turmoil_lesser, confirm: true }]`. A uses it without Shift: "Hold
    Shift", nothing consumed; with Shift: applies. A sets `Confirm destructive stones` to `Dialog` locally: a popup;
    B still gets HoldShift (unsynced).
15. **Aggregate per player.** A equips a Fleetfoot item: A's `ecraft stats` shows it, B's does not change; A
    unequips: gone. A dies and respawns wearing it: counted once. A takes a portal: unchanged. A applies Lesser
    Perfection to the equipped item: the new total shows at once. A Vigor item raises A's health bar right away.
16. **Item-local effects.** A weapon with a brand shows the added damage type in the vanilla tooltip on A and, after
    a trade, on B; Hardened armor shows the higher armor; Well-Forged shows the higher maximum durability; a hit with
    the branded weapon on a creature B owns deals the extra damage.
17. **Command access.** B, not admin: `ecraft give` refused; `ecraft inspect` works; with `Read-only commands for
    everyone` off on the server, `inspect` refused for B too.
18. **Display per player.** B switches `Tooltip detail` Compact/Standard/Full, `Show dormant affixes` off and
    `Colored item names` off: only B's view changes. Common items never glow; `Glow stones` on makes stones glow in
    their tint (white without one) on B only.
19. **Localization.** An `EliteCrafting.translations.English.yml` in A's config renaming one stone shows on A only.
20. **Version mismatch.** A client with a different mod version and a client without the mod are both refused at
    the join screen with a code in both logs.
21. **Performance.** Profiler on (PatchGuard) during 15 minutes of normal play with both clients, including a
    30-item loot explosion on screen (`ecraft roll` 30 items and drop them): no EliteCrafting method in the top
    offenders; the glow light count on each client never above `Glow max lights`.

**Phase 2 (0.2.0).** Same setup. `ecraft give <id> <n>` and `ecraft affix <affix_id> [tier]` prepare every item; after
each stone, compare A's and B's `ecraft inspect` raw lines once the item has passed to B (chest or ground).

22. **Survey and prefab names.** On A: `ecraft dump items`. Confirm the essence base `Thunderstone` exists (else the
    coded fallback was used and logged), and that `BonemawSerpent` and `Fader` are real prefab names (the economy
    YAML marks both `# verify`); fix the defaults if not. `ecraft give all 2` and `ecraft give shard_ascension 5`: the
    essences are tinted per family, Lesser smaller than Greater; the shards are small and tinted like their stone;
    each essence's description ends with "Always adds one of: ...", each shard's with "Right-click with 5 to fuse a
    ...", on A and on B after a trade.
23. **Manipulation stones.** On Epic/Legendary/Mythic items: Greater Growth adds one; Greater Turmoil swaps one;
    Greater Perfection changes values only; Lesser and Greater Upheaval reroll every affix and keep the rarity; Lesser
    and Greater Severing remove one (refused at the rarity's minimum). Unmaking without Shift: "Hold Shift", nothing
    used; with Shift: Common, no affixes, Honed/Tempered bonus and a pending sigil kept. Each result identical on B.
24. **Serpent Stone.** Test file forcing one outcome at a time (`outcomes` with every other weight 0): `seal_only`,
    `add_affix` (a Legendary ends with 6), `chaotic_reroll` (tiers above the ceiling possible), `promote` (a Mythic
    gets a 7th), `demote` (an Uncommon ends a sealed Common). Every result shows "Sealed: Corrupted" on A and B, and
    every stone, essence and sigil then refuses with "sealed" on both. With a sigil pending: `sigil_would_strand`.
25. **Binding, Chance, Reflection.** Binding marks one affix `[Bound]`; a second Binding refuses; the bound affix
    survives Turmoil, Upheaval, Perfection and Severing and goes with Unmaking. Chance on 20 Common items: rarities
    roughly 50/30/15/5, never Mythic, no item lost. Reflection: a sealed copy ("Sealed: Mirrored") in a free slot,
    not equipped even when the original was, without the original's pending sigil; with a full inventory:
    `inventory_full`, nothing used; the copy cannot be reflected. Both items survive a chest round trip to B intact.
26. **Sigils.** A applies Sigil of War to an item: "Pending: Sigil of War" in the sigil's tint on A and B; a second
    sigil refuses (`sigil_pending`). Lesser Growth then adds an offense affix and spends the sigil; a Binding in
    between leaves it pending. Preservation on Turmoil/Upheaval/Perfection keeps the highest-tier affix; Culling on
    Severing removes the lowest-tier (dormant first).
27. **Quality.** Honing a sword 3 times: "Honed +3%", the vanilla damage numbers rise 3%, a hit on a creature B owns
    deals it. Tempering a chest piece and a shield: armor, block. At +10%: `quality_capped`. Honing on armor:
    `wrong_item_type`. The bonus survives Unmaking and a workbench upgrade and is copied by Reflection.
28. **Phase 2 state round trips.** An item with a bound affix, a quality bonus and a pending sigil, and a sealed item,
    through step 6's round trips (chest, ground, tombstone, portal, relog, server restart, workbench upgrade): every
    `ecf_` key survives (`inspect` raw lines equal on A and B).
29. **Essences.** A Lesser Venom Essence on a Rare sword: rarity kept, a bound affix kept, one of the new affixes is
    Venombrand or Blood Drinker, message "... is reshaped around ..."; on a shield: `essence_wrong_item`. A Greater
    essence rolls its family affix at the item's top tier (`ecraft inspect`). Kill Eikthyr: one Lesser Storm Essence
    at least (plus the Awakening stones); a serpent killed by A in B's area rolls its Tide chances on B's peer. With
    Preservation pending the protected affix is kept and the sigil spent; with War pending the sigil stays.
30. **Salvage and fusing.** A hovers a Rare in their inventory and presses End: "Hold Shift"; Shift + End: the item
    becomes 2 Shards of Ascension. Refused, nothing lost: an equipped item, an item with a pending sigil, an item in an
    open chest, a Common, a full inventory (`salvage_no_room`). Right-click 5 shards: one Stone of Ascension; Shift +
    right-click on 12 shards: 2 stones, 2 shards left. Shards trade to B and survive a tombstone. Server `Salvage`
    off: grinding refused for A and B ("disabled on this server"), fusing still works. With OpenKeep installed too:
    OpenKeep's Salvage tab does not list magic items (SAL-14) and the End key clashes with none of its keys.
31. **Kill restore (Reaper, Soul Reaper).** A wears Reaper; B stands so B's peer owns a creature (step 7), A kills it:
    A's health is restored once, on A only; B killing one restores nothing for A. A kill by a fall or a pet
    restores nothing.
32. **Dodge and leech.** A wears Evader's Fury and dodges a melee attack of a creature B's peer owns: the Evader's
    Fury icon appears on A only; a dodged arrow does not start it. A wears Blood Drinker: hits on a creature heal A;
    hits on B with PvP off, on A's own tamed wolf, and chopping a tree heal nothing. Momentum starts on a real dodge
    only (not on an empty stamina bar). Mist Veil: an avoided hit shows "Avoided" and does no damage.
33. **Values other peers apply.** A equips Hearthlight: B sees the warm light around A. Mistbane: B sees A's demister
    widened. Hamstring and Dazing Blows on a creature B's peer owns: slowed and longer staggers on both screens,
    never on a boss or a player. Beast Whisperer near a tameable creature B owns: faster taming. Fair Winds while A
    steers a ship B owns. Harvester, Deep Vein and Heartwood on resources B's peer owns. A staff summon with Grave-Lord's
    Command and Grave Vigor hits harder and has more health on B's screen too. **IMP-120 measurement:** mine one copper
    deposit and fell one birch with and without Deep Vein / Heartwood and record the multiplier.
34. **Loot find.** A wears Fateweaver and Norns' Favour (`ecraft affix fateweaver`, `ecraft affix norns_favour`); `ecraft stats` on A shows them.
    B's peer owns a creature, A lands the killing blow: B's `Log rolls` line uses A's find totals; B landing it uses
    B's (zero). Trophy Taker and Hoardfinder change trophies and coins the same way.
35. **Chests and world containers.** Test file `drops.chests: { stone_chance: 100, gear_chance: 100 }`. B enters an
    unexplored dungeon first: each chest holds stones and a magic item on both screens, rolled once (the log line is
    on the container owner's side; relogging, reopening or another player's first visit never rolls again). A
    player-built chest never rolls. `containers: { TreasureChest_meadows: { multiplier: 0 } }` stops that prefab.
    Also: B joins and walks straight into an ungenerated location; its chests follow the server's test file, not B's
    local rules (review D's question on join timing).
36. **Elite Creatures Reborn** (both mods on the server and both clients). `Synergy` off: stone and gear odds unchanged,
    but a Cloven twin and Phantom husks drop nothing of ours. `Synergy` on: a 3-star ECR troll pays about x2 stones
    (`ecraft ecr` on the hovered creature shows the terms, the same on A and B). ECR removed from all three: nothing
    changes and `ecraft ecr` says it is not installed.
37. **Display leftovers.** At a workbench, A's upgrade tab lists a magic item in its rarity color (dimmed when A lacks
    the materials); selecting it shows its colored name and its affix block under the recipe's stats; a plain recipe
    after it is drawn in the normal color. A places a magic sword on an item stand and a magic helmet on an armor
    stand: hovering shows the colored name and the affix block on A and on B; empty stands, guardian stones and the
    no-access text are vanilla.
38. **Phase 2 performance.** Step 21 again with Phase 2 affixes equipped on both clients (leech, Runic Ward, Momentum,
    Hearthlight, a Volley bow), the crafting panel open on an upgrade target for a minute, a stand hovered for a
    minute and 20 items ground in a row: no EliteCrafting method among the top offenders.

---

# 7. Decisions

This file's one question, where loot rolls run, is answered in `../DECISIONS.md` RC-7: on the dying creature's ZDO
owner, under the server's synced rules.
