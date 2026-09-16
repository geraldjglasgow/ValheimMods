# Elite Creatures Reborn - specification: Elemental attunements

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers what an attunement is, the six of them, what each does to a player it hits, and what a player
sees. Stars and per-star scaling are in `scaling.md`; mutations are in `mutations.md`. An attunement is
independent of both: a creature can be starred, mutated and attuned at once, and each channel says one thing.

Numbers are defaults and all of them are configurable. Where a number is a judgement call it says so.

**Status: specified, not built.** Nothing in this file exists in the mod yet. Two decisions are open - see the
last section - and they should be settled before the code starts, because one of them adds a seventh attunement.

---

# 1. What an attunement is

An attunement gives a creature an elemental character. It changes three things: what the creature deals, what it
resists, and what it does to you when it lands a hit.

It is deliberately not a damage bonus. A share of the creature's physical damage is **converted** to its element
rather than added on top, so an attunement changes the character of a fight rather than the size of the numbers.
A player who reads the creature and switches weapons is rewarded; a player who does not is inconvenienced, not
deleted.

## The six

An attunement deals its own element and resists it. That is the rule, so the table carries only what differs:

| Attunement | Vulnerable to | Beyond the rule |
| --- | --- | --- |
| Fire | Frost | |
| Frost | Fire | |
| Poison | Fire | |
| Lightning | Poison | |
| Spirit | Fire | ordinary weapons bite less |
| Gale | Lightning | deals Blunt with heavy knockback, and resists knockback and stagger rather than an element |

Five of the six are Valheim's own damage types and are named for exactly that - the name is the mechanic, and a
player who reads it knows what to switch to without learning our vocabulary first. **Gale is the one addition**,
and it is the odd one out deliberately: it is the attunement with no elemental resist gear, so it is answered by
positioning rather than by equipment.

## Nothing is immune

Resistance is resistance. A Frost creature takes reduced frost damage, never none.

An immunity turns a fight into a gear check and tells the player "you brought the wrong weapon, go home". A
resistance tells them "this will take longer". The vulnerability column is the reward for reading the creature and
switching, not a requirement for being able to fight it at all.

## The numbers

| Quantity | Default | Note |
| --- | --- | --- |
| Physical damage converted to the element | 40% | Converted, never added |
| Incoming damage of a resisted type | -50% | Never 100% |
| Incoming damage of a vulnerable type | +50% | The reward for switching |

These are the numbers most likely to feel unfair in play. Expect to move them.

---

# 2. What it does to the player on being hit

Three kinds of effect, all short and all readable. Each is a cost, not a sentence: long enough to change what you
do next, short enough that it never removes your ability to respond.

- **A brief impairment.** Fire blurs vision for a moment. Gale staggers you and throws you back.
- **Lingering harm.** Poison leaves poison ticking after you disengage. Frost leaves a chill that slows you until
  you warm up.
- **A resource drain.** Lightning drains stamina on each hit. Spirit drains eitr, and health instead if you have none.

| Attunement | Effect on the player | Default |
| --- | --- | --- |
| Fire | Vision blurs | 2 seconds |
| Frost | Movement slowed, refreshed by each hit, cleared faster near a fire | 25% for 6 seconds |
| Poison | Poison, refreshed by each hit | 8 seconds |
| Lightning | Stamina drained per hit | 15 |
| Spirit | Eitr drained per hit, or health if you have no eitr | 10 eitr, or 5 health |
| Gale | Staggered and thrown back | about 3 metres |

**All six effects can be switched off as a group**, leaving the damage conversion, resistances and vulnerabilities
working. A server that wants elemental creatures without status effects on its players gets exactly that.

The effects apply to players only. An attuned creature's hits on another creature convert damage and nothing more
- the same rule Miasmic follows in `mutations.md`, and for the same reason: a hazard that quietly thins the local
wildlife is a hazard nobody can see working.

---

# 3. What a player sees

**The creature itself is tinted its element's colour.** An Fire wolf runs hot orange, a Frost wolf pale blue, a
Spirit wolf washed-out and grey. This is the tell that reads at distance, long before a nameplate resolves, and it
is the one visual in the mod that changes the creature rather than decorating it.

A coloured flame effect rides along with the tint, brighter at higher star counts.

The tint is strong enough to read at a glance and weak enough that the creature stays recognisably itself. **Tint
strength and flame brightness are per-player settings, down to off**, because some players find it garish and some
play at night. Like every display preference they are never locked by the server - they change only what one
player sees.

The attunement joins the creature's name: *Sinewed Fire Greydwarf*.

Together with mutations this gives the mod one consistent visual language, each channel carrying exactly one
thing:

| Channel | Tells you |
| --- | --- |
| The creature's colour | Its attunement - what element you are fighting |
| Its stars' colours | Its mutations - what kind of fight this is |
| How many stars | How strong it is |
| Its name | All of the above, in words |

---

# 4. How often, and where

Attunement chance is set **per biome and per star count**, the same shape mutations use, so the Ash Lands can run
hot with attuned creatures and the Meadows stay ordinary. An attunement is rolled independently of any mutation:
a creature may have both, either or neither.

Whether a biome favours particular elements is a rule-file decision, not a hard-coded one. The file ships with a
sensible starting set - frost in the Mountain, poison in the Swamp - as suggestions with a reason, to be argued
with and edited.

Bosses do not take attunements. They take stars on their own table (`scaling.md`) and, when that feature is built,
aspects of their own.

---

