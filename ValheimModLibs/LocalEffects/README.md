# LocalEffects

Local, cosmetic copies of the game's effect prefabs: an ice burst, a sound, a lingering glow. A copy is seen and heard
on the machine that made it and nowhere else. It is never networked and never deals damage. Each machine draws its own
from state it already has (a ZDO value, an RPC it received), so effects cost no network traffic.

## Use

```csharp
GameObject? burst = ZNetScene.instance.GetPrefab("vfx_ice_destroyed");
LocalEffect.Flash(burst, position, radius: 2f);                  // one-shot, cleans itself up
LocalEffect.FlashWhole(burst, position, radius: 3f, scale: 1f);  // one-shot, every child system scaled too
LocalEffect.Sound(ZNetScene.instance.GetPrefab("sfx_ice_destroyed"), position);
GameObject? aura = LocalEffect.Attach(prefab, creature.transform, position, endless: true, density: 0.5f);
```

- The copy is instantiated with `ZNetView.m_forceDisableInit` set, the way the game makes its own local-only objects,
  so no ZDO is ever made. Never "ghost init": a ghost view still registers a ZDO, and ZNetScene then spawns a full
  networked copy of it on every peer a frame later.
- Area damage (`Aoe`), `Projectile`, `ZSyncTransform` and `ZNetView` are removed and colliders switched off.
- `density` (0 to 1, default 1) thins particle emission and dims lights; 0 spawns nothing. Sounds are never thinned.
  A mod with a per-player effect setting passes it here.
- `Attach(..., endless: true)` removes the effect's own timer, so it lasts as long as its parent.

Consumers: EliteCreaturesReborn (every mutation and aspect effect, through its `CosmeticClone` with the player's
`Effect density`), EliteCreaturesPack (the Rime Giant's ice, frost and avalanche).
