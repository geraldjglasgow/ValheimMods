# Elite Creatures Reborn - specification: Display preferences

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the settings that are **deliberately exempt from server enforcement**. What is enforced, and how,
is `server-enforcement.md`. What the colours mean is `mutations.md` and `attunements.md`; where they are drawn is
`creature-naming.md`.

**Status: partly built.** The colour palette and nameplate settings exist for stars and mutations. Attunement
tint and flame brightness are specified against a feature that is not built.

---

# 1. The rule

**Every setting in this file is per player and never locked by the server**, because it changes only what that
player sees.

This is the line the whole workspace draws, and it is drawn in the same place in every mod:

> Anything that changes gameplay is synced and lockable. Anything that changes only what one player looks at is
> theirs.

A server has a legitimate interest in how much health a two-star troll has, because that is the game everyone is
playing together. It has **no** legitimate interest in whether one player finds the orange tint garish, plays at
night with the brightness down, or wants nameplates off entirely. Those decisions affect nobody else's game, and
taking them away from a player gains the server nothing.

The practical consequence, stated so no feature gets it wrong: **no feature may assume another player has the same
display settings it does.** Two people looking at one creature may correctly see different colours. Anything that
must be the same for both belongs in the gameplay settings instead.

---

# 2. The settings

| Setting | What it changes |
| --- | --- |
| **Mutation colour palette** | The colour of each star, editable per mutation |
| **Attunement tint palette** | The creature's body tint, editable per attunement |
| **Creature tint strength** | How strong that tint is, **down to off** |
| **Attunement flame brightness** | The flame effect riding along with the tint, **down to off** |
| **Nameplate display distance** | How far away a modified creature's nameplate resolves |
| **Whether trait names show at all** | Stars without the adjectives, for a player who wants them |
| **Show stolen items** | Whether a Thieving creature's carried-item icons draw on its nameplate at all (`thieving.md`) |
| **Stolen item icon size** | Size of those icons as a multiple of vanilla's own star size, **down to off** via the setting above |

Three of these go **all the way to off**, and that is intentional rather than generous. The visual language in
this mod is deliberately loud - tints, coloured stars, flames - because difficulty should be legible. A player who
finds it garish, or who plays mostly at night, or who is colourblind in a way the palette does not suit, needs a
real way out and not a smaller version of the same thing.

---

# 3. The default palettes

The defaults are built from **the four mutation colours** in `mutations.md` and **the six elements** in
`attunements.md`.

That is the point: they are **derived from the mod's own design rather than being an arbitrary table someone has
to justify.** A mutation's colour is the colour that mutation already means. Nobody has to defend the choice of
green for Miasmic, because green is what Miasmic is.

**A player who dislikes them replaces them wholesale.** The palette is a full set of values in the settings file,
not a theme name, so a player can hand the mod any twelve colours they like.

---

# 4. Multiplayer

There is nothing to replicate here, and that is the whole design.

- **These values never leave the machine they are set on.** They are not sent to the server, not sent to other
  clients, and not bound by Charter.
- **Every visual in the mod is drawn locally**, from replicated trait state plus these local preferences
  (`creature-naming.md`). That is what makes them possible: if a name or a colour were ever transmitted, a
  per-player palette could not exist.
- **A player's preferences survive joining a locked server**, unchanged, and are still theirs when they leave. A
  locked server changes the gameplay values in force; it never touches this file's half of the settings.
- Two players correctly seeing the same creature in different colours is **not a bug** and should not be
  "fixed" by anyone who encounters it in testing.

---

# 5. Configuration

All of it in the main settings file, in the display section, **unsynced and unlockable**. Every value is
hot-reloaded on edit like everything else, and takes effect on creatures already on screen rather than only on the
next one to spawn.

Configuration UIs present this section normally; there is no locked state to grey out, on any server.

---

# 6. Open decisions

**Whether a colourblind-friendly default palette should ship alongside the design one.** The current palette is
derived from the mod's own colour meanings, which is the right *default*, but "green means poison, red means fast"
is a language that does not reach every player. Offering one alternative palette out of the box is cheap; deciding
which one, and on what basis, is not, and nobody has done it.

**What nameplate distance does at its extremes.** Down to off is specified for tint and brightness, but the
nameplate distance is a number with no stated floor or ceiling. Whether zero means "never show a nameplate" and
whether a very large value is allowed to reveal every creature in a valley is not written down - and the second
one is arguably a gameplay setting wearing a display setting's clothes, since it tells a player what is out there.
That is the one entry in the table where the line in section 1 is genuinely debatable.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

> Items are unticked because nothing here has been verified against the code in this pass. Tick them as you
> confirm each one, and bring the `Status:` line into agreement.

## The rule

- [ ] Every setting here is per player and never locked by the server
- [ ] No feature assumes another player has the same display settings
- [~] Colour palette and nameplate settings exist for stars and mutations

## The settings

- [ ] Mutation colour palette, per mutation
- [ ] Attunement tint palette, per attunement
- [ ] Creature tint strength, down to off
- [ ] Attunement flame brightness, down to off
- [ ] Nameplate display distance
- [ ] Whether trait names show at all

## Multiplayer

- [ ] These values never leave the machine they are set on, and are not bound by Charter
- [ ] Preferences survive joining a locked server, unchanged
- [ ] Hot-reloaded on edit, taking effect on creatures already on screen

## Blocked on a decision

- [ ] Whether a colourblind-friendly default palette ships alongside the design one.
- [ ] What nameplate distance does at its extremes.

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-16 | Build checklist and work log added; `README.md` written to define the convention. | bfd5d8f |
