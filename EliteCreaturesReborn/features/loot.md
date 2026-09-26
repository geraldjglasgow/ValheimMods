# Elite Creatures Reborn - specification: Loot

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers how a kill decides what it drops: the four modes, how stars raise quantities, and why trophies
are handled on their own. **Boss loot** is multiplied further by the boss's aspect - that table is in
`boss-aspects.md`. **Dungeon chests refilling over time** is a different feature entirely and lives in
`scaling.md`. Changing an item once it exists - weight, stack size, what survives death - is `item-rules.md`.

Numbers are defaults and all of them are configurable.

**Status: built, not yet verified in game.** All four modes, the extra-roll settings, the global and boss
multipliers, trophy separation, the `creatures:` rules and `elite reference` are implemented
(`Loot/LootEngine.cs`, `Rules/LootOverlay.cs`, `Commands/ReferenceCommand.cs`; parsing verified outside the game,
including the error paths). Nothing below is ticked until it has been seen working on a dedicated server.
**Per-tier loot quantity is not built** - it waits on `world-tiers.md`.

---

# 1. Four modes

How drops are decided, chosen server-wide:

| Mode | Behaviour |
| --- | --- |
| Vanilla | Untouched, save a boss aspect's own multiplier (`boss-aspects.md`). The mod's other features still work |
| Scaled | The creature's own drop table, with quantities raised by star level |
| Rolled | The creature's own drop table, rolled once more per star, each roll independent |
| Curated | The rule files decide entirely, ignoring the creature's own table |

**Rolled is the default.** Scaled gives predictable abundance - a three-star kill is reliably three-star worth of
resin - and predictable is the less interesting feel. Rolled gives the *chance* of something rare out of a hard
fight, because each extra roll is independent and each one could come up with the thing that only drops five
percent of the time. A hard fight that might pay out unusually is a better story than one that pays out exactly as
arithmetic predicted.

**Vanilla** exists so the mod can be installed for its other features alone, the same way the Vanilla star preset
does in `pressure.md`. **Curated** exists for servers building a custom economy, where the creature's own table is
the wrong starting point rather than a thing to be multiplied.

The mode is one server-wide choice, not a per-creature one. Per-creature adjustment is what the rule files are
for.

---

# 2. Trophies are handled separately

**Trophies are not multiplied by default, whatever the mode.**

Twelve identical trophies from one kill is clutter, not a reward. A trophy is usually wanted once, for a wall, and
every copy after the first is inventory weight the player carries to a chest and then never opens again.

A server that disagrees can switch trophy multiplication on, and then trophies follow the same mode as everything
else. It is a **single setting, off by default**, because the default should suit the player who has not thought
about it yet - and the player who has thought about it will find the setting.

---

# 3. Mutations and attunements do not change loot

Not by default. A mutation is a **change to the fight, not to the reward**: Cloaked makes a greydwarf harder to
find and Plated makes it harder to hurt, and neither is a reason for it to carry more resin.

Stars are the channel that says "this was harder, here is more". Letting mutations pay as well would mean every
trait in the mod eventually became an economic decision rather than a tactical one, and a player would farm the
mutation that paid best rather than fighting whatever the world produced.

A server can make them pay through the rule files if it wants to. The default is that they do not.

**Thieving's pouch is the one exception, and it is not really an exception.** What it drops on death is the
player's own property being handed back, never anything the creature's own loot table produced - so it is never
multiplied by mode, by `drops`, by star count, or by a boss aspect. See `thieving.md` for the full rule; this is
not a mutation "paying" in the sense the paragraph above is about.

Boss aspects are the deliberate exception, and `boss-aspects.md` explains why: an aspect is **visible and
rerollable before you commit**, so without a loot difference a group would simply wait for the easiest one every
time. That argument does not apply to a mutation, which you meet rather than choose.

---

# 4. Multiplayer

- **The drop list is built on the dying creature's owner**, which is where the game builds it, and read from the
  traits in the creature's ZDO. Those traits were rolled once by that same owner, so the multiplier is identical
  wherever the creature was rolled and no message is needed to agree on it.
- **The extra rolls in Rolled mode happen once, on the owner**, not once per client. Every player then sees one
  drop pile, which is the same rule the game already follows for ordinary loot.
- Because the mod keeps creatures at **vanilla level 1** and scales them itself (`scaling.md`), the game applies
  no level loot bonus of its own. The mod's multiplier is the whole story, and there is no second multiplier
  hiding underneath it.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 5. Configuration

In a `loot:` block of `creature_rules.yml` - the .cfg is per-player display preferences only, and everything here
changes gameplay, so it lives with the other synced, lockable rules:

- **Mode**: Vanilla, Scaled, Rolled or Curated.
- **The per-star quantity line** for Scaled, one entry per star count. Already built.
- **Extra roll chance** for Rolled: one entry per star count, the percent chance that that star's extra roll
  happens at all. Default 100 everywhere, which is plain Rolled; lowering entries turns "one more roll per star"
  into "a chance of one more roll per star".
- **Max extra rolls** for Rolled: a cap on extra rolls however many stars the creature has. Default 5, matching
  the default star ceiling. 0 means uncapped, for the server that has raised the ceiling and means it. This
  answers the twenty-star problem the same way the Splintering caps do in `mutations.md`.
- **Global loot multiplier**: one number over every dropped quantity, applied after the mode. Default 1.
- **Boss loot multiplier**: the same, bosses only, applied on top of the boss `drops` line and the aspect
  multiplier (`boss-aspects.md`). Default 1.
