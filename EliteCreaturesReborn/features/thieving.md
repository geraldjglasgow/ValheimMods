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
checklist below - this clean room has no game to run. The 3.9.0 changes - one item per star, the icon layout and
the despawn duplication fix - are built and not tested in game.

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
| Thieving | Takes one item from your inventory on each strike that lands, until it holds one per star (never fewer than `max items`, never more than 8), and carries them | None |

Like Bloated, Cloaked, Leeching, Warding and Miasmic it is a pure gain with no drawback, and for the same reason:
the balancing lever for mutations is how often they appear, not making each one internally fair.

---

# 1. The theft

## When it fires

**On every melee strike it lands on a player that deals damage**, until its pouch is full.

**The pouch holds one item per star**, never fewer than `max items` (1 by default) and never more than 8. An
unstarred or one-star thief robs you once, on the first landed hit of the fight, and nothing afterwards; a
three-star thief robs you three times. The danger grows with the stars, the way everything else about a creature
does. *Judgement call:* capacity is `max(max items, stars)`, then capped at 8, so `max items` is the floor for every
thief rather than the limit for all of them.

- A hit that deals **no damage to health** - fully resisted - takes nothing, and neither does a hit you **parry**
  or **dodge through**. You stopped the blow; you stopped the hand that came with it. *Judgement call.* The
  alternative, stealing on contact regardless, makes blocking feel broken. The game's block works like armour and
  never takes a hit to zero, so a parry still lets a sliver of damage through; it is ruled out as a parry, not by
  the damage left over. An ordinary block that lets damage through still steals. A parry counts whenever the game
  counts it, including a window another mod has widened.
- A hit that deals damage takes exactly **one item**, never two, whatever the damage was.
- There is **no cooldown and no chance roll**. The pouch's size is the whole limit. A separate per-strike chance
  would be a second dial doing the first one's job.
- **Only melee hits count.** A thrown stone, a spear, an arrow or an area blast takes nothing, however much it
  hurts: the thief has to reach you. *Decided with the author 2026-09-28*, reversing the first version, where a
  Greydwarf's thrown rock robbed players from across the clearing. The test is the game's own `HitData.m_ranged`,
  which every projectile and area-of-effect hit carries; a creature's area swing (a troll's slam) is not ranged
  and counts as melee.
- **A creature whose pouch is full stops stealing entirely.** It never swaps, upgrades or drops what it holds to
  make room. The first things it took are the things it keeps.

## What it may take

Two pools, in order. The first pool that has anything in it is the one it takes from; within that pool the slot
is chosen **uniformly at random**.

