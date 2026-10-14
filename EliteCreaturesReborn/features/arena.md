# Elite Creatures Reborn - specification: the Arena (Hel's Trial)

Status: specified 2026-10-12, being built. Not seen in game.

An endless arena in Hel's hall: players step into the hall, wake Hel on her throne, and fight waves of the dead that
grow stronger until every player has fallen or left. Requested on Discord (an "Infinite Arena"); the user chose Hel's
realm as its setting and ECR as its home. The hall's art is made in `..\ValheimAssets\Assets\Locations\HelHall` and
`Assets\Creatures\Hel`.

The entrance is decided (section 2): **Helgrind**, Hel's gate, a craftable endgame portal piece. The rewards are
decided (section 3, "Rewards"): idols, coins and skill XP by how far a trial got, and EliteCrafting's runes and essence
where it is installed; arena creatures themselves still drop nothing. Open, decided later with the user (do not build):
a leaderboard.

---

# 1. The hall

Since 2026-10-13 an **outdoor arena**, 2.5 times the first hall across, with eight archways and no ceiling (the user:
"the indoor arena is too small. I need an arena 2.5x larger and it needs to be outdoors"). Its pieces are made to
`..\ValheimAssets\Assets\Locations\HelArena\CONTRACT.md`; the indoor hall's pieces stay in the workshop, and only its
throne is used.

- **One arena per world**, at a fixed point nobody reaches but by teleport: origin **(-10304, 5000, 0)**, due west
  10,304 m from the world's centre over the far ocean, on a zone's centre so it spans exactly 3 x 3 zones, where the world's land has faded out, 5,000 m up (until 3.26 it
  was (0, 5000, 0), over the world's spawn). Unrotated, +Z toward the gate. Everything solid stays inside the game's
  edge (r 10,420), whose push and kill act only near the sea; at that height the game treats it as indoors
  (`Character.InInterior`), like its dungeons, though it is open above.
- **Loaded only for those who go there.** While `Arena` is on, a player's machine builds the arena (as local,
  non-networked scene objects, never ZDOs) just before their jump in (Helgrind or `elite arena`), so the jump still
  lands on a floor with no loading screen, and also if they are found there (a login inside it); it is removed a
  minute after they last were there. A dedicated server builds only the colliders while any player is there, and
  removes them a minute after the last leaves. Helgrind is closed only while `Arena` is off. When the setting turns
  off (server sync), the arena is removed at once.
- **Old worlds** (3.25 and 3.26): graves left in the old hall are moved once, by the server, to just inside the new
  arena's gate; the trial removes the old core and its creatures.
- **Inside the arena** (`ArenaHall.Contains`): within the wall's inner face (r 66), from 5 m under the floor to 40 m
  over it.
