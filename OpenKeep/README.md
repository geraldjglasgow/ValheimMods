# OpenKeep

Storage and inventory for Valheim in one mod: craft, build and feed stations straight from nearby chests, ship
holds and carts; stow, top up, sort, junk and route from the inventory panel; a Salvage tab that gives
materials back; a `- amount +` stepper to craft many at once; stack sizes and weights; bigger chests with their contents on hover; carts that carry a
workbench; how much each smelter and kiln holds; a sign above every chest that names what is inside; and a few base
tweaks: respawn at the bed you pick on the map (sooner the nearer), quicker jumps between near portals, campfires on wooden floors, honey per day, fires that refuel from nearby chests,
smelters and kilns that feed themselves from the chests beside them, tamed animals that eat from nearby chests,
torches lit only at night (or kept lit),
Rested sooner, area repair with the hammer and gear repaired as you open a workbench or forge. Every gameplay setting is server synced and lockable, every hotkey is
per player, and everything is tunable in the config file and seven YAML files that hot reload. Convenience features are on by default; anything
that changes balance (stack sizes, weights, chest sizes) defaults to vanilla.

Looking for a bigger inventory, labelled armour, utility, food and ammo slots, a key ring or backpacks? They are in
the companion mod PackPanel, which works with OpenKeep or on its own (see "With PackPanel" below).

### Reach: craft from storage
- Wherever the game counts what you carry (crafting rows, the craft and upgrade buttons, the hammer's piece list
  and requirement rows, station hover texts) it now counts your inventory plus every reachable container.
- Paying takes from the inventory first, then from containers nearest first. The requirement rows show
  `3 + 12` (inventory plus storage, storage in its own colour), or the total, or the vanilla number
  (`Requirement Display`), and flash once after a pull.
- Stations: use a smelter, kiln, blast furnace, spinning wheel, windmill, campfire, torch, hot tub, cooking
  station, oven or fermenter with nothing to add in your inventory and one unit comes from the nearest container.
  Hold `Fill Modifier` (Shift) to fill it up, hold `Pull Modifier` (Alt) to move a stack into your inventory
  instead. Hover texts get a `From storage: n` line. The `stations:` map in `OpenKeep.Reach.yml` narrows this
  per station prefab: `deny` keeps items out of the feeding (raw meat you want to keep raw, say), `allow` limits
  it to a list, `enabled: false` leaves a station entirely alone — crafting and building are untouched.
- A reachable container has the game's `Container` component and is within `Range` (20 m by default), passes the
  game's own privacy and ward checks, and is not open by another player (see "Shared chests"). Detection is by
  component, never by a name list: any object with a container is a container, it is a ship when it carries the
  game's ship component and a cart when it carries the cart component. Vanilla ships (Karve, Longship, Drakkar),
  modded ships and modded chests are all found the same way. Standing on a moored ship you craft, build and feed
  stations from its hold; the ship and cart switches are in section `0. Containers`.
- `Toggle Key` (Alt + R) switches the whole module off for your character; `Link Key` (Alt + L) draws lines from
  every container in range to the stations it serves, and placing a chest draws them too.

