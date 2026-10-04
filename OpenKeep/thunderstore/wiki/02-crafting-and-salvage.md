# Crafting and Salvage

## Crafting from chests

Containers near you count as part of your inventory for crafting, building, upgrading and feeding stations (section `1. Reach`). Your inventory pays first, then the nearest containers. Requirement rows show `3 + 12`: what you carry, plus what the containers add.

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Enabled` | on | Server | Master switch for this section. |
| `Range` | 20 m | Server | How far a container may be from you. |
| `Crafting` | on | Server | Recipes (and Epic Loot's enchanting table) use containers. |
| `Building` | on | Server | Hammer, hoe and cultivator pieces use containers. |
| `Upgrading` | on | Server | Item upgrades use containers. |
| `Feed Stations` | on | Server | Using a station takes what it needs from containers. |
| `Toggle Key` | Left Alt + R | Player | Turns this off or on for your character. |
| `Requirement Display` | Split | Player | `Split` (`3 + 12`), `Total` (`15`) or `Vanilla`. |
| `Storage Colour` | #6fc3ff | Player | Colour of the container amounts and link lines. |
| `Flash On Pull` | on | Player | Rows paid from containers flash after crafting. |
| `Show Links` | on | Player | Placing a container draws lines to the stations it reaches; needed for `Link Key`. |
| `Link Key` | Left Alt + L | Player | Draws those lines for every container in reach. |
| `Link Seconds` | 5 | Player | How long lines and Find marks stay. |
| `Fill Modifier` | Left Shift | Player | Hold while using a station to fill it. |
| `Pull Modifier` | Left Alt | Player | Hold while using a station or pressing Craft to move the materials into your inventory instead. |

### Which containers count

A container counts when it is in range, you could open it, no other player has it open, and these switches allow it. `openkeep containers` lists the ones within 20 m.

| Setting (`0. Containers`) | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Ships` | on | Server | Ship storage counts. |
| `Carts` | on | Server | Cart storage counts. |
| `Player Chests` | off | Server | The personal chest counts, for its owner. Also remove its entry from `OpenKeep.Reach.yml`. |
| `Honour Wards` | on | Server | Skip containers in wards you have no access to. |
| `Shared Chests` | Off | Server | What happens with a chest another player has open; see [Multiplayer and Commands](wiki:Multiplayer and Commands). |

### Feeding stations

Use a smelter, kiln, refinery, spinning wheel, windmill, hot tub, cooking station, oven, fire, torch or fermenter while carrying nothing it takes, and one unit comes from a nearby container. Hold Shift to fill it, or Alt to pull a stack into your inventory. The hover text shows `From storage` with how much the containers hold for it.

Hold Alt and press Craft to move the missing materials for the whole batch into your inventory first.

### OpenKeep.Reach.yml

| Key | Meaning |
| --- | --- |
| `range:` | Range for every container, replacing the cfg `Range`. |
| `groups:` | Named item lists for `group:` entries. |
| `containers:` `<prefab>:` `enabled: false` | No OpenKeep feature uses this container. |
| `containers:` `<prefab>:` `range:` | Range for this container only. |
| `containers:` `<prefab>:` `allow:` / `deny:` | Which of its items may be used. Deny wins. |
| `stations:` `<prefab>:` `enabled: false` | This station never takes from containers. |
| `stations:` `<prefab>:` `allow:` / `deny:` | Which items this station may take. Crafting still sees them. |

```yaml
containers:
  piece_chest_private:
    enabled: false
  VikingShip:
    deny: [type:Trophy]
stations:
  piece_cookingstation:
    deny: [NeckTail]
```

### Cart workbench

| Setting (`6. Carts`) | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Cart Workbench` | off | Server | Every cart carries a workbench; `Shift + Use` opens it. No roof or fire needed. |
| `Cart Station Level` | 1 | Server | Its level; workbench extensions nearby add to it. |
| `Cart Station Range` | 10 m | Server | Build and craft range around the cart. |

### Epic Loot

Its enchanting table takes materials from chests in reach. Gear stored in chests is never offered for enchanting or sacrifice.

## Batch crafting

A `- amount +` stepper beside Craft makes many at once (section `10. Batch Crafting`).

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Enabled` | on | Server | Show the stepper. Off: the game's panel, where Shift + Craft makes 5. |
| `Max Amount` | 100 | Server | The most one Craft makes (1 to 1000). |

| Input | Effect |
| --- | --- |
| Click `+` / `-` | One more or fewer. |
| `Shift` + click | To the next multiple of ten. |
| `Ctrl` + click | To 1, or to the most you can make. |
| Click the number | Type an amount. |
| Gamepad D-pad left / right | Step the amount. |

The amount counts crafts: 5 crafts of 20 arrows makes 100. It stops at what your materials (chests included) and inventory room allow. With GrindstoneSkills, each dish rolls its own stars.

## Salvage

Turns a whole stack back into materials (section `3. Salvage`): pick it on the **Salvage** tab of the crafting panel, or hover it and press `Backspace`. Returns must fit in your inventory.

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Enabled` | on | Server | Salvage tab and key work. |
| `Return Fraction` | 0.75 | Server | Share of each material returned (0 to 1). |
| `Rounding` | Round | Server | `Floor`, `Round` or `Ceil`. |
| `At Least One` | on | Server | Each material returns at least one. |
| `Upgrade Materials` | on | Server | Upgraded items also return their upgrade materials. |
| `Require Known Recipe` | on | Server | Only items whose recipe you know. |
| `Require Station` | off | Server | The recipe's station must be in range. |
| `Skip Items With Mod Data` | on | Server | Never salvage items another mod keeps data on. |
| `Mod Data Prefixes` | ecf_ | Server | Which mods' data to look for (`ecf_` is EliteCrafting). |
| `Salvage Key` | Backspace | Player | Salvage the hovered stack, after asking. |

Example at the defaults: an item costing 20 Iron and 3 Wood returns 15 Iron and 2 Wood. Durability does not matter. Trophies, quest items, favourite and equipped items, and items with no recipe are never salvaged.

### OpenKeep.Salvage.yml

| Key | Meaning |
| --- | --- |
| `deny:` | Items never salvaged. Default `[type:Trophy, prefix:MeadBase]`. |
| `overrides:` | A return fraction per item: `ArrowFlint: 0.5`. |
| `groups:` | Named item lists for `group:` entries. |