- **Pieces** (bundles embedded in ECR, one per platform; prefabs in arena coordinates, origin at the floor centre):

  | Prefab | Where | What |
  | --- | --- | --- |
  | `ecr_helarena_floor` | origin | 125 m walking floor (r 62.5), kerb to r 65.6; a 22 m rune circle, 24 rune children `glow_rune_00` to `_23` (one shared emissive material) |
  | `ecr_helarena_walls` | origin | ring, inner face r 66, 10 m tall (16 m behind the throne); gate at +Z with door PARTS `door_left`, `door_right` and a passage closed at its back; eight blind archways at 22.5 + 45k degrees; 16 sconces; its invisible sky boxes (up to a cap at 40 m) lose their colliders: the arena is open above its walls |
  | `ecr_helarena_cover` | origin | ten broken pillars and ten low walls to take cover behind |
  | `ecr_helarena_land` | origin | the ground outside the wall and a ring of mountains 400 to 990 m out; its plateau boxes on every peer, its look on clients; low mist (the game's Swamp ground fog, tinted) at its `mist_*` points |
  | `ecr_helhall_throne` | (0, 0, -55.6) | stepped dais and throne against the wall's backdrop; ember glows `glow_brazier_0`, `_1` |
  | `ecr_hel` | the throne's `seat_root` | Hel, on the player's skeleton (scale on `Visual`) |

- **Points** (empty children of the pieces; read them from the built objects, never retype them): `spawn_0` to `_7`,
  `portal_0` to `_7`, `arrive` (in the gate passage, facing the arena), `gate_inside`, `flame_0` to `_15` (walls);
  `seat_root`, `seat`, `spear_rest`, `spear_top`, `boss_stand`, `fire_0`, `_1` (throne).
- **Materials:** stone pieces wear the game's building shader (`Custom/Piece`) borrowed from a stone piece's **body**
  renderer (never the snow overlay), tinted white, with the bundle's albedo and normal map; Hel wears the creature
  shader (`Custom/Creature`). Glow parts keep the bundle's own emissive material. Collider-only parts are left as
  they are.
- **Colliders:** the bundles' `col_*` boxes, on `static_solid`, so players stand on the floor, the walls and cover
  block players and creatures, and creatures path on the floor. **No invisible boundary:** the walls' sky boxes are
  stripped of their colliders on every peer, so nothing stops a player or a flier above the wall.
- **Hel's river kills:** under the black lake round the arena (its surface 8 m under the floor) nothing is solid. Any
  player or creature that goes under it near the arena dies at once, on its owner, through the game's own damage path:
  the player reads "Hel's river takes you.", a creature drops nothing, and every client sees a splash (the river's
  bubbles and the game's big water impact). It is not a death in the arena: the game's own skill loss applies, the
  price of escaping. The player's tombstone is moved from the air over the lake to just inside the arena's gate
  (`gate_inside`).
- **Light and mood:** the arena gets its own outdoor sky: Hel's realm under a dim sky, a copy of the game's
  `Twilight_Clear` (a real sky, clouds and aurora) made the same at every hour, dark, cold and blue-green, with a light
  fog so the mountains show through, and `Morkhalla`'s ambience and music, through an environment zone round the arena
  (160 x 60 x 160 m). Blue fire (from the game's blue brazier) at the sixteen sconces and both throne braziers, shadows
  only on the braziers. Visuals only on clients.
- **The runes** in the floor light at random: one to three at a time fade in, hold one or two seconds, fade out. During a
  run more of them, faster as the waves rise, calm again in the breaks between waves; on a boss wave all of them flare.
  Client only.
- **Hel** sits on her throne, a still presence using the player's own throne-sitting animation (the player prefab's
  animator and avatar on her `Visual`, `attach_throne` set). Not a creature yet (a boss later). Hovering her shows
  "Hel" and, when no trial is running and the player is in the arena, `[E] Begin the trial`.
- **The gate doors** stand open between trials and close while one runs (on every client, from the run state).
- **No building** in or round the arena: placing a piece within 75 m of the arena origin above 4900 m is refused with a
  message.

# 2. Getting in and out

- **Helgrind**, Hel's gate (piece `ECR_Helgrind`, "Helgrind": "Hel's gate, which leads to her hall and her trial"): a
  portal piece about the size of the game's wood portal (4.61 x 4.20 x 1.94 m, an arched opening 2.44 x 3.22 m), in the hammer's Misc tab while `Arena` is on, built at the **Black
  Forge** from **1 Sacrificial Blood** (`FrozenKingDrop`), **2 Frostcore**, **10 Bloodgold** (`Gold`) and **5 Memorial
  Coal**. Its art is `..\ValheimAssets\Assets\Props\HelGate\ecr_helgate` and the swirl `Assets\Effects\ecr_helgate_swirl`.
  - A copy of the game's `portal_stone` (placement, health, stone wear, sounds, its teleport trigger's box) **without
    its pairing**: no tag, no link with any other portal; the game's portal lists never see it.
  - **Walking into the opening** jumps you to the hall's `arrive` point at once (no loading screen) and keeps the gate's
    `exit` point (on the ground 2.25 m in front of it, facing away) as your way back. With `Arena` off or the hall not
    built, a centre message: "Hel's gate is closed." Carrying what the game will not let through a portal (ores, metals
    and the like), the game's own message, and you stay.
  - While `Arena` is on, its swirl turns in the opening with a cold blue-green light (#80ebe0, intensity 2, range 6)
    and the stone portal's hum; off, the gate stands dark but for its glowing Frostcores, coal and runes. Clients only.
  - With `Arena` off, Helgrind leaves the hammer; gates already built stay, closed.
- **The way out:** the wall that closes the hall's gate passage, behind `arrive`, carries Helgrind's swirl and light
  facing into the passage. Walk into it and you are jumped back, with the game's normal loading, to the gate you came
  in by (in front of it, facing away), or to where an admin stood for `elite arena`, or to your spawn point if nothing
  was kept.
- `elite arena` (admin, or devcommands in single player) stays: in the world, keeps where you stand and jumps you to
  `arrive`; in the hall, jumps you back as the way out does.
- Dying in the hall costs no skill levels and no gear (section 3, "Death"): no tombstone, you respawn with everything
  you carried.

# 3. The trial

- **Start:** a player in the hall interacts with Hel when no trial is running. Every player in the hall then is in it;
  players who arrive during it join it. A countdown of 10 seconds, the doors close, the first wave comes.
- **Waves** come out of the arena's archways (the outdoor arena's **eight**, round the wall at 22.5 + 45k degrees, their
  `portal_N` points about 67 m from the centre; the trial uses as many `portal_0`, `portal_1` ... as the built hall
  has), each wave's creatures given to all of them in turn, the next wave starting where the last stopped - not to the
  archways nearest the players, so a player standing by a wall is not handed the whole wave and the middle is come at
  from every side. An archway with
  creatures to send opens a **portal** in its recess (the workshop's `ecr_helhall_portal`, standing on the walls'
  `portal_N` point, with a cold light #80ebe0, intensity 1.8, range 7, faded in over 0.5 s and out over 1.5 s): about
  half a second later its first creature steps out of the recess, and the rest follow **one at a time**, 0.8 to 1.2 s
  apart, each archway at its own pace, each creature with the game's spawn sound, hunting the players at once (they
  come in near the walls, so the middle of the hall stays the players'): each is set on the nearest player in the hall
  and to the game's own hunt, which keeps it running in from 60 m whether or not it can see or hear its prey (the hunt
  finds the nearest player within 200 m again whenever the target is lost, and the trial points it at one every
  second). The portal closes a second after its last creature (the loop stops and fades, the closing one-shot
  `ecr_helhall_portal_close` plays); its light is drawn up to 140 m away, so a portal across the arena still glows.
  Never more than **24** arena creatures alive (20 in the indoor hall); the rest wait. Portals are drawn on clients only, local copies; which archways are open comes from
  the core's ZDO, so every client sees the same.
- **A wave is a round:** the next wave comes only when the current one is **cleared** - nothing of it alive or still
  to come, held for 2 s (a boss's next phase, a Splintering creature's copies). Then a **break**: 20 s, 25 s after a boss
  wave (15 and 20 before the anvils: time to reach one in the bigger arena); every player of the trial reads "Wave 7
  cleared. The next comes in 20 seconds.", and the event bar shows "Hel's Trial - Swamp, wave 7 cleared - next wave
  in 14 - 342 souls" (the biome of the wave; "Hel's Trial - Swamp - the first wave in 0:07" in the countdown). A wave not cleared within **5 minutes** (3 in the indoor
  hall: creatures now run about 60 m in, a slow one for half a minute) counts as cleared:
  its stragglers vanish with no drops, scoring nothing, and the break begins.
- **Stragglers** (a player's report, 2026-10-14: a first-wave Ghost, slow and hard to see in the dark, took a long
  walk to find; the user chose a recall with marks): once every creature of the waves fighting has come out and no more
  than a quarter of them is left (rounded, at least 1: 6 leave 2, 8 leave 2, 10 leave 3, 12 leave 3), Hel recalls them a
  minute later. Every player of the trial reads "Hel marks the last of the dead on your map. She recalls them in 60
  seconds."; on every client each straggler flares once in cold soul-fire, carries the hall's cold light (range 8 m,
  seen across the arena) and shows on the map as the game's event marker with its name; the event bar reads "Hel's
  Trial - Swamp, wave 7 - 2 left, recalled in 0:45 - 342 souls". When the minute is up the waves fighting end as a
  swept wave does (the stragglers vanish, scoring nothing, the break begins, "Wave 7 cleared."). Never while a boss
  lives or is still to come; a wave called early calls it off; creatures joining after it started (Splintering copies)
  are marked too. Kept in the core's ZDO (`ecr_arena_recall`, `ecr_arena_size`); the marks are drawn locally, nothing
  is sent for them.
