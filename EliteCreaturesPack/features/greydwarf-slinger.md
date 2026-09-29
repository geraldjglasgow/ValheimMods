# Elite Creatures Pack - specification: The Greydwarf Slinger

One feature of the mod, specified on its own. The other feature files sit beside it.

This file covers the Greydwarf Slinger: a greydwarf with a slingshot that stands and shoots stones like the game's
skeleton archer. Its body is the game's own Greydwarf; the slingshot, its bands and pouch, the satchel of stones and
the shot animation come from the workspace's `AssetWorkshop` (`assets/ecr_slinger`: Blender for the parts, Unity for
the clip, which is authored on the game's Greydwarf skeleton) and ship in the plugin as an embedded asset bundle. The
design was settled with the user on 2026-09-28.

**Status: built, not tested in game.** Everything below is built, the patches verify offline against the game's
assemblies and the workshop's Unity preview shows the shot; nothing has been seen in a running game yet.

---

# 1. Where slingers come from

When the game is about to spawn a `Greydwarf` in one of the `Biomes` (default: the **Black Forest**), it is a
slinger instead at `Share` percent (default **10%**):

- in the wild (the game's spawn system: packs roaming the forest, and the Greydwarf raids), and
- from greydwarf nests, unless `Nests` is off.

The pack keeps its size and each creature its level roll; only which creature comes changes. Camps and other fixed
spawners (locations' creature spawners) keep their greydwarfs. The machine that runs the spawner decides, as for every
spawn. `spawn ECP_GreydwarfSlinger` (or, with Elite Creatures Reborn, `elite spawn ECP_GreydwarfSlinger <stars>`) makes one
by hand.

A slinger does not count as a Greydwarf towards the wild spawner's limit of greydwarfs nearby, so a forest with
slingers in it can hold a slightly bigger crowd of the two together.

# 2. The fight

- **The shot.** It raises the slingshot in its left fist, takes the pouch with its right hand, draws it back to its
  cheek, lets go (the stone leaves at 1.12 s of the 2 s shot, with a bow's twang) and recovers. It shoots from `Range`
  (22 m) down to point blank, once every `Shot Interval` (3.5 s) at most, and keeps turning after its target while it
  draws.
- **The stone** is half a greydwarf's thrown rock (about 9 x 9 x 15 cm) and flies at `Stone Speed` (16 m/s, a little
  faster than the 12 m/s a greydwarf throws), falling at half the thrown rock's rate. Each shot is launched on the low
  arc that lands on the target's middle from where the target stands at the release (about 6 degrees up at 15 m,
  13 at 22 m), with 2.5 degrees of spread. It does not lead a moving target, so running across its line, a dodge or a
  shield all beat it. It does `Stone Damage` (12) blunt, scaled by stars (and the world tier, with Elite Creatures Reborn) like any creature's hit;
  a thrown greydwarf rock does 10.
- **Standing its ground, like the skeleton archer.** The slingshot is its only weapon (the Greydwarf's claw and
  thrown rock are gone), as the bow is the archer's, and like the archer's bow it has no minimum range. The game's AI
  walks it up until its target is in range and in sight, then it stops, turns after its target and shoots; up close
  it keeps shooting. It does not run round its target between attacks as a greydwarf does (the Greydwarf's AI circles
  for 3 s in every 6; the slinger's, like the skeleton's, never). It does not back away and never melees.

# 3. Stats and look

A copy of the game's `Greydwarf`, so it has the Greydwarf's 40 health (80 at one star, 120 at two), resistances,
faction (forest creatures), sounds, senses, star looks and death. On top: the slingshot in its left fist (about 54 cm
from grip to prong tips), the pouch and bands, and a leather satchel of stones on its right hip (about 39 cm across),
so a slinger can be told from the pack at a distance.

# 4. Loot

The Greydwarf's own drops, plus 2 to 4 stones (the satchel's). Elite Creatures Reborn's loot rules act on it like on any creature.

# 5. Multiplayer

- The prefabs (creature, shot, stone) are built once and registered identically on the server and every client
  whenever the game's network scene wakes, so the prefab hash in a ZDO means the same thing everywhere. The plugin
  embeds a bundle per platform (Windows, Linux) because a bundle only loads on the platform it was built for; a
  dedicated server runs the Linux player.
- Spawning is decided by whoever runs the spawner (the owner of the zone's spawn system or of the nest).
- The AI and the shot run on the creature's owner only (the game's AI does). The shot's animation
  reaches every client through the game's animator sync (the "throw" trigger); the stone is the game's own networked
  projectile, aimed on the owner; its damage goes through the game's damage RPC.
