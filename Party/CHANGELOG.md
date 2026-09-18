# Changelog

## 0.1.0

- First release: party membership (server-owned roster, one leader, size cap, automatic leadership succession,
  parties survive logout and server restart), invites with a timeout and an accept/decline prompt, `/party`
  commands with short aliases where free (`/invite`, `/leave`, `/remove`, `/promote`, `/p`), party chat with a
  toggle mode, a draggable/configurable health panel, colored floating names, always-on party map/minimap pins,
  friendly-fire protection, party-only map pings (hold a modifier key), and a public API for other mods.
- Gameplay settings (party size, friendly fire, invite timeout, vitals update rate) are server-synced and
  lockable; display settings (colors, panel layout, ping key) are always local to each player.
- Death notices (chat line + temporary map pin), distance and off-screen direction arrows on the health panel,
  named parties (`/party name`), and an admin `/party status` command.