- **Getting harder** (all fixed numbers in the code; the user, 2026-10-13: "the difficulty needs to scale up a bit. I am
  on wave 6 now still fighting neck, boar, greyling"):
  - **First biome from the players' gear:** as Hel wakes, the core's owner reads the strongest gear among the players
    alive in the arena with the raids' own gear check (`Raids/Heat/GearTier`: the best metal a player has ever held and
    the armour worn, published by each player's machine in its player's ZDO): 0 (no metal: leather, troll, bone)
    Meadows, 1 Bronze the Black Forest, 2 Iron the Swamp, 3 Silver the Mountains, 4 Black metal the Plains, 5 Mistlands
    gear (carapace, eitr-weave) the Mistlands, 6 Flametal the Ashlands, 7 Bloodgold or a Deep North set the Deep North
    (the raids still count that as 6). The trial begins in that biome and skips the earlier ones; it is kept in the
    core's ZDO, so players who come later change nothing.
  - **Biome by wave:** five waves a biome from the first: waves 1-4 its creatures, wave 5 its boss, then the next
    biome. After the Deep North's boss it stays in the Deep North and only the stars and mutations keep rising.
  - **Creatures:** each biome's hostile land creatures, read at runtime from the game's own spawn lists (so other mods'
    creatures can appear too), with no bosses, no water-only creatures and nothing passive.
  - **How many:** 6, 8, 10 and 12 in a biome's four waves (alone), then its boss; a quarter more for each player
    past the first.
  - **Elites** (the user, 2026-10-13: "bears/trolls should spawn at the later waves of this category, and we need to
    limit the amount"): a kind whose body ECR's own measure finds large (`Traits/BodySize`: a body 1 m or more wide or
    3.5 m or more long with 300 or more base health; modded creatures measured the same) or whose base health is over
    2.5 x its biome's middle kind's. A biome's waves 1 and 2 have none, wave 3 at most 1, wave 4 at most 2; past the
    Deep North's boss at most 3, one more every 10 waves past it, at most 6; each +1 for every two players past the
    first. An elite drawn past that is drawn again from the biome's other kinds, so the count stays; never two elites
    out of one archway in a wave. Bosses are apart. In this game (2026-10-13): the Meadows none; the Black Forest Bjorn,
    Troll; the Swamp Abomination, Writhan; the Mountains Stone Golem (and the Elite Creatures Pack's Rime Giant); the
    Plains Unbjorn, Fuling berserker, Lox; the Mistlands Gjall, Seeker Soldier; the Ashlands Fallen Valkyrie, Morgen,
    Asksvin; the Deep North Barka, Moose, Jotun warrior. The log's biome lines mark them "(elite)".
  - **Stars and mutations:** each creature rolls as ECR rolls any creature (`CreatureRoll.For`), plus extra stars
    (+1 from the 3rd wave of each biome, +1 more for every 5 waves after the Deep North's boss, up to ECR's maximum)
    and a mutation chance of 15% in the first wave, 4% more each wave (at most 60%, from wave 13).
- **Bosses:** every 5th wave is a boss wave instead: the biome's boss alone on the rune circle (Eikthyr, the Elder,
  Bonemass, Moder, Yagluth, the Queen, Fader, Kall Fimbulbringer; after Kall the cycle starts again from the trial's
  first boss, with more stars),
  rolling ECR's boss aspects as any boss does, with the game's spawn effect (no portal). The runes flare as it appears.
  The wave is cleared when it and everything it brought are dead.
