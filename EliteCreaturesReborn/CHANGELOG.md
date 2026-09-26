# Changelog

## 3.6.1

**Admin commands work for admins on a dedicated server.** A player on the server's `adminlist.txt` could be told that
`elite spawn`, `inspect`, `purge`, `effects` and `reference` need admin rights. The mod now asks the server, which
checks the player against its own admin list the same way the game checks `kick` and `ban`.

- **Update the server as well as every player**: the server answers the check. A player on 3.6.1 whose server still
  runs an older version is told the server did not answer.
- A refusal names the exact ID the server knows the player by, ready to add to `adminlist.txt`. The server reads the
  file again within 10 seconds, so no restart or reconnect is needed, and it logs every check.
- A mistyped `elite` subcommand now lists the subcommands for every player, admin or not.

## 3.6.0

**World tiers.** The world now hardens as bosses fall. It starts at tier 0 and goes up one tier the first time each
boss is defeated - by anyone, in any order - so the seven bosses take it to tier 7. Killing a boss again changes
nothing. Every rise is announced to everyone on the server ("The world hardens: tier 3 of 7").

- Each tier makes **stars and mutations more common in every biome**. A star boost leans each biome's star chances
  upward (the top end most), and a mutation boost multiplies every mutation chance. At the defaults, by tier 7 the
  unstarred share of the Meadows falls from 73 in 100 to 44, five-star Plains creatures rise from 5 in 100 to 22,
  and mutation chances double.
- **Bosses are unaffected**, and **creatures already alive keep what they rolled** - the tier decides what the next
  one rolls. A respawned camp comes back at the current tier.
- The tier is read from the game's own record of defeated bosses, so **a world that killed bosses before this
  version starts at that tier**, and an admin can try a tier out with the game's `setkey` and `removekey`.
- **`elite tier`** shows the tier, what it does to the rolls and which bosses count, and it is open to every player,
  not just admins. `elite inspect` now says which tier a creature was rolled at.
- A new `world tiers:` block in `creature_rules.yml` has the off switch, the list of bosses that count (add a modded
  boss's defeat key to count it; `elite tier` lists every boss's key) and the star and mutation boost for each tier.

**Breeding.** Tamed creatures now pass their traits on, where their young used to roll like wild creatures.

- A newborn - a pup, a piglet, a calf, or an egg and the chick that hatches from it - takes **one of its parents'
  mutations** whenever either has one, and **a star count from 0 up to its stronger parent's**, every count equally
  likely. Two plain parents have plain young, and nothing new is ever rolled, so a strong line stays strong only if
  you keep the good ones.
- **Young creatures keep their traits when they grow up.** They used to roll again as adults.
- **Eggs keep their traits** when carried or stored, and say what they will hatch - "Hatches with 2 stars,
  Leeching" - on their hover text and tooltip. An egg's stars are its quality, so eggs of different stars no longer
  stack together.
- A bred mutation behaves exactly as it does in the wild - a Miasmic pet still trails poison.
- A new `breeding:` block in `creature_rules.yml` has the off switch (newborns then roll like wild creatures, as
  before) and the chance a newborn takes a parent's mutation, 100 by default.

A rule file from an earlier version has neither block and takes the defaults, with both features on. To tune them,
move your `creature_rules.yml` aside, start the game once for a fresh documented copy, and carry the two blocks
across.

## 3.5.0

**Boss aspects.** A boss never takes a mutation; it now takes an **aspect** - one modifier that changes what kind of
fight it is, named before the boss ("Twin Bonemass"). Eight of them, plus a plain fight one time in five:

- **Reflective** - 15% of each hit you land comes back to you as true damage armour does not reduce. Burn and
  poison ticks are never returned.
- **Shielded** - 30% less damage from bows and crossbows.
- **Mending** - regenerates 0.3% of its health every second, in combat too.
- **Summoner** - each time it loses 33% of its health it calls two 2-star creatures of its own biome.
- **Elementalist** - 20% more fire, frost, lightning, poison and spirit damage.
- **Enraged** - 20% more physical damage.
- **Twin** - a second copy of the boss arrives with it; the two share one health pool, each has 25% less health and
  damage, they die together and both drop full loot.
