# Catalogue: the game's effects to reuse

Often the best new effect is one of the game's own, cloned on each machine, recoloured and resized: it matches the game
exactly, costs nothing in the bundle and updates with the game. Every name below was checked in the reference export and
in the game's ZNetScene prefab list (4,644 prefabs, 768 of them `fx_`/`vfx_`). Build a new effect in `AssetWorkshop/vfx`
only when nothing here fits.

## How to reach one

- **Registered (most):** `ZNetScene.instance.GetPrefab("vfx_RockHit")`.
- **A child of a registered prefab** (the fires and torches are pasted into their pieces):
  `GameMaterials.Find(ZNetScene.instance.GetPrefab("piece_groundtorch").transform, "fx_Torch_Basic").gameObject`;
  instantiating that child copies just its subtree.
- **In an EffectList** (not registered on its own): `Player`'s `m_skillLevelupEffects` holds `vfx_skilllevelup`; a
  character's `m_hitEffects`, `m_critHitEffects`, `m_backstabHitEffects`, `m_deathEffects`, `m_waterEffects`,
  `m_tarEffects`; a humanoid's `m_perfectBlockEffect`, `m_pickupEffects`, `m_consumeItemEffects`; an item's
  `m_itemData.m_shared.m_hitEffect`, `m_blockEffect`, `m_triggerEffect`. Read `m_effectPrefabs[i].m_prefab`.

## How to use one

- **Local only:** `LocalEffect.Flash(prefab, position, radius)`, `FlashScaled(prefab, position, radius, scale)` (every
  part at `scale` times, including lights and the camera shake), `Attach(prefab, parent, position, endless: true)` for
  loops. The copy has no ZNetView, Aoe or Projectile and draws only on the machine that makes it; make it on every peer
  from shared state.
- **Recolour:** make a copy under the prefab bench (`PrefabBench.Copy(prefab, "mymod_frost_burning")`) once, then
  `EffectTint.Shift(copy, hue, saturation, value)`: particles' start colours, colour over lifetime, the gradient-mapped
  fire's two custom colours, trails, lights and material tints all turn together. A hue of 0.33 turns the game's
  orange fire green; 0.55 turns it blue. Pass the copy to LocalEffects as the prefab.
- **Thin:** LocalEffects' `density` (0 to 1) thins emission and dims lights, for a per-player effects setting.
- **Never** instantiate a game effect with its ZNetView outside the game's own code: it makes a real networked copy.

## By purpose

### Fire and heat
| Effect | Where | What it is |
| --- | --- | --- |
| `fx_Torch_Basic` | child of `piece_groundtorch` (`_enabled/fx_Torch_Basic`) | small gradient-mapped flame (20 a second, 0.25 to 0.33 m) and a 2 m orange halo; no light (the piece has it) |
| `fx_Torch_Green`, `fx_Torch_Blue` | children of `piece_groundtorch_green`, `_blue` | the same in green and blue: the game's own recolour |
| `fx_Torch_Carried` | child of `Torch` (`attach/equiped/fx_Torch_Carried`) | carried torch: flames, local flames, smoke, embers, halo |
| `fx_BonfireFlames` | child of `bonfire` (`_enabled_high`) | big fire from a mesh emitter, 200 flames a second, sparks, smoke |
| `vfx_Burning`, `vfx_Burning_blue`, `vfx_Burning_green` | registered | the burning status on a body: pixel flames, gradient flames, cinders, halo, light (1.0, 0.69, 0.46) 8 m; blue and green variants with a blue light (0.09, 0.66, 0.86) 5 m |
| `fx_CinderFire_Burn` | registered | a small ground fire (Ashlands cinders) |
| `fx_fenring_burning_hand` | registered | flames and sparks round a hand, light (0.96, 0.61, 0.5) 6 m |
| `vfx_FireAddFuel`, `vfx_HearthAddFuel` | registered | a one-second flare-up: pixel flames, flipbook flames, smoke |

### Impacts
| Effect | What it is |
| --- | --- |
| `vfx_HitSparks` | metal: 30 fast stretched sparks and a small gold puff, 0.5 s |
| `vfx_RockHit`, `vfx_MarbleHit` | stone: 100 grey lit chips and 8 rock mesh chips |
| `vfx_SawDust` | wood: 200 yellow-brown chips and a puff |
| `vfx_clubhit` | a blunt thud: one dust puff |
| `vfx_arrowhit`, `vfx_ProjectileHit` | small chip bursts for arrows and thrown things |
| `vfx_blocked`, `vfx_perfectblock` | a shield block: chips and a ring |
| `vfx_ice_hit`, `fx_iceshard_hit`, `vfx_frostarrow_hit`, `vfx_ColdBall_Hit`, `fx_DvergerMage_Ice_hit` | frost: pale cyan lit bits, ice chunks, blue mist; light (0.455, 0.842, 1.0) 3 to 5 m |
| `vfx_poisonarrow_hit` | poison: green bits and mist, light (0.41, 1.0, 0.27) 3 m |
| `fx_lightningweapon_hit`, `fx_chainlightning_hit` | lightning: bits, smoke, a ring and ribbons, light (0.23, 0.78, 1.0), camera shake |
| `vfx_FireballHit`, `fx_DvergerMage_Fire_hit` | fire: pixelated fireballs and smoke |
| `fx_bloodweapon_hit`, `fx_natureweapon_hit` | blood-magic and nature weapon hits |
| `vfx_BloodHit` | flesh: red blobs, drops, chunks, a splash and a ground decal |
| `fx_crit`, `fx_backstab` | the critical and sneak-attack bursts |

