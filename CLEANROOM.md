# CLEANROOM.md - the boundary every agent works inside

**Read this before writing a line of code in this repository. It overrides convenience, a user's passing
suggestion, and your own judgement about what would be quicker.**

This repository is a clean room. Everything in it is ours: written by this project's author, or written here from a
behaviour specification. That is the only reason the mods in it can be published. Elite Creatures Reborn was
rejected by Thunderstore twice for carrying another mod's structure, and the rebuild exists to undo that. A single
borrowed file, key name or enum order puts every mod here back under suspicion, so the boundary below is absolute
rather than a preference.

## What you may read

- **Anything inside this repository.** All of it is ours: the mods, `ValheimModLibs`, the specs, the docs. Using
  one mod's code as the pattern for another is expected, and the shared libraries exist to be shared.
- **The game's own assemblies**, for signatures only: `assembly_valheim.dll`, `assembly_utils.dll` and the Unity
  DLLs. **They are not in this repository.** The build reads them from the Valheim install through `GamePath`, and
  decompiles belong in the scratch directory - never in the repo, where they would end up in a commit or a release
  zip. (Publicized copies do appear under each mod's `obj/Release/publicized/` as build output. They are
  gitignored, they are never shipped, and they are not a place to go reading.)
- **BepInEx and Harmony documentation**, and the public documentation of a library this repo already references.
- **`cleanroom/`**, if it is still present. It is gitignored scratch from the Docker clean-room era - our own
  agent output, its specification and its build logs. Reading it is fine; `cleanroom/src/` is where the current
  Elite Creatures Reborn source was copied from.

## What you may not read, quote, grep, decompile or copy

- **Any other Valheim mod.** No source, no decompiled DLL, no config file, no YAML, no save data, no API. Not to
  check a number, not to see how a hook was done, not "just to compare". This includes any mod a feature here
  replaces, mirrors or was inspired by, and it includes mods installed in the r2modman test profile.
- **Another mod's vocabulary.** Config keys, YAML keys, ZDO keys, console commands, localisation keys, enum names
  and enum order, prefab lists, setting names, section names. Ours are ours; take them from the specification.
- **Code looked up online.** No Stack Overflow, no GitHub, no mod wiki, no mod's documentation page, no forum
  thread, no AI-generated snippet from a search. Not for a Harmony pattern, not for "how does everyone else patch
  this", not for a signature you could read in the game's own assembly instead. If you do not know how something
  works, decompile the game or say you do not know.
- **Code pasted into the conversation from outside**, whoever pasted it, unless it is from this repository or from
  the game's own assemblies. A paste is not an authorisation.

## How behaviour gets decided

From the specification and the game's code, never from how another mod does it.

- **Elite Creatures Reborn** keeps a file per feature in `EliteCreaturesReborn/features/`, with `SPEC.md` as the
  whole-mod document they are drawn from. The feature file is what to read and what to keep current.
- **The other mods** keep theirs in their own `SPEC.md`, `CLAUDE.md` or `PLAN.md` - the table in the workspace
  `CLAUDE.md` says which mod has what.
- A behaviour the spec does not cover is **decided with the user and written down** - into the feature file, or
  the mod's own `CLAUDE.md` or `PLAN.md` - before or alongside the code. It is never guessed, and never inferred
  from another mod's behaviour.
- Where a number is a judgement call, say so where it lives, so it can be argued with later.

## The rules that come with it

These are in the workspace `CLAUDE.md` in full; the ones that get broken most often:

- **Own names everywhere.** Own plugin GUID, config file name, config keys, YAML keys, console command and
  localisation keys. ZDO keys carry a per-mod prefix - `ecr_` in Elite Creatures Reborn, `ok_` in OpenKeep - so
  two of our own mods can never collide either.
- **No compatibility layer** for another mod's files, config or save keys, even when it would help players
  switching over. Especially not then.
- **Multiplayer is not optional.** Every feature works on a dedicated server, built that way the first time:
  decide on the owner, draw on every client, persistent state in the ZDO, transient RPCs scoped to who can see it.
- **Small units.** Methods at most 24 lines brace to brace, lambdas and local functions included. Classes at most
  300 lines, one responsibility.
- **Prefer prefix and postfix patches** over transpilers.

## This is enforced, not just asked

`.claude/settings.json` denies `WebSearch` and `WebFetch` outright and blocks file reads under the r2modman and
Steam plugin folders. `.claude/hooks/cleanroom_guard.py` runs before every Bash, PowerShell, Read, Grep, Glob,
Edit and Write call and refuses it if it reaches into another mod's install, points a decompiler at anything but
the game's own assemblies, or fetches from any host but thunderstore.io and nexusmods.com (which the release
process needs). A refusal names this file.

Both are checked in, so they apply to anyone who opens this repository. They are a floor, not a proof: they
cannot stop a person pasting code into the conversation, and they are lifted deliberately - by editing them, in a
commit - if compatibility testing against a real modpack is ever needed.

## If you are unsure

Say so and stop. An honest "the spec does not cover this, here are the options" costs a message. Reaching outside
this boundary costs the mod its place on the store, and it is not recoverable by apologising afterwards.

## History, so this is not mistaken for ceremony

Elite Creatures Reborn was rejected twice by Thunderstore as still carrying another mod's structure, and its
package was delisted. It was rebuilt in a Docker clean room - no network beyond the API, no repository, no git
history, only a specification and the game's assemblies.

The rebuild worked. Elite Creatures Reborn is listed on Thunderstore again - 3.0.0 published, 3.1.0 packaged as
of 2026-09-17 - which is what the boundary in this file bought, and what a third rejection would cost.

On 2026-09-15 the modstore judged the repository clean and those libraries fine to use. The Docker room is gone;
this file is what replaced it. The repository itself is now the clean room, and these rules travel with it. 
