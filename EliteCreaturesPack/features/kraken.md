# Elite Creatures Pack - specification: The Kraken

One feature of the mod, specified on its own. The other feature files sit beside it.

This file covers the Kraken: a boss of the deep ocean that hunts ships. Outrun it under sail and it gives up; let it
catch you and it grabs the ship from below, holds it still, and fights the crew in two phases: six tentacles slamming
across the deck one at a time, then its head beside the ship biting at the rail and squirting ink. Its body is the
game's sea serpent (for swimming, sync, sounds, hit effects) wearing the kraken's own model; everything it does is
drawn by the mod's code, not by animation clips. The head, the long tentacle and the screen's ink splats come from the
workspace's `AssetWorkshop` (`assets/ecp_kraken`) and ship in the plugin as an embedded asset bundle.

Asked for on 2026-09-28: "players need to be able to escape it on their ship; if they fight it, the kraken goes under
the ship, stops it, then does tentacle attacks across the ship. The tentacles shouldn't be too thick on the ends so
players can dodge. Six tentacles come out of the water and one at a time swing down on the ship hitting players. Then
it shifts to a phase where its head comes out of the water alongside the ship, bites at close players, has an ink
squirt in a line (dodgeable and parryable without getting hit); a player hit gets ink splats over the screen for 1.5
seconds that then fade. The kraken goes between those two phases. The ship can't move while this happens, but the
kraken is slow enough to outrun." Decided with the user the same day: a tentacle smashes the ship every 10 seconds
**in the head phase only**; rare, in the deep ocean; a **full boss** (8000 health, boss bar; lowered to 3000 after the
first test, see section 7).

**Status: built, not tested in game.** Everything below is built, the build is clean and the patches verify offline
against the game's assemblies; nothing has been seen in a running game yet.

---

# 1. Where krakens come from

- The kraken has its own entry in every zone's spawn system (the game's wild spawning, run by whoever owns the zone).
  A zone of the **Ocean** that a player is in rolls `Chance` percent (default **0.3**) once every `Interval` seconds
  (default **3600**), for a point 60 to 100 metres from them where the water is at least `Min Depth` deep (default
  **25 m**).
- The spawn goes ahead only with a ship carrying a crew within 150 metres, and never while another kraken is loaded
  within a kilometre. A crew sailing open sea crosses a zone every ten seconds or so, so at 0.3 they meet one roughly
  every hour or two.
- It comes up a few metres under the surface (the game would put it on the sea floor) and starts to hunt.
- A kraken never lingers: one that finds nothing to hunt for a minute and a half, is outrun, or loses its crew leaves
  and is gone for good. `spawn ECP_Kraken` makes one by hand.

# 2. The hunt

- **Lurking**, it cruises slowly under the surface in deep water, looking for a ship with a crew within `Hunt Range`
  (default **90 m**).
- **Hunting**, it swims straight for the ship at `Swim Speed` (default **3.5 m/s**) with its mantle breaking the
  water behind its eyes and its tentacles trailing. It roars as it starts.
- **Outrunning it.** A ship sails far faster than 3.5 m/s under sail with a fair wind (a karve about 5.5, a longship
  about 7 at full sail and full wind); rowing (about 2) never escapes it, nor does a raft. A ship that stays beyond
  `Hunt Range` for 8 seconds has escaped: it dives and is gone. It never follows into water shallower than
  `Min Depth`: a ship that runs for the shallows and stays there 20 seconds has escaped too.
- **Caught.** Reaching the hull (within 3 m of it), it dives under the ship with a great splash, and the ship is held.

# 3. The grip

- From the moment it dives, the ship **cannot sail, drift or turn**: its sail comes down and stays down, and it is
  held where it was caught, still free to bob and roll on the waves. The kraken's body sits under the hull (or beside
  it, with the head); it never shoves the ship.
- The grip ends when the kraken dies, when no one has been aboard or right beside the ship for 12 seconds (the crew
  died, or swam off), or when the ship is gone. Then it lets go and leaves.
- **The crew cannot hurt their own ship** while it is held (the user, 2026-09-28): blades, arrows and fire from players
  do the ship no harm, so they can swing at the kraken over the rail. The kraken's smashes still damage it.

# 4. The rhythm

