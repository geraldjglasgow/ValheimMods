# Changelog

## 3.18.0

- Portalbound: Bonemass rolls it too, its slime ball arcing out of a portal above you; only the Elder and Bonemass get it.
- Thieving never steals the Wishbone.

## 3.17.0

- New `creature stars: false` rule: creatures keep the game's or another mod's stars, and still mutate.
- `stars: false` under `bosses:` keeps a boss's game level instead of resetting it to 1.
- Bosses drop one more trophy for each player within 100 m.
- `elite tier`, `elite inspect` and the death recap show kept levels.
- Fixed: Epic Loot's boss trophy mode no longer replaces the boss trophy count.
- Less work every frame; faster loading, the config file is written once instead of once per setting.

## 3.16.0

- New mutations: Frostbound (ice trail, chilling aura), Mudbound (mud trail), Corrodent (ruins armour) and Cloning (hides behind a decoy).
- New boss aspects: Nightfall (storming midnight, hunting tornadoes) and Brutal (heavy blows throw players far).
- Death recap: `Recap key` (F10) or `/deaths` replays your last seconds and every hit.
- Altars show the boss's stars before the offering.
- Phantom's boss hides among its copies; Bountiful doubles boss trophies; Tethered pairs share one damage board.
- New rule files: altars shift every 15 s (`shift seconds`); stars hit softer; bats are never Mad or Cloaked.
- Fixed: Stormbound no longer strikes players in nearby dungeons.

## 3.15.0

