# Elite Creatures Reborn - specification: Retaliation zones

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers what a zone is, how it rises and decays, and what a player is told about it. A zone's effect is
expressed as **local pressure**, so `pressure.md` is what it feeds into and what turns it into star counts.

**Status: specified, not built.** Nothing in this file exists in the mod. It depends on pressure, which is also
not built - a zone has nothing to raise until then.

---

# 1. What a zone is

**Where a player kills heavily, the world pushes back locally.**

Sustained killing in one area raises that area's pressure above the world's. A favoured grinding spot becomes
progressively more dangerous, and leaving it alone lets it settle again.

This is the mod's answer to a farming spot. **Rather than forbidding it, it makes it escalate.** Nothing stops a
player parking at a greydwarf nest and killing everything that walks out; the nest simply starts sending back
things worth killing. The player who wanted a safe, repeatable grind has to move, and the player who wanted a
fight has found one - and both outcomes came from the player's own behaviour rather than from a rule telling them
no.

It is the counterpart to the hearth-distance term in `pressure.md`, and deliberately so. Building makes a place
calmer; killing makes it wilder. Both are levers the player pulls with their own hands, and a player can end up
holding both at once - a well-defended base in a region they have farmed to boiling.

---

# 2. Rising and decaying

**Zones decay on their own over time.** A zone is a memory of recent killing, not a permanent mark on the map, and
a place left alone returns to the world's ordinary pressure.

The decay is what makes the feature a cycle rather than a ratchet. Without it, a long-running server would
eventually have every reachable area at maximum pressure and the mechanic would have flattened into a slow
global difficulty increase - which is what `world-tiers.md` is for, and doing it twice would be worse at both.

---

# 3. What a player is told

**A player is told when a zone rises.** This is not optional decoration:

> A zone that escalates silently is indistinguishable from bad luck.

A player who starts meeting three-star greydwarves where there were none must be able to connect it to what they
have been doing. Otherwise the feature is a difficulty spike with no explanation, and the honest player conclusion
is that the mod is broken.

Three channels, **each separately switchable**:

- **An on-screen message** when a zone's level rises.
- **A map marker**, optional.
- **A minimap label**, optional.

`elite zones` lists the zones near you, their level and their decay, for a player who wants the numbers rather
than the notification (`console-commands.md`). It is read-only and safe to leave open to everyone on a locked
server.

---

# 4. Multiplayer

A zone is **shared world state**. It belongs to the place, not to the player who raised it - which is the whole
point: a group that farms one spot together makes it dangerous for all of them, and for anyone else who wanders
in.

- **The server owns every zone**: its level, its position and its decay clock. Kills are reported to it, it does
  the accounting, and it sends zone state to the clients that could see it.
- **A zone raised by one player is felt by every player** who goes there. A newcomer who walks into someone else's
  farming spot meets what that farming built. That is intended, and it is the reason zones are not per-player.
- **Announcements are scoped to the players inside or entering the zone**, not broadcast to the server. A player
  alone in the Mistlands is not told about a zone rising in the Meadows.
- **Map markers and minimap labels are drawn locally** from the zone state each client already has.
- **The pressure contribution is read by the creature's owner at spawn**, like every other pressure term, and
  baked into the star count it rolls once. A creature does not become stronger because the zone rose after it
  spawned.
- **Zones survive a server restart.** A decay clock that reset on every restart would make the feature depend on
  the server's uptime, and a nightly-restarting server would never hold a zone at all.

This must work on a dedicated server the first time it is built, not in a later pass.

---

# 5. Configuration

In the main settings file's retaliation section:

- **An off switch** for the whole feature. With zones off every other feature keeps working, and pressure simply
  loses one of its terms.
- **Zone radius** - how large an area one grinding spot covers.
- **How much a kill raises it**, and whether a kill's contribution depends on what was killed.
- **The ceiling**, so a zone cannot rise indefinitely.
- **Decay rate**, in world time.
- **Announcements, map markers and minimap labels**, each independently, with the display halves of them being
  per-player preferences that the server never locks (`display-preferences.md`).

---

# 6. Open decisions

Almost all of the numbers. `../SPEC.md` specifies this feature's **behaviour** completely and its **quantities**
not at all - there is no radius, no per-kill increment, no ceiling and no decay rate written down anywhere. Those
four numbers are the entire feel of the feature, and they want proposing and arguing about before the code starts
rather than being invented by whoever implements it.

**Whether all kills count equally.** A player clearing a greydwarf nest and a player killing one two-star troll
have done different amounts of violence to the same place. Weighting a kill by the creature's stars, or by its
biome, is the obvious refinement and is not specified either way.

**How a zone is anchored.** A zone has a position and a radius, but nothing says whether that position is fixed at
the first kill, or is the moving centre of mass of recent kills. The first is simple and can be walked out of by
fifty metres; the second follows the player and is harder to escape by accident.
