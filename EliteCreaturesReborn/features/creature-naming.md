# Elite Creatures Reborn - specification: Creature naming and nameplates

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers what a modified creature's nameplate says and how its stars are drawn. The **colours** those
stars are drawn in belong to mutations (`mutations.md`) and attunements (`attunements.md`); which of them a player
can change is `display-preferences.md`.

**Status: built, not tested in game.** Naming and the star row are built and shipping for stars and mutations.
Attunements are not built, so the attunement slot in the name is specified and unused. The **translation file does
not exist** - see the last section.

---

# 1. The name

A modified creature's nameplate reads its traits in a fixed order:

> **mutations, in colour order - then attunement - then the creature's own name.**

*Distended Warding Fire Troll.*

The order is fixed rather than roll order so that the same combination always reads the same way. A player who
learns to recognise "Warding Fire" as a shape of fight should see those two words in that order every time,
rather than having to parse an adjective list that shuffles.

**An unmodified creature's nameplate is untouched.** A plain greydwarf is a Greydwarf, with the game's own
nameplate, drawn the game's own way. The mod appears when there is something to say and is otherwise invisible -
which is also what makes a modified creature legible at a glance: the fact that the name is long *is itself* the
first warning.

---

# 2. Stars are always drawn individually

**However many there are, stars are drawn one by one and never collapsed to a numeral.**

Not "x7". Seven stars.

The reason is that a star is not just a count in this mod - **each star carries a mutation's colour**
(`mutations.md`), and a numeral throws that information away. "x7" tells a player the creature is strong. Seven
stars in four colours tells them which four kinds of fight they are about to be in at once, before anything has
moved.

The consequence is deliberate: a 10-star creature on a server that has raised the ceiling
(`pressure.md`) shows **ten stars**, and a row of ten stars is itself the warning. A server that has made the
world extreme should have nameplates that look extreme.

---

# 3. All on-screen text is translatable

Every piece of text the mod puts on screen - the trait names in a nameplate, the messages, the setting
descriptions - **comes from a translation file that can be replaced or added to**, so the mod can be played in
another language without editing the mod.

This covers more than the nameplate: zone announcements (`retaliation-zones.md`), altar text
(`boss-aspects.md`), console command output (`console-commands.md`) and every setting description
(`configuration.md`) are all text a player reads.

---

# 4. Multiplayer

The nameplate is **drawn locally, from replicated state** - never sent.

- The creature's traits are rolled once by its owner and stored in its ZDO. **Every machine reads the same
  traits** and builds the same name from them, deterministically.
- **No name is sent over the wire.** Two players looking at one creature see the same name because they computed
  the same thing from the same data, not because anybody transmitted it.
- That also means the name is built **in each player's own language** from their own translation file, which is
  the correct behaviour and would be impossible if the owner had sent the string.
- **Colours are per-player** (`display-preferences.md`), so two players may correctly see the same creature's
  stars in different colours while reading the same words. This is fine; it is the one place where two screens
  legitimately differ.
- A machine that meets a creature whose owner has not rolled it yet **draws the plain vanilla nameplate** and
  switches to the full one once, cleanly, the moment the traits arrive - never a flicker, never a half-built name.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 5. Configuration

Everything here is a **per-player display preference**, never locked by the server:

- **Nameplate display distance.**
- **Whether trait names show at all** - a player who wants the stars and not the adjectives can have that.
- **The star colour palette**, per mutation.
- **The attunement tint palette**, per attunement.

The full list and the reasoning for why none of it is lockable are in `display-preferences.md`.

---

# 6. Open decisions

**The translation file does not exist.** No localisation system is built: the mod's on-screen text is currently in
the code. Nothing about the shipped behaviour is wrong, but the specification's promise - that the mod can be
played in another language without editing it - is not yet true, and every string added between now and then is
one more to retrofit. Worth doing before the text grows much further.

**What happens when a name gets very long.** A creature carrying four mutations and an attunement reads as six
words before the creature's own name, and the specification caps mutations at four with no cap on the resulting
string. Whether a long name wraps, truncates, or is simply allowed to be long is not decided. On a server running
a raised star ceiling with stacking turned up, this stops being hypothetical.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

> Items are unticked because nothing here has been verified against the code in this pass. Tick them as you
> confirm each one, and bring the `Status:` line into agreement.

## The name

- [ ] Order is mutations in colour order, then attunement, then the creature's own name
- [ ] Stars are always drawn individually, never collapsed into a count
- [ ] All on-screen text is translatable

## Multiplayer

- [ ] No name is sent over the wire; every client computes it from replicated trait state
- [ ] Colours are per-player, so two players may correctly see one creature differently

## Configuration

- [ ] Nameplate display distance
- [ ] Whether trait names show at all - stars without the adjectives
- [ ] Star colour palette, per mutation
- [ ] Attunement tint palette, per attunement

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |
