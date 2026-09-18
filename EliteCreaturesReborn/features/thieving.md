# Elite Creatures Reborn - specification: Thieving

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file specifies **one new mutation**, added to the set in `mutations.md`. Everything general about mutations -
how one is rolled, how they stack additively, how the wire is kept quiet, how the star row is drawn - lives in
`mutations.md` and is not repeated here. Read that file first; this one only says what Thieving *is* and the
handful of places the general rules need an extra sentence to cover it.

Numbers are defaults and all of them are configurable through `creature_rules.yml`. Where a number is a judgement
call it says so.

**Status:** built; verified by build and by reading, not yet tested in a live multiplayer session against the
checklist below - this clean room has no game to run.

**Bosses never take mutations**, this one included.

---

# What it is

**A Thieving creature takes your things when it hits you, wears them where you can see them, and gives every one
of them back when you kill it.**

It is the only mutation whose threat is not damage. A Thieving Greydwarf that has your silver is not dangerous -
it is *urgent*. You cannot walk away from it, you cannot let it wander off into the fog, and if it is Cloaked as
well you have a genuine problem. The mutation is built entirely around that pressure, which is why nothing it
takes is ever destroyed: the punishment for losing the fight is the errand of finding it again, not the loss.

| Mutation | Gains | Costs |
| --- | --- | --- |
| Thieving | Takes one item from your inventory on each strike that lands, up to `max items`, and carries it | None |

Like four of the other nine it is a pure gain with no drawback, and for the same reason: the balancing lever for
mutations is how often they appear, not making each one internally fair.

---

# 1. The theft

## When it fires

**On every strike it lands on a player that deals damage**, until it is carrying `max items`. At the default of
one item, that is the first landed hit of the fight and nothing afterwards.

- A hit that deals **no damage to health** - fully blocked, fully parried, fully resisted - takes nothing. You
  stopped the blow; you stopped the hand that came with it. *Judgement call.* The alternative, stealing on
  contact regardless, makes blocking feel broken.
- A hit that deals damage takes exactly **one item**, never two, whatever the damage was.
- There is **no cooldown and no chance roll**. `max items` is the whole limit. A separate per-strike chance would
  be a second dial doing the first one's job.
- **Ranged and area hits count**, if they damage the player. A Thieving Fuling's spear takes something from
  across the clearing, and a player who has been robbed by a thrown rock understands the mutation immediately.
- **A creature already at `max items` stops stealing entirely.** It never swaps, upgrades or drops what it holds
  to make room. The first things it took are the things it keeps.

## What it may take

Two pools, in order. The first pool that has anything in it is the one it takes from; within that pool the slot
is chosen **uniformly at random**.

| Order | Pool |
| --- | --- |
| 1 | Every **unequipped** item in the inventory grid **below the hotbar row** |
| 2 | Every **unequipped** item in the **hotbar row** |
| - | If both pools are empty, **nothing is taken** and the hit is otherwise ordinary |

**It never takes equipped gear.** Not the weapon in your hands, not your shield, not armour, not a utility item,
not an equipped tool - wherever that item physically sits in the grid. Being disarmed mid-fight by a Greyling is
a loss players file as a bug rather than as a story, and a thief that cannot take the sword out of your hand is
still a thief. This is a fixed rule with **no config switch**: a server that turns it on would be shipping a
different mutation, and the spec would rather say what this one is.

**The hotbar is the last resort on purpose.** It is the row a player has arranged deliberately, so the backpack
is robbed first and the arranged row only when there is nothing else - which also means a player carrying nothing
but their hotbar still gets robbed rather than being quietly immune.

**It takes the whole stack.** One slot, emptied. Forty-seven wood leaves as forty-seven wood. A stack docked by
one is barely noticeable, and the whole point of the mutation is that you notice. Nothing is lost by taking the
larger amount, because all of it comes back.

**The item is preserved exactly, not re-made.** Stack size, quality/upgrade level, durability, variant, crafter's
name and any custom data ride with it and come back unchanged. A player must be able to kill the creature and put
their own upgraded, named axe back in the same slot. An item that returns as a fresh copy of its prefab is a bug,
not an approximation.

## What a player experiences

