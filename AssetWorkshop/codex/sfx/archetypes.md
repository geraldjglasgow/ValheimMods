# Sound archetypes

Every kind of sound the game makes, measured. An archetype is a role (a creature's idle, a sword's hit, a fire loop),
found from how the game uses each sound prefab (the component and field that play it: `m_idleSound`, an item's
`m_hitEffect`, a piece's `m_placeEffect`, a FootStep entry for stone at a run), not from names. A sound that plays
several roles counts in each (`sfx_metal_blocked` is both the metal block and the metal piece breaking). Keys are the
`sfx.*` categories of `data/sfx.json`; `measure/sfx_tables.py archetypes` prints these tables again after a game update.

How to read the tables:

- **sounds/clips**: distinct sounds in the archetype and their clips. Statistics are over clips, as median (p25..p75).
- **played LUFS**: the loudest 400 ms of a clip plus the gain its ZSFX volume and AudioSource volume give it, before
  distance and the mixer. This is the game's mix: the column to match.
- **attack**: onset to within 3 dB of the peak. **tail**: peak to 40 dB under it (the audible length).
- **centroid** and **low/mid/high** (energy under 300 Hz / 300 to 3000 / over 3000, shares): brightness.
- **flatness**: 0.56 is noise, 0.1 to 0.3 a voice or a ringing tone, near 0 a pure tone.
- **variations, pitch, volume, reach**: per sound, medians: clips per ZSFX, `m_minPitch`-`m_maxPitch`,
  `m_minVol`-`m_maxVol`, AudioSource max distance; roll-off and mixer group are the most common.

Texture words come from the contact sheets (spectrogram over envelope per clip, `codex/out/sfx/sheets/<key>.png`,
made by `measure/sfx_sheets.py`), looked at, not guessed.

## Creature voices and bodies

| archetype | sounds/clips | length s | played LUFS | attack s | tail s | centroid Hz | flatness | low/mid/high | variations | pitch | volume | reach m | roll-off | group |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| creature.idle | 96/642 | 2.00 (1.19..3.07) | -20.3 (-25.8..-17.0) | 0.22 (0.10..0.46) | 1.20 (0.72..2.09) | 860 (336..1655) | 0.43 (0.35..0.49) | 0.21/0.53/0.03 | 6 | 0.95-1.07 | 0.70-0.80 | 80 (40..100) | custom | Effects/SFX |
| creature.alert | 78/389 | 2.24 (1.32..3.08) | -15.7 (-19.1..-13.0) | 0.17 (0.08..0.33) | 1.35 (0.79..1.93) | 1078 (488..1689) | 0.43 (0.35..0.48) | 0.22/0.62/0.04 | 4 | 0.95-1.07 | 0.80-0.80 | 60 (40..100) | custom | Effects/SFX |
| creature.attack | 259/891 | 1.95 (1.35..2.99) | -13.3 (-17.3..-10.8) | 0.17 (0.09..0.38) | 1.19 (0.82..1.89) | 590 (303..1232) | 0.48 (0.43..0.53) | 0.68/0.24/0.04 | 3 | 0.95-1.00 | 1.00-1.00 | 80 (30..100) | custom | Effects/SFX |
| creature.attack_hit | 45/171 | 1.14 (0.77..1.92) | -18.2 (-20.3..-13.9) | 0.05 (0.01..0.12) | 0.69 (0.49..1.43) | 670 (327..1266) | 0.52 (0.47..0.55) | 0.76/0.18/0.06 | 4 | 0.90-1.00 | 0.50-0.55 | 50 (35..100) | custom | Effects/SFX |
| creature.hurt | 54/276 | 1.09 (0.62..2.00) | -17.2 (-21.3..-14.8) | 0.06 (0.03..0.10) | 0.71 (0.47..1.11) | 1112 (697..1994) | 0.40 (0.28..0.47) | 0.22/0.51/0.07 | 5 | 0.97-1.00 | 0.50-0.60 | 40 (35..100) | custom | Effects/SFX |
| creature.death | 104/372 | 1.76 (1.14..3.09) | -13.0 (-16.3..-10.5) | 0.07 (0.03..0.12) | 1.34 (0.89..1.84) | 757 (415..1238) | 0.44 (0.30..0.51) | 0.54/0.34/0.03 | 3 | 0.95-1.00 | 1.00-1.00 | 50 (33..100) | custom | Effects/SFX |
| creature.footstep | 76/670 | 0.70 (0.56..0.99) | -28.3 (-33.9..-19.8) | 0.03 (0.01..0.05) | 0.42 (0.29..0.57) | 379 (120..1114) | 0.53 (0.51..0.56) | 0.88/0.09/0.03 | 9 | 0.88-1.00 | 0.65-0.80 | 40 (30..50) | log | Effects/SFX |
| creature.move | 22/137 | 1.25 (0.70..1.75) | -24.1 (-28.6..-16.7) | 0.10 (0.04..0.17) | 0.71 (0.43..1.05) | 257 (169..422) | 0.53 (0.50..0.56) | 0.89/0.09/0.02 | 8 | 0.90-1.00 | 0.90-1.00 | 52 (40..80) | custom | Effects/SFX |
| creature.anim | 27/99 | 2.27 (0.75..4.54) | -18.7 (-26.0..-12.7) | 0.14 (0.05..0.22) | 1.30 (0.44..1.69) | 452 (184..998) | 0.51 (0.43..0.54) | 0.83/0.12/0.03 | 3 | 0.90-1.00 | 1.00-1.00 | 30 (30..55) | log | Effects/SFX |
| creature.spawn | 7/16 | 1.58 (1.18..3.82) | -11.0 (-13.0..-10.9) | 0.07 (0.03..0.25) | 1.33 (1.07..2.11) | 355 (321..481) | 0.53 (0.51..0.55) | 0.82/0.15/0.03 | 3 | 0.90-1.00 | 1.00-1.00 | 50 (40..75) | log | Effects/SFX |
| creature.tame | 7/42 | 0.93 (0.80..1.86) | -21.0 (-24.2..-16.1) | 0.12 (0.02..0.34) | 0.78 (0.60..1.13) | 764 (322..857) | 0.42 (0.19..0.47) | 0.54/0.41/0.01 | 3 | 0.90-1.10 | 0.70-0.70 | 15 (15..15) | log | Effects/SFX |
| creature.breed | 12/33 | 2.04 (0.84..2.54) | -16.8 (-18.1..-13.2) | 0.13 (0.04..0.45) | 1.13 (0.70..1.73) | 709 (256..1294) | 0.44 (0.40..0.51) | 0.58/0.31/0.02 | 2 | 0.90-1.10 | 1.00-1.00 | 50 (28..50) | log | Effects/SFX |
| creature.corpse | 3/12 | 8.15 (7.41..8.15) | -11.6 (-13.6..-10.6) | 0.09 (0.07..0.10) | 3.66 (3.32..4.41) | 194 (182..212) | 0.51 (0.51..0.52) | 0.93/0.06/0.01 | 4 | 0.90-1.10 | 0.80-0.80 | 30 (25..45) | log | Effects/SFX |
| creature.jump | 3/8 | 1.56 (1.34..1.62) | -15.4 (-16.0..-14.9) | 0.12 (0.03..0.21) | 0.96 (0.76..1.13) | 117 (70..246) | 0.56 (0.55..0.57) | 0.93/0.07/0.00 | 3 | 0.80-1.00 | 0.80-0.80 | 40 (35..70) | custom | Effects/SFX |

Roles come from `BaseAI.m_idleSound` and `m_alertedEffects`, `Character.m_hitEffects` (hurt) and `m_deathEffects`, the
attack items' `m_startEffect`/`m_triggerEffect`/`m_trailStartEffect` (attack) and `m_hitEffect` (attack_hit),
`FootStep` entries, animation events (`anim`, `move`), `Tameable`, `Procreation`/`EggHatch`, `SpawnArea` and
`Ragdoll.m_removeEffect` (corpse). Per-creature detail, sizes and variants: [creatures.md](creatures.md).

- **Idle**: a murmur, growl or chatter that swells in (0.1 to 0.46 s) and fades; two to five syllables that run into
  each other (the Greydwarf's idle never drops to silence inside the clip); harmonics curve up and down within each
  syllable; the quietest vocal (-20.3 LUFS played, 4.5 dB under an alert). Six variations: it repeats most.
  References: `sfx_draugr_idle`, `sfx_dragon_idle`, `sfx_seeker_idle`, `sfx_ghost_idle`, `sfx_greydwarf_idle`.
- **Alert**: the loudest held call (-15.7 LUFS), a flat-topped envelope over 1.3 to 3 s, often two calls in one clip
  (`sfx_skeleton_big_alerted`), harmonics and formant bands clearly visible, the brightest vocal (centroid 1.1 kHz,
  62 % mid). References: `sfx_wraith_alerted`, `sfx_boar_alerted`, `sfx_skeleton_big_alerted`, `sfx_greydwarf_alerted`.
- **Attack** (own sounds only; shared weapon swings stay in the weapon rows): mostly a body-and-effort sound, darker
  than the voice (68 % under 300 Hz: grunt, whoosh and thump together), loud (-13 LUFS), 1.4 to 3 s because many carry
  their swing and a trailing breath. Three variations, pitch barely randomised (0.95 to 1.0). References:
  `sfx_lox_attack_bite`, `sfx_morgen_attack`, `sfx_charred_twitcher_attack`, `sfx_asksvin_pounce`.
- **Attack hit**: a short noisy impact on the target (flatness 0.52, attack 50 ms), played at half volume (0.5 to 0.55)
  so the victim's own hurt sound leads. Many creatures share one: `sfx_greydwarf_attack_hit` is the same clips as the
  wolf's, neck's, leech's and serpent's (0.27 s, 4 kHz slap).
- **Hurt**: a sharp yelp or grunt, attack 30 to 100 ms, falling in pitch (harmonics slant down), 0.6 to 2 s; five
  variations, almost no pitch spread (0.97 to 1.0) so the variations carry the difference; played at half volume
  (0.5 to 0.6) because the weapon's hit plays with it. References: `sfx_boar_hit`, `sfx_wolf_hit`, `sfx_troll_hit`,
  `sfx_goblin_hit`.
- **Death**: loud (-13 LUFS), fast in (70 ms), a long fall of pitch and level with a breathy or gurgling tail, often a
  second event (the body's collapse, a last gasp); darker than hurt (54 % under 300 Hz). Three variations.
  References: `sfx_draugr_death`, `sfx_fenring_death`, `sfx_ulv_death`, `sfx_greydwarf_deepnorth_death`.
- **Footstep and move**: dark noisy thumps (88 % under 300 Hz) 0.4 to 1 s, quiet (-28 LUFS), eight or nine
  variations; big creatures get their own (`sfx_troll_footstep`: 0.8 s at pitch 0.6 to 0.85, 50 m reach), small ones
  borrow the player's surface steps (`fx_footstep_run`, `fx_footstep_mud_run`).

## Weapons and combat

| archetype | sounds/clips | length s | played LUFS | attack s | tail s | centroid Hz | flatness | low/mid/high | variations | pitch | volume | reach m | roll-off | group |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| weapon.sword.swing | 8/36 | 1.04 (0.64..1.33) | -24.0 (-25.9..-16.4) | 0.08 (0.05..0.15) | 0.50 (0.38..0.76) | 265 (134..1171) | 0.41 (0.30..0.52) | 0.91/0.06/0.02 | 5 | 0.90-1.00 | 0.82-0.90 | 22 (20..31) | custom | Effects/SFX |
| weapon.sword.hit | 8/41 | 1.25 (1.00..1.50) | -15.8 (-20.3..-14.5) | 0.03 (0.00..0.09) | 0.65 (0.59..0.85) | 512 (256..1837) | 0.52 (0.39..0.53) | 0.84/0.10/0.05 | 5 | 0.80-1.10 | 0.88-1.00 | 40 (29..42) | custom | Effects/SFX |
| weapon.axe.swing | 2/8 | 0.84 (0.67..0.94) | -27.9 (-29.5..-26.2) | 0.08 (0.04..0.13) | 0.46 (0.33..0.53) | 115 (69..161) | 0.52 (0.51..0.56) | 0.96/0.03/0.01 | 4 | 1.05-1.20 | 0.60-0.70 | 20 (20..20) | linear | Effects/SFX |
| weapon.axe.hit | 4/21 | 0.60 (0.37..1.31) | -22.6 (-23.9..-20.7) | 0.03 (0.00..0.04) | 0.49 (0.31..0.59) | 692 (534..790) | 0.43 (0.39..0.50) | 0.86/0.08/0.07 | 5 | 0.90-1.05 | 0.40-0.55 | 75 (42..100) | custom | Effects/SFX |
| weapon.battleaxe.hit | 7/36 | 1.12 (0.66..1.33) | -16.0 (-22.6..-15.4) | 0.04 (0.02..0.09) | 0.63 (0.49..0.88) | 638 (304..798) | 0.50 (0.43..0.53) | 0.86/0.08/0.07 | 5 | 0.80-1.10 | 0.75-0.60 | 40 (40..75) | custom | Effects/SFX |
| weapon.club.swing | 5/19 | 1.75 (1.06..2.27) | -18.6 (-26.8..-17.0) | 0.10 (0.06..0.34) | 0.77 (0.38..1.51) | 164 (126..183) | 0.53 (0.51..0.57) | 0.93/0.06/0.01 | 4 | 0.90-1.00 | 0.70-0.80 | 20 (20..40) | linear | Effects/SFX |
| weapon.club.hit | 8/29 | 1.25 (1.00..1.38) | -15.4 (-17.3..-13.0) | 0.07 (0.00..0.10) | 0.71 (0.62..0.91) | 479 (258..861) | 0.53 (0.50..0.54) | 0.84/0.12/0.04 | 4 | 0.80-1.15 | 0.95-0.95 | 45 (40..50) | custom | Effects/SFX |
| weapon.sledge.swing | 6/24 | 1.88 (1.64..2.12) | -15.5 (-18.2..-12.9) | 0.06 (0.00..0.09) | 1.34 (0.64..1.50) | 187 (156..315) | 0.52 (0.51..0.54) | 0.93/0.06/0.01 | 3 | 0.85-1.00 | 0.70-0.80 | 45 (40..50) | log | Effects/SFX |
| weapon.spear.swing | 2/7 | 0.64 (0.60..0.68) | -31.5 (-32.0..-31.0) | 0.09 (0.07..0.10) | 0.34 (0.27..0.36) | 184 (128..1170) | 0.52 (0.46..0.53) | 0.93/0.06/0.01 | 4 | 0.90-1.00 | 0.70-0.80 | 25 (22..28) | log | Effects/SFX |
| weapon.spear.hit | 7/35 | 1.12 (0.58..1.34) | -16.0 (-20.2..-15.4) | 0.03 (0.01..0.09) | 0.63 (0.34..0.88) | 512 (297..877) | 0.51 (0.47..0.53) | 0.85/0.09/0.05 | 5 | 0.80-1.10 | 1.00-1.00 | 40 (25..40) | custom | Effects/SFX |
| weapon.polearm.swing | 3/9 | 0.64 (0.58..0.67) | -28.1 (-28.1..-18.8) | 0.07 (0.07..0.08) | 0.38 (0.35..0.38) | 138 (117..184) | 0.53 (0.53..0.54) | 0.94/0.05/0.01 | 4 | 0.90-1.00 | 0.70-0.90 | 20 (15..25) | linear | Effects/SFX |
| weapon.knife.swing | 1/5 | 0.34 (0.31..0.36) | -39.1 (-39.1..-39.1) | 0.04 (0.04..0.04) | 0.14 (0.14..0.15) | 80 (78..180) | 0.53 (0.52..0.56) | 0.97/0.02/0.01 | 5 | 0.90-1.10 | 0.75-0.90 | 20 (20..20) | linear | Effects/SFX |
| weapon.unarmed.swing | 3/14 | 0.83 (0.69..1.01) | -27.0 (-27.6..-26.0) | 0.10 (0.07..0.31) | 0.37 (0.31..0.41) | 115 (96..503) | 0.42 (0.34..0.53) | 0.94/0.04/0.01 | 4 | 1.00-1.20 | 0.50-0.60 | 20 (20..20) | linear | Effects/SFX |
| weapon.unarmed.hit | 3/14 | 0.88 (0.45..1.51) | -21.4 (-21.7..-20.7) | 0.00 (0.00..0.01) | 0.36 (0.29..0.59) | 1813 (665..2381) | 0.28 (0.16..0.32) | 0.65/0.11/0.21 | 5 | 0.80-1.10 | 0.70-1.00 | 25 (22..25) | log | Effects/SFX |
| weapon.pickaxe.hit | 3/18 | 0.48 (0.41..0.59) | -17.1 (-18.1..-10.7) | 0.01 (0.00..0.03) | 0.31 (0.26..0.41) | 3735 (3394..4207) | 0.15 (0.10..0.24) | 0.07/0.20/0.63 | 7 | 0.90-1.10 | 0.70-0.90 | 60 (40..60) | custom | Effects/SFX |
| weapon.bow.draw | 1/3 | 2.38 (2.31..2.44) | -33.9 (-33.9..-33.9) | 0.04 (0.04..0.04) | 2.21 (2.09..2.23) | 1896 (1854..2107) | 0.56 (0.56..0.56) | 0.20/0.58/0.18 | 3 | 1.10-1.40 | 0.50-0.60 | 20 (20..20) | log | Effects/SFX |
| weapon.staff.swing | 15/71 | 2.23 (1.00..3.69) | -13.2 (-17.0..-10.8) | 0.20 (0.12..0.33) | 1.23 (0.65..2.27) | 367 (254..692) | 0.50 (0.44..0.54) | 0.86/0.11/0.03 | 4 | 0.90-1.10 | 1.15-1.35 | 60 (42..80) | custom | Effects/SFX |
| combat.block.wood | 2/7 | 0.73 (0.58..0.77) | -15.2 (-15.5..-14.9) | 0.00 (0.00..0.01) | 0.42 (0.39..0.43) | 333 (292..470) | 0.54 (0.32..0.55) | 0.84/0.13/0.03 | 4 | 0.72-0.90 | 1.10-1.25 | 25 (25..25) | linear | Effects/SFX |
| combat.block.metal | 5/17 | 0.90 (0.84..1.08) | -17.9 (-20.4..-15.0) | 0.00 (0.00..0.01) | 0.62 (0.40..0.65) | 1680 (235..2370) | 0.35 (0.12..0.41) | 0.53/0.12/0.15 | 3 | 0.80-1.00 | 0.80-1.00 | 25 (25..25) | linear | Effects/SFX |
| combat.perfect_block | 2/2 | 1.49 (1.43..1.56) | -10.2 (-11.8..-8.6) | 0.07 (0.04..0.09) | 1.20 (1.18..1.23) | 1332 (1111..1553) | 0.46 (0.46..0.46) | 0.62/0.17/0.21 | 1 | 0.90-1.05 | 1.00-1.00 | 25 (25..25) | linear | Effects/SFX |
| combat.crit | 3/8 | 4.24 (1.26..4.62) | -14.3 (-17.1..-12.2) | 0.07 (0.05..0.08) | 3.53 (1.10..4.03) | 279 (245..2398) | 0.48 (0.42..0.52) | 0.81/0.14/0.01 | 2 | 0.90-1.05 | 0.70-0.70 | 40 (35..70) | log | Effects/SFX |

The archetypes above include the elemental enchantments' impacts (`sfx_weapons_blood_impact`, `_lightning_`,
`_nature_`) that every enchanted weapon also plays. The defining sound of each weapon kind, by name:

| sound | clips | length s | played LUFS | attack s | tail s | centroid Hz | flatness | pitch | volume | reach m | group |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `sfx_sword_swing` | 4 | 0.64 | -25.6 | 0.10 | 0.43 | 918 | 0.46 | 0.8-1.0 | 0.3-0.4 | 5-20 linear | SFX |
| `sfx_sword_hit` | 5 | 1.67 | -22.0 | 0.00 | 0.60 | 2251 | 0.14 | 0.75-0.95 | 0.5 | 2-25 log | SFX |
| `sfx_knife_swing` | 5 | 0.34 | -39.1 | 0.04 | 0.14 | 80 | 0.53 | 0.9-1.1 | 0.75-0.9 | 5-20 linear | SFX |
| `sfx_axe_swing` | 4 | 0.66 | -31.2 | 0.04 | 0.32 | 68 | 0.56 | 1.1-1.2 | 0.4-0.5 | 5-20 linear | SFX |
| `sfx_axe_hit` | 5 | 0.47 | -24.3 | 0.03 | 0.35 | 790 | 0.39 | 0.95-1.0 | 0.4-0.45 | 5-100 custom | SFX |
| `sfx_battleaxe_swing_wosh` | 4 | 0.94 | -24.6 | 0.13 | 0.53 | 163 | 0.51 | 1.0-1.2 | 0.8-0.9 | 5-20 linear | SFX |
| `sfx_battleaxe_hit` | 5 | 0.47 | -23.8 | 0.03 | 0.35 | 790 | 0.39 | 0.9-1.0 | 0.4-0.5 | 5-100 custom | SFX |
| `sfx_club_swing` | 4 | 0.46 | -26.8 | 0.05 | 0.20 | 96 | 0.49 | 0.85-1.0 | 0.7-0.8 | 5-20 linear | SFX |
| `sfx_club_hit` | 1 | 0.74 | -25.0 | 0.01 | 0.21 | 1140 | 0.54 | 0.7-1.1 | 0.4 | 5-50 log | SFX |
| `sfx_sledge_swing` | 3 | 1.75 | -18.6 | 0.92 | 0.56 | 129 | 0.51 | 1.0-1.04 | 0.5 | 5-20 linear | SFX |
| `sfx_sledge_hit` | 3 | 1.89 | -11.5 | 0.02 | 1.43 | 128 | 0.53 | 0.85-1.0 | 0.8-0.9 | 5-50 log | SFX |
| `sfx_spear_poke` | 4 | 0.61 | -30.6 | 0.07 | 0.36 | 128 | 0.53 | 0.9-1.0 | 0.6 | 3-30 log | SFX |
| `sfx_spear_throw` | 3 | 0.69 | -32.4 | 0.11 | 0.24 | 1419 | 0.46 | 0.9-1.0 | 0.8-1.0 | 5-20 linear | SFX |
| `sfx_spear_hit` | 5 | 0.25 | -19.3 | 0.01 | 0.20 | 634 | 0.46 | 0.8-1.0 | 1.0-1.2 | 2-20 custom | SFX |
| `sfx_atgeir_attack` | 4 | 0.61 | -28.1 | 0.07 | 0.36 | 128 | 0.53 | 0.9-1.0 | 0.7-0.9 | 5-20 linear | SFX |
| `sfx_unarmed_swing` | 4 | 0.80 | -28.2 | 0.39 | 0.26 | 104 | 0.54 | 1.0-1.2 | 0.5-0.6 | 5-20 linear | SFX |
| `sfx_unarmed_hit` | 4 | 0.38 | -21.4 | 0.01 | 0.25 | 345 | 0.44 | 0.8-1.1 | 0.8-1.0 | 5-20 linear | SFX |
| `sfx_claw_swing` | 4 | 0.64 | -27.0 | 0.10 | 0.43 | 918 | 0.46 | 1.3-1.5 | 0.3 | 5-20 linear | SFX |
| `sfx_pickaxe_swing` | 1 | 0.45 | -35.9 | 0.10 | 0.20 | 202 | 0.57 | 1.1-1.2 | 0.7 | 5-15 log | SFX |
| `sfx_pickaxe_hit` | 7 | 0.50 | -17.1 | 0.01 | 0.33 | 4196 | 0.10 | 0.9-1.1 | 0.4 | 5-60 custom | SFX |
| `sfx_bow_draw` | 3 | 2.38 | -33.9 | 0.04 | 2.21 | 1896 | 0.56 | 1.1-1.4 | 0.5-0.6 | 2-20 log | SFX |
| `sfx_bow_fire` | 3 | 1.00 | -19.3 | 0.01 | 0.52 | 1497 | 0.50 | 1.0-1.1 | 0.5-0.6 | 5-30 custom | SFX |
| `sfx_arrow_hit` | 3 | 0.25 | -22.7 | 0.04 | 0.17 | 1783 | 0.53 | 0.9-1.0 | 0.5-0.7 | 2-40 custom | SFX |
| `sfx_arbalest_fire` | 6 | 1.69 | -16.2 | 0.10 | 1.05 | 810 | 0.51 | 0.9-1.0 | 1.0 | 5-30 custom | SFX |
| `sfx_torch_swing` | 4 | 1.12 | -29.1 | 0.27 | 0.68 | 165 | 0.59 | 0.9-1.0 | 0.5-0.7 | 5-20 linear | SFX |
| `sfx_kromsword_swing` | 5 | 1.00 | -14.0 | 0.12 | 0.60 | 1964 | 0.29 | 0.95-1.0 | 0.95-1.0 | 1-15 log | SFX |
| `sfx_metal_blocked` | 4 | 0.88 | -17.9 | 0.00 | 0.41 | 2618 | 0.08 | 0.7-0.9 | 0.6-0.9 | 5-25 linear | SFX |
| `sfx_wood_blocked` | 3 | 0.58 | -15.8 | 0.00 | 0.42 | 333 | 0.26 | 0.7-0.8 | 1.3-1.5 | 5-25 linear | SFX |
| `sfx_perfectblock` | 1 | 1.63 | -13.4 | 0.12 | 1.15 | 889 | 0.47 | 0.9-1.05 | 1.0 | 5-25 linear | SFX |

- **Swings are air and they are quiet.** A dark band-passed whoosh (centroid 70 to 200 Hz, 90 % or more under
  300 Hz, flatness 0.5), a rising-then-falling bell 0.3 to 1 s long, played 3 to 15 dB under the weapon's hit, short
  reach (20 m, linear). Light weapons swing shorter and quieter (knife 0.34 s at -39 LUFS), heavy ones longer and
  louder (`sfx_sledge_swing` 1.75 s at -18.6, its peak late: attack 0.9 s). Blades add a thin overlay
  (`sfx_sword_swing` has a child `sfx_sword_swing_overlay` with its own clips: a lighter, brighter layer on top).
  Recipe: `recipes.swing.whoosh(weight=...)`.
- **Hits are the object, not the air**: short (0.25 to 0.75 s tail), instant (attack under 30 ms). Edged metal rings:
  `sfx_sword_hit` shows clean partials 1 to 5 kHz (flatness 0.14) with a noisy slap under them; the pickaxe's clink is
  the brightest and most tonal sound in combat (4.2 kHz, flatness 0.10); axes and clubs are noisy thuds and chops
  (flatness 0.4 to 0.55, centroid 0.5 to 1.1 kHz). The target adds its own layer: a creature's hurt sound, or the
  material's hit (`sfx_tree_hit`, `sfx_rock_hit`, see materials.md), so a weapon's hit is played at 0.4 to 0.5 volume.
- **Blocks** are instant (attack 0 ms) and short: wood a dull knock (333 Hz, pitch 0.7 to 0.8; its ZSFX volume of
  1.3 to 1.5 plays at 1, since Unity clamps AudioSource volume), metal a clang with ringing partials (flatness 0.08);
  the perfect block adds a bright 1.6 s flourish (-13.4 LUFS) played on top.
- **Staff casts** (`weapon.staff.swing`) are the loudest weapon sounds (-13.2 LUFS played, the lightning staff -8.8,
  60 m reach):
  a 0.1 to 0.3 s swell into a dark roar or burst, 1 to 3.7 s. References: `sfx_staff_elder_cast`,
  `sfx_stafffrostorbs_cast`, `sfx_staff_trollstav_cast`, `sfx_staffspiritcaller_cast`, `sfx_staff_lightning_fire`.

## Impacts, breaking and building

The material pages hold the detail: [materials.md](materials.md). In short (played LUFS; see that page for the rest):
placing a piece plays a 1.27 s hammer knock with the material's rattle (`sfx_build_hammer_wood` -21.4, `_stone`
-20.8, `_metal` -22.3, `_crystal` 0.86 s -22.1, `_default` -23.2), five variations at pitch 0.8 to 1.1, 30 m
logarithmic; hitting a world object or piece is 0.6 to 1.6 s at -15 to -22; breaking is 2 to 3.7 s at -12 to -25
(wood and stone the loudest, -14 to -17, `world.destroy.stone` reaching 90 m).

## Player footsteps

The player's `FootStep` has an entry per motion (walk, jog, run, sneak, land, swim, climb) and ground material; each
plays an `fx_footstep_*` prefab whose child `sfx_footstep_*` holds the clips. Full table in
[materials.md](materials.md). In short: 0.45 to 1 s, the quietest sounds in the game (walk -34 to -42 LUFS played,
run -31 to -37, land -18 to -30), 8 to 20 variations with little pitch spread (0.8 to 1.0), 20 to 40 m logarithmic.
Stone and wood are deep thumps (centroid 100 to 130 Hz, 95 % under 300 Hz); grass and earth a soft brush with some
grit (220 to 640 Hz; walking on grass 3.1 kHz, the swish of blades); snow a crunch (0.9 to 1.3 kHz); mud and water
wet and bright (2.6 to 3.5 kHz, 60 to 70 % in the mids).

## Items, the player and the interface

| archetype | sounds/clips | length s | played LUFS | attack s | tail s | centroid Hz | flatness | low/mid/high | variations | pitch | volume | reach m | roll-off | group |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| item.pickup | 1/3 | 0.56 (0.56..0.56) | -34.2 (-34.2..-34.2) | 0.01 (0.01..0.01) | 0.48 (0.48..0.48) | 1593 (1531..1796) | 0.51 (0.50..0.51) | 0.37/0.46/0.16 | 3 | 0.85-1.00 | 0.80-0.80 | 25 (25..25) | linear | Effects/SFX |
| item.drop | 1/1 | 0.53 (0.53..0.53) | -32.9 (-32.9..-32.9) | 0.06 (0.06..0.06) | 0.30 (0.30..0.30) | 1186 (1186..1186) | 0.59 (0.59..0.59) | 0.34/0.58/0.08 | 1 | 0.80-0.80 | 1.00-1.00 | 10 (10..10) | linear | Effects/SFX |
| item.eat | 3/3 | 1.24 (0.89..1.29) | -32.3 (-33.1..-31.6) | 0.11 (0.06..0.14) | 1.09 (0.71..1.20) | 2295 (1427..2336) | 0.46 (0.44..0.47) | 0.17/0.58/0.25 | 1 | 0.90-1.00 | 0.80-0.90 | 10 (10..10) | log | Effects/SFX |
| item.drink | 3/8 | 0.58 (0.48..1.54) | -22.9 (-25.0..-22.8) | 0.04 (0.02..0.16) | 0.46 (0.33..1.45) | 1017 (730..1210) | 0.28 (0.26..0.43) | 0.05/0.87/0.07 | 1 | 0.90-1.00 | 0.40-0.40 | 25 (18..25) | custom | Effects/SFX |
| item.equip | 5/17 | 1.12 (0.86..1.50) | -23.4 (-25.7..-21.3) | 0.12 (0.10..0.23) | 0.78 (0.53..0.91) | 747 (233..1384) | 0.53 (0.48..0.55) | 0.74/0.17/0.08 | 3 | 0.70-1.30 | 0.60-0.80 | 14 (10..14) | custom | Effects/SFX |
| status.start | 23/57 | 4.49 (2.78..5.08) | -18.5 (-22.8..-12.3) | 0.14 (0.07..0.21) | 2.85 (2.25..3.92) | 748 (541..1201) | 0.49 (0.46..0.52) | 0.63/0.28/0.05 | 3 | 0.90-1.00 | 1.00-1.00 | 25 (25..30) | log | Effects/SFX |
| status.other | 11/36 | 1.06 (0.88..1.90) | -20.8 (-24.5..-12.0) | 0.04 (0.03..0.08) | 0.79 (0.66..1.30) | 1494 (588..2351) | 0.53 (0.43..0.57) | 0.65/0.15/0.15 | 2 | 0.90-1.00 | 1.00-1.00 | 25 (17..28) | log | Effects/SFX |
| player.hurt | 2/10 | 2.45 (2.27..2.54) | -20.8 (-24.5..-17.1) | 0.03 (0.03..0.07) | 0.55 (0.52..0.56) | 674 (606..747) | 0.40 (0.36..0.44) | 0.87/0.06/0.08 | 5 | 0.80-1.00 | 0.75-0.75 | 20 (20..20) | linear | Effects/SFX |
| player.jump | 2/6 | 0.70 (0.65..0.71) | -36.2 (-39.3..-33.2) | 0.05 (0.00..0.11) | 0.37 (0.37..0.42) | 541 (150..1072) | 0.53 (0.53..0.55) | 0.85/0.10/0.05 | 3 | 0.80-0.90 | 0.40-0.50 | 20 (20..20) | linear | Effects/SFX |
| player.skillLevelupEffects | 1/1 | 5.81 (5.81..5.81) | -23.2 (-23.2..-23.2) | 0.05 (0.05..0.05) | 2.72 (2.72..2.72) | 200 (200..200) | 0.13 (0.13..0.13) | 0.93/0.05/0.02 | 1 | 0.96-1.00 | 1.30-1.30 | 20 (20..20) | linear | Effects/SFX |
| ui | 15/22 | 0.59 (0.34..0.94) | -25.6 (-32.7..-18.9) | 0.01 (0.00..0.02) | 0.48 (0.26..0.75) | 1622 (516..5599) | 0.51 (0.18..0.56) | 0.38/0.21/0.17 | 1 | 1.00-1.00 | 1.00-1.00 | 50 (50..50) | linear | GUI |
| ui.stinger | 1/1 | 4.12 (4.12..4.12) | -14.6 (-14.6..-14.6) | 0.71 (0.71..0.71) | 2.88 (2.88..2.88) | 178 (178..178) | 0.46 (0.46..0.46) | 0.85/0.14/0.00 | 1 | 1.00-1.00 | 0.70-0.70 | 50 (50..50) | linear | GUI |

- **Handling sounds are small and close**: pickup 0.56 s at -34 LUFS, drop -33, eat -31 (a crunchy chew, 2.4 kHz,
  several bites in one clip), equip -28.6 (a cloth-and-metal rustle), reach 10 to 25 m. One or three variations. The
  exception is drinking (`sfx_drink` -22.7, `sfx_MeadBurp` -22.9 with a separate `slurp` child): a swallow and a burp
  carry.
- **Potions and status effects** start with a 2.8 to 5 s swell (-19 LUFS): a magical shimmer or bubbling rising over
  0.1 to 0.2 s and ringing out; `sfx_Potion_health_Start` 3.8 s at 1.2 kHz.
- **The interface is short and bright and has no variations**: `sfx_gui_button` and `sfx_gui_select` 0.18 s, 5.6 kHz
  clicks at -30 LUFS in the GUI group; crafting at a station is a 3D sound in the SFX group instead
  (`sfx_gui_craftitem_workbench`: five hammer knocks over 1.86 s, 122 Hz; `_forge`: a ringing clang, flatness 0.08).
  The one stinger (`sfx_gui_biomefound`, 4.1 s, -14.6) is a slow musical swell (attack 0.7 s).
- **Levelling up** is a 5.8 s tonal swell (flatness 0.13, 200 Hz): the only musical sound outside the music.

## Pieces and stations

| archetype | sounds/clips | length s | played LUFS | attack s | tail s | centroid Hz | flatness | low/mid/high | variations | pitch | volume | reach m | roll-off | group |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| piece.door | 17/27 | 1.06 (0.78..1.90) | -19.1 (-26.4..-17.5) | 0.12 (0.06..0.43) | 0.67 (0.42..1.23) | 354 (224..1269) | 0.49 (0.39..0.53) | 0.83/0.14/0.03 | 1 | 1.00-1.00 | 0.80-0.80 | 30 (30..40) | custom | Effects/SFX |
| piece.container | 4/12 | 0.58 (0.49..0.66) | -23.8 (-26.2..-21.8) | 0.06 (0.04..0.13) | 0.38 (0.33..0.55) | 574 (291..932) | 0.43 (0.42..0.47) | 0.81/0.14/0.05 | 3 | 0.90-1.00 | 1.00-1.00 | 30 (30..30) | custom | Effects/SFX |
| craft.station | 9/20 | 1.68 (0.60..4.32) | -13.0 (-20.4..-12.3) | 0.04 (0.01..0.08) | 1.04 (0.49..3.33) | 585 (442..994) | 0.49 (0.47..0.53) | 0.72/0.22/0.05 | 1 | 0.95-1.00 | 0.60-0.70 | 20 (20..30) | linear | Effects/SFX |
| piece.station | 22/36 | 1.89 (1.11..4.53) | -14.3 (-20.9..-12.9) | 0.06 (0.04..0.11) | 1.47 (0.81..3.30) | 867 (380..1406) | 0.53 (0.50..0.55) | 0.69/0.22/0.06 | 1 | 0.90-1.05 | 1.00-1.00 | 32 (25..48) | log | Effects/SFX |
| piece.fire | 9/26 | 2.50 (2.00..2.58) | -16.1 (-18.7..-10.8) | 0.11 (0.03..0.29) | 1.44 (0.95..1.99) | 575 (267..1628) | 0.52 (0.47..0.54) | 0.67/0.15/0.04 | 3 | 0.90-1.10 | 1.00-1.00 | 400 (400..500) | log | Effects/SFX |
| piece.trap | 6/14 | 1.78 (0.97..1.92) | -14.7 (-17.8..-12.5) | 0.07 (0.05..0.12) | 1.58 (0.60..1.75) | 1324 (925..1875) | 0.52 (0.46..0.53) | 0.72/0.18/0.16 | 2 | 0.80-1.05 | 0.90-0.95 | 30 (30..30) | log | Effects/SFX |
| piece.ward | 5/5 | 1.04 (1.01..1.13) | -11.2 (-13.2..-10.4) | 0.23 (0.19..0.24) | 0.76 (0.64..0.81) | 217 (208..274) | 0.32 (0.32..0.34) | 0.85/0.15/0.00 | 1 | 1.00-1.00 | 1.00-1.00 | 30 (30..60) | log | Effects/SFX |
| piece.stand | 6/8 | 1.27 (0.81..2.51) | -13.0 (-13.1..-12.6) | 0.03 (0.02..0.19) | 1.11 (0.71..2.01) | 818 (480..982) | 0.49 (0.46..0.53) | 0.50/0.45/0.04 | 1 | 0.90-1.10 | 1.00-1.00 | 30 (22..30) | log | Effects/SFX |

- **Doors** have one clip each (no variations, no pitch spread), 0.7 to 1.9 s: a creak and a thud; open and close
  are separate prefabs, and the close carries a child `slam` with its own clip.
- **Chests** are small (0.38 to 0.54 s, -23 to -31 LUFS): the open a soft creak, the close a knock.
- **Stations** (smelter, kiln, oven, fermenter, spinning wheel) play 1 to 4.5 s one-shots for adding and producing at
  -13 to -21 LUFS, one clip each; the loops that run while they work are under Loops.
- **Fireworks and fuel** (`piece.fire`) reach 400 m (fireworks are meant to be heard across the map); adding fuel is
  a 1.1 s whoomph (`sfx_FireAddFuel`, -10 LUFS, 206 Hz).
- **Wards** are 1 s tonal swells (flatness 0.32, 217 Hz), one clip each.

## Loops

| archetype | sounds/clips | length s | played LUFS | attack s | tail s | centroid Hz | flatness | low/mid/high | variations | pitch | volume | reach m | roll-off | group |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| loop.fire | 33/42 | 10.00 (3.77..20.25) | -32.3 (-36.1..-30.7) | 0.31 (0.27..0.59) | 9.68 (2.59..14.94) | 694 (427..999) | 0.56 (0.55..0.56) | 0.78/0.14/0.06 | 1 | 0.90-1.00 | 0.60-0.95 | 15 (12..20) | log | Effects/SFX |
| loop.magic | 23/28 | 17.00 (8.42..33.00) | -29.7 (-34.2..-23.1) | 0.46 (0.15..1.20) | 16.66 (7.84..32.86) | 844 (294..2137) | 0.50 (0.39..0.53) | 0.51/0.28/0.06 | 1 | 1.00-1.00 | 0.85-1.00 | 30 (18..50) | custom | Effects/SFX |
| loop.machine | 15/21 | 2.47 (2.11..3.06) | -29.4 (-32.7..-19.4) | 0.15 (0.03..0.26) | 2.20 (1.88..2.90) | 533 (464..706) | 0.36 (0.26..0.52) | 0.38/0.55/0.01 | 1 | 1.00-1.00 | 0.90-0.90 | 15 (10..20) | log | Effects/SFX |
| loop.creature | 23/35 | 4.05 (2.11..8.23) | -23.3 (-33.5..-17.9) | 0.13 (0.04..0.87) | 3.46 (2.08..7.74) | 533 (183..1499) | 0.53 (0.45..0.55) | 0.53/0.20/0.02 | 1 | 1.00-1.00 | 1.00-1.00 | 40 (22..90) | log | Effects/SFX |
| loop.ship | 11/11 | 53.06 (2.81..92.00) | -28.3 (-28.9..-26.2) | 0.38 (0.23..1.93) | 32.94 (2.56..90.06) | 690 (644..1104) | 0.56 (0.54..0.57) | 0.28/0.68/0.05 | 1 | 1.00-1.00 | - | 20 (16..28) | log | Effects/SFX |
| loop.ambient | 8/12 | 6.00 (5.04..60.00) | -30.7 (-34.9..-20.9) | 1.35 (0.11..3.75) | 5.85 (2.31..56.59) | 1046 (361..1868) | 0.57 (0.56..0.58) | 0.50/0.31/0.08 | 1 | 1.00-1.00 | 0.90-1.00 | 32 (11..170) | custom | Effects/SFX |

A loop is an AudioSource with Loop on, with or without a ZSFX (114 plain looping AudioSources on creatures and props,
47 more with particle systems on stations). All use one clip. They are quiet: a fire at -32 LUFS played sits 16 dB
under a creature's alert, and reaches 15 m. `AudioMan` keeps the nearest `m_maxConcurrentSources` + 1 copies of the
same loop audible and fades the rest out over 0.5 s (every 16 frames it re-sorts by distance to the camera): the fire
loops in pieces set 2 to 6, so a hall of torches plays three to seven of them; a loop left at 0 keeps only the
nearest one; -1 turns the limit off.

- **Fire** (`sfx_fire_loop`, 10 s, -30 LUFS momentary, -32 integrated, played at volume 0.6 to 1.0 and pitch 0.9 to
  1.1): steady broadband noise heaviest under 500 Hz, a fine vertical crackle through the whole clip, no swell, no
  audible pops above the bed. Torches, braziers and bonfires use the same clip. Recipe: `recipes.fire.fire_loop`.
- **Magic** hums (portals, shield generator, frost foundry, gates) run 8 to 33 s, darker and more tonal (flatness 0.39
  to 0.53), often a slow drone with a shimmer.
- **Machines** (windmill, spinning wheel, fermenter, beehive) are short cycles (2 to 3 s) with a rhythm or a tone
  (flatness 0.26 to 0.52).
- **Creature loops** (bat wings, deathsquito buzz, surtling fire, wisp magic) follow the creature: 2 to 8 s.
- **Ships** layer several long loops per hull (sail, deck, creak, bow and stern water, 53 to 92 s), mid-heavy
  (68 % mid) at -28 LUFS.

## Ambience, weather, water and music

| archetype | sounds/clips | length s | played LUFS | attack s | tail s | centroid Hz | flatness | low/mid/high | variations | pitch | volume | reach m | roll-off | group |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ambient.loop | 19/19 | 64.00 (48.50..93.00) | -35.3 (-41.7..-25.9) | 0.97 (0.45..9.24) | 63.20 (32.61..86.88) | 244 (131..459) | 0.54 (0.52..0.59) | 0.77/0.21/0.00 | 1 | 1.00-1.00 | - | - | 2D | Effects/Ambient |
| ambient.random | 13/407 | 4.39 (3.00..6.82) | -34.8 (-41.7..-25.9) | 0.54 (0.18..0.99) | 2.79 (1.82..4.77) | 686 (391..1273) | 0.51 (0.39..0.55) | 0.26/0.55/0.01 | 20 | 1.05-0.90 | 1.00-1.00 | 40 (40..40) | linear | Effects/SFX |
| ambient.wind | 1/1 | 146.00 (146.00..146.00) | -23.6 (-23.6..-23.6) | 15.18 (15.18..15.18) | 130.80 (130.80..130.80) | 745 (745..745) | 0.57 (0.57..0.57) | 0.35/0.62/0.04 | 1 | 1.00-1.00 | - | - | 2D | Effects/Ambient |
| ambient.ocean | 1/1 | 204.00 (204.00..204.00) | -22.2 (-22.2..-22.2) | 12.25 (12.25..12.25) | 191.70 (191.70..191.70) | 939 (939..939) | 0.58 (0.58..0.58) | 0.46/0.47/0.08 | 1 | 1.00-1.00 | - | - | 2D | Effects/Ambient |
| ambient.lava | 1/17 | 2.41 (1.85..3.33) | -20.9 (-20.9..-20.9) | 0.16 (0.10..0.27) | 1.75 (1.42..2.10) | 500 (292..576) | 0.53 (0.50..0.54) | 0.51/0.46/0.00 | 17 | 1.05-0.90 | 1.00-1.00 | 40 (40..40) | linear | Effects/SFX |
| water.splash | 4/10 | 2.72 (2.49..2.73) | -21.3 (-22.1..-19.6) | 0.25 (0.09..0.39) | 2.39 (1.65..2.43) | 779 (766..886) | 0.54 (0.51..0.57) | 0.34/0.61/0.04 | 2 | 0.90-1.10 | 0.75-0.75 | 50 (45..50) | log | Effects/SFX |
| music.track | 46/47 | 172.00 (118.00..243.40) | -18.1 (-19.9..-15.5) | 2.93 (0.06..12.74) | 151.90 (92.67..228.70) | 342 (249..394) | 0.36 (0.31..0.45) | 0.69/0.30/0.00 | 1 | 1.00-1.00 | - | - | 2D | Music |
| music.location | 14/14 | 149.80 (117.10..183.50) | -20.1 (-22.2..-17.7) | 1.21 (0.09..4.30) | 146.90 (112.60..181.80) | 321 (283..348) | 0.32 (0.31..0.38) | 0.70/0.30/0.00 | 1 | 1.00-1.00 | - | 999 (999..999) | linear | Music_ontop |

- **The environment's ambient loop** (EnvMan: one per weather, `m_ambientLoop` at `m_ambientVol` 0.2 to 2.0) is a
  48 to 93 s 2D bed of wind in reeds, rain or cave air, dark (centroid 244 Hz, 77 % under 300 Hz) and quiet (-35 LUFS
  played). Wind (146 s) and ocean (204 s) are AudioMan's own loops, their volume and pitch following the wind
  (volume 0 to 0.85, pitch 0.7 to 1.0) and the water depth (volume 0.3 to 1.0).
- **Random ambient one-shots** (AudioMan `m_randomAmbients`, 13 lists: Forest, Swamp, the crypts and caves,
  Ashlands, the Deep North) play every 5 s with a chance of 0.5 at a random point 10 to 30 m away: AudioMan
  instantiates `Audio/Ambients/RandomAmbientBase.prefab` locally (not networked), puts one clip in its ZSFX and plays
  it, 3D with a linear roll-off to 40 m in the SFX group, pitch 0.9 to 1.05. Birds, creaks, drips, distant calls,
  3 to 7 s, very quiet (-35 LUFS), up to 71 clips a list (FrostCaves). Lava noises in the Ashlands work the same way
  (every 2 s, chance 0.33, 2 to 20 m).
- **Weather one-shots**: `sfx_thunder` (12 clips, 8.4 s, reach 500 to 1,000 m linear, SFX_LARGE) and
  `sfx_mistlands_thunder`.
- **Music** (MusicMan, 46 tracks, 2 to 4 minutes, 2D, the Music group) sits at -18 LUFS; location music
  (`MusicLocation`, 14 prefabs) plays in `Music_ontop`, which ducks the main music. Music volumes run 0.2 (biome
  ambience music) to 0.8 (locations); fades in over 1 to 10 s.

## Projectiles, magic and bosses

| archetype | sounds/clips | length s | played LUFS | attack s | tail s | centroid Hz | flatness | low/mid/high | variations | pitch | volume | reach m | roll-off | group |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| projectile.hit | 53/194 | 1.71 (1.03..3.28) | -12.8 (-18.7..-10.8) | 0.04 (0.02..0.09) | 1.20 (0.80..2.19) | 440 (218..1268) | 0.53 (0.49..0.56) | 0.82/0.12/0.04 | 4 | 0.90-1.05 | 1.00-1.00 | 50 (40..80) | custom | Effects/SFX |
| projectile.aoe | 28/125 | 3.00 (1.63..6.29) | -14.6 (-17.7..-12.6) | 0.09 (0.04..0.23) | 1.77 (1.07..3.99) | 322 (208..703) | 0.54 (0.50..0.56) | 0.83/0.13/0.02 | 4 | 0.90-1.02 | 1.00-1.00 | 55 (48..92) | custom | Effects/SFX |
| boss.summon | 12/12 | 5.58 (1.69..9.81) | -10.7 (-11.8..-9.8) | 0.11 (0.06..0.44) | 3.39 (1.60..5.65) | 317 (191..455) | 0.50 (0.48..0.54) | 0.77/0.19/0.01 | 1 | 1.00-1.00 | 1.00-1.00 | 60 (40..100) | custom | Effects/SFX |

- **Projectile hits and areas** are big dark impacts (82 % under 300 Hz, -12 to -14 LUFS), a fast crack into a rumble
  1 to 4 s; lightning adds a crackle over 2 kHz, frost a glassy shatter, fire a whoosh.
- **Bosses** are loud and long: their idles, alerts and deaths run 2 to 15 s at -8 to -16 LUFS (Eikthyr's and the
  Elder's alerts -7.8; Fader's loudest sounds are events: its meteor call -8.8, its death explosion -7.6), their own sounds reach 40 to 120 m (the median per boss), and many play in SFX_LARGE, the group
  with the 3.5 s hall reverb (Eikthyr 9 of 10 sounds, Yagluth 10 of 12, the Queen 15 of 17; Moder, the Elder,
  Bonemass about half; Fader and the Frozen King keep most in SFX).
  Summoning at an altar (`sfx_offering`, 4 s, -11.5) and the spawn (`sfx_spawn`, 1.7 s, -11.6) are single clips.
  See [creatures.md](creatures.md) for each boss.

## What is left over

`sfx.creature.other` (18 sounds: boss special moves nothing references directly), `sfx.other.unused` (57: siege
engines, lever sparks, old variants) and the `player.*` rows the table does not show (dodge, drown, snow shovel) are
in `data/sfx.json` with their numbers.
