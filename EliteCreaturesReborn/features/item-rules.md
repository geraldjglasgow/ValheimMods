# Elite Creatures Reborn - specification: Item rules

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the second rule file: changing **items themselves**, independently of creatures. What a creature
drops is `loot.md`; this is what the item then is once it exists. The two are deliberately separate - a server can
run item rules with the whole creature side of the mod switched off.

**Status: specified, not built.** Nothing in this file exists in the mod. One part of it - stack size - has a
cross-mod decision already made against it, in section 3.

---

# 1. What can be changed

Per item, or per group of items:

| Property | What it means |
| --- | --- |
| **Weight** | What one unit weighs |
| **Stack size** | How many fit in one inventory slot |
| **Crafting yield** | How many a recipe produces |
| **Automatic pickup** | Whether walking over it picks it up |
| **Floating** | Whether it floats or sinks in water |
| **Portal transport** | Whether it may be carried through a portal |
| **Kept on death** | Whether it stays with you when you die |

Two of these change the game's progression rather than its convenience - **portal transport** is the one most
worth thinking about before switching on, since ore that travels by portal removes the boat trip that the metal
tiers are partly built around. The mod takes no position on whether that is good; it just makes it one line.

## Changes reach items that already exist

**A rule change never requires a new world, a fresh character, or a newly dropped item to take effect.** Editing
weight reaches:

- items lying in the world,
- items in your inventory right now,
- items in containers, **including containers open at the time**.

This is the behaviour that makes the feature usable at all. A stack size that only applied to items picked up
after the edit would mean a server's chests held two kinds of the same item, and no player would ever be able to
tell which was which.

It is provided by the workspace's own `ItemCopies` library, which exists for exactly this: write the values into
the prefab, into every live copy of its shared data, and into new copies as they are made.

---

# 2. Groups

Rules target a **group** rather than listing every item by name. The groups are drawn from what the game actually
has, not from an abstract taxonomy:

| Group | Holds |
| --- | --- |
| Ore | Raw ore, scrap and smelted bars |
| Hides | Skins, pelts, leather and scales |
| Wood | Every wood type, resin and tar |
| Stone | Stone, flint, obsidian and the like |
| Seeds | Seeds, saplings and crops |
| Food | Anything edible, cooked or raw |
| Meads | Every mead and potion |
| Trophies | Every creature trophy |
| Weapons | Everything wielded offensively, ammunition included |
| Armour | Everything worn |
| Tools | Hammer, hoe, cultivator, pickaxe, axe |
| Valuables | Coins, amber, rubies and other treasure |

**A rule naming a single item beats a rule naming a group that contains it.** So a group can be set broadly and
one item pulled back out of it - all Ore stacks to 50, but Black Metal stacks to 30 - without having to abandon
the group and list forty items.

Groups also survive a game update better than a list does: a new ore added by Valheim joins the Ore group without
anyone editing a rule file.

---

# 3. One caution about stack size

**Stack size overlaps with OpenKeep**, which owns stack sizes in its own right.

Two mods setting the same item's stack size is a conflict that is **invisible in play and miserable to diagnose** -
the player sees a number that is neither of the ones they configured, in whichever mod loaded last, with nothing
in any log to say so. And both of these mods are ours, so there is nobody else to blame for it.

The rule, decided and not open:

> **OpenKeep wins on stack size.** Elite Creatures Reborn applies stack sizes only when OpenKeep is absent.

A player running this mod alone still gets the setting. A player running both never gets a fight. Nothing else in
the property list overlaps between the two mods.

Worth revisiting only if OpenKeep's scope ever changes.

---

# 4. Multiplayer

Item rules are **server settings, not world state**. There is nothing per-item to replicate, because the change is
to the item's definition rather than to any one copy of it.

- **The rule file is bound to the server's copy** while lock-to-server is true, reaching clients on join and on
  every edit (`server-enforcement.md`).
- **Each client applies the values locally** to its own prefabs and live copies. No item is sent over the wire.
- Because every machine applies the same values from the same file, **an item weighs the same everywhere** - which
  matters, since weight decides whether a player is overburdened and that is computed locally.
- **Kept on death and portal transport must agree between client and server**, since the server is what enforces
  them. A mismatch here is the failure mode worth testing first: a client that thinks ore is portal-safe and a
  server that does not.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 5. Configuration

All of it in the item rule file, which is **optional** - the mod runs without it, as `configuration.md` requires.

- **Per group**, any of the seven properties.
- **Per item**, any of the seven, beating the group.
- **Per world tier**, so a property can change as the world matures (`world-tiers.md`).
- **An off switch** for the whole feature, leaving every creature feature working.

Validation follows the same rule as the creature file: errors are reported line by line in the log and the
previously loaded rules stay in force. A typo in an item rule must never silently reset every stack size on a
server to vanilla.

---

# 6. Open decisions

**Whether "kept on death" can be set per group at all.** The other six properties are per-item values that happen
to be convenient to set in bulk. Keep-on-death is a rule about a player's death, and a group-wide setting - "keep
all Valuables" - is a much larger change to the game than the same setting applied to one item. It may want to be
item-only, or it may want a warning where it is documented. Not decided.

**What happens to an item already in the world when its group changes underneath it.** `ItemCopies` reaches live
copies, but an item that was in the Wood group and is now named individually has been through two rules in one
session. The precedence is defined; what is not defined is whether the re-application is idempotent for
properties like weight that a player may have already been carrying.