- The item leaves the inventory the instant the hit lands - no delay, no animation the player has to wait out.
- A **message in the top-left status area** names what was taken: `Thieving Greydwarf stole Silver x14`. Losing
  something silently, and finding out at the crafting bench an hour later, is the failure mode this exists to
  prevent.
- A **sound and a small effect at the player**, once per theft, so it is legible to anyone watching as well.
- The creature's nameplate then **shows the item's own icon** for as long as it holds it. See section 4.

---

# 2. Giving it back

## On death, everything drops

When a Thieving creature dies, **every item it is carrying drops at its corpse**, exactly as it was taken. This is
not part of its loot table and is not decided by it.

- **Stolen goods are never multiplied by anything.** Not by `drops`, not by star count, not by the loot mode in
  `loot.md`, not by a boss aspect, not by `large star power`. They are the player's own items being handed back;
  multiplying them is an item duplication exploit with extra steps. This is the single most important rule in
  this file.
- They drop **whoever lands the kill**, and whoever picks them up keeps them. The mod does not try to return an
  item to the player it came from. On a shared server that is a social problem with a social solution, and the
  alternative - items teleporting across the map to their owner - is a worse thing to watch happen.
- They drop **at the corpse**, alongside ordinary loot, spread the way vanilla spreads a drop so a stack does not
  land inside the terrain.
- A creature carrying nothing drops nothing extra, and the death path costs nothing.

## And when it is removed without dying

**The goods always drop.** Every path that removes a marked creature from the world drops what it was carrying
first:

| How it goes | What happens |
| --- | --- |
| Killed | Goods drop at the corpse, with its loot |
| `elite purge` | Goods drop, **even though purge deliberately drops no loot at all** |
| Despawned by the game | Goods drop where it stood |

Stolen goods are not loot - they are player property the mod is holding temporarily, and **nothing a player owned
is ever destroyed by this mutation**. `elite purge` is documented as "without drops"; this is the one exception
and `console-commands.md` must say so, because an admin clearing test creatures should not be quietly eating a
player's silver.

Where the game removes a creature without running a path the mod can hook, say so in `DECISIONS.md` and name the
path, rather than leaving it as a silent hole.

## Splintering does not copy the pouch

A creature that is both Thieving and Splintering **drops everything it holds when it splits**, at the moment it
dies, exactly as any other death. The copies are born carrying **nothing**.

A splinter inheriting the pouch would duplicate the pouch - two copies, two sets of the same items - which is the
duplication exploit again, arriving by a different door. A splinter *sharing* the pouch means an item that exists
in two places until one of them despawns. Dropping at the parent's death is the only version with exactly one of
each item in the world at all times, and it is also the clearest to a player: the thing that robbed you died, so
your things fell out of it.

## Tamed creatures

**A tamed Thieving creature never steals**, from its owner or from anyone. If it is tamed while holding goods, it
keeps holding them and still drops them on death. See `tamed-creatures.md` for how mutations behave on tamed
creatures generally; this is the per-trait rule that file anticipates.

---

# 3. Multiplayer

The general rules are in `mutations.md` - rolled once by the owner, stored in the ZDO, read by everyone; damage is
the owner's decision, drawing is everyone's. Thieving needs two extra paragraphs, because it is the one mutation
whose state does not begin on the creature's owner.

## Two authorities, one packet

**A player's inventory is authoritative on that player's own machine, and nowhere else.** A creature's owner may
not reach into it, and must not try.

So the theft splits:

| Machine | Decides |
| --- | --- |
| The **player's own client** - where `RPC_Damage` on that player runs | Whether this hit steals, which pool, which slot, and removes the item |
| The **creature's owner** | Whether the creature has room, and banks what it was sent into the creature's ZDO |

One packet, one direction: the player's client takes the item and sends it to the creature's owner, routed
through **that creature's own `ZNetView`**, never the global bus. Nobody who cannot see the creature receives it.

- The packet carries a **steal id** - the taking player's id and a counter - so a packet delivered twice is banked
  once. A retried packet must never produce a second copy of the item.
- The client reads the creature's carried count from the ZDO **before** taking anything, and takes nothing if the
  creature is already at `max items`. The owner checks again when it banks, and **drops on the ground at the
  creature what it cannot fit** rather than discarding it. The two checks can disagree by a frame on a busy
  server; that disagreement must cost nobody an item.
