# OpenKeep

Storage and inventory for Valheim in one mod: craft, build and feed stations straight from nearby chests, ship
holds and carts; stow, top up, sort, junk and route from the inventory panel; a Salvage tab that gives
materials back; stack sizes and weights; bigger chests with their contents on hover; carts that carry a
workbench; and a sign above every chest that names what is inside. Every gameplay setting is server synced and
lockable, every hotkey is per player, and everything is tunable in the config file and six YAML files that hot
reload. Convenience features are on by default; anything
that changes balance (stack sizes, weights, chest sizes) defaults to vanilla.

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
own wood plate between the armour and weight readouts on the panel's right (those two show a larger icon behind
their number and stay put when the inventory grows), and a `Sort` button below the open container's panel, under
its `Take all` line; `Button Row Offset` moves the rows up or down if your screen layout needs it. Every action also has a hotkey that works while the inventory is open. The module is
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
stowed and sorted too, set `Main Inventory Rows` to the number of rows to use. What you never want to keep is junk: mark an item with `J`, and
`Destroy Junk` (Shift + Delete) destroys every junk stack in one go; `Trash Key` (Delete) destroys the hovered
stack and the trash can takes a dragged one, at once (turn on `Confirm Trash` for the game's popup first). What does not belong in
the open chest is routed: Ctrl + click sends a stack to the nearest container that holds the item, an item of its
group, or accepts it in `OpenKeep.Stow.yml`; Store one (`V`) sends a single item; Find (`Z`) marks every nearby
container holding the hovered item with a line and a count; Dump (Alt + D) quick stacks to every nearby container
without opening the inventory; the left and right arrows or the mouse wheel over the container grid cycle
through the chests around you.

- Favourite item (`F`): the item is never quick stacked, stowed, dumped, trashed or salvaged. Favourite slot
  (Shift + F): the slot's content never moves. Top up still refills favourites.
- Ground pickup (off by default): containers whose prefab has `pickup: true` in the YAML pull dropped items
  from the ground after `Pickup Delay` seconds, only items they already hold unless the YAML says otherwise.

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

### Stacks: stack sizes and weights
`Stack Multiplier` and `Weight Multiplier` (both 1 by default) scale every item; `OpenKeep.Stacks.yml` sets
absolute values per item, pattern or group. Values show in tooltips before an item is ever moved. `Ignore
Teleport Restriction` lets everything through portals. `Merge Into Chests` fills a chest's partial stacks when
you drag a stack in. `Per Item Config Entries` generates one stack and one weight entry per item into the config
file for Configuration Manager users. `Write Documentation` writes `OpenKeep.Items.txt` (every item, prefab name,
vanilla and current values) and `OpenKeep.Containers.txt` (every container prefab and its size) next to the cfg.

### Capacity: container sizes and hover contents
`OpenKeep.Containers.yml` is filled with every container prefab of your game (modded ones included) and its
vanilla size the first time a world loads, all commented out; uncomment a line and change the numbers to resize
new and existing containers of that prefab. Hovering a container shows how full it is and up to `Hover Lines`
lines of contents.

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
same way; the message for such a chest arrives when its answer does (`Moved n stacks to Chest`). Sorting a
chest someone else is using is refused. Crafting, building and station feeding never count or pay from a chest
another player is using, whatever the mode, so a requirement is never shown as covered by an item the other
player may take first. Both players need the mod; a player without it gets the game's usual refusal.

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
| `2. Stow / Store One Key` | V | one item to the open or nearest holding container |
| `2. Stow / Find Key` | Z | mark every nearby container holding the hovered item |
| `2. Stow / Dump Key` | LeftAlt + D | outside the inventory: quick stack to every nearby container |
| `2. Stow / Cycle Previous Key`, `Cycle Next Key` | LeftArrow, RightArrow | switch to the previous or next chest around you |
| `2. Stow / Button Row Offset` | 0 | moves the button row up (positive) or down (negative) by that many pixels; applies at once |
| `3. Salvage / Salvage Key` | Backspace | salvage the hovered stack |

Single keys only fire while the inventory is open and no Shift, Ctrl or Alt is held, so `F` and `Shift + F` never
clash.

### Config file
`BepInEx/config/milkyteam.openkeep.cfg`, written on first start. Sections: `0. Containers` (`Ships`, `Carts`,
`Player Chests`, `Honour Wards`, `Shared Chests`), `1. Reach`, `2. Stow`, `3. Salvage`, `4. Stacks` (plus `4a. Item
Stacks` and `4b. Item Weights` when per item entries are on), `5. Capacity`, `6. Carts`, `7. Signs` (`Enabled`,
`Show Counts`, `Max Items`, `Max Characters`, `Update Seconds`, `Height`, `Rotation`, `Empty Text`), `9. Shared`
(`Request Timeout`, `Touch Seconds`; per player `Show Touches`, `Touch Colour`), and `General / Lock Configuration`.
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