- **One attack at a time**, a random `Attack Gap Min` to `Attack Gap Max` seconds apart (default **2.5 to 4 s**,
  decided with the user after the first in-game test, 2026-09-28: "like 1 attack every 3 seconds, random 2.5 to 4"),
  with three or more players aboard; with two the gap is `Gap Factor Pair` times as long (default **1.2**), alone
  `Gap Factor Solo` (default **1.1**: a little slower alone, a bit slower as a pair; the user's call, same day).
  Under the ship the slams come quicker: `Tentacle Gap Factor` times the gap (default **0.7**; the user, same day:
  "single player needs it to attack faster when under the ship").
- **Phases by count** (the user, same day: "first go under the ship and do the arm thing, then head phase, after 6
  strikes, then it goes under the ship again, keep repeating"): `Phase Attacks` (default **6**) tentacle slams under
  the ship, then as many attacks from the head beside it, then back under, round and round until it dies.

# 5. The tentacle phase

- Under the ship the kraken is **upside down**: its head deep below, its body and the roots of its tentacles reaching
  up to just under the keel (it rolls over as it dives). Asked for by the user, 2026-09-28.
- Six tentacles rise out of the water round the hull, three down each side, 1.8 m out from the hull and spread along
  its whole length (at least 3.5 m apart, even round a raft): each comes up straight and curls over as it clears the
  surface, then flails restlessly, its curl breathing and its tip whipping, each a little different.
- **One at a time** on that rhythm (each tentacle once, in a random order), a tentacle rears up high and back, curled
  over towards its target (0.6 s), hangs there (0.4 s: the warning), then whips down across
  the ship along the line from its base to the player nearest it (or straight across if nobody is in its reach),
  lying on the deck over the rail, benches and cargo as they are, and down into the sea past the far side.
- It hits anyone it lands on: `Slam Damage` (default **60**) blunt, knocking them away from it. The tentacles are
  8 m long (halved after the first test: "the tentacles need to be way shorter, like 1/2 the height"), standing about
  4 m out of the water, and thin towards the end (a metre across at the base, about 55 cm where it comes over the
  rail, narrowing across the deck to a few centimetres at the tip): **step out of the
  line, or roll through it**; a raised shield blocks it and a parry staggers the kraken.
- It lies there about a second, where it can be hit, then lifts back up. After the sixth slam the tentacles sink and
  the head comes up.
- **The grab** (the user, 2026-09-28): one of the six (never the first) is special. A deep bellow and the sea churning
  where it rises warn of it; the tentacle rears higher and hangs trembling for longer (1.5 s) before it whips down.
  Whoever it lands on is wrapped at its tip, hoisted up and out over the water and tossed into the sea (3 m/s out and
  3 m/s up from about 5 m off the hull, landing some 7 to 9 m from it; the first build's 11 and 7 flung players 16 to
  25 m, far too far, the user said 2026-09-28), taking the slam's damage. A roll escapes it; a shield does not stop a grab. The thrown player's
  own machine moves them, as the game moves every player.
- **Every blow that lands** on a player (not rolled through, not taken on a shield) is heard by everyone: a heavy smack
  for a tentacle, a crunch for the beak, a wet splat for the ink.

# 6. The head phase

- The head rises out of the water right against the hull, level with most of the crew, **on the side with no
  ladder** (the user, 2026-09-28: the raft and the karve have one, on the left, so there it comes up on the right).
  On a ship with ladders down both sides (the longship, the drakkar) it takes the crew's side and keeps 3 m along the
  hull from that side's ladder, so the crew can always climb back aboard. Ladders are found by the game's ladder
  component, so a modded ship's count too.
  facing the deck and **close enough to hit with a melee weapon from the rail** (asked for by the user, 2026-09-28),
  with two tentacles gripping the rail three metres to either side of it. It roars as it comes up.
- **Bite**: whoever stands within about two and a half metres of the rail in front of it: the head turns to them, rears with
  the beak gaping, then lunges up over the rail and snaps shut. `Bite Damage` (default **80**) pierce. Dodge it,
  step back, or block it.
- **Whole-head lunge** (asked for by the user the same day, "the entire body can lunge forward to strike if needed"):
  while its ink is recovering, it goes for whoever is further in, up to about five metres in from the rail. The whole
  head throws itself up over the rail and down onto the deck as far as it needs (the lunge grows from 2.2 m to 5.5 m,
  rising higher and leaning further the further it goes) and snaps there, for the same bite. The same warning, the
  same dodge: roll, or get further away.
- **Ink** (spat from its mouth, the user's call, 2026-09-28): whoever is further off (within 28 m): the head turns,
  rears back and up with its beak gaping (a second of
  warning), then squirts a straight stream of ink at where they stood, flying at 28 m/s. It is stopped by the first
  solid thing in its way (hide behind the mast). Caught in it: `Ink Damage` (default **15**) blunt, and **ink covers
  the screen** for `Ink Blind` seconds (default **1.5**) before fading away. **Step out of the line, roll through it,
  or block or parry it facing the kraken**: then no ink reaches the eyes. At most one squirt every `Ink Interval`
  seconds (default **6**).
- **About every `Ship Hit Interval` seconds** (default **10**) while the head is up, its turn is a tentacle from the far
  side rising and smashing across the ship towards the head (also when nobody is in reach of the head): `Ship Damage`
  (default **40**) to the ship, `Slam Damage` to anyone in its line, and the ship rocks. The first is due half an
  interval after the head comes up, so a head phase (about 20 s) holds two smashes: about 80 hull damage per ~42 s
  cycle, some 115 a minute (the first build started the timer at zero and fitted only one).
- After its `Phase Attacks` attacks the head goes back under and the tentacles come up again.
- **Under the head** is its body, a thick column going down into the sea, so a head raised to bite shows the animal's
  body under it (the short arm stubs of the first model looked wrong in game and are gone).

# 7. Stats, look and loot

- A **boss**: the big health bar at the top of the screen while it hunts and fights, `Health` (default **3000**;
  Bonemass: 5000, a serpent: 400). The game's own scaling adds 30% for every other player within 100 m (up to five),
  so a crew of three faces 4800.
- **Balance** (the user, 2026-09-28, after the first test at 8000: "enough hp, but not so much that its impossible to
  kill ... risky to fight on the karve unless you have good gear, but better on the longship"). Vanilla ship health:
  raft 300, karve 500, longship 1000, drakkar 3000 (blunt is not resisted by any; GrindstoneSkills' Sailing can add up
  to 50% for a ship its builder placed). At ~115 hull damage a minute a karve lasts about 4 1/2 minutes of fighting, a
  longship about 9, a drakkar far longer. Estimated solo fights at 3000 (to check in game): bronze-age gear about 4
  minutes (the karve limps home or sinks), iron about 2 (the karve comes out at about half), later gear under 2. The sea serpent's resistances and hit effects; the sea monsters'
  faction. The harpoon can't drag it (the game's rule for bosses).
- Weapons and arrows hit it anywhere on the head or the tentacles; every hit counts against its one health bar.
- **A stagger** (a parried bite, ink or slam, or enough damage at once) calls off the blow about to land and holds the
  next blows back two seconds.
- **Look**: a dark crimson-brown octopus head with a chitin beak and big amber eyes over a thick body going down into
  the sea; six tapering tentacles with pale suckers underneath. A level's growth does not change its size: its fight
  is measured against the ship. With Elite Creatures Reborn it is a boss like any other (boss stars, a boss
  aspect), but never Twin or Phantom: that mod's rules leave them out of `ECP_Kraken`'s rotation from 3.12.0 (the
  in-game test of 2026-09-28 drew Twin, two krakens on one Karve; the user, same day: "it would be too powerful").
  Against an older version it is marked rolled with nothing as it is created, so it gets no stars or aspect. It wears the serpent's creature material
  without the serpent's metal and glow maps (in the first test they turned it metallic teal with cyan patches). Lit by the game's own creature material.
- **Death**: its corpse comes up beside the ship, rolls onto its side with its tentacles limp on the water, then sinks.
  **Loot**, dropped on the held ship's deck (or floating where it died): 20-29 Chitin, exactly **1 Kraken beak** (never
  scaled by the world's resource rate), **5-7 Raw kraken tentacle**, 150-299 Coins (the game's roll never reaches a
  drop's maximum). The beak and the meat are the kraken's own items (section 7a).

# 7a. The kraken's loot

Asked for by the user, 2026-09-28: "the kraken beak and raw tentacle. The beak should drop 1, and the dropped version
a little larger in game. It should also drop 5 to 7 raw tentacle meat. The tentacle meat can be cooked into the cooked
version, and the beak can be crafted into a kraken shield, which does a fair amount of damage to any creature you
parry. The meat should give better stats than cooked serpent meat, and the shield better stats than the serpent
shield." The models and icons were made outside the workshop's own scripts (AssetWorkshop `assets/kraken_beak_set`,
`assets/kraken_food`) and bundled as `ecp_kraken_loot`.

- **Kraken beak** (`ECP_KrakenBeak`): a crafting material, stack 10, weight 3. The dropped beak is drawn 1.3 times the
  size of the one set in the shield, so it stands out on a deck.
- **Raw kraken tentacle** (`ECP_KrakenMeat`): a crafting material like serpent meat, not edible raw. It cooks on every
  cooking station that cooks serpent meat (the game's iron cooking station), in the same time (60 s).
- **Cooked kraken tentacle** (`ECP_KrakenMeatCooked`): **80 health, 28 stamina, 4 healing a tick, 30 minutes**, better
  than cooked serpent meat on every count (70, 23, 3, 25 minutes). Steams like it.
- **Kraken shield** (`ECP_ShieldKraken`): 10 fine wood, 5 silver and 1 kraken beak at the forge, at the serpent scale
  shield's station level (3) and with its upgrade item; each of its 3 levels' upgrades takes 10 more fine wood and 3
  more silver, never another beak. Against the serpent scale shield:

  | | Kraken shield | Serpent scale shield |
  | --- | --- | --- |
  | Block | 70 (+6 a level) | 60 (+6) |
  | Deflection | 110 (+5) | 100 (+5) |
  | Parry | 1.5x, 5 adrenaline | cannot parry |
  | Movement | -5% | -10% |
  | Durability | 300 (+50) | 250 (+50) |
  | Weight | 4 | 5 |
  | While blocking | pierce resisted | pierce resisted |

- **The parry bite**: a creature whose blow is parried with the shield (the game's own parry: raised within its window,
  facing the blow, stamina left, not staggered) takes `Shield Parry Damage` (default **60**) pierce, plus
  `Shield Parry Damage Per Level` (default **10**) for each upgrade, from the parrying player: melee or ranged, any
  creature, the kraken too; never a player. Its resistances and hit effects apply, and a kill counts as the player's.
- Each item is a copy of the game's nearest item (serpent scales, serpent meat raw and cooked, the silver shield), so it
  keeps that item's sounds, sparkle, steam, block effects and its place in the hand and on the back; its model wears
  that item's own material with the model's albedo. The shield lies in the hand as the silver shield does, front out,
  its right-hand grip at the hand.

# 8. Multiplayer

- The prefabs (creature, corpse) are built once and registered identically on the server and every client whenever
  the game's network scene wakes. The plugin embeds the bundle per platform (Windows, Linux).
- **One owner decides** (the kraken's ZDO owner, where the game runs its AI): what it hunts, when it grabs, the phases,
  and every blow. Its state travels in its ZDO (`ecp_kraken_phase`, `ecp_kraken_ship`, `ecp_kraken_anchor`,
  `ecp_kraken_side`, `ecp_kraken_along`); each blow is one small RPC to everybody (`ecp_kraken_slam`,
  `ecp_kraken_bite`, `ecp_kraken_ink`, `ecp_kraken_flinch`), with targets in the held ship's own space.
- **Any ship**: nothing assumes a ship's shape. Each ship's hull is measured from its own colliders (the game's ships
  keep it on the vehicle layer; a modded ship without one is measured from all its solid colliders), capped by its
  float box; the six tentacle places, the head's place, the grips and the deck each tentacle lies on all come from
  that and from rays onto the ship's own parts. Checked in Blender on the raft, karve, longship and Ashlands drakkar.
- **Every machine draws the fight itself**, in the ship's own space as that machine sees the ship, so the tentacles and
  the head ride the ship's rolling without lag, and plays each blow from the moment its order arrives.
- **Each machine judges its own player** when a blow lands there: what a player sees is what hits them, so a dodge
  that looks clear is clear. The hit goes through the game's own damage on that player's machine, where dodging,
  blocking, parrying and the ink status work as for any hit.
- **The ship is held on the machine that sails it** (its owner), from the kraken's ZDO. A smash's ship damage is dealt
  once, by the kraken's owner, through the game's damage for ships.
- The settings come from the server when it binds its players.

# 9. Configuration

Section `5 - Kraken` of `com.EliteCreaturesPack.cfg`, synced from the server and locked while `Lock Configuration` is on
there:

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | true | off: no new krakens; one already in the world stays |
| `Chance` | 0.3 | percent per zone roll while a crewed ship crosses deep ocean |
| `Interval` | 3600 | seconds between a zone's rolls |
| `Min Depth` | 25 | metres of water it spawns in and follows ships into |
| `Health` | 3000 | its health; the game adds 30% per other player within 100 m |
| `Swim Speed` | 3.5 | metres a second as it chases |
| `Hunt Range` | 90 | metres at which it notices a ship, and beyond which it gives up |
| `Slam Damage` | 60 | blunt, a tentacle landing on you |
| `Attack Gap Min` | 2.5 | seconds from one attack to the next, at the least |
| `Attack Gap Max` | 4 | seconds from one attack to the next, at the most (random between) |
| `Gap Factor Pair` | 1.2 | the gap is this many times longer with two players aboard |
| `Gap Factor Solo` | 1.1 | the gap is this many times longer with one player aboard |
| `Tentacle Gap Factor` | 0.7 | under the ship the gap between slams is this many times the gap |
| `Phase Attacks` | 6 | attacks in each phase before it changes |
| `Ship Damage` | 40 | to the ship per smash in the head phase |
| `Ship Hit Interval` | 10 | seconds between smashes in the head phase (a smash takes an attack's turn) |
| `Bite Damage` | 80 | pierce |
| `Ink Damage` | 15 | blunt |
| `Ink Blind` | 1.5 | seconds the ink covers the screen before it fades |
| `Ink Interval` | 6 | seconds between squirts at the least |
| `Shield Parry Damage` | 60 | pierce, to a creature whose blow is parried with the Kraken shield (quality 1) |
| `Shield Parry Damage Per Level` | 10 | added for each quality level above the first |

# 10. Decisions

- **Built on the sea serpent**, not from nothing: the serpent's swimming, sync, sounds, hit effects and resistances
  come with it; its body is hidden and its AI replaced. Its animator still runs, unseen, for the game's creature code.
- **Procedural motion, no clips**: the tentacles aim at players and lie on whatever deck the ship has, which keyed
  animation cannot do. Poses are built in code bone by bone (`Kraken/Motion`, plain Unity maths).
- **The head's side**: the side of the ship with the most crew; the grip tentacles are the two anchors nearest it.
- **Tentacle targets**: the player nearest each tentacle's base within its reach.
- **Loot**: the game's chitin and coins, and items of its own (section 7a). The serpent meat of the first build was a
  stand-in for its own meat and is gone.
- **The parry bite is found, not re-computed**: the game clears a hit's status effect exactly when a block holds, so
  the patch marks that slot for a kraken shield raised within its parry window and reads whether the game cleared it.
  Parrying ranged attackers bites them too ("any creature you parry").
- **Meat and shield numbers are fixed**, not settings: FeastMaster tunes any food, and the shield's stats follow the
  game's own shields.
- **Leaving**: a kraken that is outrun or loses its crew is gone for good (it does not wait around), so the sea is not
  left with a boss swimming about.

# 11. Open decisions

- A trophy of its own.
- One beak per kill whatever the crew's size: the game can drop one per player instead.
- Whether hitting a tentacle should do less (or more) than hitting the head.
- Whether it should also hunt players swimming far from any ship.
- Translations: its name is English in every language for now.

# Build checklist

## Spawning and the hunt
- [ ] `spawn ECP_Kraken` near a ship at sea: it lurks, notices the ship, roars and chases with its mantle breaking water
- [ ] A ship under full sail with a fair wind outruns it; after 8 s out of range it dives and is gone
- [ ] Rowing it catches the ship; it never follows into water shallower than Min Depth
- [ ] Wild spawns: only in deep Ocean near a crewed ship, rare

## The grip
- [ ] It dives under the ship with a splash; the sail comes down and the ship can't sail or turn, but still bobs
- [ ] With no one aboard for 12 s it lets go and leaves; the ship sails again

## Tentacle phase
- [ ] Under the ship the body is upside down, reaching up to the keel
- [ ] Six tentacles rise round the hull, three a side, well spread, flail, and look right against the hull
- [ ] One slam in six is the grab: a warning bellow, a long trembling rear, and whoever it hits is thrown into the sea
- [ ] A blow that lands is heard (smack, crunch, splat); a roll or a shield keeps it quiet
- [ ] Hitting the kraken over the rail never damages the held ship; the kraken's smashes do
- [ ] With three or more the attacks come fastest; alone a little slower; as a pair a bit slower
- [ ] One at a time: rear, hold, whip down across the deck along the line to a player, lie on the rail and deck
- [ ] Stepping out of the line or rolling avoids it; standing in it hurts and knocks you aside; blocking works
- [ ] A tentacle on the deck can be hit and the damage shows on the boss bar

## Head phase
- [ ] The head rises against the hull level with the crew, in melee reach from the rail, two tentacles gripping the rail
      either side of it
- [ ] On the raft and the karve it rises on the right (no ladder); on the longship and the drakkar it stays off the ladder
- [ ] A grabbed player lands in the sea a few metres off the hull, close enough to swim back
- [ ] It bites players at the rail; stepping back or rolling avoids it
- [ ] With the ink recovering, it throws its whole head over the rail at a player a few metres in, and snaps there
- [ ] It spits ink from its mouth at players further off after rearing with its beak open; getting hit covers the screen 1.5 s then
      fades; rolling, stepping aside, blocking or parrying facing it keeps the screen clear
- [ ] Every 10 s a tentacle from the far side smashes the ship: the ship loses health and rocks
- [ ] After six head attacks the head sinks and the tentacles come up again; one attack every 2.5-4 s throughout

## Death and loot
- [ ] The boss bar shows while it hunts and fights; it dies at 0
- [ ] The corpse surfaces beside the ship, rolls over and sinks; loot lands on the deck

## Loot
- [ ] It drops one beak (larger than the shield's) and 5-7 raw tentacle; icons and names show in the inventory
- [ ] Raw tentacle cooks on the iron cooking station and shows on the spit; the cooked one steams and eats as 80/28/4/30 min
- [ ] The Kraken shield is in the forge's list (level 3) for 10 fine wood, 5 silver, 1 beak; upgrades need no beak
- [ ] In the hand it faces out with the beak forward and the grip in the hand; on the back it hangs right; dropped it looks right
- [ ] A parry with it bites the creature (damage number, its hit effect); a plain block or a staggered parry does not
- [ ] Parrying the kraken's bite, slam or ink bites the kraken; a parried ink hit leaves no ink on the screen

## Multiplayer
- [ ] A second player sees the same phases and blows on their ship; each is hit only by what they see land on them
- [ ] A client's parry bites a creature owned by another machine; a client sees the loot and the shield on another player
- [ ] On a dedicated server (Linux): the prefabs register and the fight runs with the kraken owned by a client

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-28 | At the user's request: the kraken's own loot (section 7a): beak (1, drawn 1.3x), raw tentacle (5-7, cooks where serpent meat cooks), cooked tentacle (80/28/4/30 min), the Kraken shield (forge 3: 10 fine wood, 5 silver, beak) whose parry bites (`Shield Parry Damage` 60 +10 a level); serpent meat drop removed. Bundle `ecp_kraken_loot` (four models, four icons) built with the workshop's `build.ps1 -SkipBlender`; bundles now carry `<asset>_icon.png` as sprites. Build clean, 33 patch classes verified offline. Not yet seen in game. | |
| 2026-09-28 | At the user's request: the head comes up 1.8 m from the hull (was 2.8) so the crew can melee it, grips move to either side of it; ink spat from the mouth; livelier tentacles (per-tentacle shape, breathing curl, tip flick, rise straight then curl, whip-like strikes, writhing on deck); any ship (hull measure falls back to all solid colliders, capped by the float box). Blender preview `AssetWorkshop/assets/ecp_kraken/fight.py` (the mod's motion ported) with the game's own ships via the new `workshop.prefab`. | |
| 2026-09-28 | Built: the creature on the sea serpent, the hunt, the grip, both phases, the ink with its screen splats, the corpse, the spawn entry and the `5 - Kraken` section; the model in AssetWorkshop (`assets/ecp_kraken`). Build clean, 29 patch classes verified offline. | |