- **Souls** (the user, 2026-10-13: "can score instead be souls?"): each kill is worth **2 x the biome tier (1 Meadows to
  8 Deep North) x (stars + 1)**, x 1.5 for each mutation it carries (x 1.5 per mutation); a boss **50 x the tier x
  (stars + 1) x its aspects' loot multiplier** (ECR's own value for its aspect, all of a Bountiful boss's multiplied).
  Copies, decoys, a boss phase that brings the next, and creatures the river drowned are worth nothing. Souls are per
  player: every kill's souls go to **every player of the trial in full**, each into their own wallet kept in the core's
  ZDO (the core's owner is the one who writes it; a player who joins later has only what was earned since). Each player's
  event bar shows their own: "Hel's Trial - Mountains, wave 12 - 7 left - 342 souls", in a break "Hel's Trial -
  Mountains, wave 12 cleared - next wave in 14 - 342 souls".
- **Ready (F):** during a break, or the countdown before wave 1, a player of the trial alive in the arena presses
  **F** (fixed, no setting; read through ValheimModLibs' Hotkeys, so not while typing in chat, the console or a text box,
  and only when the game takes the player's input: no menu, inventory or map open) to say they are ready; a second press
  takes it back. The vote goes to the core's owner, which keeps the break's ready set in the core's ZDO (emptied as
  every wave begins). When every player of the trial alive in the arena at that moment is ready - the dead, the
  respawning and those away neither count nor hold it up - the break or countdown ends at once and the next wave comes.
  Each vote: "Ready: 2 of 3" to the trial's players; the last: "All ready - the dead come!". The event bar in a rest
  ends "- F: ready (1/3)", or "- (ready) 1/3" for a player who has voted: "Hel's Trial - Mountains, wave 12 cleared -
  next wave in 14 - 342 souls - F: ready (1/3)". Hel's Anvil works until the wave starts.
- **Calling the next wave** (the user, 2026-10-13: "make it so pressing E on Hel will spawn in the next wave during
  this wave"): during a wave, once all its creatures have come out (none still waiting), Hel's hover shows a player of
  the trial alive in the arena "[E] Call the next wave". One press by any of them begins the next wave at once on top
  of the one fighting ("Ana calls the next wave!"); the called wave can be called past in its turn once its own
  creatures are out. A boss wave may be called early; past a living boss Hel answers the caller "Hel waits until Moder
  falls.". The alive cap holds (the called wave's creatures wait their turn). Each wave fighting is cleared by its own
  creatures ("Wave 6 cleared."; a boss wave's idol then); there is no break between them, the break follows the last to
  clear. The 5-minute limit restarts with each call and sweeps every wave fighting.
- **Hel's Anvil:** two anvils, one each side of Hel's dais (the workshop's `ecr_helarena_anvil`, placed by the hall).
  Hover: "Hel's Anvil" and, for a player of the trial in the arena, during the countdown or a break "[E] Repair your gear
  (120 souls)" or "Not enough souls (120)"; during a wave "The anvil is cold while the dead come". A repair costs **10 x
  the biome tier of the wave coming x 1.5 per repair the player has already made in this trial** (rounded). E asks the
  core's owner, which checks the player's souls, takes them and answers; the player's own machine then mends every worn
  item they carry (worn or in the bags) to full, as the game mends at a station, with the game's repair effect and
  sound (a forge's) at the anvil, and every player of the trial sees cold soul-fire flare there. Nothing worn: "Nothing
  you carry needs mending."
- **Boss cache:** when a boss of the trial falls (the last of Kall's phases; not a copy), **60%** of the time, for each
  player of the trial **1 to 3 healing meads** of the boss's tier drop on the rune circle near where it died (drawn in to
  9 m of the centre), with a burst of cold soul-fire: tiers 1-2 `MeadHealthMinor`, 3-4 `MeadHealthMedium`, 5-6
  `MeadHealthMajor`, 7-8 `MeadHealthLingering`, or the best of those the game has. Real items on the floor, made once by
  the boss's owner: the one thing an arena creature leaves.
- **End:** when no player of the trial is alive in the hall (dead, left, logged out). The arena creatures left
  vanish; the doors open; every player of the trial reads "Hel's Trial ends at wave 14: 132 kills, 1,842 souls." (the
  souls they earned in it)
  `elite arena stop` (admin) ends it the same way.
- **Arena creatures drop nothing** (no loot, trophies or coins). Skills rise from the fighting as always.
- **Rewards** (the user, 2026-10-13: "reward players with Potential Forge Totems, XP, or something else depending on
  how far they managed to progress"; "if EliteCrafting is installed, runes and essence drop from the arena depending
  on how far you get"). Paid when the trial ends to **every player of it, dead or alive** (it ends only when nobody is
  left standing, so dying forfeits nothing); a player offline at the end gets nothing. Fixed numbers
  (`Arena/Rewards/RewardTable.cs`); "waves cleared" counts swept waves too, "boss waves beaten" only those whose boss
  fell:

  | Reward | How much |
  | --- | --- |
  | Idols | one Forge of Potential idol per boss wave beaten, at the tier of that boss's biome: Meadows 0 (Wooden), Black Forest 1 (Bronze), Swamp 2 (Iron), Mountains 3 (Silver), Plains 4 (Black Metal), Mistlands 5 (Black Marble), Ashlands 6 (Flametal), Deep North 7 (Bloodgold), every boss after Kall another 7; Battle (`Upgrader<t>Weapon`) and Protection (`Upgrader<t>Armor`) in turn, the trial's first boss Battle |
  | Coins | the player's unspent souls / 2 |
  | Skill XP | each skill the player raised during the trial (in the hall, while it ran) raised again by that gain x (waves cleared / 10), at most x2 |
  | Runes (EliteCrafting) | per regular wave cleared, by the wave's biome, 1, 2, 2, 2, 2, 2, 3, 3 (Meadows ... Deep North); per boss wave beaten 5, 3, 4, 4, 5, 6, 7, 7 (Eikthyr ... Kall, then 7); each drawn by EliteCrafting's default weights at the wave's tier (Awakening, Recasting, Ascension from the Black Forest, Cleansing, Sealed from the Swamp) |
  | Essence (EliteCrafting) | per regular wave cleared a trophy of its biome at the Rune Table: 2, 4, 6, 8, 10, 12, 14, 16; per boss wave beaten its trophy: 20, 30, 40, 50, 60, 70, 80, 90 |

  The runes are about half EliteCrafting's open-world rate for the same kills (its 35-70% a kill would be 3-6 for a
  wave's 8 creatures), since every player of the trial gets the whole reward and the trial can be run again at once.
  So a solo trial begun in the Meadows that ends at wave 12 (11 cleared, two bosses beaten) pays a Wooden Battle Idol,
  a Bronze Protection Idol, half its unspent souls in coins, +110% skill XP and, with EliteCrafting, 22 runes and 80
  essence; begun in the Mistlands, its first boss (the Queen) pays a Black Marble Battle Idol.
  Items go into the inventory, what does not fit falls at the player's feet; a dead player gets theirs at the respawn,
  where they respawn. Each item shows at the top left with its icon as it arrives, as the game shows an item picked up
  ("Added", or "Dropped" when it fell at the player's feet). Four seconds after the end message: "Hel rewards you:
  Wooden Battle Idol, Bronze Protection Idol, 412 coins, 22 runes, 80 essence, +110% skill XP.", in the centre and in
  the chat window (which opens for a moment), where the end message goes too (a player's report, 2026-10-14: the
  reward was hard to make out). The trial's first cleared wave says when it is paid: "Wave 1 cleared. The next comes
  in 20 seconds. Hel pays your reward when her trial ends." While a trial runs, Hel's hover lists what it would pay
  this player now: "Current rewards: Bronze Battle Idol, 120 coins, 6 runes, 20 essence, +30% skill XP" ("none yet"
  before anything is earned; a player's question, 2026-10-14). Items the game does not have are left out; EliteCrafting's
  only where it is installed.
- **Death:** a player who dies inside the hall loses no skill levels: on the dying player's machine the death is the
  game's own soft death (the one it gives within ten minutes of the last; the game says "The Gods are merciful"), so the
  game's skill loss never runs, and a world that resets skills on death does not reset them there. GrindstoneSkills
  applies its own loss inside that same game step, so it takes nothing either. After the respawn: "Hel spares your
  skills and your gear." No tombstone is made (the user, 2026-10-14: "they shouldn't have a tombstone for dying in the
  arena. this is an endgame challenge, you're kinda supposed to keep going until you die for maximum reward"): the
  player respawns with everything they carried, still worn, as with the game's own "keep inventory" world setting.
  Everything else is the game's own death (food lost, effects cleared).

# 4. Multiplayer

Built for a dedicated server first.

- **The trial lives in one networked object:** a hidden, persistent `ECR_ArenaCore` at the arena's origin (far out
  over the ocean, loaded only by those who jump there), placed by the server when the world has none: the server holds
  every ZDO, so it makes the core's without the zone being loaded. Its ZDO holds the trial: its phase (countdown, wave,
  break), wave, the souls earned and each player's wallet, kills, creatures left, the wave's creatures still to come by archway, the archways with open
  portals, the last wave cleared, the boss waves beaten, the end of the countdown, the wave's 5 minutes or the break
  (server clock), and the trial's id.
- **Who runs it:** the core's owner, by the game's own ownership: a player at the arena (the server never has the
  arena in its area, so it never owns the core while anyone is there). The trial runs on that player's game - spawning,
  counting, ending, paying - as it would on a server. If the owner leaves (the way out, `elite arena`, a logout) while
  others are still there, the game hands the core to one of them within about two seconds and the trial carries on
  from the ZDO (each archway's pace starts afresh); the creatures the old owner held pass the same way. An owner whose
  hall is not built (its player has just left) does nothing meanwhile, never ends the trial.
- **When the last player leaves:** a player dying in the arena stays there until they respawn, so the owner sees the
  arena empty and ends the trial after 5 s as before. One jumping out or logging out takes the owner with them, so the
  server watches instead (`ArenaOrphanWatch`, every 5 s): while a trial runs and no machine has the arena in its area
  for 10 s, the server takes the core, ends the trial as an empty arena does (the end told, the reward paid to every
  player of it), and removes the trial's creatures by their ZDOs (no death, no drops). A server restarted mid-trial
  ends it the same way. Arena off: the owner ends a trial being fought; one nobody is at, the server.
- **Older worlds:** a world from 3.25.0 or 3.26.0 has its core at the old origin, (0, 5000, 0) over the world's centre.
  At the server's first check of a session, everything of the old arena still there is removed (`ArenaLegacy`): the
  old core and any creature tagged as an arena creature, in the 3 x 3 zones round the old origin and within 100 m of
  its height (nothing on the ground), with no death and no drops; the new core is then placed. Tombstones of players who
  died in the old hall stay where they were, high over the world's centre.
- **Starting** sends an RPC from the player's client to the core's owner.
- **Ready votes** go to the core's owner (`ecr_arena_ready_vote`, on the core), which keeps the ready set in the
  core's ZDO (`ecr_arena_ready`, peers) and counts it against the players alive in the arena as it sees them; every
  client draws its own "(ready)" and the count from the same set.
- **Souls** are counted once on the core's owner: a kill adds its souls to the trial's total (`ecr_arena_souls`), and each
  player's wallet (`ecr_arena_wallets`: their peer, the total when they joined, what they spent, their repairs) makes
  their own share. **Hel's Anvil** asks the core's owner (`ecr_arena_repair`, on the core), which answers the asker
  (`ecr_arena_repaired`, routed) and has every player of the trial see the flare (`ecr_arena_flare`, routed); the
  asker's own machine mends its gear. The **boss cache** is made by the boss's owner as it dies, which holds the core
  and so knows the players; its flare goes the same way.
- **The owner counts the creatures it has loaded.** The arena (wall r 66, about 69 m with the wall) spans the 3 x 3
  zones round its origin when the origin is at a zone's centre (x and z multiples of 64); off-centre it would cross
  four zones a side and a player at one edge would not hold the other at the default distance. The game's near
  simulation distance (2 zones by default) gives the core's owner every arena zone from anywhere in the arena. At the
  lowest simulation setting (1 zone) a player owning the core from the arena's edge would not hold the far side: a wave
  could count as cleared with creatures still there, which then vanish. There is no server fallback for this any more
  (the server never owns the core); it needs the lowest setting and an owner at the edge, so it is left as it is.
- **Arena creatures** are tagged in their ZDO with the trial's id and their wave; a tagged creature with no running
  trial behind it, or whose wave has been cleared or swept, vanishes on its owner. Their owners skip their drops.
- **Every client** draws the doors, the runes, the portals and the event bar from the core's ZDO.
- **A death in the hall** is decided on the dying player's own machine, where the game takes the skill levels.
- **Messages** (start, boss, end) go by RPC to the players of the trial only.
- **Rewards:** the core's owner plans the reward once as the trial ends (the same for every player of it) and sends
  each player of the trial one RPC with the list (items and counts, coins, EliteCrafting's items, the XP factor); the
  player's own machine adds it, and applies the XP bonus from what it recorded itself: each raise through the game's
  `Skills.RaiseSkill` while the local player was in the hall during that trial, raised again through the same path so
  other mods see it.

# 5. Configuration

One setting, synced from the server and lockable (the Charter clause, as raids):

- `12 - Arena` / `Arena` = `false`: the hall exists, Hel's Trial can be run and Helgrind is in the hammer. Off: no hall
  is built, Helgrind leaves the hammer and built gates are closed, `elite arena` says the arena is off, and arena
  creatures still in the world vanish.

Everything else is a fixed number in the code. Console commands: `elite arena`, `elite arena stop` (admin).

# 6. Decisions

Settled with the user on 2026-10-12:

1. The arena is in **Elite Creatures Reborn**, behind one setting `Arena`, off by default.
2. **Full arena** in the first build: the hall, Hel on her throne, endless waves, bosses, score.
3. **Normal death:** the tombstone stays in the hall. (Replaced 2026-10-14: no tombstone, gear kept.)
4. **One hall, always loaded** on every peer, at a fixed point (not an instance per gate), so jumps in are instant.
5. **Rewards wait** for the user (see the top) - decided on 2026-10-13, decision 10.
6. **The entrance is Helgrind** (decided 2026-10-12): a craftable endgame portal piece at the Black Forge (1 Sacrificial
   Blood, 2 Frostcore, 10 Bloodgold, 5 Memorial Coal), without the game's tag pairing; the way back is the wall at the
   end of the hall's gate passage, to the gate you came in by. `elite arena` stays for admins.
7. **Rounds with breaks** (the user, 2026-10-13, after playing): "a short waiting period between rounds ... a brief
   moment to prepare". The next wave comes only when the last is cleared, after 15 s (20 s after a boss); no early
   trigger and no timer pushing waves in; a wave not cleared in 3 minutes is swept.
8. **Creatures come out at the walls, one at a time, through portals** (the user, 2026-10-13): "a little closer to the
   arena walls, giving players more space in the centre", "run out of the portal one at a time", "show a portal
   animation at the walls".
9. **No skill loss in the hall** (the user, 2026-10-13): "it should feel like a dedicated challenge rather than a
   punishing death". Decision 3 (normal death, the tombstone in the hall) stands for everything but the skills.
10. **Rewards by progress** (the user, 2026-10-13): Forge of Potential idols per boss beaten, coins (from souls since
    decision 11),
    bonus skill XP, and EliteCrafting's runes and essence when it is installed, paid at the end to every player of the
    trial, dead or alive; arena creatures still drop nothing. Section 3, "Rewards".
11. **Souls, Hel's Anvil and the boss cache** (the user, 2026-10-13: "I like Hel's Anvil, and can score instead be
    souls? Each creature has a soul value ... modified based on star level and mutation/aspect? Maybe a boss cache"):
    souls replace the score, per player; two anvils mend gear for souls in the breaks (20 s, 25 s after a boss); a boss
    may leave healing meads; coins are unspent souls / 2.
12. **Ready with F** (the user, 2026-10-13: "allow pressing F key to go to the next wave, multiple people... everyone
    must press"): every player alive in the arena presses F and the break ends at once; a second press undoes it; F is
    fixed, no setting.
13. **Harder sooner, from the players' gear** (the user, 2026-10-13: "the difficulty needs to scale up a bit. I am on
    wave 6 now still fighting neck, boar, greyling"): the trial starts at the biome of the strongest gear in the arena
    (Bloodgold or a Deep North set the Deep North), five waves a biome (the fifth its boss), 6/8/10/12 creatures, +1 star
    from a biome's 3rd wave, mutations 15% +4% a wave; rewards follow the biomes fought, skill XP x waves / 10.
14. **Calling the next wave with E at Hel** (the user, 2026-10-13): any player of the trial alive in the arena, one press,
    once the wave's creatures are all out; not past a living boss; the break waits for every wave fighting.
15. **Few elites, late in a biome** (the user, 2026-10-13: "bears/trolls should spawn at the later waves of this
    category, and we need to limit the amount"): ECR's large-body measure or 2.5 x the biome's middle health; none in
    waves 1-2, 1 in wave 3, 2 in wave 4, 3 to 6 past the Deep North, +1 per two extra players; one per archway.
11. **No invisible boundary, and the river kills** (the user, 2026-10-13): "can we get rid of the invisible boundary,
    and if a player escapes the arena and touches the black lake, then they die instantly". The walls' sky boxes lose
    their colliders; a fall into the river is a normal death (skills lost as usual), its grave at the arena's gate.
12. **The arena is loaded only for players who teleport there** (the user, 2026-10-13: "we also should have this area
    somewhere, where the only way it's loaded is if players teleport to it"): it moved from over the world's spawn to
    the far western ocean beyond the land, and is built on a machine only while its player (or, on a server, any
    player) is there.

---

# Build checklist

`[ ]` not started, `[~]` partly, `[x]` built and seen working on a dedicated server.

## The hall (`Arena/Hall/`)
- [~] Bundles embedded, both platforms; prefabs loaded on every peer
- [~] Hall built at load when `Arena` is on, removed when it turns off; on the dedicated server too
- [~] Materials dressed (stone body material, creature material, glow kept); colliders on the right layer
- [~] Environment zone, blue fires, oculus light (clients only)
- [~] Runes lighting at random, more during a trial, all on a boss wave
- [~] Hel seated with the player's throne animation; hover and `[E] Begin the trial`
- [~] Doors open between trials, closed during one
- [~] Building refused in the hall

## The trial (`Arena/Trial/`)
- [~] `ECR_ArenaCore` registered and placed by the server; state in its ZDO; owner runs it
- [~] Start RPC from Hel; countdown; joiners
- [~] Waves: biome lists from the game's spawn lists, first biome from gear, counts, stars, mutations, 24 alive, timers
- [~] Boss waves every 5th; elites capped per wave; calling the next wave at Hel
- [~] Tagging, no drops, orphans vanish
- [~] Souls and wallets, event bar line, messages; Hel's Anvil; the boss cache
- [~] End conditions, cleanup, `elite arena stop`

## Getting in (`Arena/Travel/`, `Arena/Gate/`)
- [~] `elite arena` in and out, return point saved
- [~] Helgrind: bundles embedded, piece from `portal_stone` without its pairing, hammer and Black Forge while `Arena`
  is on, walk-in jump with the gate's exit kept, closed and no-teleport messages, swirl, light and hum on clients
- [~] The way out: swirl, light and trigger on the passage's back wall, back to the gate's exit

## Docs
- [~] `CLAUDE.md` Arena section, `console-commands.md`, `configuration.md` (the hall's part and the setting: done; the
  trial's part and both commands: done)

## Work log

- 2026-10-12, the hall: the four hall bundles and Hel's (`ecr_hel`) embedded for Windows and Linux (21.6 MB in all);
  the `Arena` setting bound and held by the server's charter; `Arena/Hall/` builds the hall on every peer from a host
  made in `ZNetScene.Awake`, removes and rebuilds it on the setting's change; stone in `Custom/Piece` from
  `stone_wall_4x2` (`New/stone`), Hel in `Custom/Creature` from the Draugr, glow parts their own; everything on
  `static_solid`; on clients the `ECR_HelHall` environment (Morkhalla, fog 0.015, not cold), blue fire and flickering
  lights from `MountainKit_brazier_blue` at the ten bowls (shadows on the throne's two), a cold spot in the oculus, the
  runes; Hel seated on the player's throne loop with her spear, her hover and `[E] Begin the trial`; the doors on every
  peer; building refused (the game's no-build status). Judgement calls: the hall's sky is not cold (no fire to warm
  by); the lights are tinted the runes' blue-green; Hel's hover reaches 3 m further than the game's. Builds clean;
  offline patch check passes; nothing seen in game. Code map and the lights in `CLAUDE.md`, "Arena: Hel's hall".
- 2026-10-12, the trial and the travel: `Arena/Trial/` and `Arena/Travel/`, `elite arena` and `elite arena stop`. The
  hidden persistent `ECR_ArenaCore` (added to `ZNetScene` in an `Awake` prefix on every peer; the server keeps exactly
  one at the origin) holds the trial in its ZDO (`ecr_arena_*`), its owner steps it twice a second; Hel's ask is an RPC
  to that owner. Creature lists from the game's spawn lists by each creature's home biome (the first a keyless entry
  names, else a single-biome keyed one), fixed fallback lists checked in game; bosses Eikthyr to FrozenKing, his later
  phases adopted as the wave's boss. Arena creatures tagged, rolled through the raids' `RaiderTraits.Roll` (a second
  overload: stars, mutation chance and barred mutations given; Gilded barred), hunting at once, allied by one group,
  dropping nothing (the drop list emptied last). Everything else that appears in the hall during a trial (a phase, a
  Summoner's adds, copies) is adopted on its owner; a tagged creature with no trial behind it vanishes. Raid files
  touched: `RaiderTraits.cs` (the overload; raids unchanged) and `RaidHudPatch.cs` (asks the arena's line first).
  Judgement calls: the boss comes on the rune circle at the floor's centre, not the dais's `boss_stand`; a wave lasts at
  least 5 s; the hall ends 5 s after its last living player; kinds tougher than the middle of their biome come less
  often (square root of the health ratio); an arena boss sets no world key, plays no dream, counts for neither `harder
  each kill` nor the boss hints; copies and a phase that brings the next score nothing; a tamed arena creature leaves
  the trial; `elite arena` lets a player out of the hall even with Arena off. Builds clean; offline patch check passes;
  nothing seen in game. Code map in `CLAUDE.md`, "Arena: Hel's Trial".
- 2026-10-12, the gate: Helgrind (`Arena/Gate/`). Bundles `ecr_helgate` and `ecr_helgatefx` embedded for Windows and
  Linux (2,139,736 bytes; the smaller revision the same day 1,114,847). `ECR_Helgrind` copied from `portal_stone` in a `ZNetScene.Awake` prefix on every peer; its
  `TeleportWorld` removed and its `TELEPORT` box's trigger swapped for ours; the swirl's particles and light removed,
  its hum kept under its `EffectFade`; the model, its mesh and floor colliders replaced by the gate's, dressed on
  clients in the hall's stone (`Custom/Piece` from `stone_wall_4x2`, gloss 0.15) with its Bloodgold metal mask at 0.76.
  In the hammer as the Raiders Chest is. Walking in: `ArenaTravel.Enter` with the gate's `exit` point, 1 m up, as the
  return point (now kept with its facing, `x,y,z,yaw`; an older `x,y,z` still reads). The way out: built from the hall
  by one call in `HallBuilder.Look`, 1.6 m behind `arrive` (the walls' back wall at r 29.5), the swirl's sill on the
  passage floor (centre 1.73 m up, read from the gate), its light 0.2475 m out, a 2.24 x 3.12 x 0.7 m trigger on
  `character_trigger`. Revised the same day to the smaller gate: the walk-in box moved to `portal_center` and sized
  (2.24, 3.12, 0.40), lights range 6, the points read from the gate's model. Judgement calls: the item rule is the game's
  own per-item one (`IsTeleportable(false)`), whatever the stone portal allows; the world's no-portals key does not
  close Helgrind (the way out comes back to the same gate, so it is no shortcut); the gate has a hover (name and open
  or closed). Builds clean; offline patch check passes; nothing seen in game. Code map in `CLAUDE.md`, "Arena:
  Helgrind".
- 2026-10-13, the trial after the user's first games (ECR 3.25.0): **rounds with breaks** - the next wave only when
  the last is cleared (nothing alive or to come, held 2 s), then 15 s (20 s after a boss) with "Wave N cleared" and the
  break's line in the event bar; the "2 or fewer" trigger and the wave timer are gone; a wave not cleared in 3 minutes
  is swept (its creatures, tagged with their wave now, vanish on their owners once the core's last swept wave reaches
  theirs, scoring nothing). **Archways**: each wave's creatures are given to the four archways in turn and each sends
  one at a time, 0.8 to 1.2 s apart, out of its recess (0.8 m off the `portal_N` back face, on the recess floor), the
  first about 0.6 s after its portal opens; bosses keep the rune circle. **Portals**: the core's ZDO carries which
  archways have creatures to come; every client opens the workshop's `ecr_helhall_portal` (bundle
  `ecr_helhall_portalfx`, both platforms, embedded) on that `portal_N` point with its light, and a second after the last
  creature stops the loop and plays `ecr_helhall_portal_close` (`Arena/Trial/ArenaPortals.cs`, `ArenaPortal.cs`,
  `ArenaPortalLook.cs`). **Death**: a death inside the built hall is the game's own soft death on the dying player's
  machine (no `Skills.OnDeath`, so no GrindstoneSkills loss either, no change there), `Skills.Clear` skipped during it,
  "Hel spares your skills." after the respawn (`ArenaDeath.cs`). `ArenaTrial.Break` added for the hall's runes. The
  core steps four times a second. Builds clean; offline patch check passes; nothing seen in game.
- 2026-10-13, rewards (`Arena/Rewards/`): paid at the trial's end to every player of it, dead or alive, one RPC each
  from the core's owner (`RewardPlan`, `RewardSend`); on the player's machine (`RewardGrant`, `RewardText`) items into
  the inventory or at the feet, the XP bonus (`SkillLedger`, a `Skills.RaiseSkill` prefix at `Priority.First`, records
  each skill's raises in the hall during the trial; `SkillBonus` raises them again in steps of the trial's average raise,
  at most 2000 per skill, since the game drops what one raise earns past a level), then "Hel rewards you: ..." four
  seconds after the end message; a dead player's at the respawn. Idols per boss wave beaten (the core keeps
  `ecr_arena_bosses`, a bit per boss wave whose boss fell); coins = score / 10; XP x (cleared / 20), at most 2. With
  EliteCrafting (`CraftingLink.Present`, ValheimModLibs' EliteCraftingLink, now merged into ECR; its new
  `CraftingPrefabs` holds the published item names) runes and essence per wave as in the table. Idol prefab names
  checked against the game's item reference (`Upgrader0Weapon` ... `Upgrader7Armor`); every item is looked up in
  `ObjectDB` and skipped when missing. Builds clean; offline patch check passes; nothing seen in game.
- 2026-10-13, the outdoor arena (the user: 2.5x across, about 130 m, 8 archways; `ValheimAssets/Assets/Locations/
  HelArena/CONTRACT.md`): the trial uses every `portal_N` the built hall has, up to 8 (`ArenaTable.Archways`, counted
  by `ArenaSpots.ArchwayCount`), each wave's creatures round all of them in turn; the portal bits, the queue's lanes and
  the clients' portals take 8. Numbers for the size: first wave of a biome 6 (was 4), 24 alive (was 20), a wave swept
  after 5 minutes (was 3); the portal light drawn up to 140 m (the game's LOD stops at 40). Hunting from 60 m checked in
  the game's MonsterAI: a hunting creature keeps running at its target with no sight or hearing, the 30 s "lost it"
  drop is undone by the hunt (nearest player within 200 m) and by the trial's own one-second aim. Every "in the hall"
  test is `ArenaHall.Contains` (the hall agent's, r 66 now): players, the HUD line, adoption, the death protection and
  the XP ledger follow it with no change here. Score, rewards and the boss's rune circle unchanged. Builds clean;
  offline patch check passes; nothing seen in game.
- 2026-10-13, the hall made outdoor (the hall agent): ECR embeds `ecr_helarena_floor`, `_walls`, `_cover` (20,007,535
  bytes, both platforms) and drops `ecr_helhall_floor`, `_walls`, `_ceiling`, `_cover` (19,291,623; they stay in
  the workshop); `ecr_helarena_land` is wired for clients and logs "land missing" until its bundle is embedded. Throne
  at (0, 0, -55.6); everything on `static_solid`, the walls' sky boxes kept as colliders and never dressed.
  `ArenaHall.Contains` is r 66, y -5 to 40; no building within 75 m. The sky is `ECR_HelArena`, a copy of
  `Twilight_Clear` made dim, cold and blue-green at every hour, fog 0.0015, Morkhalla's sound, in a 160 x 60 x 160 m
  zone; the oculus light is gone; fires at 16 sconces and 2 braziers (18 point lights, 2 with shadows, lit within 90 m).
  Helgrind's way out finds the passage's back wall by a ray onto the walls' `col_box_gate_back` (z 70.8, the plan's
  2.4 m behind `arrive` as a fallback). Builds clean; no new patches; nothing seen in game.
- 2026-10-13, the open arena and the killing river (the hall agent): the walls' `col_box_sky_*` colliders are destroyed
  on every peer as the walls are built (the asset keeps them). `Arena/Hall/HallLake.cs` checks every loaded character
  four times a second on every peer; under the river (y -8 to -408, within 1,200 m) the owner deals 10,000,000 damage
  (Drowning, not blockable or dodgeable), drops off for creatures, the centre message for the local player, a splash on
  every client. `HallLakeGrave` moves a river death's grave to `gate_inside` (a `Player.CreateTombStone` prefix marks
  it, a `TombStone.Awake` prefix moves the new stone and its ZDO before the stone records its spawn point).
  `Trial/ArenaDeath` spares skills only inside `ArenaHall.Contains` (y over -5), so a river death loses them. Builds
  clean; offline patch check passes; nothing seen in game.
- 2026-10-13, the arena moved far out over the ocean (the user: "the only way it's loaded is if players teleport to it";
  the hall agent moves `ArenaHall.Origin` and builds the hall only where someone is): the core goes to the new origin
  (the server makes its ZDO without the zone loaded); its owner is a player at the arena, never the server; an owner
  with no hall built here does nothing (it never ends the trial for that); the server's `ArenaOrphanWatch` ends a trial
  nobody has had in their area for 10 s (the last player jumped or logged out, a restart) and removes its creatures by
  their ZDOs; `ArenaLegacy` removes an older world's core and arena creatures at (0, 5000, 0) once a session;
  `ArenaEnd.Close` writes an end from whoever holds the core; `elite arena` no longer asks for a built hall (the jump
  builds it). A creature the hall's river drowns (a drowning hit, or a death below the river's surface, 8 m under the
  floor) scores nothing and is no kill, but is gone from its wave. Builds clean; offline patch check passes; nothing
  seen in game.
- 2026-10-13, loaded on demand and moved (the hall agent): `ArenaHall.Origin` is (-10304, 5000, 0) (a zone's centre), over the far western
  ocean, inside the game's edge for everything solid (its push and kill act only below y 30 and -10; checked in the
  decompiled `Player.EdgeOfWorldKill`, `Ship`, `EnvMan`'s edge wind, `WorldGenerator.GetBaseHeight`). `HallHost` builds
  the arena only when wanted: `ArenaHall.Ensure()` from `ArenaTravel.Enter` before the jump, or the local player in
  the area (`ArenaHall.Near`); a dedicated server builds colliders while any peer is in the area; each removes it a
  minute after. Helgrind is open whenever `Arena` is on. Builds clean; offline patch check passes; nothing seen in game.
  Old graves at (0, 5000, 0) are moved once by the server to the new gate (`HallLegacyGraves`).
- 2026-10-13, souls, Hel's Anvil and the boss cache: the score is gone (key, HUD, messages, rewards); `ArenaSouls`
  weighs each kill (2 x tier x (stars + 1) x 1.5 per mutation; a boss 50 x tier x (stars + 1) x `AspectLoot.Factor`) on
  its owner and the core's owner adds it to `ecr_arena_souls`; `SoulWallets` keeps each player's share; the HUD and the
  end message show the player's own; coins are unspent souls / 2 (per player, `RewardPlan.CoinsFor`). `HelAnvil`
  (Hoverable, Interactable, for the hall to add to each anvil), `AnvilText` (the hover, rebuilt only on change),
  `AnvilUse` and `AnvilOwner` (cost 10 x tier x 1.5^repairs, the owner's check and answer, the client's mend of every
  worn item, the forge's repair effect, the flare). `BossCache` (60%, 1-3 meads per player of the boss's tier on the
  rune circle, `ArenaNet.Flare`). Breaks 20 s / 25 s. Mead prefabs checked against the game's item reference:
  `MeadHealthMinor`, `MeadHealthMedium`, `MeadHealthMajor`, `MeadHealthLingering`; looked up in `ObjectDB` at runtime.
  Builds clean; offline patch check passes; nothing seen in game.
- 2026-10-13, ready with F: `ArenaReady` (the owner's ready set in `ecr_arena_ready`, the toggle, "everyone alive in the
  arena" checked on each vote and each step of a rest, the messages) and `ArenaReadyKey` (F on clients, on the core's
  object; ValheimModLibs' Hotkeys gained `Hotkey.Pressed(KeyCode)` for a fixed key, with the game's `TakeInput` for menus);
  the HUD line's ready part; the set emptied as every wave and trial begins. Builds clean; offline patch check passes;
  nothing seen in game.
- 2026-10-13, harder sooner: `ArenaPower.StartBiome` (the best `GearTier.Full` among players alive in the arena as the
  trial begins) into `ecr_arena_start_biome`; every wave sum takes it (`ArenaTable.BiomeIndex(start, wave)`,
  `ArenaState.TierOf`). The raids' gear check split Bloodgold and the Deep North sets into a tier 7 of their own
  (`GearItems`); `GearTier.Of` still clamps to 6 for the raids, `GearTier.Full` reads 7. Five waves a biome, 6/8/10/12,
  +1 star from the 3rd, mutations 15% +4%, the boss cycle from the first biome's boss, idols, runes and essence by the
  biome fought, skill XP x cleared / 10; the event bar names the biome. Builds clean; offline patch check passes;
  nothing seen in game.
- 2026-10-13, calling the next wave: `ArenaTrial.CanCallNext` and `RequestNextWave` (the hall's `HelHover` shows the
  line and calls it), `ArenaCalls` (the client's ask, `ecr_arena_call_next` to the core's owner, the owner's checks),
  `ArenaWaves.Call` (the next wave planned on top, the queue kept), `ArenaClears` (each wave fighting cleared by its own
  creatures, `ecr_arena_done` for waves cleared above one still fighting, the Swept mark moved up, the break after the
  last); a boss adopted during it joins the boss wave still fighting. Builds clean; offline patch check passes; nothing
  seen in game.
- 2026-10-13, elites: `ArenaCreatures.Kind.Elite` (`BodySize.IsLarge` or 2.5 x the biome's middle health), measured
  with read-only `/eval` in the running game for the list above; `ArenaTable.EliteCap`; `ArenaWaves.Pick` draws an
  elite only while the wave has room and its archway has none. Builds clean; offline patch check passes; nothing seen in
  game.
- 2026-10-14, stragglers and the reward shown (a player's report: a slow, pale first-wave Ghost hunted for a long walk;
  the reward hard to make out): `ArenaRecall` (the owner starts the recall at a quarter left, rounded, at least 1, and
  ends the waves fighting a minute later; `ArenaTable.StragglerShare`, `RecallSeconds`, `Stragglers`),
  `ecr_arena_recall` and `ecr_arena_size` on the core, `StragglerMark` (each `ArenaFighter`'s tick on every client:
  flare, cold light, the game's event pin), the event bar's "recalled in 0:45", `ecr_arena_cleared_first`;
  `RewardGrant` shows each item at the top left and the reward in chat, `ArenaWords.ToChat` the end too. Builds clean;
  nothing seen in game.
- 2026-10-14, current rewards and gear kept: `RewardPreview` ("Current rewards: ..." on Hel's hover, the end's plan for
  this player, cached on the trial, waves cleared, bosses beaten and souls; `RewardText.Items`), `ArenaDeath.KeepGear`
  (a `Player.CreateTombStone` prefix skipping the tombstone during an arena death; PackPanel's finalizer still returns
  its kept items). Builds clean; nothing seen in game.
