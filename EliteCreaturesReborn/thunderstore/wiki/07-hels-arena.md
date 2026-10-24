# Hel's Arena

**Subject to change without warning.** The arena works and is tested, but its final shape is still being decided:
waves, numbers, rewards and the hall may change from one version to the next.

Off by default. Turn it on with `12 - Arena` / `Arena = true` in `BepInEx/config/gglasgow.elitecreaturesreborn.cfg`;
on a server the server's value holds for everyone. ECR is needed on the server and every client.

## Getting there

Build **Helgrind**, Hel's gate, from the hammer's Misc tab near a **workbench**:

| Material | Amount |
| --- | --- |
| Wood | 20 |
| Stone | 20 |
| Eikthyr trophy | 1 |

**Choose the trial** on the gate: its hover shows "[E] Trial: Swamp". **E** steps to the next trial, **Shift + E**
back. The Meadows is always open; each later biome opens once its boss has been killed in the world (Black Forest after
Eikthyr, Swamp after the Elder, and so on); then **Infinite**. Everyone sees the same choice on a gate.

Walk through it to enter Hel's hall, high above the far ocean at the world's western edge, where nobody gets but
through Helgrind. Every Helgrind leads to the same hall. Items the game
won't let through a portal (ores and the like) can't go through Helgrind either. To leave, walk into the swirl at
the end of the hall's gate passage: you come back out in front of the gate you used. Admins can also type
`elite arena` to go in and out.

## Hel's Trial

Hel sits on her throne at the far end of the hall. Hover her and press **E** to begin the trial you chose at the gate
("[E] Begin the trial: Swamp"): every player in the hall is in it, and anyone who comes in later joins. After a 10 second countdown the doors close and the dead come out
of the eight archways round the arena's wall, about 60 m from the middle: a portal opens in each archway that has
creatures to send, and they step out one at a time, about a second apart, then run for you. The portal closes after its
last one.

- **A biome's trial** is five waves: four of its creatures, then its boss. Kill the boss and you win the trial ("You
  won Hel's trial of the Swamp") and get its reward.
- **Infinite** is the highest open biome, round after round of five waves, each round a **level** harder (level 1 to
  5, then level 5 for good): one more star and a quarter more creatures each level. The bar under the minimap shows
  "Meadows, level 3".
- **Each wave** brings 6, 8, 10 and 12 creatures in a biome's four waves, a quarter more for each extra player, spread
  round all eight archways. At most 24 are alive at once.
- **Elites** - big or very tough creatures such as trolls, bears, lox, golems, Abominations, Gjall and Morgen - come
  only late in a biome: none in its first two waves, at most 1 in the 3rd and 2 in the 4th (3 to 6 from Infinite's
  level 2), one more for every two extra players, never two out of one archway.
- **A wave is a round:** the next one comes only when every creature of it is dead, after a short break - 20 seconds,
  25 after a boss - to catch your breath and reach an anvil ("Wave 7 cleared"). A wave not cleared within 5 minutes
  counts as cleared: its last creatures vanish and are worth nothing.
- **Stragglers:** once a wave's creatures are all out and only a quarter of them are left, Hel marks the last ones -
  they glow and show on your map - and recalls them a minute later (no boss wave). Kill them in that minute for their
  souls; recalled, they are worth nothing.
- **Stars and mutations** rise with the waves: +1 star from a round's 3rd wave, one more for each Infinite level; a 15% mutation chance in wave 1, 4% more each wave, up to 60%. No Gilded in the arena.
- **Every 5th wave** is the biome's boss on the rune circle: Eikthyr, the Elder, Bonemass, Moder, Yagluth, the Queen,
  Fader or Kall Fimbulbringer; in Infinite the same boss each round, with more stars. Arena boss kills don't count as world progress.
- **Souls:** every kill is worth souls, and every player of the trial gets them all: 2 x the biome's tier (1 Meadows ...
  8 Deep North) x (stars + 1), x 1.5 for each mutation; a boss 50 x the tier x (stars + 1) x what its aspects pay. Your
  souls, the biome, the wave and the creatures left show under the minimap; in a break, the seconds to the next wave.
- **Ready:** in a break (or the countdown), press **F** when you're ready, again to take it back; once every player
  alive in the arena is ready, the next wave comes at once.
- **Call the next wave:** during a wave, once all its creatures are out, press **E** at Hel to bring the next wave in
  on top of it - but not past a living boss. The break comes after the last wave is cleared.
- **Hel's Anvil:** two anvils stand beside Hel's throne. In the countdown and the breaks, press E at one to mend all
  your gear for souls: 10 x the biome's tier, half as much again for each repair you made this trial. They are cold
  while a wave is on.
- **Boss cache:** a fallen boss leaves healing meads 60% of the time - 1 to 3 for each player, minor to lingering by
  the boss's biome - on the rune circle.
- **The trial ends** when no player of it is alive in the hall.
- **Dying in the hall costs nothing:** no skill levels and no tombstone - you respawn with all your gear. "Hel spares
  your skills and your gear." That holds for GrindstoneSkills' skill loss too, and in a world that resets skills on
  death.
- **The arena is open above its walls, and Hel's river kills:** anyone who escapes over the wall into the black lake
  round it dies at once, with the game's usual skill loss; the tombstone appears just inside the arena's gate.
- **Rewards**, when the trial ends, for every player of it, dead or alive (be online at the end):
  - a **Forge of Potential idol** for each boss beaten, at its biome's tier: Meadows Wooden, Black Forest Bronze, Swamp
    Iron, Mountains Silver, Plains Black Metal, Mistlands Black Marble, Ashlands Flametal, the Deep North (and every
    boss after Kall) Bloodgold; Battle and Protection Idols in turn;
  - **coins**: half your unspent souls;
  - **skill XP**: every skill you raised in the trial gets that much again times the waves cleared / 10 (5 waves:
    +50%, 10 waves: +100%, at most +200%);
  - with **EliteCrafting**: runes (1-3 a wave by the wave's biome, more for each boss, later runes in later biomes) and
    essence (a trophy's worth a wave, a boss trophy's for each boss).

  The arena creatures themselves drop nothing, but for a boss's meads. Items that don't fit in your inventory fall at your feet.
  Each item shows at the top left as it arrives, and the whole reward ("Hel rewards you: ...") is written in the chat.
  During a trial, look at Hel to see your **Current rewards**.

`elite arena stop` (admin) ends a trial.