- **Phantom** - four copies arrive with it, with 100 health and half its damage. They drop nothing, leave no body,
  never count as the boss's defeat, and vanish when the boss dies.

**Read it at the altar.** Hover the offering bowl to see the current aspect, what it does, what it pays and when it
shifts. Every altar shifts to a different aspect each in-game hour (75 real seconds), independently of the others,
and every player at a bowl reads the same thing, across restarts. The aspect on the bowl when you make the offering
is the one you fight. The Queen, and any boss spawned by console or another mod, rolls its aspect when it appears.

**Harder aspects pay more**: each has a loot multiplier (x1.1 Shielded to x1.5 Summoner), applied on top of the
boss's star drops and the boss multiplier - in Vanilla loot mode too, where it is the only thing that touches a
boss's drops.

**Boss stars now show** under the boss health bar, in the same row of small and large stars as a creature's
nameplate. They were rolled and scaled before but never drawn.

All of it lives in a new `aspects:` block under `bosses:` in `creature_rules.yml`: an off switch, the shift
interval (0 fixes each altar), each outcome's chance, each aspect's loot multiplier and numbers, and per boss what
Summoner calls and which aspects it may roll. A rule file from an earlier version has no such block and takes the
defaults. `stars: false` now turns off only boss stars; a boss is left exactly as the game ships it when aspects
are off too.

`elite spawn` makes bosses now, with one aspect word in place of mutations (`elite spawn Bonemass 2 Twin`), and
`elite inspect` reports a boss's aspect, its loot multiplier and its twin or phantom links.

## 3.4.0

**Loot has grown from one multiplier into a system.** A `loot:` block in `creature_rules.yml` picks one of four
server-wide modes:

- **Vanilla** - drops untouched; the loot feature's off switch, the rest of the mod still works.
- **Scaled** - the creature's own table, quantities raised by the star `drops` line: predictable abundance.
- **Rolled** (the default) - the creature's own table rolled once more per star, each roll independent, so a
  hard fight has a real *chance* at the rare drop instead of a guaranteed stack of the common one.
- **Curated** - the rule files decide entirely, for servers building their own economy.

The knobs around them:

- `extra roll chance` per star and `max extra rolls` (default 5, 0 = uncapped) tune Rolled.
- `global multiplier` over every kill and `boss multiplier` on top for bosses, applied after the mode.
- **Trophies are not multiplied** unless `multiply trophies` says so - twelve identical trophies from one kill
  is clutter, not a reward. A per-creature override exists for the server that disagrees about one creature.
- **Per-creature rules** in a `creatures:` section, matched by prefab name so modded creatures work exactly like
  vanilla ones: a `drops` line, `drop overrides` that change or remove rows of the creature's own table, and
  `extra drops` additions - which is also the whole of Curated's format, not a second one.
- Mutations and attunements still change the fight, not the reward; stars are what pays.

**`elite reference` writes `creature_reference.yml`**: every creature the running game knows, grouped by biome,
with prefab name, display name, base health and its vanilla drop table. Paste it and `creature_rules.yml` at an
assistant, describe the economy you want, and get back rules that use real names against real tables.

**Plays fair with other loot mods.** The engine only reworks rows it owns - the creature's own table and rows
your rule files name. Drops another mod injects into the same kill (EpicLoot's enchanting materials, say) pass
through untouched in every mode, whichever mod's patch happens to run first.

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

**Joining a server no longer refuses a correct client under load.** The version check raced the config pushes over
the same connection and was judged by one 5-second deadline, so a valid reply that arrived slightly late was
reported as "you have no copy" and the join was refused. The check is now sent before the config pushes, and
retries every 5 seconds up to 20 before concluding a player is genuinely unmodded.

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
