# Changelog

## 3.34.2

- Fixed: translated text no longer turns back to English with OdinHorse or similar mods installed.

## 3.34.1

- Russian translation updated.

## 3.34.0

- Rendering Table: a decorated model of its own, half the workbench's width and as tall.
- Metal on the mod's pieces (Rendering Table, Helgrind, Hel's Anvils) now shines.
- Corrodent is stronger: each hit that gets through your armour also takes 4% of the piece (new `wear` field).
- Hel's Arena: E on Helgrind picks the trial: any biome whose boss your world has killed, or Infinite.
- Hel's Arena: a biome's trial is won by beating its boss; Infinite climbs five levels, each harder.
- Helgrind is cheaper: 20 wood, 20 stone and an Eikthyr trophy, at a workbench.
- Raids: the Raiders Chest takes trophies as a stake too; a win gives them back multiplied.

## 3.33.1

- Russian translation, used while the game's language is Russian.

## 3.33.0

- Rendering Table: only a boss trophy makes a Forge idol; other trophies give ores or ingredients.
- Compendium: a Rendering Table page lists each biome's trophies, ores, ingredients and idols.

## 3.32.1

- Fixed: a tame whose stars `15 - Tamed` took no longer shows them on nameplate mods such as SeneaL UI.

## 3.32.0

- `Lock to server`, `Difficulty`, `Creature stars`, `Max mutations`, each mutation's on/off and respawning moved from `creature_rules.yml` into the .cfg, so a configuration manager shows them.
- An existing rule file hands its values over once by itself and keeps its old text as `creature_rules.yml.bak`.

## 3.31.3

