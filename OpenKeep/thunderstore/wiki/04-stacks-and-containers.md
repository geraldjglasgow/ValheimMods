# Stacks and Containers

## Stack sizes and weights

Section `4. Stacks`.

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Enabled` | on | Server | Master switch. Off: vanilla stacks and weights. |
| `Stack Multiplier` | 1 | Server | Multiplies every stack size (0.01 to 1000). Items that never stack stay at 1. |
| `Weight Multiplier` | 1 | Server | Multiplies every item's weight (0 to 100). |
| `Ignore Teleport Restriction` | off | Server | Portals accept every item, ore included. |
| `Merge Into Chests` | on | Server | A stack dragged onto a chest tops up its partial stacks first. |
| `Per Item Config Entries` | off | Server | Adds a stack and weight entry per item to the cfg, for configuration managers. |
| `Write Documentation` | on | Server | Writes the item, container and station lists next to the cfg when a world loads. |

**Warning:** lowering a stack size loses the items above the new limit.

`OpenKeep.Stacks.yml` sets single items and overrides the cfg:

```yaml
stack multiplier: 2
weight multiplier: 1
items:
  Wood: { stack: 100 }
  Stone: { stack: 100, weight: 1.0 }
  prefix:Trophy: { weight: 0.5 }
```

With PackPanel, its key ring keys stack to at least its `Key Stack`.

## Container sizes and station capacities

Section `5. Capacity`.

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Enabled` | on | Server | Master switch. Off: vanilla sizes and capacities. |
| `Hover Contents` | on | Player | Hovering a container lists what it holds. |
| `Hover Lines` | 8 | Player | How many lines (0 to 40). |
| `Hover Fill` | Fraction | Player | `Fraction` (`12 / 24 slots`), `Percent` (`50% full`) or `Off`. |

`OpenKeep.Containers.yml` sets each container's grid. The first time a world loads, it lists every container with its vanilla size, commented out: remove the `#` and change the numbers. A chest never shrinks below a slot that holds an item. The chest panel shows at most 8 columns.

```yaml
containers:
  piece_chest_wood: { width: 8, height: 4 }
```

`OpenKeep.Stations.yml` sets how many items and how much fuel smelters, blast furnaces, kilns, refineries, spinning wheels, windmills and hot tubs hold (1 to 1000). It is filled in the same way. Lowering a cap loses nothing.

```yaml
stations:
  smelter: { items: 30, fuel: 60 }
```

## Contents signs

With signs on, a sign above every chest you build names what it holds, most first: `Wood, Stone`. Ships, carts and dungeon chests get none. Section `7. Signs`.

| Setting | Default | Who | Meaning |
| --- | --- | --- | --- |
| `Enabled` | off | Server | Signs on. Off removes them. |
| `Show Counts` | off | Server | `Wood 120, Stone 45` instead of `Wood, Stone`. |
| `Max Items` | 4 | Server | Item kinds per sign (1 to 20). |
| `Max Characters` | 50 | Server | Longest text (10 to 200). |
| `Empty Text` | empty | Server | Text on an empty chest's sign. |
| `Update Seconds` | 2 | Server | Fastest a sign is rewritten (0.5 to 60). |
| `Height` | 0.1 | Server | Metres above the chest (-2 to 5). |
| `Rotation` | 0 | Server | Degrees to turn the sign (0 to 359). |

- Write on a sign yourself and it keeps your words until you clear them.
- Remove a sign with the hammer and that chest gets no new one; `openkeep signs reset` brings them back.
- Signs never wear out, and a broken one comes back.

`OpenKeep.Signs.yml` changes signs per chest type:

```yaml
containers:
  piece_chest_wood: { offset: [0, 0, 0.3], rotation: 0 }
  piece_chest_private: { enabled: false }
```

`offset` is `[right, up, forward]` in metres.