# 5. Multiplayer

The rule everything in this mod follows: **rolled once by the owner, stored in the ZDO, read by everyone.**

- The **owner** rolls the attunement when the creature is first resolved and writes it to the ZDO, under this
  mod's own key prefix. It is never re-rolled.
- **Every machine** reads it and draws the tint, the flames and the name itself, locally and deterministically -
  so two players looking at the same creature see the same colour, and no tint is sent over the wire.
- **Damage conversion, resistance and vulnerability** are decided wherever the game decides damage, from the
  attunement in the ZDO, so the numbers agree on every machine without a message.
- **The effect on a player** is applied to that player's own machine, scoped to the player who was hit. Nobody
  else's screen blurs, and a player alone in the Mistlands is not sent anything about a fight in the Meadows.
- A machine that meets a creature its owner has not rolled yet leaves it plain and applies the attunement once,
  cleanly, the moment the state arrives - never a flicker, never twice.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 6. Configuration

All of it in `creature_rules.yml`, bound to the server's copy while `lock to server` is true, hot-reloaded on
edit. Keys are ours, taken from this document.

- An **off switch** for the whole feature. With attunements off, every other feature keeps working - stars,
  mutations and scaling are untouched.
- An **off switch for the player effects** as a group, leaving damage and resistances working.
- **Attunement chance** per biome and per star count.
- **Per-attunement power fields**: the conversion share, the resist and vulnerable multipliers, and each effect's
  own numbers - duration, slow percentage, drain amount, knockback distance.
- **Per-attunement tint colour**, so a server can retheme the whole set, with tint strength and flame brightness
  living in the `.cfg` as per-player display preferences.

`elite inspect` reports the resolved attunement and its numbers alongside the stars and mutations it already
reports.

---

# 7. Open decisions

Two, both from the older feature list rather than from this specification. **They need settling before the code
starts.**

## A shifting attunement

The old feature list had a seventh, "Wild": a creature that picks its element at spawn and picks again each time
it loses a third of its health. That behaviour is not in this specification and does not exist in the mod.

It is a good mechanic - it forces a player to keep reading the creature mid-fight rather than reading it once -
but it needs three things decided:

1. **Whether to have it at all.** It is the only attunement whose answer changes while you are fighting, which is
   either the most interesting one or the one that makes the tint meaningless.
2. **Its name.** "Wild" is the old build's word and is not ours to take. `Wyrd` is proposed: Norse, fits the
   game's vocabulary, and does not collide with the boss aspects, where `Shifting` is already taken.
3. **What a player sees at the moment it changes.** A tint that changes with no event is a bug to the player. It
   wants a visible break - a flash, a shed of the old element - or it should not shift at all.

## The effect mapping

The old list paired the effects differently: lightning hits staggering harder, spirit hits slowing you, poison
hits leaving a longer poison. This specification instead gives the stagger to **Gale**, the slow to **Frost**, the
poison to **Poison**, and makes Lightning and Spirit resource drains.

The specification's mapping is the one written here because each effect follows from its own element rather than
being distributed for variety - a gale throws you, ice slows you, blight poisons you. If the older pairing is
wanted, it is a table edit, but it should be a decision rather than a drift.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

## Core

- [ ] Attunement rolled once by the owner, written to the ZDO under our own key prefix, never re-rolled
- [ ] Read deterministically by every machine; a creature met before its owner has rolled resolves cleanly, once
- [ ] Chance configurable per biome and per star count, rolled independently of mutations
- [ ] Bosses excluded

## Damage

- [ ] 40% of physical damage converted to the element, never added
- [ ] Resisted type −50%; vulnerable type +50%; no immunities anywhere
- [ ] Spirit: ordinary weapons bite less
- [ ] Gale: deals Blunt with heavy knockback, resists knockback and stagger
- [ ] Creature-on-creature hits convert damage and nothing more

## Player effects

- [ ] Fire — vision blurs, 2s
- [ ] Frost — movement slowed 25% for 6s, refreshed per hit, cleared faster near a fire
- [ ] Poison — poison 8s, refreshed per hit
- [ ] Lightning — 15 stamina drained per hit
- [ ] Spirit — 10 eitr per hit, or 5 health with no eitr
- [ ] Gale — stagger and knockback
- [ ] Scoped to the player hit; nobody else's screen is touched
- [ ] Off switch for all six as a group, leaving damage and resistances working

## Presentation

- [ ] Creature tinted its element's colour, readable at distance
- [ ] Coloured flame effect, brighter at higher star counts
- [ ] Tint strength and flame brightness are per-player, down to off, never server-locked
- [ ] Attunement joins the creature's name

## Configuration

- [ ] Whole-feature off switch; every other feature keeps working without it
- [ ] Per-biome and per-star chance in `creature_rules.yml`
- [ ] Per-attunement power fields: conversion share, resist and vulnerable multipliers, each effect's numbers
- [ ] Per-attunement tint colour; tint strength and flame brightness in the `.cfg`
- [ ] Hot reload on edit; bound to the server's copy while `lock to server` is true
- [ ] `elite inspect` reports the resolved attunement and its numbers

## Blocked on a decision

- [ ] The shifting attunement (`Wyrd`) — see section 7. Do not build until settled.
- [ ] Effect mapping confirmed as written — see section 7.

## Work log

Newest last. One line per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Attunements renamed to plain element names (Fire, Frost, Poison, Lightning, Spirit); Gale kept. Table restructured to carry only what differs. Checklist added. | _pending_ |