- If the creature's `ZNetView` is not valid at the moment of the hit, **no theft happens at all** and the item
  stays where it is. Check first, take second.

**The accepted risk, stated rather than hidden:** the item is removed locally and banked remotely, so a packet
lost between the two - the owning machine crashing in that exact window - loses that item. There is no two-phase
commit available here that does not risk duplicating the item instead, and duplication is the worse failure.
Log it at the taking end so it is diagnosable, write the window into `DECISIONS.md`, and do not pretend it is
impossible.

## What lives on the ZDO

The pouch is creature state and belongs on the creature's ZDO like everything else, so it survives ownership
changing hands, a reload, and the owner logging out. The key is `ecr_stolen`, carrying the item count and each
item written with the game's own item serialisation.

- **A hard cap of 8 items regardless of configuration.** ZDO data is replicated to everyone near the creature and
  a pouch is not a chest. A server setting `max items` above 8 is clamped, with a warning logged once naming the
  setting. *Judgement call* - 8 is comfortably above any sane configuration and comfortably below anything that
  would cost bandwidth.
- **Every client can read it**, which is what lets every client draw the same icons on the nameplate.

## Cases that must work

- **A dedicated server**, where the creature is owned by the server and the player's inventory is not.
- **Ownership changing hands** mid-fight: a creature handed to an approaching player still holds what it took,
  and does not steal again if it is already full.
- **Two players robbed by the same creature.** Their items sit in one pouch, in the order taken, and all of them
  drop together on its death.
- **The robbed player logging out** before the creature dies. The goods stay on the creature and drop normally;
  they are not held for that player and not returned to them.
- **A player arriving late** sees the icons for goods taken before they joined.
- **`max items` lowered in the rule file** while a creature is already carrying more. It keeps what it has and
  steals no more - a rule change never touches a creature already spawned, and never destroys held property.

---

# 4. What a player sees

## Name and stars

Thieving joins the table in `mutations.md` as the tenth mutation, in **last position** - after Devouring, so no
existing enum value, name order or star colour changes.

| Mutation | Star colour |
| --- | --- |
| Thieving | **Violet** `#A64BE0` |

Violet is unused, sits clearly apart from Mad's red, Cloaked's bright blue and Warding's navy, and stays readable
on a small glyph against snow and against night - which is where it must be judged, not on a swatch.

The name follows the ordinary rule: "Thieving Greydwarf", the word in front of the creature's name, in table
order when it carries more than one.

## The icons on the nameplate

**A Thieving creature shows the icon of every item it is carrying, on its nameplate, so you can see what it has
from across the clearing.** This is the mutation's real display: the star colour says *what it is*, the icons say
*what it cost you*, and the second one is what makes you chase it.

- **The item's own inventory icon**, the sprite the game already draws for it in a slot - including the right
  variant for an item that has several.
- **Right-justified to the right edge of the health bar**, on the same line as the star row, growing **leftward**,
  oldest item leftmost.
- **Nothing the mod adds may make a nameplate taller or wider than vanilla's** - that rule is in `mutations.md`
  and it holds here. The icons live inside the plate's existing width, which is why they take the right end of a
  line that already exists rather than a new one. "To the right of the nameplate" in the literal sense would
  widen the plate and start plates colliding in a crowd, which is exactly when you most need to read them.
- **At most 4 icons are drawn.** Beyond that, and whenever the star row would reach them, icons are dropped from
  the **left** - the oldest - until they fit. The star row is never clipped to make room: stars are the reading a
  player needs mid-fight, icons are the reading they need afterwards.
- **Size: 1.6x the size vanilla draws a star at**, a shade larger than a small glyph so a small item icon is
  recognisable rather than a smudge. A setting, like both star sizes.
- No stack counts, no numerals, no text. Fourteen silver and one silver look the same on the plate - the message
  when it was taken carried the number, and killing it returns the stack whatever it is.
- **Every client draws the same icons**, from the ZDO, whether or not it owns the creature.
- **A Cloaked Thieving creature's icons hide and fade with the rest of its plate.** Cloaked is the one that makes
  this mutation genuinely nasty and nothing here may undo it.

## The settings that control it

Per-player, in the `.cfg`'s display section, never locked by the server - they change only what one player sees:

