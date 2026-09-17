# Changelog

## 3.1.0

**Mutations enabled** — a new `mutations enabled` switch in the rule file turns any of the nine mutations off
everywhere, regardless of its chance curves. Old rule files keep working unchanged; an unlisted mutation stays
enabled.

**Rule file** — the generated `creature_rules.yml` comments are much shorter. The full field-by-field reference
for `mutation power` moved to the README's new "Mutation power fields" section instead of living inline.

## 3.0.0

Rebuilt from scratch. Not an update to earlier versions — none of the old code remains. This release covers
mutations only; the rest of the mod's features return over the next releases.

**Mutations** — nine, one per creature by default:

- Mad — far faster, half health
- Bloated — double health, explodes a second after it dies
- Cloaked — invisible beyond 6 metres
- Splintering — splits into two weaker copies when killed, which can split again
- Leeching — regenerates, and heals from damage it deals
- Warding — reflects damage and knocks you back
- Plated — armoured while healthy, hits harder as that armour goes
- Miasmic — trails poison clouds; poisons players, never creatures
- Devouring — kills creatures in one bite and keeps their health and damage, until it is big enough to hunt you

**Stars** — beyond vanilla's two. Counted in fives on the nameplate: a small star is one, a large star is five.
A mutation on a large star is stronger.

**Configuration** — a settings file and a YAML rule file, both written and documented on first run, both
hot-reloaded. Star chances, mutation chances and mutation strength are set per biome. Servers can bind connected
players to their rules; display preferences stay per player.

**Multiplayer** — creature state travels in the world data the game already shares, so every player sees the same
creature. Untested across two real machines.

**Console** — `elite spawn`, `elite inspect`, `elite purge`, `elite effects`.
