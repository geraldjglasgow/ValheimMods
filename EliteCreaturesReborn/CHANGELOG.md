# Changelog

## 3.3.0

**Devouring reined in.** Out of the box it ate a camp in under two minutes and came out one-tapping players
through a parry; the defaults now leave it a situation you can respond to. A YAML file from an earlier version
pins the old numbers in its `mutation power:` block - edit its Devouring line (or delete it to take the new
defaults).

- `devour cooldown` default 10 → 60: one bite a minute, not six.
- `absorb health` default 100 → 50, `absorb damage` default 100 → 25: it keeps a share of a meal, not the whole
  animal; damage is cut hardest because damage is what kills.
- New `move` field: the devourer's base speed multiplier before its well-fed slow, default 1. Set `move: 0.5`
  to ship them at half speed from the first bite. Never enhancement-scaled.

## 3.2.0

**Thieving** — a tenth mutation, and the only one whose threat is not damage. A Thieving creature takes one item
from your inventory when it lands a hit, carries it where you can see it, and drops every bit of it when it dies.

- **What it takes** — one whole stack from an unequipped slot, backpack first and the hotbar only as a last
  resort. **It never takes equipped gear** — not your weapon, shield, armour or tools — and a hit you fully
  block, parry or resist takes nothing. The item is preserved exactly: stack size, quality, durability, crafter's
  name and custom data all ride with it and come back unchanged.
- **You are told immediately.** A status message names what went — `Thieving Greydwarf stole Silver x14` — with
  a sound and an effect, and the creature's nameplate shows the item's own icon for as long as it holds it.
- **Nothing is ever destroyed.** Goods drop at the corpse when it dies, and also when it despawns or is cleared
  with `elite purge` — which otherwise drops nothing at all. Stolen items are **never** multiplied by `drops`,
  star count, loot mode, a boss aspect or `large star power`; they are your own items being handed back.
- **They drop for whoever lands the kill**, and the mod does not chase items back to their original owner.
- **Configured** by one field, `mutation power: Thieving: {{ max items: 1 }}`. One item is the default and what the
  mutation is designed around; it is hard-capped at 8 whatever you set. Suggested biome bumps for Black Forest,
  Plains and Mistlands are in the written rule file; Meadows deliberately gets none.
- Spawn one to look at with `elite spawn Greydwarf 0 Thieving`.

Bosses never take mutations, this one included. Existing rule files keep working untouched — an unlisted mutation
stays enabled and `max items` falls back to its default.

## 3.1.0

**Mutations enabled** — a new `mutations enabled` switch in the rule file turns any of the nine mutations off
everywhere, regardless of its chance curves. Old rule files keep working unchanged; an unlisted mutation stays
enabled.

**Rule file** — the generated `creature_rules.yml` comments are much shorter. The full field-by-field reference
for `mutation power` moved to the README's new "Mutation power fields" section instead of living inline.

**Leeching and Plated rebalanced** — both could make a creature nearly unkillable:

- Leeching's regen dropped from 2% to 0.5% max health per second, and lifesteal from 30% to 10%. Regen now pauses
  for `combat cooldown` (5s default) after the creature last took damage, is hard-capped at `regen cap` (20 hp/s
  default) so a high-health creature cannot out-heal a fight on regen alone, and is no longer large-star enhanced.
- Plated's `armour` field is now a damage-reduction *percentage* (40% default, was a flat 100 armour points fed
  into vanilla's armour curve, whose quadratic low end erased almost all of a weak hit's damage). A new
  `max reduction` field (55% default) hard-caps it so large-star enhancement cannot approach invulnerability.
  **This changes what an existing `armour` value in a customised rule file means** — a server that set its own
  number should revisit it.

**Attack animation speed fixed** — a starred or Mad creature's animator speed was multiplied every fixed step
during attacks, minor actions, emotes and stagger, because the game only resets it while idle or moving. It
compounded once per step and ran away. The scaled speed is now written absolutely and only inside the game's own
reset window, so an attack inherits it once for the whole swing. (Shipped in 3.1.0; missing from its notes.)

**Large stars toned down** — `large star power` defaults to `1` instead of `2`: a large star (already worth five
ordinary ones) no longer also doubles every mutation's bonus on top of that. `star power`'s `hp` line is flattened
at the top (`3.85`/`5.4` at 4/5 stars down to `3.3`/`4.0`) so a five-star creature's health climbs less steeply.
Ash Lands and Deep North's `star chances` trim the 4-5 star tail (`15, 9` down to `11, 4`) and pad the bulk of the
distribution instead, so their harshest creatures are rarer. `max mutations: 1` already capped how many of these
a single creature can stack, and stays unchanged.

## 3.0.0

Rebuilt from scratch. Not an update to earlier versions — none of the old code remains. This release covers
mutations only; the rest of the mod's features return over the next releases.

**Mutations** — nine, one per creature by default:

- Mad — far faster, half health
- Bloated — double health, explodes a second after it dies
- Cloaked — invisible beyond 6 metres
- Splintering — splits into two weaker copies when killed, which can split again
- Leeching — regenerates, and heals from damage it deals
- Warding — reflects damage and knocks you back
- Plated — armoured while healthy, hits harder as that armour goes
- Miasmic — trails poison clouds; poisons players, never creatures
- Devouring — kills creatures in one bite and keeps their health and damage, until it is big enough to hunt you

**Stars** — beyond vanilla's two. Counted in fives on the nameplate: a small star is one, a large star is five.
A mutation on a large star is stronger.

**Configuration** — a settings file and a YAML rule file, both written and documented on first run, both
hot-reloaded. Star chances, mutation chances and mutation strength are set per biome. Servers can bind connected
players to their rules; display preferences stay per player.

**Multiplayer** — creature state travels in the world data the game already shares, so every player sees the same
creature. Untested across two real machines.

**Console** — `elite spawn`, `elite inspect`, `elite purge`, `elite effects`.
