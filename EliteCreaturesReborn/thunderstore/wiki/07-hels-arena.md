# Hel's Arena

**Subject to change without warning.** The arena works and is tested, but its final shape is still being decided:
waves, numbers, rewards and the hall may change from one version to the next.

Off by default. Turn it on with `12 - Arena` / `Arena = true` in `BepInEx/config/gglasgow.elitecreaturesreborn.cfg`;
on a server the server's value holds for everyone. ECR is needed on the server and every client.

## Getting there

Build **Helgrind**, Hel's gate, from the hammer's Misc tab near a **Black Forge**:

| Material | Amount |
| --- | --- |
| Sacrificial Blood (Kall Fimbulbringer) | 1 |
| Frostcore | 2 |
| Bloodgold | 10 |
| Memorial Coal | 5 |

Walk through it to enter Hel's hall, high above the world. Every Helgrind leads to the same hall. Items the game
won't let through a portal (ores and the like) can't go through Helgrind either. To leave, walk into the swirl at
the end of the hall's gate passage: you come back out in front of the gate you used. Admins can also type
`elite arena` to go in and out.

## Hel's Trial

Hel sits on her throne at the far end of the hall. Hover her and press **E** to begin the trial: every player in the
hall is in it, and anyone who comes in later joins. After a 10 second countdown the doors close and the dead come out
of the four archways: a portal opens in each archway that has creatures to send, and they step out one at a time, about
a second apart, then run for you. The portal closes after its last one.

- **Waves** grow every time: waves 1-10 are Meadows creatures, 11-20 Black Forest, then the Swamp, Mountains, Plains,
  Mistlands, Ashlands and, from wave 71, the Deep North, which stays.
- **Each wave** brings 4 creatures in the first of a biome and one more each wave after, a quarter more for each
  extra player. At most 20 are alive at once.
- **A wave is a round:** the next one comes only when every creature of it is dead, after a short break - 15 seconds,
  20 after a boss - to catch your breath ("Wave 7 cleared"). A wave not cleared within 3 minutes counts as cleared: its
  last creatures vanish and score nothing.
- **Stars and mutations** rise with the waves: +1 star from a biome's 4th wave, +2 from its 8th, more after wave 80.
  No Gilded in the arena.
- **Every 10th wave** is a boss on the rune circle: Eikthyr, the Elder, Bonemass, Moder, Yagluth, the Queen, Fader,
  Kall Fimbulbringer, then round again with more stars. Arena boss kills don't count as world progress.
- **Score:** each kill scores more for its stars and biome; a boss scores most. The wave, creatures left and score
  show under the minimap; in a break, the seconds to the next wave.
- **The trial ends** when no player of it is alive in the hall.
- **Dying in the hall costs no skill levels:** "Hel spares your skills." That holds for GrindstoneSkills' skill loss
  too, and in a world that resets skills on death. The rest is the game's own death: your tombstone stays in the hall;
  go back in to fetch it.
- **Rewards**, when the trial ends, for every player of it, dead or alive (be online at the end):
  - a **Forge of Potential idol** for each boss beaten, at its tier: Eikthyr Wooden, the Elder Bronze, Bonemass Iron,
    Moder Silver, Yagluth Black Metal, the Queen Black Marble, Fader Flametal, Kall and every boss after Bloodgold;
    Battle and Protection Idols in turn;
  - **coins**: your score / 10;
  - **skill XP**: every skill you raised in the trial gets that much again times the waves cleared / 20 (wave 10:
    +50%, wave 20: +100%, at most +200%);
  - with **EliteCrafting**: runes (1-3 a wave, more for each boss, later runes in later biomes) and essence (a trophy's
    worth a wave, a boss trophy's for each boss).

  The arena creatures themselves drop nothing. Items that don't fit in your inventory fall at your feet.

`elite arena stop` (admin) ends a trial.
