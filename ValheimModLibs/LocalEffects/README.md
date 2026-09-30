# LocalEffects

Local, cosmetic copies of the game's effect prefabs: an ice burst, a sound, a lingering glow. A copy is seen and heard
on the machine that made it and nowhere else. It is never networked and never deals damage. Each machine draws its own
from state it already has (a ZDO value, an RPC it received), so effects cost no network traffic.

## Use

```csharp
GameObject? burst = ZNetScene.instance.GetPrefab("vfx_ice_destroyed");
LocalEffect.Flash(burst, position, radius: 2f);                  // one-shot, cleans itself up
LocalEffect.FlashWhole(burst, position, radius: 3f, scale: 1f);  // one-shot, every child system scaled too
LocalEffect.FlashScaled(burst, position, radius: 2f, scale: 0.2f); // one-shot, every part at a fifth of Flash's size
LocalEffect.Sound(ZNetScene.instance.GetPrefab("sfx_ice_destroyed"), position);
LocalEffect.SoundOnly(burst, position, volume: 0.3f);            // the burst's own sound, quieter, nothing drawn
GameObject? aura = LocalEffect.Attach(prefab, creature.transform, position, endless: true, density: 0.5f);
```

- The copy is instantiated with `ZNetView.m_forceDisableInit` set, the way the game makes its own local-only objects,
  so no ZDO is ever made. Never "ghost init": a ghost view still registers a ZDO, and ZNetScene then spawns a full
  networked copy of it on every peer a frame later.
- Area damage (`Aoe`), `Projectile`, `ZSyncTransform` and `ZNetView` are removed and colliders switched off.
- `density` (0 to 1, default 1) thins particle emission and dims lights; 0 spawns nothing. Sounds are never thinned.
  A mod with a per-player effect setting passes it here.
- `SoundOnly(prefab, position, volume)` plays an effect prefab's own sound with nothing of it drawn: particle
  systems emptied and stopped, renderers and lights off, and the game's `LightLod`, `LightFlicker` and `CamShaker`
  disabled before they can light it again or shake the camera. `volume` multiplies each `ZSFX`'s volume modifier
  (the game recomputes the source volume every frame, so a source's own volume would not last), or a bare
  `AudioSource`'s volume.
- `Attach(..., endless: true)` removes the effect's own timer, so it lasts as long as its parent.
- Scaling a copy's transform does not resize most game effects: a particle system in `Local` scaling mode ignores
  its parents' scale. `FlashWhole` switches every system to `Hierarchy` and scales the root. `FlashScaled` leaves the
  modes alone and multiplies each system's own numbers instead (start size, start speed, gravity, the velocity, force,
  limit and noise modules, the emitter shape, the light module's range), plus plain lights' range, the offsets
  between the parts, leaf meshes and any camera shake's range and strength (`ScaleParts`), so every part ends at
  exactly `scale` times what `Flash` draws.
  The game's `LightLod` and `LightFlicker` read a light's range and position as it wakes and restore them later, so
  `ScaleLights` resizes their remembered values too (by reflection).

Consumers: EliteCreaturesReborn (every mutation and aspect effect, through its `CosmeticClone` with the player's
`Effect density`), EliteCreaturesPack (the Rime Giant's ice, frost and avalanche).
