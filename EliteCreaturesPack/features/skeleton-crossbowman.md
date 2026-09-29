# Elite Creatures Pack - specification: The Skeleton Crossbowman

One feature of the mod, specified on its own. The other feature files sit beside it.

This file covers the Skeleton Crossbowman: the game's skeleton archer with a crossbow and a bolt quiver in place of its
bow. It fights like the archer, but every shot is a crossbow's: it raises the crossbow, aims and fires, then spans and
loads it again. Its body, AI and sounds are the game's own Skeleton; the bone crossbow, its string, its bolts, the
quiver and five animation clips come from the workspace's `AssetWorkshop` (`assets/ecp_crossbowman`: Blender for the parts, Unity for the
clips, which are authored on the game's Skeleton through its own Animator) and ship in the plugin as an embedded asset
bundle. Asked for by the user on 2026-09-28 ("use valheim base archer skeleton, but
add a bolt quiver and give it a crossbow. have it act like an archer skeleton, but when it attacks it raises, aims, and
fires the crossbow"); the rest was decided in building it and is listed under open decisions.

**Status: built, not tested in game.** Everything below is built, the patches verify offline against the game's
assemblies, and the workshop's Unity preview (stills and a video played through the game's own shot states) shows the
carry, the shot and the reload; nothing has been seen in a running game yet.

---

# 1. Where crossbowmen come from

The crossbowman is a skeleton unit like the game's others (decided with the user on 2026-09-29): it comes wherever and
whenever the game spawns one of its **archer skeletons**, in their place at `Share` percent (default **15%**, about one
in seven), and each is that skeleton's kind:

| The game's skeleton | Health | Its archer's bow | Where it spawns | Its crossbowman |
| --- | --- | --- | --- | --- |
| `Skeleton` | 40 | 20 pierce | Black Forest burial chambers, stone tower ruins (Black Forest, Mountains), the Meadows' combat ruin, bone piles, the wild at night once Bonemass is dead | `ECP_SkeletonCrossbowman` |
| `Skeleton_Meadows` | 30 | 15 pierce | the Meadows' skeleton spawners | `ECP_SkeletonCrossbowman_Meadows` |
| `Skeleton_Swamps` | 60 | 55 pierce | swamp bone piles, graves, swamp spawners, the wild | `ECP_SkeletonCrossbowman_Swamps` |
| `Skeleton_Mountains` | 75 | 60 pierce | mountain log cabins and stone tower ruins | `ECP_SkeletonCrossbowman_Mountains` |

Every kind of spawner counts: the wild spawn system, fixed creature spawners and bone piles. The spawn keeps its levels;
only which creature comes changes. The no-archer, poison, Hildir, Deep North and summoned skeletons never carry a bow
and never come as crossbowmen. The machine that runs the spawner decides, as for every spawn. `spawn
ECP_SkeletonCrossbowman` (or `_Meadows`, `_Swamps`, `_Mountains`) makes one by hand (with Elite Creatures Reborn:
`elite spawn ECP_SkeletonCrossbowman <stars>`).

A fixed spawner's creature is swapped for that one spawn only and put back afterwards, so the spawner keeps tracking
what it spawned and may roll again next time. A crossbowman does not count as its skeleton towards the wild spawner's
limit nearby.

# 2. The fight

- **Standing its ground, like the skeleton archer.** The crossbow is its only weapon (no sword, no shield), and the
  game's AI uses it as it uses the archer's bow: it walks up until its target is within `Range` (25 m) and in sight,
  stops, turns after its target and shoots, from range down to point blank, at most every `Shot Interval` (6 s). It
  does not circle its target and never melees.
- **The shot.** The game's own two bow states play it (their blends and timing are the archer's): it brings the
  crossbow up from the low ready to its right shoulder in about half a second, the right hand on the stock's wrist,
  lays its skull on the stock and holds the aim for about another half second, turning after its target; then it looses
  (the game's attack trigger, about 0.27 s into the second state), and the crossbow kicks. The bolt leaves the muzzle
  with the game's arbalest sound, flies straight at `Bolt Speed` (40 m/s) at the target's middle as it stood at the
  release, with 1.5 degrees of spread, and does as much damage as its skeleton's archer (20, 15, 55 or 60, times `Damage Factor`), as blunt (its head
  is a knuckle of bone), scaled by stars (and the world tier, with Elite Creatures Reborn) like any creature's hit. The
  archer's arrow flies at 30 m/s.
