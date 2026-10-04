# Elite Creatures Reborn - specification: Death recap

One feature of the mod, specified on its own. Asked for by the user on 2026-10-04: a way to see how you died,
without ever holding up the respawn, watched when you want from a key or a command, and with a video as the point
of it. Decided with the user the same day: kept for the session only (until the game closes), no hints, playback
at 0.25×, 0.5×, 1× and 2× with play, pause, a timeline, a thumbnail on hover and a timestamp. It must look like
Valheim's own windows without PackPanel and take PackPanel's reskin with it.

**Status: built, not tested in game.**

---

# 1. What the player sees

- **Nothing changes about dying.** The game's own death, "You died" and the ten-second respawn are untouched.
  About two seconds after the death, one line at the top left names the killer and the key:
  `Killed by Mad Greydwarf 2★. F10: death recap` (`Died: Fall. F10: death recap` with no dealer). It can be off.
- **The recap window** opens on `F10` (a setting), `/deaths` in chat or `deaths` in the F5 console, any time in a
  world, alive or dead. Esc, the Close button, the key again or a click outside closes it.
- **Left: the deaths**, newest first, up to five (a setting, 1 to 10): the picture at the moment of death, the
  killer, the in-game day, the clock time and the clip's length. Click one to watch it.
- **Right, top: the killer and a summary.** `Killed by Mad Greydwarf 2★ (ranged)` and
  `Day 96, 14:32 · 312 damage in 8.4 s: slash 62%, frost 30%, fall 8%`. Facts only: no advice, no hints.
- **The video**: the last 15 seconds (a setting, 5 to 30) of the player's own screen before the death, HUD
  included, and two seconds after it. It plays from the start as soon as a death is selected.
- **The timeline** under it: drag or click to move; a small orange mark at every hit and a red one at the death.
  Hovering it shows a small picture of that moment and its time above the pointer.
- **Controls**: Play/Pause, the speeds 0.25×, 0.5×, 1×, 2× (the chosen one in orange), and `0:07.4 / 0:17.0`.
  Space plays or pauses; the left and right arrows step one frame.
- **The hits**, under the controls, one row each: clip time, who (or the cause), damage, damage by type
  (coloured: fire orange, frost blue, lightning yellow, poison green, spirit pale), and the health left. The latest
  hit by the playback time is highlighted and kept in view while playing; clicking a hit plays from a second
  before it.

# 2. What is recorded

- **Hits**: every hit that lands on this machine's own player (`Character.ApplyDamage` with health lost): creature
  and player hits, arrows and other projectiles (`ranged`), burning and poison ticks, falls, drowning, the game's
  other causes and the mod's own storms. The damage is what landed, after armour, resistances and the game's
  damage rates. Burning and poison ticks carry no attacker in the game, so the last creature whose hit carried fire,
  spirit or poison is remembered and named on them.
- **Who**: read the moment the hit lands, since the creature may be gone when the recap is watched: the creature's
  name with its mutation and aspect words and its stars (`Bountiful Enraged Eikthyr 3★`, `Mad Greydwarf 2★`), or a
  player's name. The stars are ECR's own; a creature ECR has not resolved shows the game's level.
- **The killing blow** is the last hit logged; with none (a console kill), the game's own last hit says how.
- **Video**: the finished screen, captured at the end of a frame at 15 frames per second (a setting, 5 to 30),
  shrunk on the GPU to 360 pixels high (a setting, 180 to 720; the width follows the screen), read back without
  stalling and encoded to JPEG on a background thread. A frame is skipped rather than queued when the GPU or the
  encoder falls behind. Only the newest seconds are kept: about 10 MB at the defaults, the same per kept death.
- **Not recorded**: while the recap window is open (it would record itself), while the game is paused, on a
  machine without graphics, or with `Record deaths` off (the hits are still listed; the video box says why there
  is none).

# 3. Multiplayer

Nothing is networked, and nothing needs to be. A hit on a player is applied on that player's own client, which owns
the body, so every client logs its own player's hits on a dedicated server exactly as in single player; the
creature's mutations and stars are read from its synced ZDO on that client. The video is the player's own screen.
The dedicated server records nothing (no local player, no graphics). Players without the mod are unaffected.

# 4. Look: the game's own window

The window is a copy of the game's compendium window (the "Texts" dialog): its wood frame, title, list, inset areas,
Close button and fonts. The timeline is a copy of the stack split slider (its tick sound removed), the buttons copies
of the compendium's Close button. It has a canvas of its own beside the game's screens, sorted just above the
inventory and scaled by the game's GUI scale. PackPanel's Timber theme reskins every game wood panel it sees, so it
reskins this one with nothing shared between the mods; without PackPanel it is the game's look. While it is open the
game treats it like its own windows: free cursor, no attacks or hotbar, no camera turning or zoom, Esc closes it
(the `WindowInput` library). Walking stays possible, as with the inventory.

# 5. Configuration

`10 - Death Recap (per player)` in the .cfg, all client side and never locked (they change only what this player
records and sees): `Record deaths` (on), `Recap key` (F10), `Death notice` (on), `Deaths kept` (5),
`Seconds before death` (15), `Video frames per second` (15), `Video height` (360).

Judgement calls, open to argument: F10 because EarthWright already uses F6 to F9; two seconds after the death so
the fall is in the clip; the frame cap of three GPU readbacks and six queued encodes; JPEG quality 75.

# 6. Open decisions

- Gamepad: the window opens from the command on a gamepad, but has no gamepad controls of its own yet.
- A server switch to turn the window off (for groups that want deaths unexplained) was offered, not asked for.

# 7. Code map

`Recap/`: `RecapSettings`, `HitLog` (+ `HitRecord`, `HitDescribe`), `FrameGrabber` (+ `ScreenGrab`,
`FrameEncoder`, `FrameRing`, `JpegCodec`), `RecapStore` (+ `DeathRecap`, `RecapPictures`, `RecapNotice`),
`RecapHost` (frame loop, key, window registration). `Recap/Window/`: `RecapWindow` (open/close), `WindowBuilder`
(+ `WindowParts`: the compendium copy), `PaneBuilder` (header, video, timeline, preview, controls), `WindowView`
(drives the parts), `Playback`, `VideoScreen`, `Timeline`, `TimelineHover`, `ControlRow`, `ElementList`,
`DeathList`, `HitList`, `RecapText`, `UiParts`. Patches: `RecapHitPatch` (`Character.ApplyDamage` prefix and
postfix, `Character.RPC_Damage` prefix), `RecapDeathPatch` (`Player.OnDeath` prefix). Command: `DeathsCommand`.

# 8. Build checklist

- [~] Hits logged with dealer, cause, damage by type and health left - built, not seen in game
- [~] Burning and poison ticks named after the creature that caused them - built, not seen in game
- [~] Video recorded at the end of each frame, shrunk, read back and encoded off the main thread - built, not seen
  in game; the picture's orientation and colours on Direct3D 11 are unverified
- [~] Recap made two seconds after death, notice line shown, respawn untouched - built, not seen in game
- [~] Window from the compendium copy: deaths list with thumbnails, header, video, timeline marks, hover preview,
  controls, hits list - built, not seen in game
- [~] PackPanel Timber theme on the window, vanilla look without PackPanel - built, not seen in game
- [~] Free cursor, no attacks, Esc closes (`WindowInput`) - built, not seen in game
- [ ] Verified on a dedicated server

| Date | Change | Commit |
| --- | --- | --- |
| 2026-10-04 | Feature specified with the user and built; `WindowInput` library added for the window's input | uncommitted |
