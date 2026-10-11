# Public API

**Status: built, not tested in game**

Other mods give their own creatures Elite Creatures Reborn's mutations and boss aspects through a supported API, first
for Elite Creatures Pack's custom creatures (`EliteCreaturesPack/features/custom-creatures.md`, section 8). Settled with
the user on 2026-10-09; the shape follows EliteCrafting's API. Names and behaviour are ours.

## 1. Shape

- `EliteCreaturesReborn.Api.EliteCreaturesApi`, a `public static class` in `EliteCreaturesReborn.dll` (ILRepack
  internalizes only the merged libraries, so it stays public). Only BCL types in signatures.
- **No reference needed.** Mods bind by reflection through `ValheimModLibs/EliteCreaturesLink` (`EliteLink`,
  `EliteTraits`), typed wrappers that do nothing and answer empty while ECR is absent, older or lacks an endpoint.
  Renaming a method needs a matching change in both.
- **Versioning**: `const int ApiVersion = 1`, `GetApiVersion()`, `GetPluginVersion()`, `HasEndpoint(name)`,
  `GetEndpointNames()`. An endpoint is added, never changed; a removed one stays as a no-op.
- **Never throws**: an internal failure is logged once per endpoint and answers false or an empty array.
- **Main thread only.**

## 2. Endpoints

| Endpoint | What it does |
|---|---|
| `string[] GetMutationNames()` | The mutations by their rule-file names (`Mad` ... `Flamebound`), catalog order. |
| `string[] GetAspectNames()` | The aspects by their rule-file names (`Reflective` ... `Echoing`), catalog order, without `none`. |
| `string[] SetMutations(string prefab, string[] mutations)` | The prefab's fixed mutations. |
| `string[] SetAspects(string prefab, string[] aspects)` | The aspects the prefab rolls: one is fixed, several are drawn among. |
| `string[] SetPortalAttacks(string prefab, string[] attacks)` | Attack item prefab names (the creature's own projectile attacks, like the Elder's `gd_king_shoot`) Portalbound may send through its portals. |
| `string[] SetSummons(string prefab, string[] creatures, int[] stars)` | What the prefab's Summoner calls: creature prefabs and the stars of each (same length; a negative star is the aspect's own `stars`). |
| `void Clear(string prefab)` | Forgets all four for the prefab. |

Each `Set` answers the problems it found, one sentence each naming the endpoint and the prefab, never null. A later call
replaces the earlier one; an empty array clears that part. Names are matched ignoring case; prefab names exactly. Only an
unknown name (not a mutation, not an aspect) and an empty name are left out. Everything else is kept and checked again
when it is used, since a server's rule file or the prefab may change after the call: a mutation ECR's limits refuse now,
an attack item the creature does not hold yet, a summoned creature the game does not know yet.

## 3. What a registration does

- **Mutations** replace the random mutation roll of the prefab from every spawn source - wild, raid, the game's
  `spawn`, other mods' spawners, a Summoner's adds. Stars still roll as usual: the boss table for a boss, the biome
  otherwise, none with `creature stars: false`. Several mutations on one creature are allowed. ECR's limits stay: a
  mutation its body bars (Gilded and Relentless on a large creature), its kind bars (Cloaked on a Deathsquito) or the
  rule file turns off (`mutations enabled`) is skipped and logged once per prefab with the reason; the rest apply.
  `max mutations` and the chance curves do not apply. A boss carrying mutations takes their numbers from the rules of
  the biome it was rolled in. A newborn still inherits from its parents (breeding is not a roll).
- **Aspects** are rolled for the prefab whether or not the game marks it a boss: one name always, several by the rule
  file's aspect weights (evenly when the file weighs all of them at 0), Bountiful's extras from the same list (from the
  rule file's rotation when the list names nothing else). The list is the prefab's whole rotation; the `aspects:`
  switch and an altar's shift still hold. A creature can carry mutations and aspects together; both kinds of behaviour
  are installed. Twins, tethered partners and Phantom copies carry the mutations too (a Phantom copy only wears them).
  A creature that is not a boss pays its aspects' loot multiplier but has no boss bar, damage board or boss trophies.
- **Portal attacks**: Portalbound fires only for the listed attack items, and only for a projectile attack; the hand
  portal opens on the bone the item's attack is let go from (its `m_attackOriginJoint`; without one, the far portal
  alone). Without portal attacks the aspect never fires, with no error.
- **Summons** replace the rule file's `summons` for the prefab. A Summoner without any list - this API's or the
  file's - calls the file's list for the game boss of the biome it was rolled in (Meadows: Eikthyr's ... Ashlands:
  Fader's); in the Ocean and the Deep North it calls nothing.
- `elite spawn <prefab> <stars> [words]` takes mutation and aspect words together for a registered prefab;
  `elite inspect` shows the aspects of any creature carrying one.

## 4. Multiplayer

Registrations are code, not data: every peer's mod registers the same things (Elite Creatures Pack registers from its
synced definitions), and nothing new crosses the network. The creature's owner reads them at its roll, which goes into
the ZDO as every roll does; every machine loads the traits from there. A creature already rolled keeps what it has
when a registration changes.

## 5. Configuration

None. The rule file's switches, weights and numbers apply to registered creatures as to any other.

## 6. Open decisions

- Whether a newborn of a registered kind should take its kind's fixed mutations rather than inherit (now: inherits).
- A Summoner fallback for the Ocean and the Deep North (now: none).

## 7. Build checklist

- [~] Endpoints and `EliteCreaturesLink` - built, compiled, not run in game
- [~] Fixed mutations replace the roll, with ECR's limits - built, not tested
- [~] Registered aspects on bosses and non-bosses, with mutations - built, not tested
- [~] Portalbound with registered attacks - built, not tested
- [~] Summoner: registered list and biome fallback - built, not tested
- [ ] Verified on a dedicated server with Elite Creatures Pack's custom creatures

| Date | What moved |
|---|---|
| 2026-10-10 | API, registry, roll entry point (`Runtime/CreatureRoll`), library; uncommitted |