| Setting | Default | Does |
| --- | --- | --- |
| `Show stolen items` | `true` | Draw the icons at all |
| `Stolen item icon size` | `1.6` | Icon size as a multiple of the size vanilla draws a star at |
| `Thieving star colour` | `#A64BE0` | Its entry in the existing palette section, bound like the other nine |

---

# 5. Configuration

One new entry in `mutation power`, in `creature_rules.yml`, and its per-biome chances. Everything else about the
rule file - how a biome overrides `defaults`, how the file reloads, `lock to server` - is unchanged and is
specified in `mutations.md`.

```yaml
  mutation power:
    # ... the nine existing entries, unchanged ...
    Thieving:    { max items: 1 }
```

| Mutation | Field | Meaning |
| --- | --- | --- |
| Thieving | `max items` | The most items one creature may ever hold. `1` by default: it robs you once, and the rest of the fight is ordinary. Hard-capped at 8 whatever is set here, because the pouch rides the creature's ZDO. `0` disables the theft while leaving the mutation rollable, which is not useful and is not an error. |

**`max items: 1` is the shipped default and the one the mutation is designed around.** One item is a single, clear
loss with a single, clear remedy. Raising it to three turns an ordinary Greydwarf fight into a real reversal, and
a server that wants that can have it; raising it to eight means a creature that walks off with a working set of
gear, which is a different game and is why the cap exists.

`max items` is an integer and the only field. Deliberately: a per-strike chance, a cooldown and a pool weighting
were all considered and all left out, because `max items` and the mutation's own rarity already control how often
this happens to a player, and a second dial doing the same job is how a rule file becomes unreadable.

## Large stars

**`max items` is enhanced on a large star**, like any other bonus: the bonus above the baseline is multiplied by
`large star power` and the result is **rounded to the nearest whole item, never below 1**. At the shipped
`large star power: 1` this changes nothing at all, which is the intent - a large star is already a size-and-
damage event.

Add the row to the enhancement table in `mutations.md`:

| Mutation | Enhanced on a large star | Left alone |
| --- | --- | --- |
| Thieving | `max items` | - |

## How often it appears

It uses the biome's ordinary `mutation chance` with no global override, like most of the nine. Suggested biome
overrides, **suggestions with a reason rather than rules**, in the style of the existing ones:

```yaml
  - match: BlackForest
    mutation chances:
      Thieving:    [6, 8, 11, 14, 17, 21]   # greydwarves already take things that are not theirs

  - match: Plains
    mutation chances:
      Thieving:    [8, 11, 14, 18, 22, 27]  # fulings, and a biome where you are carrying something worth taking

  - match: Mistlands
    mutation chances:
      Thieving:    [7, 10, 13, 17, 21, 26]  # a thief you cannot see is the encounter this mutation is for
```

Left at the biome default everywhere else. Meadows deliberately gets no bump: the first hour of a world is not
where losing your only axe is instructive.

---

# 6. Console commands

No new command. The existing ones must cover it, and one needs a line added:

| Command | What must work |
| --- | --- |
| `elite spawn <prefab> <stars> Thieving` | Spawns one on demand, bypassing every chance roll - as it already must for the nine |
| `elite inspect` | Prints the **resolved** `max items`, and **what the creature is currently carrying**: each item by name and stack size |
| `elite purge` | Drops stolen goods before removing the creature - the documented exception to "without drops" |

`elite inspect` listing the pouch is not optional. It is the only way to check that the item on the nameplate is
the item in the ZDO, and the gap between those two is where this feature's bugs will live.

---

# 7. Changes needed in the other feature files

This mutation cannot be added without editing these. Make the edits in the same change as the code, not after:

| File | What changes |
| --- | --- |
| `mutations.md` | "The nine" becomes ten, with Thieving last in the table and in the enum; star colour table; large-star enhancement table; the mutation power block; the `Splintering` interaction; a line in "Cases that must work" for the two-authority steal |
| `loot.md` | Stolen goods are not loot: never multiplied by mode, by `drops`, or by stars |
| `console-commands.md` | `elite purge` drops stolen goods; `elite inspect` prints the pouch |
| `tamed-creatures.md` | A tamed Thieving creature does not steal |
| `display-preferences.md` | `Show stolen items` and `Stolen item icon size` |
| `boss-aspects.md` | Nothing changes - bosses take no mutations - but confirm the stolen-goods drop path cannot be reached from a boss death |