### Deaths, spawns, summons
| Effect | What it is |
| --- | --- |
| `vfx_skeleton_death` | bone-dust column, grey chips, bone and skull meshes in the Skeleton's material |
| `vfx_ghost_death`, `vfx_wraith_death` | dark smoke shot outward, glowing specks, a light; ghostly deaths |
| `vfx_greydwarf_death` | yellow-brown sap blobs, clouds and decals |
| `vfx_BloodDeath`, `vfx_blob_death` | blood, slime |
| `vfx_spawn`, `vfx_spawn_large`, `vfx_spawn_small` | magenta pixel puffs and cinders with a magenta light 15 m (small: 3 m) |
| `fx_summon_skeleton`, `fx_summon_spirit_spawn` | purple summons: debris, glowing bits, a light 5 m, camera shake |
| `vfx_DraugrSpawn`, `vfx_prespawn`, `vfx_odin_despawn` | rising from the ground, a warning before a spawn, Odin's leaving |

### Explosions and slams
| Effect | What it is |
| --- | --- |
| `fx_dynamite_explosion` | the big one: dark smoke, flame spikes, flipbook fireball, fast sparks, hanging glow, 15 m refraction |
| `fx_fireball_staff_explosion` | fire staff: sparks, smoke, flames, debris, light (1.0, 0.71, 0.3) 10 m intensity 4, shake |
| `vfx_BombBlob_explode_frost`, `_poison`, `_lava` | ooze bombs: slime, smoke and bits in the ooze's colour |
| `fx_fenring_icenova`, `fx_DvergerMage_Nova_ring` | frost novas: a ring, bits and mist, light (0.455, 0.82, 1.0) |
| `vfx_GodExplosion` | Eikthyr's: embers, smoke, ribbons, light (0.66, 1.0, 0.32) |
| `vfx_troll_groundslam`, `vfx_gdking_stomp`, `fx_eikthyr_stomp` | slams: a ring of dust shot along the ground, chips, rock meshes; Eikthyr's blue with a 20 m light |

### Status effects and auras (loop them with `Attach(..., endless: true)`)
| Effect | What it is |
| --- | --- |
| `vfx_Poison` | green smoke round the body |
| `vfx_Frost`, `vfx_Cold`, `vfx_Freezing` | pale blue mist, glints, snow |
| `vfx_Wet` | 20 drips a second |
| `vfx_Smoked`, `vfx_Slimed`, `vfx_Tared` | smoke, slime drips, tar drips |
| `vfx_MeadStrength`, `fx_Potion_frostresist` | a mead's buff: ribbons, embers, a glow, light 4 m in the mead's colour |
| `fx_Adrenaline1` | a warm pulse with refraction |
| `vfx_TrollPheromones` | a blue aura with ribbons, embers and a glow, light (0.35, 0.82, 1.0) 6 m |
| `fx_shaman_protect` | a shield burst: 1,000 blue dots on a sphere, a shockwave and a dome, light 10 m |
| `vfx_StaffShield` | the staff shield's glow, light (1.0, 0.3, 0.37) 5 m |

### Level-ups, pickups, love, placement, steps, water
| Effect | Where | What it is |
| --- | --- | --- |
| `vfx_skilllevelup` | `Player.m_skillLevelupEffects` | gold glow, 200 rising cinders, ribbons, light (1.0, 0.87, 0.32) 4 m |
| `vfx_HealthUpgrade`, `vfx_StaminaUpgrade` | registered | red or yellow swirl round the player for two seconds, light 6 m, shake |
| `vfx_pickable_pick`, `vfx_lootspawn`, `vfx_offering` | registered | a small beige puff; a pixel puff; an offering's pink glow |
| `fx_creature_tamed` | registered | 30 gold hearts rising |
| `vfx_Place_*` (42) | registered | a dust cloud from a box the size of the piece: `vfx_Place_wood_wall` 2 x 2 x 0.2 m, `vfx_Place_wood_pole` 0.3 x 1 x 0.3, `vfx_Place_stone_wall_2x1` 2 x 1 x 1 shell, `vfx_Place_workbench` 3 x 1 x 1 |
| `fx_footstep_*` | characters' FootStep effect lists | beige dust puffs; snow, mud, ash, water variants |
| `fx_WaterImpact_Big`, `vfx_WaterImpact_Karve`, `fx_float_hitwater` | registered | foam sheets and drops; a small splash |

### Ambient and weather
`FireFlies` (blue specks with a light), `Flies`, `vfx_swamp_mist`, `vfx_mistlands_mist`, `vfx_darkland_groundfog`: all
registered and looping. The weather lives in `Systems/_Environment.prefab` under `FollowPlayer` and is driven by the
game's environment manager; do not copy it.

### Camera shakes
`fx_hit_camshake`, `fx_swing_camshake`, `fx_block_camshake`, `fx_damage_camshake` are a CamShaker and a timer only;
they sit in weapons' and creatures' EffectLists (for example the Dverger staffs' projectiles).