### Stow: stow, top up, sort, junk, route
A row of buttons just below the player panel (`Quick stack`, `Store all`, `Top up`, `Sort`), a trash can on its
own plate in the game's style under the armour readout on the panel's right (hover it for what it is; OpenKeep
leaves the game's armour and weight readouts as they are), and a `Sort` button below the open container's panel, under
its `Take all` line; `Button Row Offset` moves the rows up or down if your screen layout needs it, and
`Trash Can On Stat Column` off puts the trash can at the end of the button row instead. Every action also has a hotkey that works while the inventory is open. The module is
one workflow for coming home with a full inventory. Open a chest and stow: quick stack (`Q`) moves every stack
whose item the open container (or, with `Quick Stack Nearby`, any container within `Nearby Range`) already holds,
and `Store all` (`G`) moves everything that may move into the open container; neither touches your hotbar. Then top up (`R`): every stack you
carry that is not full is refilled from the chests, so you leave with what you came with. Sort (`T` for the
inventory, `Y` for the container) orders by `Category`, `Name`, `Weight` or `Value`; the hotbar, favourite slots
and equipped items stay put and stacks merge; with `Sort Favourite Items` off, favourite items stay put too —
turn it off when another mod keeps items in extra slots the sort would pull out, and favourite those items. Quick
stack, store all, dump and sort work only on the rows the game gives you (four, more once you buy them from the
trader). Mods that add equipment, food or ammo slots keep them in rows below those, so these actions leave the
slots alone, and top up still refills the food and ammo there. If a mod adds ordinary inventory rows you want
stowed and sorted too, set `Main Inventory Rows` to the number of rows to use; with PackPanel they use its whole
grid by themselves. What you never want to keep is junk: mark an item with `J`, and
`Destroy Junk` (Shift + Delete) destroys every junk stack in one go; `Trash Key` (Delete) destroys the hovered
stack and the trash can takes a dragged one, at once (turn on `Confirm Trash` for the game's popup first).
Shift + click on the trash can starts trash mode: the pointer becomes the trash can and every stack you click in the
inventory or the open chest is destroyed, with no question asked (favourites and worn items are kept); let go of
Shift to stop. What does not belong in
the open chest is routed: Ctrl + click sends a stack to the nearest container that holds the item, an item of its
group, or accepts it in `OpenKeep.Stow.yml`; Store one (`V`) sends a single item. When a chest fills up, the rest
goes on to the next nearest container that holds the item, and the next, until the stack is gone or no such
container is left in range; quick stack and Dump work the same way. Find (`Z`) marks every nearby
container holding the hovered item with a line and a count; Dump (Alt + D) quick stacks to every nearby container
without opening the inventory; the left and right arrows or the mouse wheel over the container grid cycle
through the chests around you.

- Favourite item (`F`): the item is never quick stacked, stowed, dumped, trashed or salvaged. Favourite slot
  (Shift + F): the slot's content never moves. Top up still refills favourites.
- Ground pickup (off by default): containers whose prefab has `pickup: true` in the YAML pull dropped items
  lying within `Pickup Range` (2 m) from the ground after `Pickup Delay` seconds, only items they already hold
  unless the YAML says otherwise. When several chests could take a drop, the one already holding that item and
  nearest to the drop takes it; when that chest is full, the next nearest. A kiln's coal or a smelter's bars drop at
  the station's output and go into the nearest pickup chest holding them the same way.

Favourites and junk marks are saved with the character and follow it between worlds.

### Salvage
A third tab, `Salvage`, after Craft and Upgrade. It lists every stack in your inventory that has a recipe; select
one and the requirement rows show what comes back, the craft button reads `Salvage`. Returns are `Return
Fraction` (0.75) of each material, rounded, at least one, upgrade materials included; never more than the item
cost. Trophies, quest items, favourites and items on the deny list of `OpenKeep.Salvage.yml` are not listed. Nor are
items carrying another mod's item data (`Skip Items With Mod Data`, on; `Mod Data Prefixes`, default `ecf_`), so
salvaging never destroys affixes or other state a mod keeps on the item. `Salvage Key` (Backspace) salvages the
hovered stack after confirmation. If the materials do not fit, nothing happens.

With EliteCrafting installed: OpenKeep salvages ordinary crafted items into materials and leaves EliteCrafting's
magic items alone; those are ground into shards by EliteCrafting's own Salvage key.

### Batch crafting: craft many at once
The Craft tab of every workbench, forge, cauldron and other crafting station, and crafting by hand, gets `-` and `+`
buttons with the amount between them, left of the Craft button. Set the amount and press Craft once: that many are
made in one bar. The requirement rows show the materials for the whole batch, and a recipe that makes several (20
arrows) makes that many per step; the name above shows the total.
- Click `-` or `+` to change the amount by one. Hold Shift to go by tens (1, 10, 20 ...), hold Ctrl for 1 or for
  as many as you can make. Or click the number, type an amount and press Enter. On a gamepad the D-pad's left and
  right step the amount.
- `+` and a typed amount stop at what you can make: your materials (with Reach, the chests around you too), room in your inventory,
  and `Max Amount` (100). The amount goes back to 1 when you pick another recipe and drops by itself once the
  materials run out.
- It is the game's own multi-craft (Shift + Craft makes 5 in vanilla) with a chosen amount: every item costs, earns
  skill and rolls the crafting bonus as if crafted alone; one bar takes the game's multi-craft time. Hold Reach's
  `Pull Modifier` (Alt) as you press Craft and the materials for the whole batch move into your inventory first.
- Not on the Upgrade tab (the game has no multi-upgrade) or the Salvage tab. `Enabled = false` brings back the
  game's panel and Shift + Craft.

### Stacks: stack sizes and weights
`Stack Multiplier` and `Weight Multiplier` (both 1 by default) scale every item; `OpenKeep.Stacks.yml` sets
absolute values per item, pattern or group. Values show in tooltips before an item is ever moved. `Ignore
Teleport Restriction` lets everything through portals. `Merge Into Chests` fills a chest's partial stacks when
you drag a stack in. `Per Item Config Entries` generates one stack and one weight entry per item into the config
file for Configuration Manager users. `Write Documentation` writes `OpenKeep.Items.txt` (every item, prefab name,
vanilla and current values) and `OpenKeep.Containers.txt` (every container prefab and its size) next to the cfg.

### Capacity: container sizes, station capacity and hover contents
`OpenKeep.Containers.yml` is filled with every container prefab of your game (modded ones included) and its
vanilla size the first time a world loads, all commented out; uncomment a line and change the numbers to resize
new and existing containers of that prefab. Hovering a container shows how full it is and up to `Hover Lines`
lines of contents.

`OpenKeep.Stations.yml` does the same for workstations: every smelter, blast furnace, charcoal kiln, eitr refinery,
spinning wheel, windmill, hot tub and modded station built like them is listed with how many items it holds
(`items:`) and how much fuel (`fuel:`); uncomment a line and change the numbers (1 to 1000). Lowering a cap loses
nothing: a station holding more keeps working and takes more only once it is below the cap. A station only catches
up an hour of work when you come back to it (the game's limit), so very large item caps fill up faster than an
unattended station empties them.

### Carts as workbenches
`Cart Workbench` (off by default) gives every cart a workbench: craft (Shift + E on the cart), repair and build
within `Cart Station Range` as if a workbench stood there, no roof needed. `Cart Station Level` sets its level;
workbench extensions near the cart add to it.

### Signs: what is in the chest, without opening it
Off by default (`7. Signs / Enabled`). When on, every container a player has built gets a real vanilla sign
floating just above it, facing the same way, that lists the contents most numerous first: `Wood, Stone, Resin,
Flint`, or `Wood 120, Stone 45` with `Show Counts`. The sign is rewritten within `Update Seconds` (2) of any
change by whoever owns the chest at the time, lists at most `Max Items` (4) kinds, never exceeds `Max Characters`
(50) and ends with an ellipsis when something was left out; an empty chest shows `Empty Text` (blank by default).
`Height` (0.1 m above the chest's top) and `Rotation` (degrees added to the chest's facing) move every sign;
`OpenKeep.Signs.yml` switches prefabs off and moves or turns the sign per prefab.

- Your own words win: edit a sign with `E` and the mod leaves it alone until you clear the text.
- Removing a sign with the hammer means "no sign here": the chest gets none again, even after a reload, until
  `openkeep signs reset`. A sign a troll smashes simply comes back. Automatic signs never crumble or rot: they
  need no support and no roof (player-placed signs are unchanged).
- Removing the chest removes its sign. Ship holds, carts, dungeon chests and spawned treasure get no sign; the
  private chest gets one.
- The sign costs nothing, but it is the vanilla sign: removing it with the hammer returns one wood, so a removed
  sign is a wood each. The signs are ordinary pieces of the world: every player sees them, they are saved with the
  world, and they stay in the world when the mod is removed (they just stop updating). Switch `Enabled` off before
  removing the mod if you want them gone; the chest owners' games take every sign down as the chests load.

### Shared chests: several players in one chest
The game gives a chest to the player who opened it and refuses everyone else while it is in use. `Shared Chests`
in section `0. Containers` (default `Off`) changes that:

- `Off`: as the game: a chest someone else has open is skipped by every OpenKeep action and refused when you
  interact with it.
- `View`: interacting with a chest another player has open shows its contents read-only, titled
  `Chest (in use by <player>)`, refreshed as they change; when the other player closes it your panel turns live
  without closing.
- `Full`: as `View`, and two people can work the same chest at once. Click, drag and drop, the split dialog,
  Take all and Stack all send a request to the chest owner's game, which applies it or refuses it; nothing is
  moved on your side until it says yes, so if both of you grab the same stack exactly one gets it and the other
  sees `Someone else got there first`. A slot the other player pressed the mouse on is tinted (`Touch Colour`)
  and its tooltip says `<name> is moving this`. A request without an answer after `Request Timeout` (2 s)
  counts as refused: `The chest did not answer`.

OpenKeep's own quick stack, store all, top up, routing, store one, dump and trash work into a shared chest the
same way; the message for such a chest arrives when its answer does (`Moved n stacks to Chest`), and what it had no
room for goes on to the next nearest chest holding the item (after a quick stack or dump, a line in the top left
says where). Sorting a
chest someone else is using is refused. Crafting, building and station feeding never count or pay from a chest
another player is using, whatever the mode, so a requirement is never shown as covered by an item the other
player may take first. Both players need the mod; a player without it gets the game's usual refusal.

### Homestead: beds, portals, campfires on wood, honey, fires, torches, stations, rest, repair and pets
Section `8. Homestead`, synced from the server like every gameplay setting.

- Beds (`Nearest Bed Respawn`, on): every bed you own is a spawn bed. When you die you wake in your own bed
  nearest to where you died; if that bed is gone or no longer yours the next nearest is tried, and only when none
  is left do you wake at the world's start. Any of your beds lets you sleep, with the game's usual checks (night,
  no enemies, roof, fire, dry), and the bed you last claimed or slept in stays your spawn point for your first
  spawn in a world. Your beds are remembered per character and per world when you claim one, use one or come near
  one you own, so beds claimed before the mod count once you have been near them. Every one of them shows on your
  map with the game's bed icon (`Beds On Map`, on, your own choice). Off: only your last bed counts, as in the game.
- Choose your bed after death (`Bed Choice Seconds`, 30): with two beds or more, dying opens the map with your beds
  on it, the nearest pulsing, and a countdown in the map's upper right corner. Click a bed to wake there; with no
  click, you wake in the bed nearest to where you died when the countdown ends. The map key or Escape takes the nearest at once. The map zooms out far enough to show every bed. 0: no map,
  always the nearest. Not in worlds without a map.
- Quick respawn (`Quick Respawn`, on): the closer to where you died you wake, the sooner you wake. The game waits
  10 seconds after a death and then 8 seconds of loading; that wait shrinks in proportion to the distance between
  where you died and the bed (the world start without one), from `Quick Respawn Seconds` (1) right beside it to the
  game's full wait at `Quick Respawn Range` (1000 m) and beyond. Die next to your bed and you are back in a second.
  With the bed choice, the time you spend choosing counts towards the wait, so clicking a near bed wakes you at
  once. A far area still takes as long as it needs to load. After a death you also wake standing, ready to move,
  instead of the game's getting-up animation (`Stand Up On Respawn`, on); logging in keeps the game's.
- Quick portals (`Quick Portals`, on): the closer together two portals are, the quicker the jump. The game's
  8 seconds shrink in proportion to the distance, from `Quick Portal Seconds` (0.5) for portals side by side to the
  full 8 seconds at `Quick Portal Range` (10000 m) and beyond: about 0.7 s for 250 m, 1.2 s for 1 km, 4 s for 5 km.
  Lower the range (4000 to 5000) for a bigger difference between near and far portals. On a server you also wait
  until the server has sent everything around the far portal, so you never land before your base is there, and
  never longer than the game's 8 seconds. Every long jump counts: the game's portals, portal
  mods that jump the game's way (Wayfare's map portals too) and the console's `goto`; dungeon doors are left alone.
  The game still waits for a far area to load. A jump to a place already loaded around you (roughly 100-150 m)
  keeps the screen clear, with no black screen and no teleport swirl; farther jumps show the game's teleport screen
  while the area loads (`Portal Screen Only When Loading`, on, your own choice). Behind that screen, and while you
  wait to respawn, the land and everything on it load as fast as your PC allows instead of the game's one 64 m square
  every 0.1 s and 100 objects 30 times a second (`Quick Area Loading`, on, your own choice): the ground and buildings
  near the far portal come first, the far edge of your view finishes after you land. With a high simulation
  distance the game otherwise spends several seconds per long jump loading land before anything appears.
- Campfires on wooden floors (`Build On Wood`, default `fire_pit`): the game refuses a campfire on a wooden floor;
  the pieces listed here may be built on wooden floors and other wooden pieces anyway. The game has the same rule
  for `bonfire`, `smelter`, `charcoal_kiln`, `blastfurnace`, `eitrrefinery`, `piece_FrostKiln` and `windmill`: add
  them comma separated, or leave the value empty for the game's rule. Every other placement rule stays (nearly
  level, no clipping into walls, wards, no-build zones). The fire burns, smokes and warms as on the ground and rests
  on the floor like any piece, so removing the floor breaks it. Fires already built stay when you take a name out.
- Honey (`Honey Per Day`, default 0 = the game's 1.5 a day): how much honey each beehive makes per in-game day.
  With `Honey Per Player Online` on, each hive makes as much honey per day as there are players on the server (1 in
  single player), following players as they join and leave. A hive still holds at most 4 honey, so at high rates
  empty it more often. Changing the rate keeps the progress a hive has made toward its next honey. Bird nests keep
  the game's rate.
- Fires that feed themselves (`Auto Fuel`, on; `Auto Fuel Range`, 20 m): every fire you built that burns an item
  (campfires, hearths, bonfires, braziers, standing torches, sconces, the jack-o-turnip, the snow lantern and modded
  fires) takes its own fuel (wood, resin, coal, guck or greydwarf eyes) from the nearest containers within range of
  the fire, not of you. A burning fire stays full while a chest nearby has fuel, and one that burned out while you
  were away fills up when you come back. Fires with endless fuel, the resin candle (it cannot be refilled) and torches
  out for the day are left alone. Reach's rules apply: `enabled: false` for a fire in the `stations:` map of
  `OpenKeep.Reach.yml` stops it, its `allow`/`deny` narrow the fuel, and the `containers:` map and the switches of
  section 0 decide which chests give. Chests another player has open, private chests and wards you have no access to
  are skipped: the game of whoever is nearest runs the fire and uses that player's access. On a dedicated server the
  fires right around the world's centre are run by the server and do not feed themselves.
- Torches at night (`Torches Night Only`, on; `Torch Pieces`; `Torch Margin`, 1): the standing torches (wood, iron,
  green, blue) and the sconce light before nightfall and go out after daybreak, so they burn fuel only around the
  dark. `Torch Margin` is how many in-game hours early they light and late they go out (an in-game hour is 75
  seconds of the game's 30 minute day; 0 follows the game's own night, at most 4). A torch out for the day gives no
  light, burns nothing, and its hover says `Lights at nightfall`. To keep one torch lit day and night, look at it and
  press `Torch Switch Key` (O): the hover shows `[O] Keep lit`, and on a torch kept lit `[O] Light at night only`,
  which puts it back on the schedule (by day it goes out at once). The choice is stored in the torch, so every
  player sees it and it survives restarts; inside a ward only players with access can switch it. Other fires can be
  added to `Torch Pieces` (braziers, the jack-o-turnip, the snow lantern, the resin candle); campfires, hearths and
  braziers also give the warmth beds and resting need, which is gone while they are out. Each torch is switched once
  at nightfall and once at daybreak, so a fire you can switch by hand (the resin candle) keeps your choice until the
  next one. With `Torches Night Only` off every torch simply burns and the key does nothing; torches kept lit stay
  marked for when it is on again.
- Stations that feed themselves (`Auto Feed Stations`, on; `Auto Feed Range`, 4 m; `Auto Feed Skip`; `Auto Feed Leave`, 1): every smelter,
  blast furnace, charcoal kiln, eitr refinery, spinning wheel, windmill and hot tub you built (modded stations of the
  same kind too) takes what it works (ore, scrap, wood, soft tissue, flax, barley) and its fuel (coal, sap, wood) from
  the containers beside it. The range runs from the station's outer edge to the middle of a container, so 4 m
  reaches the chests standing around even a big station, a step away too. While there is room the station takes one item and one
  fuel a second, as if someone fed it by hand, and then one of each as it works them off; with the chests stocked
  it never stops. `Auto Feed Skip` names items it never takes (default `FineWood, RoundLog`, so the charcoal kiln
  burns only plain wood; you can still put them in by hand). `Auto Feed Leave` is how many of each item stay in every
  chest (1: the chest keeps its last ore, wood or coal, so quick stack still sends that item to it; 0: it empties;
  feeding by hand is not limited). Cooking stations, ovens and fermenters are not fed. The
  same rules as fires apply: `stations:` in `OpenKeep.Reach.yml` (`enabled: false` stops a station, `allow`/`deny`
  narrow what it takes), `containers:`, the switches of section 0, and the access of whoever's game runs the station.
- Rested sooner (`Rested Delay`, 5 s): the Rested buff comes after this many seconds of resting instead of the
  game's 20. Resting is still the game's: near a fire, sitting or under a roof, dry, warm enough and with no enemy
  aware of you; stepping away starts the wait over. How long Rested lasts still grows with comfort, and the Resting
  icon still shows your comfort level. 20 keeps the game's wait, 0 gives Rested at once.
- Area repair (`Area Repair`, on): repairing a piece with the hammer also repairs every damaged piece touching it:
  the floor tiles, walls, beams and furniture right next to it, not the whole building, at most 64 per swing. Each
  of them needs what a repair by hand needs: its crafting station within range of you (stone pieces the
  stonecutter) and access to any ward it stands in; the others are skipped quietly. The swing costs the stamina and
  hammer wear of one repair, the extra pieces are free, and a line under the game's own says how many were repaired
  too. Hitting a piece that needs no repair repairs nothing around it. Ships and carts are never repaired this way.
- Repair at stations (`Auto Repair`, on): opening a workbench, forge, black forge, galdr table, artisan table or any
  other station with the game's repair button repairs, at once, every item in your inventory that this station can
  repair at its current level, what you wear included, exactly as if you pressed its repair button once for each: the
  same items (a forge item waits for a forge, an item needing a higher station level waits for the extensions), the
  same Crafting skill gain, no cost. One repair sound and one message, `Repaired 3 items`; nothing when nothing needed
  repair. A cart with `Cart Workbench` on repairs like a workbench when you open it. Each player's own gear, on every
  server.
- Pets eat from chests (`Pets Eat From Chests`, on; `Pet Chest Range`, 10 m): a hungry tamed animal (wolf, boar, lox,
  hen, asksvin, any modded tame) walks to a container within range that holds food it eats and eats one item from
  it, just as it eats from the ground, so its fed timer starts again and tames in a pen stay fed from a stocked chest.
  Food on the ground near it still comes first, and animals still being tamed eat only from the ground. The same
  rules as fires apply: `containers:` in `OpenKeep.Reach.yml`, the switches of section 0, and the wards and chest
  access of whoever's game runs the animal.

### With PackPanel
PackPanel is a separate mod for the player's own inventory: a bigger grid, labelled slots for armour, a backpack,
worn utilities, food, meads and ammo, a coin purse, a key ring, craftable backpacks and a new look. OpenKeep needs
none of it, and PackPanel works without OpenKeep. With both installed they fit together by themselves:

- The button row (`Quick stack`, `Store all`, `Top up`, `Sort`) sits inside the inventory panel, under the grid, and
  the trash can joins it right of `Sort`.
- Quick stack, store all, dump and sort work on PackPanel's whole grid, a backpack's rows included, and never touch
  its slots or its key ring; so does a shared chest's stack all. Top up still refills the food and ammo in the slots.
- PackPanel's `Key Stack` is written by OpenKeep's `4. Stacks` like any stack size, so an `OpenKeep.Stacks.yml` entry
  for a key still wins.

### Hotkeys (per player, not synced)

| Setting | Default | Does |
| --- | --- | --- |
| `1. Reach / Fill Modifier` | LeftShift | held while using a station: keeps adding until it is full |
| `1. Reach / Pull Modifier` | LeftAlt | held while using a station or clicking Craft: materials move into the inventory instead |
| `1. Reach / Toggle Key` | LeftAlt + R | Reach on or off for this character |
| `1. Reach / Link Key` | LeftAlt + L | draws the container to station lines for `Link Seconds` |
| `2. Stow / Quick Stack Key` | Q | quick stack |
| `2. Stow / Store All Key` | G | store all into the open container |
| `2. Stow / Top Up Key` | R | top up from containers |
| `2. Stow / Sort Inventory Key` | T | sort the player inventory |
| `2. Stow / Sort Container Key` | Y | sort the open container |
| `2. Stow / Favourite Item Key` | F | hovered item becomes a favourite |
| `2. Stow / Favourite Slot Key` | LeftShift + F | hovered slot becomes a favourite |
| `2. Stow / Junk Key` | J | hovered item is marked as junk |
| `2. Stow / Trash Key` | Delete | trash the hovered stack |
| `2. Stow / Destroy Junk Key` | LeftShift + Delete | destroy every junk stack |
| `2. Stow / Route Modifier` | LeftControl | with a left click: route the stack |
| `2. Stow / Store One Key` | V | one item to the open or nearest holding container (the next holding one when it is full) |
| `2. Stow / Find Key` | Z | mark every nearby container holding the hovered item |
| `2. Stow / Dump Key` | LeftAlt + D | outside the inventory: quick stack to every nearby container |
| `2. Stow / Cycle Previous Key`, `Cycle Next Key` | LeftArrow, RightArrow | switch to the previous or next chest around you |
| `2. Stow / Button Row Offset` | 0 | moves the button row up (positive) or down (negative) by that many pixels; applies at once |
| `2. Stow / Trash Can On Stat Column` | on | the trash can on its own plate in the game's style under the armour; off puts it in the button row; next time you enter a world |
| `3. Salvage / Salvage Key` | Backspace | salvage the hovered stack |
| `8. Homestead / Torch Switch Key` | O | outside the inventory, looking at a torch: keep it lit day and night, or put it back on the night schedule |

Single keys fire only while no Shift, Ctrl or Alt is held, so `F` and `Shift + F` never clash; they work while the
inventory is open, except `Torch Switch Key`, which works only outside it, looking at a torch. A key with modifiers
(`LeftAlt + D`) fires while you walk: other keys may be held, other modifiers may not.

### Config file
`BepInEx/config/milkyteam.openkeep.cfg`, written on first start. Sections: `0. Containers` (`Ships`, `Carts`,
`Player Chests`, `Honour Wards`, `Shared Chests`), `1. Reach`, `2. Stow`, `3. Salvage`, `4. Stacks` (plus `4a. Item
Stacks` and `4b. Item Weights` when per item entries are on), `5. Capacity`, `6. Carts`, `7. Signs` (`Enabled`,
`Show Counts`, `Max Items`, `Max Characters`, `Update Seconds`, `Height`, `Rotation`, `Empty Text`), `8. Homestead`
(`Nearest Bed Respawn`, `Bed Choice Seconds`, `Quick Respawn`, `Quick Respawn Range`, `Quick Respawn Seconds`,
`Stand Up On Respawn`, `Quick Portals`, `Quick Portal Range`, `Quick Portal Seconds`, `Build On Wood`, `Honey Per Day`, `Honey Per Player
Online`, `Auto Fuel`, `Auto Fuel Range`, `Torches Night Only`, `Torch Pieces`, `Torch Margin`, `Auto Feed Stations`,
`Auto Feed Range`, `Auto Feed Skip`, `Auto Feed Leave`, `Rested Delay`, `Area Repair`, `Auto Repair`, `Pets Eat From
Chests`, `Pet Chest Range`; per player `Beds On Map`, `Portal Screen Only When Loading`, `Quick Area Loading`, `Torch Switch Key`),
`9. Shared`
(`Request Timeout`, `Touch Seconds`; per player `Show Touches`, `Touch Colour`), `10. Batch Crafting` (`Enabled`,
`Max Amount`), and `General / Lock Configuration`.
Every entry has a description in the file. Gameplay settings are synced from the server and locked; keys,
display formats, confirmations, sort orders and colours are yours.

### YAML files
Next to the cfg, created on first start, hot reloaded a few seconds after a save, synced from the server, and
editable in game through the Configuration Manager entries `Edit YAML`. Extra files named
`OpenKeep.<Module><anything>.yml` are merged in. Every list uses the same vocabulary: `Wood` (prefab name or
`$item_wood`), `prefix:Trophy`, `suffix:Ore`, `type:Material`, `group:Ores` (a group of the file's `groups:` map),
`*` (everything). Container keys are prefab names; `OpenKeep.Containers.txt` lists them all.

`OpenKeep.Reach.yml`: which containers Reach may use, per prefab range and item lists, and what each station
prefab may be fed (feeding, Fill, Pull and the hover line only; crafting and building are untouched).
```yaml
# range: 20
groups:
  Ores: [prefix:CopperOre, TinOre, IronScrap, SilverOre, BlackMetalScrap, FlametalOre]
containers:
  piece_chest_private:
    enabled: false
  Cart:
    range: 10
  VikingShip:
    deny: [type:Trophy]
  piece_chest_blackmetal:
    allow: [group:Ores, type:Material]
stations:
  piece_cookingstation:
    deny: [NeckTail]        # never pulled onto the cooking station; crafting still sees it
  smelter:
    allow: [group:Ores]
  charcoal_kiln:
    enabled: false          # no feeding, filling, pulling or hover line for this station
```

Ground pickup is off unless `Ground Pickup` is on in the config and the chest prefab has `pickup: true` in the YAML; it is a world tick that runs on the chest owner's client, so turn it on deliberately.

`OpenKeep.Stow.yml`: groups for routing and the ground pickup rules.
```yaml
groups:
  Ores: [prefix:CopperOre, TinOre, IronScrap, SilverOre, BlackMetalScrap, FlametalOre]
containers:
  piece_chest_blackmetal:
    pickup: true              # takes items from the ground (needs the cfg Ground Pickup switch)
    accept: [group:Ores]      # taken and routed here even when the chest does not hold them yet
    refuse: [type:Trophy]     # never taken, never routed here
```

`OpenKeep.Salvage.yml`: what is never salvaged and per item fractions.
```yaml
deny: [type:Trophy, prefix:MeadBase]
overrides:
  ArrowFlint: { fraction: 0.5 }
```

`OpenKeep.Stacks.yml`: absolute stack sizes and weights; the multipliers here replace the cfg ones.
```yaml
stack multiplier: 2
items:
  Wood: { stack: 100 }
  Stone: { stack: 100, weight: 1.0 }
  group:Ores: { weight: 5 }
```

`OpenKeep.Containers.yml`: container grid sizes.
```yaml
containers:
  piece_chest_wood: { width: 8, height: 4 }
  VikingShip: { width: 8, height: 3 }
```

`OpenKeep.Stations.yml`: how much each station holds, `items:` (ore, wood, flax, barley waiting) and `fuel:`
(coal, wood), from 1 to 1000. A station that takes no items (the hot tub) or no fuel (the charcoal kiln, spinning
wheel, windmill) keeps it that way; such a key is ignored with a warning. `OpenKeep.Stations.txt` lists every
station prefab with its vanilla values.
```yaml
stations:
  smelter: { items: 30, fuel: 60 }
  blastfurnace: { items: 30, fuel: 60 }
  charcoal_kiln: { items: 100 }
  windmill: { items: 100 }
```

`OpenKeep.Signs.yml`: which containers get a contents sign and where it sits. `offset` moves the sign from the
computed place (the middle of the container's top, `Height` metres up) in the container's own axes, x right, y up,
z forward, in metres; `rotation` is added to the container's facing on top of the `Rotation` setting.
```yaml
containers:
  piece_chest_wood: { enabled: true, offset: [0, 0, 0.3], rotation: 0 }
  piece_chest_private: { enabled: false }
```

### Server settings

Gameplay settings come from the server. With `Lock Configuration` on (the default), every player uses the server's
values and cannot change them locally; server admins can still edit them in game. With it off, each player uses
their own file. In the console (F5), `charter status` shows whether the server binds your settings, `charter diff`
lists where the server's values differ from your own file, and `charter versions` lists the mods on both sides.
Joining with a missing or mismatched version of the mod shows one screen naming the mod and both versions, with a
refusal code that is also written to the server's and your own log.

### Console command
`openkeep reload` reloads the cfg and every YAML file (admin or host on a server). `openkeep containers` lists the
reachable containers around you with prefab name, distance and item count. `openkeep write docs` writes
`OpenKeep.Items.txt` and `OpenKeep.Containers.txt`. `openkeep signs` lists the loaded containers with their sign
state (sign, player text, no sign, opted out); `openkeep signs reset` (admin or host on a server) allows signs
again on containers whose sign was removed with the hammer; `openkeep signs rewrite` rewrites the sign of every
loaded container your game owns.

### Install
1. Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Install this mod. On a server, install it on the server and on every client.

For manual install, drop `OpenKeep.dll` into `BepInEx/plugins`. The config file and the YAML files are created on
the first run. A server pushes its gameplay settings and YAML files to every client; with `Lock Configuration` on
(the default) clients cannot change them while connected.

### Warnings
- Items in the extra rows and columns of an enlarged container are invisible to players without the mod and to
  the vanilla game. Everyone on a server should run the same container sizes.
- Lowering a stack multiplier or an absolute stack size, or removing the mod, loses the part of a stack above the
  new maximum when the inventory that holds it loads. Split large stacks before lowering a limit.
- Signs are real vanilla signs. They stay in the world when the mod is removed, and each one removed with the
  hammer returns one wood. Switch `7. Signs / Enabled` off before removing the mod to have them taken down.
- In the Ashlands, and on worlds with the Fire world key, campfires and bonfires throw embers that set nearby wood
  alight. A campfire built on a wooden floor there sets that floor on fire; put a stone floor under it.
- A torch that `Torches Night Only` put out for the day stays dark without the mod, and the game has no switch to
  light it. Before removing OpenKeep, turn the setting off and visit your bases by day (or rebuild the torches).

### Building
Requirements: .NET SDK 8, Valheim installed with BepInEx, and the `ValheimModLibs` folder next to this one.
`pack.ps1` builds the mod and creates a Thunderstore zip in `thunderstore/`.

```
dotnet build OpenKeep/OpenKeep.csproj -c Release
```

### Bugs and feature requests
The source is open, at the repository linked from the store page (`website_url` in `thunderstore/manifest.json`):
https://github.com/geraldjglasgow/ValheimMods. Found a bug or want a feature? Open an issue at
https://github.com/geraldjglasgow/ValheimMods/issues and name the mod, its version and what happened.

### Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.

## License

GNU General Public License v3.0 (GPL-3.0). You are free to use, study, share and modify it, and anything you distribute that is built from it must carry the same freedoms and be released under the same licence, with source. See the `LICENSE` file for the full terms.