- **Trophy multiplication**: off by default; on makes trophies follow the mode. A drop is a trophy when its item
  type is the game's own Trophy type, so modded trophies are covered without a name list.
- **Per-creature and per-boss drop rules** in `creature_rules.yml` - section 6.
- **Per-tier loot quantity**, so a server can make a mature world pay differently (`world-tiers.md`).

Order of application, so two settings never argue: the mode produces quantities, per-creature rules override and
extend them, then the global (or boss) multiplier scales the result. Trophies step out of all of it unless trophy
multiplication is on.

**Another mod's drops pass through untouched.** A mod that injects rows into the same drop list (EpicLoot's
materials, for instance) keeps them exactly as it rolled them: the engine only reworks rows it owns - the
creature's own table and rows the rule files name - so the outcome never depends on which mod's patch happens to
run first. Curated replaces our table, not theirs. The one exception is a row the rule file names explicitly,
which is the server's own words and is honoured wherever the row came from.

An off switch for the whole feature leaves every other feature working, as `configuration.md` requires of all of
them - the mod with loot off still stars, mutates and scales creatures and simply never touches what they drop.

---

# 6. Per-creature rules

A `creatures:` section in `creature_rules.yml`, matched by **prefab name** - the same name the game itself uses,
so a row works for a modded creature exactly as it does for a vanilla one. One vocabulary serves every mode: in
Vanilla/Scaled/Rolled it adjusts the creature's own table, and Curated mode is simply this format used alone,
ignoring the creature's table. That is the whole answer to what Curated's file looks like - it is not a second
format.

```yaml
creatures:
  - match: Troll
    drops: [1, 1.5, 2, 3, 4, 5]      # overrides the biome/default drops line
    multiply trophies: true           # per-creature exception to the global switch
    drop overrides:                   # adjust rows of the creature's own table
      - item: TrollHide
        amount: [2, 5]
        chance: 100
      - item: Coins
        remove: true
    extra drops:                      # additions; Curated mode uses only these
      - item: Ruby
        chance: 10
        amount: [1, 1]
        per star: true                # rerolls per star like the creature's own rows
```

A drop row can say everything the game's own drop entries can - item, min and max amount, chance, one per player -
so nothing expressible in the game's tables is inexpressible here. Bosses use the same section; the boss-wide
knobs (its `drops` line, the boss loot multiplier, aspects) stack on top as section 5 orders.

---

# 7. The creature reference file

**`elite reference` writes `creature_reference.yml` next to the config files**: every creature the running game
has registered, grouped by biome, with its prefab name, display name, base health and its vanilla drop table -
item, amount range, chance. Bosses are a section of their own rather than a biome's - nothing in the game's data
ties a boss to a biome without loading location assets.

The point is delegation: a server owner pastes this file and `creature_rules.yml` at an assistant, describes the
loot economy they want, and gets back rules that use real prefab names and adjust real drop tables. The file's
header comment says exactly that, so the paste carries its own instructions.

**Generated, never shipped or hand-written**, for three reasons that are really one reason - it cannot be wrong:

- It is read from the game's own registry (every registered prefab with a Character component), so **creatures
  added by other mods appear automatically**, under the prefab names the `creatures:` rules match on.
- A game update changes the world; the next dump is correct. A shipped list rots.
- Biome grouping comes from the game's spawn data (open-world spawn lists, and the spawners placed in camps and
  dungeons). A creature nothing spawns naturally - summons, event-only, some modded ones - still appears, in an
  `Unassigned` section at the end, because a creature Claude cannot see is a creature nobody writes rules for.

It runs in-game because prefabs do not exist in the main menu, and the file is written on the machine that typed
the command - which is where the person who wants to paste it somewhere actually is. It is read-only in the world
- it writes one fixed-name report and changes nothing - so it sits with Inspect and Pressure in the access model
(`console-commands.md`).

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

> Items are unticked because nothing here has been verified against the code in this pass. Tick them as you
> confirm each one, and bring the `Status:` line into agreement.

## The modes

- [ ] Vanilla, Scaled, Rolled and Curated all selectable
- [~] Per-star quantity multiplier for Scaled - built and shipping
- [ ] Trophies handled separately, off by default
- [ ] Mutations and attunements do not change loot
- [ ] Rows another mod injects into the drop list pass through untouched, in every mode

## Multiplayer

- [ ] Drop list built on the dying creature's owner, where the game builds it
- [ ] Rolled mode's extra rolls happen once on the owner, not once per client

## Configuration

- [ ] Mode selection
- [ ] Extra roll chance and max extra rolls for Rolled
- [ ] Global and boss loot multipliers
- [ ] Per-creature `creatures:` rules - drops line, trophy switch, drop overrides, extra drops
- [ ] Per-tier loot quantity

## The reference file

- [ ] `ecr reference` writes `creature_reference.yml`, grouped by biome, prefab names, drop tables
- [ ] Modded creatures appear, from the game's own prefab registry
- [ ] Creatures with no spawn data land in `Unassigned` rather than vanishing

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |
| 2026-09-20 | Decided with the user: extra roll chance, max extra rolls, global and boss loot multipliers, the `creatures:` rule format (which is Curated's format - open decision resolved), and the generated creature reference file. Sections 5-7 rewritten; both former open decisions closed. Then built: modes, trophy separation, per-creature rules, `elite reference`; judgement calls in `DECISIONS.md`. | pending |
| 2026-09-20 | Drop-mod compatibility: the engine now only reworks rows it owns (the creature's table plus rule-file rows), so drops another mod injects - EpicLoot was the prompt - survive every mode, strip and multiplier, whatever the Harmony patch order. | pending |