- The pouch, bands and pebble are moved on every client from the animator's state alone (`SlingRig`), so they need no
  network traffic and an interrupted shot lets go on every screen at once.
- The settings come from the server when it binds its players. A change reaches the
  interval, range and damage of the slingers already loaded as well as new ones.

# 6. Configuration

Section `3 - Greydwarf Slinger` of `com.EliteCreaturesPack.cfg`, synced from the server and locked while
`Lock Configuration` is on there:

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | true | off: no new slingers; ones already in the world stay |
| `Share` | 10 | percent of the greydwarfs spawning in the biomes that are slingers |
| `Nests` | true | greydwarf nests spawn them too |
| `Biomes` | BlackForest | where; several separated by commas |
| `Shot Interval` | 3.5 | seconds between shots |
| `Stone Damage` | 12 | blunt, before stars (a greydwarf's thrown rock: 10) |
| `Stone Speed` | 16 | metres a second (a greydwarf's thrown rock: 12); it arcs onto its target |
| `Range` | 22 | metres it shoots from |

# 7. Open decisions

- Whether camps (the Greydwarf camps' fixed spawners) should get slingers too.
- A slingshot players can use (stones as ammo): decided against for now (2026-09-28); the creature only.
- Translations: the name ("Greydwarf Slinger") and the shot's are English in every language for now.
- The bundle carries Unity's Standard shader with the placeholder materials (about 0.9 MB per platform); a lighter
  placeholder shader would shrink the plugin.
- A macOS bundle (macOS players load the Windows bundle and may not see the slingshot).

# Build checklist

## Spawning
- [ ] In the Black Forest about one greydwarf in ten is a slinger, in the wild and from nests
- [ ] `Nests = false` keeps nests to greydwarfs; `Biomes = Meadows, BlackForest` brings them to the Meadows
- [ ] `spawn ECP_GreydwarfSlinger` makes one

## Look
- [ ] The slingshot sits in the left fist, upright when it aims; the satchel hangs on the right hip
- [ ] The parts are lit like the greydwarf (the game's creature shader), not flat or pink
- [ ] Idle and walking: the slingshot swings with the hand, the pouch rests behind the fork

## Fight
- [ ] It draws, the pouch follows the right hand to the cheek, the stone leaves on the release with a twang
- [ ] The stone arcs onto a standing player from 5 to 22 m; a dodge, a raised shield or running across its line beats it
- [ ] It stands and shoots like a skeleton archer: no circling, no backing off, no claw; walking right up to it, it
  keeps shooting
- [ ] Stars and mutations show and scale it like any creature

## Death and loot
- [ ] Greydwarf drops plus 2 to 4 stones

## Multiplayer
- [ ] On a dedicated server (Linux): prefabs register, a second player sees the draw, the pouch and the stone
- [ ] A settings change reaches the shot interval of slingers already in the world

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-28 | At the user's request: `Share` default 20 -> 10 percent. | |
| 2026-09-28 | At the user's request ("act more like the skeleton archer; I don't like that it runs around and melees"): the claw and the keep-away (and its `Keep Away` setting) are gone, the slingshot has no minimum range (was 4 m; the skeleton's bow has 0) and the AI no longer circles its target (`m_circleTargetInterval` 6 -> 0, the skeleton's value). Values read from the live `Skeleton` prefab and `skeleton_bow` through DevBridge. | |
| 2026-09-28 | At the user's request: slingshot 1.25 -> 1.8 times its model, satchel 1.6 -> 2.4, stone 0.7 -> 0.5 of the thrown rock (the preview had shown a 3 cm stand-in; now the game's rock mesh), stone 30 -> 16 m/s (`stone speed`, new) with gravity 2 -> 5 and a per-shot arc (`SlingerAim`, Attack.FireProjectileBurst prefix). 68 patch classes verified. | |
| 2026-09-28 | Built: the four parts (Blender), the shot clip authored on the game's Greydwarf skeleton with two-bone IK and read back as muscles, the kit measured on the same skeleton at game scale, bundles for Windows and Linux (AssetWorkshop `assets/ecr_slinger`); the creature, shot, stone, keep-away, spawns and `slingers:` rules. Patches verified offline (67 classes). | |
| 2026-09-28 | Moved from Elite Creatures Reborn into its own mod, Elite Creatures Pack, at the user's request: prefabs `ECR_` -> `ECP_`, ZDO/RPC/global keys `ecr_` -> `ecp_`, the rule-file block became a section of the mod's .cfg (shares in percent, biomes as a flag list); Elite Creatures Reborn is optional and linked by key names only. Asset bundle and asset names kept (`ecr_*`). | |
