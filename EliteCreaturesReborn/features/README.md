# Elite Creatures Reborn - the feature files

One file per feature. `../SPEC.md` is the whole-mod behaviour document; these are what it looks like broken into
the pieces a person actually works on. **The feature file is the source of truth for its feature.** If the code
and the file disagree, the file is right and the code is a bug - unless the disagreement is a decision, in which
case the decision gets written into the file first and the code follows.

Read `../../CLEANROOM.md` before writing any code here. Nothing in this directory relaxes it.

---

## What a feature file is for

Three jobs, in this order:

1. **It says what the feature does**, in behaviour a player could observe, with the numbers that decide it.
2. **It records the decisions** - including the ones still open, so nobody quietly settles them by writing code.
3. **It tracks what is actually built**, so the next session starts from fact rather than from a guess.

A feature that is not written down here does not exist, however much code is sitting in a branch.

---

## The shape of one

They all follow the same order. Not every feature needs every section, but they appear in this order when they do:

| Section | Holds |
| --- | --- |
| Header + `Status:` | What the feature is, and how much of it is real |
| The behaviour sections | What it does, what a player sees, the numbers |
| Multiplayer | How it works on a dedicated server - never optional, see below |
| Configuration | Every knob, and where it lives |
| Open decisions | What is unsettled, and what each choice would cost |
| Build checklist | What is built, what is not, and the work log |

**Multiplayer is not a section you may skip or defer.** The workspace rule, from `../../CLAUDE.md`: every feature
works on a dedicated server, built that way the first time - decide on the owner, draw on every client, persistent
state in the ZDO, transient RPCs scoped to who can see them.

---

## The `Status:` line

One line under the header, and it must agree with the build checklist at the bottom. Four values:

| Status | Means |
| --- | --- |
| `specified, not built` | Nothing exists in the mod |
| `partly built` | Some of it ships; the checklist says which parts |
| `built, not tested in game` | Code complete, not yet proven on a dedicated server |
| `built and shipping` | Complete and verified in a live game |

`built, not tested in game` is not a lesser version of done - it is the honest state of most of this mod, and
saying so is what keeps the checklist worth reading.

---

## The build checklist

The last section of every feature file. It is both a plan and a log: it is what you read to know where the feature
stands, and what you update when you have moved it.

**Marks:**

- `[ ]` not started
- `[~]` in progress, or built in part - say which part in a note beside it
- `[x]` built, and behaves as this file says **on a dedicated server**

**The rules that make it worth trusting:**

- **Tick for behaviour, not for code existing.** An item is `[x]` when the thing a player would observe actually
  happens, in multiplayer. Compiling is not the bar.
- **Never tick from the `Status:` line, or from another checklist.** Tick from having seen it work.
- **Unticking is normal.** A spec change that invalidates finished work unticks the item and says why in the log.
  An item that quietly stays ticked while the spec moves underneath it is worse than no checklist.
- **Blocked items stay unticked**, in their own group, naming the decision that blocks them. Do not build past an
  open decision to keep a checklist tidy.
- **Keep the `Status:` line in agreement** whenever a tick changes the picture.

**The work log** is the table at the end: newest last, one row per session that changed something - what moved and
the commit it landed in. It exists so a session that starts cold can read what the last one actually did, rather
than inferring it from a diff.

---

## Working on a feature

1. Read the feature file, end to end, including the open decisions. They are the part most likely to be settled by
   accident.
2. Read `../../CLEANROOM.md` if you have not this session.
3. If a behaviour you need is not specified, **decide it with the user and write it into the file** before or
   alongside the code. Never infer it from another mod, and never leave it implied by the implementation.
4. Build it, multiplayer included, first time.
5. Update the checklist and the work log, and the `Status:` line if it moved.

A number that is a judgement call says so where it lives, so it can be argued with later rather than mistaken for
something measured.