- **The reload.** Right after the shot it lowers the crossbow in front of its belly and bends over it; the right hand
  hooks the let-go string at the prod and draws it back into the nut (it clicks home), goes to the quiver on the right
  hip, pulls out a bolt and lays it in the groove; then both hands carry the crossbow again. It stands still for the
  whole of it (about 3.4 s, slowed to twice its first length at the user's request), so the moment after a shot is
  the time to close in. The whole attack takes about 5.5 s, so at the default `Shot Interval` (6 s) it shoots again
  almost as soon as it has loaded.
- **Carrying.** Its idle, walk and run are the Skeleton's own with both hands holding the crossbow at the low ready,
  across the belly and pointing forward and down (the archer carries its bow in one hand through a shield walk, which
  would put a crossbow through its legs).

# 3. Stats and look

A copy of its skeleton (section 1), so it has that skeleton's health, resistances, faction (the undead), sounds,
senses, looks, AI and death. On top: a crossbow made of bones, like the game's Spinesnap bow and bone tower shield (0.82 m
long: a femur for the stock, its knee knuckles the butt; a row of vertebrae for the fore-stock, bowed gently down in
the middle like a real back and threaded on a long bone through their middles, a notch carved in each for the bolt; two ribs lashed on with
sinew for the 0.72 m prod; a vertebra for the nut, a long finger bone for the trigger lever, a jawbone with its teeth
for the stirrup), in the left fist round the fore-stock, its sinew string, and a stiff leather quiver of five bolts on
the right hip. The bolts are its own: a thin bone shaft grimed like the skeleton, a blunt knuckle of bone for a head and three short
dark feather vanes, the same bolt in the groove, the fingers, the quiver and in flight (on the game's bone bolt
projectile). They stand high in the quiver, aimed at the middle of its bottom and turned so their vanes interleave.

# 4. Loot

Its skeleton's own drops, plus one to three bone bolts half the time (the quiver's). Elite Creatures Reborn's loot
rules act on it like on any creature.

# 5. Multiplayer

- The prefabs (creature, shot, bolt) are built once and registered identically on the server and every client
  whenever the game's network scene wakes, so the prefab hash in a ZDO means the same thing everywhere. The plugin
  embeds a bundle per platform (Windows, Linux); a dedicated server runs the Linux player.
- Spawning is decided by whoever runs the spawner (the owner of the zone's spawn system, the crypt spawner or the bone
  pile).
- The AI and the shot run on the creature's owner only (the game's AI does). The animation reaches every client through
  the game's animator sync (the "attack_bow" trigger); the bolt is a copy of the game's networked bolt projectile, aimed
  on the owner; its damage goes through the game's damage RPC.
- The string, the bolt in the groove, the bolt in the fingers and the quiver's bolts are moved and shown on every
  client from the animator's state alone (`XbowRig`), so they need no network traffic and an interrupted reload is
  simply spanned and loaded on every screen at once. The click of the string into the nut is a local sound on each
  client.
- The settings come from the server when it binds its players. A change reaches the interval, range, damage and speed
  of the crossbowmen already loaded as well as new ones.

# 6. Configuration

Sections `6 - Skeleton Crossbowman` and `7 - Bone Crossbow` of `com.EliteCreaturesPack.cfg`, synced from the server and
locked while `Lock Configuration` is on there:

