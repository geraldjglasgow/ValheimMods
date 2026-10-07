# Changelog

## 0.4.1

- Store page: a fuller overview of what the mod does.

## 0.4.0

- The server confirms a boss really died before giving credit; unconfirmed kill reports are refused and logged.
- Less work updating the altars' waiting list when players join, leave or earn credit.
- Update the server and every client together.

## 0.3.4

- Discord link on the store page.

## 0.3.3

- Faster loading: the config file is written once instead of once per setting.

## 0.3.2

- Less work every frame keeping the server's settings in sync.

## 0.3.1

- Fixed: joining a busy server no longer refuses a correct client.
- Shorter store page.

## 0.3.0

- Server sync moved to Charter, our own library; config files work unchanged.
- A version mismatch refuses the join with a named reason and a code in both logs.
- New console command `charter` (`status`, `diff`, `versions`).

## 0.2.1

- New store icon.

## 0.2.0

- Renamed from OathBound to Lockstep: GUID `com.Lockstep`, `com.Lockstep.cfg`, `LockstepChain.yml`,
  `Lockstep.<world>.roster.yml` and console command `lockstep`.
- Old OathBound files are not read: rename them to keep your settings, and remove the old DLL.

## 0.1.4

- Store page links to the GitHub repository.

## 0.1.3

- The chain file is checked on load: a stage without `key` or `boss`, or an empty chain, keeps the previous chain.
- A duplicate `order` warns; every message names the file and path.
- Rebuilt on the rewritten shared config libraries; MIT licence added.

## 0.1.2

- Fixed: a frame rate loss.
- YAML files with a byte order mark are accepted.

## 0.1.1

- Fixed: Linux dedicated servers hung in an endless config and roster reload.
- No warning on first run when the example chain file is written.

## 0.1.0

- First working version: player roster, credit by attackers and radius, altar gate with spawn guard, YAML chain.
- Console commands, late joiner catch-up, inactive days and mid-playthrough backfill.