| Order | Pool |
| --- | --- |
| 1 | Every **unequipped** item in the player's own grid **outside the hotbar** |
| 2 | Every **unequipped** item in the **hotbar** (row 0's first eight cells, the ones the 1-8 keys use) |
| - | If both pools are empty, **nothing is taken** and the hit is otherwise ordinary |

A grid wider than eight (PackPanel's) has ordinary cells right of the hotbar on the top row; they are in the first
pool, robbed before the hotbar like any other cell.

**With PackPanel, only the main grid.** PackPanel keeps its slots - worn armour and utilities, the Backpack slot,
food, mead, ammo, the coin purse, the key ring and the tacklebox - as cells of the same inventory, in the rows under
the main grid, and publishes where the main grid ends (the character's custom data `PackPanel.mainGrid`, read through
the `PlayerGrid` library, trusted only while PackPanel is loaded and its `1. Inventory / Enabled` is on). Nothing
below the main rows is in either pool. *Decided with the author 2026-09-28*: the slot panel is the arranged kit, like
the hotbar but more so, and several of its slots hold items the game does not mark equipped - a worn backpack lies in
its slot unequipped, and taking it would shrink the grid and throw the pack's contents on the ground. A worn backpack's
own rows are part of the main grid, so what is inside a pack can be taken; the pack itself never. **Check first,
take second** holds here too: while PackPanel lays the inventory out but its grid cannot be read, the hit takes
nothing rather than risk a slot.

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
| Despawned by the game | Goods drop where it stood, once, at the moment it vanishes |

**A despawn drops the goods once, when it happens.** At dawn the game walks a night creature away from the nearest
player and only removes it once no player is within 40 metres; while anyone is closer it keeps walking. The goods
drop at that removal, not on each step of the walk. Before 3.9.0 they dropped on every step while a player stood
within 40 metres, and every drop was a fresh copy of the whole pouch - a duplication. That is fixed.

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
  creature's pouch is already full. The owner checks again when it banks, against the same pouch size, and **drops on the ground at the
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
  setting. A creature with more than 8 stars simply holds 8, with no warning: that is the pouch's own limit, not a
  misconfigured setting. *Judgement call* - 8 is comfortably above any sane configuration and comfortably below
  anything that would cost bandwidth.
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
- **A thief despawning at dawn with a player nearby.** It walks away holding its goods and drops them once, where
  it vanishes.

---

# 4. What a player sees

## Name and stars

Thieving joins the table in `mutations.md` as the tenth mutation, after Devouring, so no existing enum value, name
order or star colour changes. (Gilded, Blinking and Relentless have since followed it, the same way.)

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
- **Right-justified to the right edge of the health bar**, on the same line as the star row, in the room the star
  row leaves, growing **leftward**, oldest item leftmost and newest nearest the edge.
- **Nothing the mod adds may make a nameplate taller or wider than vanilla's** - that rule is in `mutations.md`
  and it holds here. The icons live inside the plate's existing width, which is why they take the right end of a
  line that already exists rather than a new one. "To the right of the nameplate" in the literal sense would
  widen the plate and start plates colliding in a crowd, which is exactly when you most need to read them.
- **Every item it holds is drawn, while there is room.** When they do not all fit beside the star row, the icons
  first **shrink**, but never below the size vanilla draws a star at - smaller than that an icon is a smudge - and
  only then are the oldest dropped from the **left** until the rest fit. The star row is never clipped or covered
  to make room: stars are the reading a player needs mid-fight, icons are the reading they need afterwards.
  `elite inspect` always lists the whole pouch.
- **Size: 1.6x the size vanilla draws a star at**, a shade larger than a small glyph so a small item icon is
  recognisable rather than a smudge. That is the size they start at before any shrinking. A setting, like both
  star sizes.
- No stack counts, no numerals, no text. Fourteen silver and one silver look the same on the plate - the message
  when it was taken carried the number, and killing it returns the stack whatever it is.
- **Every client draws the same icons**, from the ZDO, whether or not it owns the creature.
- **A Cloaked Thieving creature's icons hide and fade with the rest of its plate.** Cloaked is the one that makes
  this mutation genuinely nasty and nothing here may undo it.

How many icons show with a full pouch, at the default star and icon sizes:

| Stars | Pouch | Icons shown |
| --- | --- | --- |
| 0 to 3 | 1 to 3 | all |
| 4 | 4 | 3 of 4 |
| 5 | 5 | all 5 |
| 6 | 6 | 4 of 6 |
| 7 | 7 | 3 of 7 |
| 8 | 8 | 2 of 8 |
| 9 | 8 | 1 of 8 |
| 10 | 8 | 4 of 8 |

The count is not monotonic because five small stars become one large one: a 5-star row is shorter than a 4-star
row and leaves more room.

## The settings that control it

Per-player, in the `.cfg`'s display section, never locked by the server - they change only what one player sees:

| Setting | Default | Does |
| --- | --- | --- |
| `Show stolen items` | `true` | Draw the icons at all |
| `Stolen item icon size` | `1.6` | Icon size as a multiple of the size vanilla draws a star at |
| `Thieving star colour` | `#A64BE0` | Its entry in the existing palette section, bound like every other mutation's |

---

# 5. Configuration

One new entry in `mutation power`, in `creature_rules.yml`, and its per-biome chances. Everything else about the
rule file - how a biome overrides `defaults`, how the file reloads, `lock to server` - is unchanged and is
specified in `mutations.md`.

```yaml
  mutation power:
    # ... the other entries, unchanged ...
    Thieving:    { max items: 1 }
```

| Mutation | Field | Meaning |
| --- | --- | --- |
| Thieving | `max items` | The fewest items one creature holds, whatever its stars. It holds one per star, never fewer than this and never more than 8. `1` by default: an unstarred thief robs you once and the rest of the fight is ordinary. Hard-capped at 8 whatever is set here, because the pouch rides the creature's ZDO. The pouch never goes below one item, so a `0` here acts as `1`. |

**One item per star, at least one, is the shipped rule and the one the mutation is designed around.** An unstarred
thief is a single, clear loss with a single, clear remedy; a three-star thief is a real reversal, and it looks like
one before it lands a hit. Raising `max items` raises the floor for every thief: at three, an ordinary Greydwarf
fight turns into a reversal too, and a server that wants that can have it; at eight, every thief walks off with a
working set of gear, which is a different game and is why the cap exists.

`max items` is an integer and the only field. Deliberately: a per-strike chance, a cooldown and a pool weighting
were all considered and all left out, because the pouch size and the mutation's own rarity already control how often
this happens to a player, and a second dial doing the same job is how a rule file becomes unreadable.

## Large stars

**`max items` is enhanced on a large star**, like any other bonus: the bonus above the baseline is multiplied by
`large star power` and the result is **rounded to the nearest whole item, never below 1**. At the shipped
`large star power: 1` this changes nothing at all, which is the intent - a large star is already a size-and-
damage event. The stars themselves already count toward the pouch in full: a creature with one large star has
five stars and holds five items, and that one-per-star part is never enhanced.

Add the row to the enhancement table in `mutations.md`:

| Mutation | Enhanced on a large star | Left alone |
| --- | --- | --- |
| Thieving | `max items` | - |

## How often it appears

It uses the biome's ordinary `mutation chance` with no global override, like most of the others. Suggested biome
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
| `elite spawn <prefab> <stars> Thieving` | Spawns one on demand, bypassing every chance roll - as it already must for every other mutation |
| `elite inspect` | Prints the **resolved** pouch size and **everything the creature is currently carrying**, each item by name and stack size ("carrying 2 of 3: ..."), including any the nameplate has no room to show |
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

- [ ] Steals on a landed melee hit that deals damage; takes nothing on a ranged, parried or dodged hit
- [ ] Pool order: backpack unequipped, then hotbar unequipped, then nothing
- [ ] Never takes equipped gear, wherever it sits in the grid
- [ ] With PackPanel: never takes from a slot (gear, Backpack, food, mead, ammo, purse, key ring, tacklebox); a worn
  pack's rows can be robbed, the pack never; a 10-wide grid's (9,0) goes before the hotbar; PackPanel disabled in
  r2modman steals as before
- [ ] Takes the whole stack, preserving quality, durability, variant, crafter and custom data
- [ ] Holds one item per star, never fewer than `max items`, never more than 8; stops when full and never swaps
  what it holds
- [ ] Status message and the theft tell

## Giving it back

- [ ] Everything drops at the corpse on death
- [ ] Never multiplied by drops, stars, loot mode or a boss aspect
- [ ] `elite purge` drops the goods; despawn drops the goods once, where it vanishes, not on each step away
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
- [ ] Icons right-justified on the star row's line, oldest leftmost; every item shown while there is room,
  shrinking to vanilla star size before the oldest drop off
- [ ] Plate never grows taller or wider than vanilla's; the star row is never clipped for icons
- [ ] Icons hide and fade with a Cloaked creature's plate
- [ ] `Show stolen items` and `Stolen item icon size` honoured

## Configuration and commands

- [ ] `Thieving: { max items: 1 }` written into the generated rule file, with its comment
- [ ] `max items` enhanced on a large star, rounded, never below 1
- [ ] `elite spawn Greydwarf 3 Thieving` produces one
- [ ] `elite inspect` prints the resolved pouch size and the whole pouch

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
| 2026-09-27 | 3.9.0: the pouch holds one item per star, with `max items` as the floor and 8 the cap; the four-icon limit is gone - every item shows while there is room, shrinking to vanilla star size before the oldest drop off; `elite inspect` lists the whole pouch; a thief despawning at dawn with a player nearby no longer drops (and duplicates) its pouch on every step. Decisions in `../DECISIONS.md` under 3.9.0. Built, not tested in game. | - |
| 2026-09-28 | A parry no longer steals (player complaint): the game's block never reaches zero damage, so the "no damage" test never fired; the parry is now read inside `Humanoid.BlockAttack` (`Patches/ThievingParryPatch.cs`). A dodge roll's i-frames no longer steal either: the postfix ran after `RPC_Damage`'s early return with the damage untouched. Only melee hits steal now (`!hit.m_ranged`), so a Greydwarf's thrown stone takes nothing. Built, not tested in game. | - |
| 2026-09-28 | Never robs PackPanel's slots (the author's call): the pools stop at PackPanel's main rows, read from `PackPanel.mainGrid` through the new `ValheimModLibs/PlayerGrid` library (`PackPanelGrid.TryMainRows`, nothing taken when PackPanel is on and the key unreadable); the hotbar is row 0's first eight cells, so a wider grid's top-row cells right of it go first. Built (0 warnings, from a copy without the untracked creature leftovers), not tested in game. | - |