---

# 8. Where this document is silent

Decide, build it, and write the decision and your reasoning in `DECISIONS.md` alongside the source. Do not guess
at how another mod might have done it - you have no way to know what those are, and that is deliberate. Read
`../../CLEANROOM.md` before writing a line of this.

Known silences, listed so they are settled deliberately rather than by whichever line of code gets written first:

- **Which sound and effect** play at the theft, and at the drop. Pick them with `elite effects`, which exists
  precisely because prefab names are not discoverable from the game's code.
- **Whether a Thieving creature behaves differently once it is carrying something** - fleeing, keeping its
  distance, going for a player who has more. Nothing here changes its AI, and that is the shipped behaviour. A
  thief that runs is a better story and a much larger feature; if it is wanted it is a second slice with its own
  file, not a paragraph added to this one.
- **The exact status message wording** and whether it names the creature. `Thieving Greydwarf stole Silver x14`
  is the intent.
- **Whether an item stolen from a player who then dies** should interact with their tombstone in any way. The
  answer here is no - the two are unrelated - but say so in `DECISIONS.md` rather than leaving it to be
  rediscovered.

---

# Build checklist

How to read and update this section is in `README.md`. In short: `[ ]` not started, `[~]` partly, `[x]` built and
seen working on a dedicated server. Tick from observed behaviour, never from the `Status:` line.

## The theft

- [ ] Steals on a landed hit that deals damage; takes nothing on a fully blocked hit
- [ ] Pool order: backpack unequipped, then hotbar unequipped, then nothing
- [ ] Never takes equipped gear, wherever it sits in the grid
- [ ] Takes the whole stack, preserving quality, durability, variant, crafter and custom data
- [ ] Stops at `max items` and never swaps what it holds
- [ ] Status message and the theft tell

## Giving it back

- [ ] Everything drops at the corpse on death
- [ ] Never multiplied by drops, stars, loot mode or a boss aspect
- [ ] `elite purge` drops the goods; despawn drops the goods
- [ ] A Splintering parent drops its pouch; its copies are born empty
- [ ] A tamed one does not steal, and still drops what it holds

## Multiplayer

- [ ] Taken on the player's client, banked on the creature's owner, one scoped packet
- [ ] Steal id makes a re-delivered packet bank once
- [ ] Pouch on the ZDO under `ecr_stolen`, surviving handover, reload and logout
- [ ] Hard cap of 8 enforced and warned; the owner drops what it cannot fit rather than discarding it
- [ ] Every case in "Cases that must work"

## Display

- [ ] Violet star colour, bound in the palette section
- [ ] Icons right-justified on the star row's line, oldest leftmost, at most 4
- [ ] Plate never grows taller or wider than vanilla's; the star row is never clipped for icons
- [ ] Icons hide and fade with a Cloaked creature's plate
- [ ] `Show stolen items` and `Stolen item icon size` honoured

## Configuration and commands

- [ ] `Thieving: { max items: 1 }` written into the generated rule file, with its comment
- [ ] `max items` enhanced on a large star, rounded, never below 1
- [ ] `elite spawn Greydwarf 3 Thieving` produces one
- [ ] `elite inspect` prints the resolved `max items` and the pouch contents

## The other feature files

- [ ] Every row in section 7 done

## Verification

- [ ] Tested in a live multiplayer session

## Work log

Newest last. One row per session that changed something: what moved, and the commit it landed in.

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-17 | File written. Mutation specified end to end; name, equipped-gear rule, whole-stack rule and the always-drop rule decided with the author. Nothing built. | - |
| 2026-09-17 | Built end to end: the steal (`Patches/ThievingHitPatch.cs`), the pouch (`Mutations/PouchStore.cs`, `PouchDrop.cs`), the two-authority RPC (`Runtime/ThievingRpc.cs`), the despawn path (`Patches/ThievingDespawnPatch.cs`), death/purge wiring, nameplate icons (`Display/PouchIcons.cs`), config, `elite inspect`/`elite spawn`/`elite purge`, and all six other feature files. Decisions in `../DECISIONS.md`. Builds clean, 0 warnings. Not yet tested live. | - |