- Fixed: built training dummies get no stars or mutations, so a Splintering one no longer splits into extra dummies to dismantle (#36).

## 3.31.2

- Polish translation, used while the game's language is Polish.

## 3.31.1

- Fixed: a creature another mod tames (a summon) loses this mod's stars too while `15 - Tamed` Stars is false.
- `elite inspect` shows a creature's game stars and, for a tame, what `15 - Tamed` took.

## 3.31.0

- Rendering Table: renders trophies into ores or ingredients of their biome, or five into a Forge idol (`Rendering Table`).
- Fixed: creatures turn at the game's own rate again (Eikthyr turned slowly).
- Vietnamese translation updated by QuânKha (Zono) [VLHM].

## 3.30.2

- Fixed: Hel's Arena no longer sends the Deep North villages' spirits, which could not be killed (#37).

## 3.30.1

- Vietnamese translation, by QuânKha (Zono) [VLHM], used while the game's language is Vietnamese.

## 3.30.0

- Translations: every word you read is in `translations/English.yml`; an `EliteCreaturesReborn.translations.<Language>.yml` in `BepInEx/config` translates it.
- Messages sent to other players show in each player's own language.
- New section `15 - Tamed`: `Stars` (off: tamed creatures have the game's stars only) and `Mutations` (off: none), set by the server.
- Update the server and every client together.

## 3.29.0

- Boss altars: E on the bowl (Shift+E on Fader's) opens a window to choose the boss's stars and aspect.
- Boss altars: either can stay on Random and keeps rolling at every shift; a choice stays on the altar for everyone.
- New setting `Altar Choice` (on) under `14 - Boss Altars`, set by the server.
- Update the server and every client together.

## 3.28.2

- Fixed: Flamebound's burning ground now burns you; armour no longer stops it (fire resistance and being wet still help).
- New rule files: Gjall are never Cloaked. On an existing file, add a `Gjall` entry with `mutation chances: Cloaked: [0]`.

## 3.28.1

- Fixed: eggs from starred hens count for cooking recipes.
- Fixed: a player who just joined no longer rolls nearby creatures by their own rules before the server's arrive.
- Raids: extra stars never go past the difficulty's star cap for the biome.
- Fixed: players on a slow connection with many mods are no longer refused when joining a server.

## 3.28.0

- Hel's Arena: when a quarter of a wave is left, Hel marks the last ones on the map and makes them glow, then recalls them a minute later.
- Hel's Arena: reward items pop up with their icons, and the reward and the trial's end are written to chat.
- Hel's Arena: the first cleared wave says the reward comes when the trial ends.
- Update the server and every client together.

## 3.27.0

- Hel's Arena moves outdoors: 130 m across, eight archways, a cavern roof, and a black lake that kills.
- Hel's Arena: starts at your gear's biome, five waves per biome ending in its boss; big creatures come late and few.
- Hel's Arena: kills pay souls, spent at Hel's Anvil to mend gear; a fallen boss may leave healing meads.
- Hel's Arena: F when everyone is ready starts the next wave; E on Hel calls it early.
- Hel's Arena: three Bat-fiends guard it on the first visit and drop runes.
- Fixed: Oozers no longer roll Splintering.
- Update the server and every client together.

## 3.26.0

- Hel's Arena: a short break between waves; creatures come out of portals in the archways, one at a time.
- Hel's Arena: dying in the hall costs no skill levels.
- Hel's Arena: four broken pillars and four low walls give cover.
- Hel's Arena: rewards by how far you get: Forge of Potential idols, coins, bonus skill XP, and runes and essence with EliteCrafting.
- Horde (`Horde`, section `13 - Horde`, off by default): more creatures in the wild, each weaker; the extra creatures pay no extra loot.
- Compendium: the game's compendium has pages on difficulty, stars, mutations and boss aspects, with your server's numbers (`Compendium pages`).
- `elite inspect` works for every player, not only admins.
- Update the server and every client together.

## 3.25.0

- Hel's Arena (`Arena`, section `12 - Arena`, off by default): build Helgrind, Hel's gate, to enter her hall and fight endless waves, a boss every tenth. Subject to change without warning.
- Commands: `elite arena` (in and out) and `elite arena stop`.
- Eikthyr's boss reward is 2-4 Flint instead of 20-40 Coins.
- Boss rewards pay a full share to every player within 100 m of the boss; the altar says "per player".
- Death recap: the video is off by default (`Record deaths`); the hit list stays. Existing configs keep their value.
- Update the server and every client together.

## 3.24.0

- Bosses: each defeat makes that boss likelier to have stars next time (`harder each kill`); the altar shows "Defeated N times".
- Bosses drop their biome's material, one roll plus one per star (`rewards`); the altar shows what the fight pays.
- Flamebound: creatures burn with a flame aura; their burning ground is 2 m wide and lasts 7 seconds.
- Wiki: how the game's World Difficulty stacks with this mod.

## 3.23.0

- Raids: the Raiders Chest (`Raiders Chest`, section `11 - Raids`). Fill it with gold to call a raid on your base; kill every raider to win the gold back with more.
- Raiders hunt players first, then break in to the base's chests and crafting stations.
- Commands: `elite raid start <coins> [tier]` and `elite raid stop`.
- Elite Creatures Pack's custom creatures can roll mutations and boss aspects.
- Update the server and every client together.

## 3.22.0

- New mutation Flamebound: leaves burning ground that sets you on fire.
- New mutations, off by default (`mutations enabled`): Piercing ignores 15% of your armour, Howling calls up to 4 nearby creatures, Binding roots you briefly.
- Mending heals 0.2% of the boss's current health a second (was 0.3% of its maximum).
- Mending: existing rule files keep their `regen` value, now a share of current health.
- Update the server and every client together.

## 3.21.0

- New boss aspect: Echoing, a white ghost repeating the boss's every move and blow 15 s later; x1.3 loot.
- Existing rule files roll Echoing too; set `Echoing: 0` under `chances:` to leave it out.
- Boss hints no longer point to a place already explored on your map.
- A crossbow put away loaded is still loaded when drawn again.
- Fixed: the death recap key no longer stops working after Alt + Tab.
- Update the server and every client together.

## 3.20.0

- Boss stars add only trophies; other boss drops are no longer multiplied. Remove `drops` under `bosses: star power:`.
- Nightfall tornadoes come every 18-28 s, was 10-20 (new rule files; edit `every` in yours).
- Thieving: an item the creature cannot keep comes back to you.
- Devouring's `player threshold` uses the toughest player nearby, also on dedicated servers.
- Death recap: `Video frames per second` defaults to 10, was 15.
- Fixed: `dungeon loot` refilled dungeon chests that still had items in them.
- Less lag in busy fights and on busy servers.
- Update the server and every client together.

## 3.19.0

- Boss hints: when Bonemass, Moder, Yagluth or the Elder dies, players nearby are pointed to the Bog Witch, the ancient forge, Haldor or Hildir (`Boss hints`).
- Fixed: with respawning on, new dungeons and camps get their creatures at once.
- Fixed: respawn and dungeon loot days are world days (each counted as 48 before).

## 3.18.2

- Fixed: respawning `camps` no longer refills dungeons; `dungeons` and `dungeon loot` now work.
- Respawn settings described more clearly in a new rule file and on the wiki.

## 3.18.1

- Deathsquitos are never Cloaked, whatever the rule file says; one a world saved Cloaked loads without it.

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
