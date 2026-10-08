# The Rune Table

User decision 2026-10-06: a crafting station that holds every rune, banks essence from sacrificed trophies, and applies
runes to gear, an essence choosing what the Ascension Rune adds. About the size of the workbench, the game's look and feel, working with
and without PackPanel. One setting (`2 - Runes` / `Rune Table`, synced, default on); every number below is a constant.

## 1. What the player sees

A table built with the hammer at a workbench. Use it (E) and the inventory screen opens with the Rune Table's window
where the crafting panel normally is: the same frame, tabs, list, description, requirement slots and button, so it reads
as one of the game's stations. Two tabs (the Runes tab was dropped on 2026-10-07: "its sufficient to just show/store
runes on the inscribe tab"), plus the Sockets tab for gems since 2026-10-07:

| Tab | List (left) | Right side | Button |
|---|---|---|---|
| Inscribe | your gear that runes can change (inventory order) | the piece, its inscriptions, a row of the 5 runes (table + carried), the essence row: the Essence item with what you have (table + carried), then the 5 essences, their icons in greyscale unless the Ascension Rune is picked; the cost; Store runes and essence under the name | Inscribe |
| Sacrifice | the trophies you carry that the table takes, with counts | what one is worth, the essence pool; Sacrifice all trophies under the name (every trophy it takes, in one press) | Sacrifice (Shift: all of that trophy) |
| Sockets (2026-10-07, sockets.md section 6; hidden while `Gems and sockets` is off) | your gear that takes sockets | the piece, its sockets and gems, what the chosen stone does here; the rune row shows the Dvergr Chisel and the gems you carry, the essence row the item's sockets (pick a filled one to replace its gem) | Cut socket / Set gem |