| Setting | Default | |
| --- | --- | --- |
| `Enabled` | true | off: no new crossbowmen; ones already in the world stay |
| `Share` | 15 | percent of the archer skeletons' spawns that are crossbowmen |
| `Damage Factor` | 1 | a bolt's blunt damage against the bow of the skeleton it replaces (20, 15, 55, 60) |
| `Shot Interval` | 6 | seconds between shots at the least (the archer: 4) |
| `Bolt Speed` | 40 | metres a second, straight (the archer's arrow: 30) |
| `Range` | 25 | metres it shoots from (the archer: 20) |
| `Craftable` | true | the Bone Crossbow can be made (section 7 of this file) |
| `Recipe` | Wood:10:5, BoneFragments:12:6, LeatherScraps:4:2 | its cost, item:amount:amount per upgrade; a placeholder |
| `Workbench Level` | 2 | the workbench level it needs, and its bolts (1 until 0.4.0; the user, 2026-09-29) |
| `Damage` | 30 | its own blunt blow, before the bolt's |
| `Damage Per Level` | 4 | blunt added by each upgrade |
| `Reload Time` | 4 | seconds with no Crossbows skill; the skill halves it |

# 6a. The Bone Crossbow

The players' version of the crossbowmen's crossbow, made at the workbench (asked for by the user on 2026-09-29; "don't
worry about the recipe yet", so its cost is a placeholder in the `Recipe` setting). Prefab `ECP_BoneCrossbow`, words
`item_ecp_bonecrossbow` and `_description`, icon rendered from the model (AssetWorkshop `assets/ecp_xbow_crossbow/icon.py`).

- **A copy of the game's Arbalest**, so it handles as the game's crossbows do: held in the left hand, the game's
  crossbow aim, fire and reload animations, the reload a minor action whose loaded state the game keeps in the player's
  ZDO (so every player sees it), the Crossbows skill, bolts as ammo, carried on the back when put away.
- **Its models** are the Arbalest's "Unloaded" and "Loaded" children, which the game's WeaponLoadState swaps: the
  string let go, and spanned with a blunt bolt laid in the groove (whatever bolt it will actually loose). The
  crossbowmen's crossbow at 1.25 times their size (1.04 m, a 0.9 m prod), its fore-stock on the left hand's attach
  point, so its prod lands where the Arbalest's is and its grip where the Arbalest's trigger is. The collider is a box
  round it; the Arbalest's upgrade glow (laid along its own 1.7 m) is gone.
- **Numbers**: 30 blunt of its own (+4 a level), three quality levels, 100 durability (+50 a level), weight 2, bolts at
  90 m/s (the Arbalest: 200), reload 4 s (the Arbalest: 3.5), no chop (the Arbalest chops at 140). A bolt adds its own
  damage: a bone bolt 32 pierce, an iron one 42.
- **Recipe**: at the workbench (`piece_workbench`), at `Workbench Level`, the `Recipe` cost; an unknown item name is
  logged and left out; off when `Craftable` is off or no workbench exists.

# 7. Open decisions

These were decided while building it, not by the user; each is easy to change:

- Name "Skeleton Crossbowman", prefab `ECP_SkeletonCrossbowman`; its own crossbow rather than the game's Mistlands
  arbalest (made of bones since the user asked on 2026-09-29), and its own grimy bone bolts (asked for the same day, to
  match the skeleton) rather than the game's clean white ones.
- The reload (span, fetch a bolt, lay it) is part of every attack; the request named only raise, aim and fire.
- The crossbow rides the left fist (as the archer's bow does) and the right hand spans and loads from a quiver on the
  right hip; the carry is a two-handed low ready instead of the archer's one-handed carry.
- Spawns: every archer skeleton kind has its crossbowman at one share (15%); their damage is the archer's
  (`Damage Factor` 1), shot every 6 s instead of 4, so about two thirds of the archer's damage over time.
- The Bone Crossbow's numbers (30 blunt, 3 levels, 4 s reload, 90 m/s) and its placeholder recipe; ammo is the
  game's bolts (the bone bolt is Mistlands-made, but the crossbowmen drop them); no craftable blunt bolt yet.
- Loot: bone bolts, usable only with a crossbow (Mistlands).
- Translations: the name and the crossbow's are English in every language for now.
- A macOS bundle (macOS players load the Windows bundle and may not see the crossbow).

# Build checklist

## Spawning
- [ ] In a Black Forest burial chamber about one skeleton in seven is a crossbowman, from its spawners and bone piles
- [ ] Swamp graves and bone piles give Swamp crossbowmen (60 health, 55 blunt); mountain cabins Mountain ones (75, 60)
- [ ] At night after Bonemass, wild skeletons sometimes come as crossbowmen
- [ ] `spawn ECP_SkeletonCrossbowman`, `_Meadows`, `_Swamps`, `_Mountains` make one of each

## Look
- [ ] The crossbow sits in the left fist round the fore-stock, the right hand on the wrist, at the low ready while it
  idles, walks and runs; nothing cuts through the legs
- [ ] The quiver hangs on the right hip with five bolts standing in it, clear of the arm and leg when it walks and runs
- [ ] The parts are lit like the skeleton (the game's creature shader), not flat or pink
- [ ] The string runs from the prod tips to the nut, with a bolt in the groove

## Fight
- [ ] It raises the crossbow to its right shoulder, lays its skull on the stock, holds, fires; the bolt leaves the
  muzzle with the arbalest sound and flies at the target
- [ ] The string flies to the prod at the shot, the groove is empty; it lowers the crossbow, hooks the string, draws it
  into the nut (click), takes a bolt from the quiver (one fewer in it) and lays it; then carries again
- [ ] It stands and shoots like the skeleton archer: no circling, no melee; walking right up to it, it keeps shooting
- [ ] The bolt hits a standing player from 5 to 25 m; a dodge, a raised shield or running across its line beats it
- [ ] Stars and mutations show and scale it like any creature

## Death and loot
- [ ] Its skeleton's drops, plus bone bolts about half the time

## Bone Crossbow
- [ ] The workbench lists it with the placeholder cost; it crafts and upgrades to level 3
- [ ] In the hand: the left hand on the fore-stock, the right on the grip, aimed along the prod; reload shows the
  string drawn and a bolt laid, the shot shows it let go; a second player sees the same
- [ ] It shoots bone and iron bolts, hits blunt plus the bolt's pierce, raises Crossbows, chops no trees
- [ ] Dropped, it lies on the ground on its box; on the back when put away it hangs sensibly
- [ ] The icon shows in the inventory and the crafting list

## Multiplayer
- [ ] On a dedicated server (Linux): prefabs register, a second player sees the raise, the shot, the string and the reload
- [ ] A settings change reaches the shot interval of crossbowmen already in the world

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-29 | At the user's request ("a new valheim skeleton unit ... treated as other skeleton units ... similar damage numbers and hp"; "that crossbow needs to be a craftable item ... a workbench item"): four crossbowmen, one per archer skeleton (`XbowKind`: base, Meadows, Swamps, Mountains; a copy of each, its bow's damage as blunt), spawning in place of those skeletons from every spawner at `Share`; the `Biomes`, `Crypts` and `Bone Piles` settings and `Bolt Damage` gave way to `Damage Factor`. The players' Bone Crossbow (`XbowItem`, a copy of the Arbalest with our models in its Unloaded/Loaded slots, 1.25x), section `7 - Bone Crossbow` with a placeholder workbench recipe (`XbowRecipe`), its icon. Patches verified (38 classes, 0 problems). | |
| 2026-09-29 | At the user's request: the stock half as long behind the grip (the butt 15 cm back instead of 30; the aim re-authored so the butt still meets the shoulder), the spine's bow deepened to 2.2 cm (the left fist 1 cm lower with it), and blunt bolts: a knuckle of bone for a head, `Bolt Damage` now blunt instead of pierce. | |
| 2026-09-29 | At the user's request (chose this crossbow over the ChatGPT-made Gravebranch, "it fits the game better"): the vertebrae bow 1.3 cm down in the middle, each leaning with the bow, threaded on a center bone with knobbed ends; the long groove strip became a notch on each vertebra (a straight bolt spans the bow). Same points; checks unchanged. | |
| 2026-09-29 | At the user's request ("make the reload animation take twice as long"): everything after the settled aim (0.56 s into the fire clip) plays at half pace; the fire clip's reload times are now string hooked 1.52, spanned 2.12, bolt taken 2.76, laid 3.60, carry 4.68 (were 1.04, 1.34, 1.66, 2.08, 2.62), mirrored in `XbowRig`. | |
| 2026-09-29 | At the user's request: the crossbow rebuilt from bones (femur stock, vertebra fore-stock, rib prod, vertebra nut, finger-bone lever, jawbone stirrup; same points, so the clips did not change) and a bolt of its own grimed like the skeleton (`ecp_xbow_bolt`, in the kit and on the projectile) in place of the game's; the quiver's bolts no longer cut through its walls (they stood near the walls and splayed out while the case narrows, and the game bolt's 10 cm vanes reached below the rim): they stand 14 cm out, aimed at the middle of the bottom, each turned so the vanes interleave. Checks unchanged; patches verified (36 classes, 0 problems). | |
| 2026-09-28 | Built at the user's request: the three parts (Blender), the aim, fire and carry clips authored on the game's Skeleton through its own Animator (two-bone IK, read back as muscles, played back through the Animator and checked: every key lands within a millimetre, the shot is 0.9 degrees off the aim, the seams 1 cm), the kit measured on the same skeleton, bundles for Windows and Linux (AssetWorkshop `assets/ecp_crossbowman`); the creature, shot, bolt, rig, spawns and section 6. Patches verified offline (36 classes, 0 problems). | |
