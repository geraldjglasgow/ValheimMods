# Changelog

## 0.3.0

- Server sync moved to Charter, our own library. Existing config files work unchanged.
- Version mismatch refuses the join with a named reason and a code in both logs.
- New console command `charter`: `status`, `diff`, `versions`.

## 0.2.1

- New store icon.

## 0.2.0

- Renamed from OathBound to Lockstep. Everything carries the new name: GUID `com.Lockstep`, `com.Lockstep.cfg`,
  `LockstepChain.yml`, `Lockstep.<world>.roster.yml`, console command `lockstep`. Old OathBound files are not
  read — rename them to keep your settings, and remove the old DLL.

## 0.1.4

- Store page links to the GitHub repository.

## 0.1.3

- Chain file validated on load: a stage missing `key` or `boss` is an error and the previous chain stays in
  force, duplicate `order` warns, an empty chain is an error. Every message names the file and path.
- Rebuilt on the rewritten shared configuration libraries.
- MIT licence added.

## 0.1.2

- Fixed a frame rate loss from the shared library's exception-tagging finalizer.
- YAML files with a byte order mark are accepted.

## 0.1.1

- Config and roster hot reload poll every five seconds instead of watching the file, which hung Linux dedicated
  servers in an endless reload loop.
- No warning on first run when the example chain file is written.

## 0.1.0

- First working version: roster, credit by attackers and radius, altar gate with spawn guard, console commands,
  YAML chain, late joiner catch-up, inactive days, mid-playthrough backfill.