With the Ascension Rune and an essence chosen, the inscriptions it can give this item show as pills at the bottom of
the text (user request 2026-10-07): exactly the candidates the roll draws from (`EssencePills`, the roller's own
`AffixDraw.Candidates` on the item raised to Rare, the essence's inscriptions only), one rounded tag each in the essence's
colour, two rows at most, the rest in one "+N more" pill. Hovering a pill shows its line at its best value here, the
range and tiers this item level rolls, and prefix or suffix.

Every hover text in the window (pills, cost slots, the Sockets tab's slots) shows in a box beside what is hovered, to
its right or, without room there, its left, top edges level (user request 2026-10-07: "in a box and appear off to the
right or left of where you're hovering"): `HoverBox` with a `TipHover` per element, the game's own tooltip box, never
the game's `UITooltip`, which follows the pointer.

A use at the table (a rune, the chisel or a gem) plays for every client near it: the game's workbench craft sound and a
vortex in the stone's colour rising from the slab and breaking into specks about a metre above (`TableCast`,
`TableVortex`, bundle `ecf_tablefx`); the owner's payment counts the use in the ZDO (`ecf_rt_cast`,
`ecf_rt_cast_stone`), so no RPC is sent.

The window closes with the inventory screen (Esc, Tab, E, death), when the player walks more than 4 m away, when the
table is destroyed or its ownership is lost, or when the setting is turned off.

## 2. The piece

`ECF_RuneTable`: a copy of the game's workbench (`piece_workbench`: placement, wear, sounds, wood material, health)
without its crafting station or area marker, wearing the table's own model from the embedded bundle `ecf_runetable`
(ValheimAssets `Assets/Props/RuneTable`; without the bundle it keeps the workbench's look). Hammer, Crafting tab, needs
a workbench nearby. Cost: 10 Wood, 10 Stone, 5 Greydwarf eye (all recovered on deconstruction). Registered on every peer
before ZNetScene.Awake, whether the setting is on or not, so built tables always load; the setting only adds it to or
removes it from the hammer and refuses the use key.

The back shelf shows what the table holds (user idea 2026-10-07): the model has seven anchors `shelf_0` to `shelf_6`
along a widened rack, and `TableShelf` stands one rune tablet (the rune item's own dressed mesh, no colliders) at the
anchor of each rune the table holds at least one of, in the runes' order (Awakening ... Sealed). Every client draws it
from the ZDO when its data changes; nothing is sent; off on a dedicated server and on a model without the anchors. The
five runes stand on the middle five anchors (`shelf_1` to `shelf_5`).

The offering bowl fills with essence (user request 2026-10-07: "have the bowl fill with essense the more you have in the
table. maybe 500+ makes it look full. it fills more and more in increments of 50"): `TableBowl` puts a pale glowing disc
(Standard shader, emissive, no shadows, no collider) inside the bowl, found by the model's `col_box_bowl` collider, one
step higher per 50 essence in the pool, full at 500, none under 50; its width follows the bowl's inside (the workshop's
lathe profile: floor 1.09 m, rim 1.195 m). Drawn by every client from the ZDO like the shelf.

The gems it holds lie on the top (user request 2026-10-07: "if you have the freya gem in table, then you see a pink gem on
the table, if there are none, then you see no gem"): `TableGems` puts a copy of each held gem kind's own item model (no
colliders) at a fixed spot of its own on the free right-hand end of the top, three rows of 4, 3 and 4 in front of the
rack, each turned a little, scaled down to 0.16 m when wider, resting on the planks; drawn from the ZDO like the shelf.

## 3. Essences and trophies

Essence is the essence of a creature (user decision 2026-10-07): one pool per table (ZDO key `ecf_rt_essence`), filled
by sacrificing trophies, and an item, **Essence** (`ECF_Essence`, `Table/EssenceItem.cs`): a copy of the game's Wisp (a
pale glowing orb) without its mist clearing, stack 100, weight 0.1, teleportable, its own icon when
`assets/icons/ecf_essence_item.png` is embedded (else the Wisp's). Made on every peer like the runes (ZNetScene.Awake
prefix, ObjectDB.Awake and CopyOtherDB postfixes). The item appears when a table breaks (its pool drops as items) and goes
back in with Store runes and essence; carried essence also pays. The Inscribe tab's essence row starts with the item's
icon and the count you can spend (pool + carried); the Sacrifice tab shows the pool under the trophy's worth.

A trophy gives 2 (Meadows), 4 (Black Forest), 6 (Swamp, sea), 8 (Mountains), 10 (Plains), 12 (Mistlands) or 14
(Ashlands) essence (lowered 2026-10-07 by the user's word: a guaranteed inscription, 10 essence per item level, costs
about five trophies of the item's own biome; `Table/TrophyYields.cs`). Boss trophies give the most (user 2026-10-07: "boss trophies should be
in there too"): Eikthyr 20, the Elder 30, Bonemass 40, Moder 50, Yagluth 60, the Queen 70, the Fader 80. A boss trophy also unlocks a Forsaken power at the altar, so
Sacrifice all trophies never takes one (`TrophyYields.IsBoss`); only a press on it does, and its text says so.

Five essences to choose from (`Table/Essences.cs`; fire, frost, lightning and poison became one, Elemental, on
2026-10-07, and the speed and reach inscriptions that sat with lightning went to Beast), each with its own icon (the
user's own painted set, ValheimAssets `Assets/Icons/Essences`, embedded PNGs decoded at runtime by
`Window/EssenceIcons`):

| Essence | Icon | Its inscriptions |
|---|---|---|
| Beast | crossed sword and bow | physical damage, crits, slayers, stamina, attack, draw and cast speed, reach, volleys |
| Stone | banded wooden shield | armour, blocking and parry, health, stagger |
| Elemental | cracked stone mask, fire and frost eyes | fire, frost, lightning and poison damage and resistance, warmth and cold, bursting shots, hamstring |
| Spirit | glowing spiral orb | spirit damage, eitr, magic |
| Grave | skull with a blood drop | leech, blood magic, summons, undead slaying |

Utility inscriptions (movement, weather, fortune, gathering, skills, item properties) belong to no essence.

## 4. Inscribing

A press runs exactly what clicking a rune onto the item runs (`StonePipeline`, `ConfirmGate`, `StoneCommit`), with the
table as the payer (`IRuneSupply`, `Table/TableSupply.cs`): the rune from the table's store first, then from the
player's inventory. Essence works with the **Ascension Rune only** (user decision 2026-10-07: "When you click ascenion
rune you can select an essence to use with it for a garunteed mod, otherwise essences are greyed out"): with an essence
chosen, the third inscription Ascension adds is drawn only from that essence's inscriptions (`RollContext.Favoured`,
`AffixDraw`), so it is guaranteed to be one of them; it costs **10 essence per item level** (10 on a Meadows item ... 80
on a Deep North one), from the table's pool (carried Essence is stored first). When none of the essence's inscriptions can
roll on the item, the rune refuses ("No inscription can roll on this item") and nothing is spent. Every other rune
ignores essence. Shift confirms Cleansing and the Sealed Rune as a click does (or the dialog). Refusals are the runes' own,
plus "not enough essence" and "the table is no longer in your hands".

## 5. Storage and multiplayer

The table's ZDO holds an int per rune (`ecf_rt_rune_<id>`) and the essence pool (`ecf_rt_essence`). The use key asks the
ZDO owner (`ECF_RT_Open`); unless that machine has it open for someone else, the owner hands ownership to the asker and
answers yes (`ECF_RT_Opened`), as the game's chests do. Every write happens on the opener's machine while it owns the
ZDO; the window waits up to 5 s for the ownership to arrive from the server and closes if it is lost. A second player
gets the game's "in use" message. Items move only between the opener's own inventory and the table's ZDO. Runes and
essence go in with Store runes and essence, or one stack at a time with Ctrl + click on it in the player's inventory
while the window is open (`TableCtrlClick`, user 2026-10-07; it runs before OpenKeep's Route Modifier, which also takes
Ctrl + click, and clears the clicked item so nothing else moves it). Shift + click on a rune in the rune row, a stone in the
Sockets tab's row or the Essence slot opens the game's split dialog (`TableSplit`: its slider up to what the table
holds, its OK, Enter, Cancel and Escape) and takes that many into the inventory, as many as fit, Essence as items (user
2026-10-07). The Dvergr Chisel and the gems are stored the same way (Store all, Ctrl + click). The table's buttons use
only what it holds and its rows count only that (user 2026-10-07: "the chisel must be in the table to use it with the
cut socket button", the rows "should not show your inventory quantity"); stones the player carries are used the normal
way, clicked onto an item. A button is greyed only when its press would be refused (`TableUse.Works`, the press's own
dry run); the copied buttons keep the game's enabled label colour (`PanelBuilder.EnabledColours`). When the table is destroyed or taken down, its owner drops the runes it holds and its pool as Essence items
beside it, in full stacks (`RuneStash`, `WearNTear.m_onDestroyed`).

## 6. The window

Made the first time a table opens on an inventory screen, as a copy of the crafting panel (`Window/PanelBuilder`):
`Darken`, `Bkg` (with whatever a UI mod put in it, PackPanel's timber included), `topic`, one tab, the recipe list,
`Decription` with its icon, name, text, requirement slots, craft button and small style button. Everything else (other
mods' additions, station level, upgrade, repair) is removed, and so are UI groups, gamepad hooks, hold triggers,
localizers and layout groups. `Window/PanelLayout` places three tabs, lifts the list level with the description, and
puts two labelled rows of small requirement-slot copies (runes, essences) between the text and the cost slots. Shown as the
last child of the crafting panel, filling it, the panel's own parts at alpha 0 without clicks (`Window/CraftingCover`,
a CanvasGroup each), so the inventory's show and hide animations slide it with the other panels (user 2026-10-07). On
close it stays in the sliding panel; the next other inventory open uncovers the crafting panel. List rows are the game's
`m_recipeElementPrefab`. Mouse and keyboard; the gamepad can close it but not drive it yet.

## 7. Test checklist (not run yet)

- Build at a workbench; the hammer shows it in Crafting with its icon; it looks right next to a workbench.
- Open, all three tabs, with and without PackPanel; nothing of the crafting panel or OpenKeep shows through. Opening
  and closing, the window slides in and out with the other panels; Tab afterwards shows the normal crafting panel.
- Store runes and essence; take the table down with runes and 250 essence in it: the runes and 3 Essence stacks
  (100, 100, 50) drop beside it; Store them back: the pool is 250 again.
- The bowl: empty under 50, a step up per 50, full at 500 (and above); a second player sees the same.
- The shelf: the five runes stand on the middle five places.
- Sacrifice one / Shift all / Sacrifice all trophies; pool counts; a boss trophy is listed and worth 20-80, Sacrifice
  all trophies leaves it in the inventory (and is greyed with only boss trophies carried).
- Inscribe each rune; the essences are greyed unless Ascension is picked; Ascension with each essence gives one of
  its inscriptions, cost by item level, paid from the pool then carried Essence; Cleansing and the Sealed Rune need Shift.
- Dedicated server: two players, the second gets "in use"; the first walks away, the second can open; values persist
  after restart.
- Setting off: not in the hammer, use refused, window closes.