- New mutations: Juggernaut (never staggers) and Screecher (a heavy hit makes it shriek and deafen nearby players).
- New boss aspects: Tethered (two linked bosses), Bountiful (two extra aspects) and Portalbound (the Elder's vines come through a portal).
- Bosses drop one trophy per star plus one, whatever the loot settings.
- Gilded is half as common; Gilded and Relentless never roll on large creatures.
- Devouring eats only creatures with no more health than its own, never bosses or large creatures.
- Bloated explodes 1.5 seconds after death.

## 3.14.0

- New `difficulty:` line at the top of the rule file: Easy, Medium, Hard, Very Hard or Extreme.
- On a difficulty, the biome you enter starts gentle and cleared biomes harden with every later boss.
- Extreme allows up to 8 stars; `star power` lines have nine entries.
- New rule files start on Medium; existing files keep today's rules (Custom).
- `elite tier` shows the difficulty and what it gives the biome you stand in.

## 3.13.4

- A new install's rule file has short comments; the full reference is linked at its top.
- Shorter store page; the reference (mutations, aspects, rule fields, commands) is on GitHub.

## 3.13.3

- Warding no longer draws particles: a reflect is marked by sound alone, the Staff of Protection's shield at 30% volume.
- The Reflective boss aspect's tell changes the same way.
- Devouring no longer draws a burst when it eats; the sound is unchanged.

## 3.13.2

- The world tier boxes show only with PackPanel; without it the inventory and HUD stay as the game draws them.

## 3.13.1

- Fixed: the inventory broke with Jewelcrafting, CurrencyPocket, OttoPay, TrashItems or Quick Stack Store Sort Trash
  Restock installed.
- Those mods' boxes join the stat column; TrashItems' trash can takes dragged stacks there.
- OpenKeep 1.8.0 to 1.9.0 and PackPanel 0.1.0 to 0.2.0 have the same column: update them too.

## 3.13.0

- Devouring eats only creatures with at most 125% of its current health (`max prey health`, 0 lifts the limit).
- Devouring eats one creature per star, at least one (`min meals`), then is sated and acts like its kind.
- A devourer's nameplate shows what it ate (`Show devoured creatures`, `Devoured creature icon size`); so does
  `elite inspect`.
- The devour flash is a fifth of its size, and its screen shake is felt only close by.
- Bloated's fuse is 1.7 seconds; an older `creature_rules.yml` keeps its own `delay: 2.0`.

## 3.12.0

- New box with the world tier under the minimap (`Show world tier under minimap`).
- The tier reads as one number ("3"); hovering it shows the total.
- The inventory's stat plates are square boxes in a column.
- Thieving steals only with melee hits, never on a parry or dodge, and with PackPanel never from its slots.
- On a grid wider than eight, thieves no longer spare the top-row cells right of the hotbar.
- Elite Creatures Pack: a sleeping mimic hides its traits; the kraken never rolls Twin or Phantom.
- Warding's `max reflect` now caps everything reflected to you per second, not per hit.
- Rule changes reach loaded creatures' mutations at once.

## 3.11.0

- `elite purge 30` removes marked creatures within 30 m only; without a number it still removes every loaded one.

## 3.10.0

- With GrindstoneSkills 0.7.0 or later, Husbandry's "Better offspring" can give a newborn one more star.

## 3.9.1

- Starred creatures wear the game's own one-star and two-star looks again (a two-star Neck is purple).

## 3.9.0

- New mutations: Gilded (rare, flees, pays triple loot), Blinking (reappears behind you), Relentless (follows you to
  150 m).
- New boss aspects: Adaptive, Fixated, Stormbound, Gravitic and Colossal; one fight in five stays plain.
- Mutation rules per creature under `creatures:`; two entries for one creature merge.
- Cloaked shows at 10 m (was 6), at 15 m on trolls and lox, and never on drakes.
- Warding reflects only damage taken, at most `max reflect` (7.5%) of your health; smaller flashes.
- Bloated: a two-second fuse; the corpse vanishes in a smaller blast and drops its loot there.
- Thieving carries one item per star; `max items` is the minimum, 8 the most.
- Tamed Splintering creatures split into tamed copies.
- Boss damage board moved left; `/damage` shows it again; damage to Phantom copies counts.
- Fixed: smoke left after a Bloated blast, effects drawn twice, and stolen goods duplicated at dawn.
- Update the server and every player. An older rule file keeps its old numbers until you delete it.
- Gilded ignores `mutation chance`: stop it with `mutations enabled` or `mutation chances`.

## 3.8.0

- Phantom splits off one copy per player online at 66% and 33% health (`split at`, `per player`, `health per tier`).
- An older rule file's Phantom `copies` and `health` are ignored; replace that line to tune it.
- Fixed: Bloated, Warding, Devouring, Thieving, Summoner and Phantom effects drew nothing; Bloated's blast has a sound
  (`blast sound`).
- World tier plate in the inventory under the weight (`Show world tier`).
- Update the server and every player.

## 3.7.1

- Fixed: with mods that raise creature health (such as Path of Valheim), creatures spawned with 10-15% health.

## 3.7.0

- Boss damage board: when a boss dies, everyone sees who hurt it and how much.
- `Boss damage board` and `Boss damage board seconds` in the .cfg.
- Update the server and every player.

## 3.6.1

- Fixed: admins on a dedicated server's `adminlist.txt` were refused the `elite` admin commands.
- A refusal names the ID to add to `adminlist.txt`, which is read again within 10 seconds.
- A mistyped `elite` subcommand lists the subcommands.
- Update the server and every player.

## 3.6.0

- World tiers: each boss's first defeat makes stars and mutations more common; bosses are unaffected.
- A world that defeated bosses before starts at that tier. `elite tier` shows it to any player.
- Breeding: young take a parent's mutation and up to the stronger parent's stars, and keep them when grown.
- Eggs keep their traits, say what they will hatch, and stack only with eggs of the same stars.
- `elite inspect` shows the tier a creature rolled at.
- New `world tiers:` and `breeding:` blocks; an older rule file takes their defaults.

## 3.5.0

- Boss aspects: Reflective, Shielded, Mending, Summoner, Elementalist, Enraged, Twin or Phantom, or a plain fight.
- Hover the altar's offering bowl to see the aspect; it shifts every in-game hour.
- Harder aspects multiply the boss's loot, in Vanilla loot mode too.
- Boss stars show under the boss health bar.
- New `aspects:` block under `bosses:`; `stars: false` now turns off only boss stars.
- `elite spawn Bonemass 2 Twin` spawns a boss with an aspect; `elite inspect` reports it.

## 3.4.0

- Loot modes in a new `loot:` block: Vanilla, Scaled, Rolled (default) or Curated.
- `extra roll chance`, `max extra rolls`, `global multiplier` and `boss multiplier`.
- Trophies are multiplied only with `multiply trophies`.
- Loot rules per creature under `creatures:`: `drops`, `drop overrides` and `extra drops`.
- `elite reference` writes `creature_reference.yml`: every creature with its drop table.
- Drops added by other loot mods pass through untouched.

## 3.3.0

- Devouring toned down: `devour cooldown` 60 (was 10), `absorb health` 50 and `absorb damage` 25 (were 100).
- New Devouring `move` field: its base speed.
- An older rule file keeps the old Devouring numbers; edit that line or delete it.
- Fixed: joining a busy server no longer refuses a correct client.

## 3.2.0

- New mutation, Thieving: it steals an unequipped stack when it hits and drops everything when it dies.
- A message names what was stolen, and its nameplate shows it.
- Stolen items are never multiplied or destroyed; `elite purge` drops them too.
- `max items` sets how many it carries (1 by default, 8 at most).

## 3.1.0

- New `mutations enabled` switch turns any mutation off everywhere.
- Shorter rule file comments; the `mutation power` field reference moved to the README.
- Leeching weaker: regen 0.5% (was 2%), pausing after damage (`combat cooldown`), capped by `regen cap`; lifesteal 10%
  (was 30%).
- Plated's `armour` is now a damage reduction percent, capped by `max reduction`; revisit a custom value.
- `large star power` defaults to 1 (was 2), and five-star health is lower.
- Fewer 4 and 5 star creatures in the Ash Lands and Deep North.
- Fixed: starred and Mad creatures' attacks sped up out of control.

## 3.0.0

- Rebuilt from scratch: not an update to earlier versions, and none of the old code remains.
- Nine mutations: Mad, Bloated, Cloaked, Splintering, Leeching, Warding, Plated, Miasmic and Devouring.
- Stars beyond vanilla's two, counted in fives on the nameplate.
- A settings file and a YAML rule file, both hot reloaded; chances and strength set per biome.
- Servers can bind connected players to their rules; every player sees the same creature.
- Console: `elite spawn`, `elite inspect`, `elite purge`, `elite effects`.
