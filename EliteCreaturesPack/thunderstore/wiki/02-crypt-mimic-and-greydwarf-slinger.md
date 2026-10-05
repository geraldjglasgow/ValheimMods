# Crypt Mimic and Greydwarf Slinger

## Crypt Mimic

A crypt chest that bites. It looks exactly like the chest it replaced until someone opens or hits it.

- **Where:** in crypts generated after the mod is installed, each listed chest has a 15% chance to be a mimic. By
  default: Black Forest burial chambers and Swamp sunken crypts. Hildir's crypt chest and the chest also found in
  Ashlands fortresses (`TreasureChest_fCrypt`) are never mimics.
- **Disguised:** same model, name and "[E] Open" as the chest; no name plate, health bar or (with Elite Creatures
  Reborn) stars until it wakes. Only **opening** or **hitting** it wakes it.
- **Ambush:** open it and the lid snaps on you for 20 slash that **cannot be dodged or blocked**. Tip: hit a chest you
  distrust before opening it, which wakes a mimic without the free bite.
- **Lunge**, its only attack: the lid gapes, it leaps about 2.5 m and snaps. **Roll** to make it miss; a shield does
  **not** stop it. It bites at most every 3 seconds and bounds after you a little faster than a skeleton runs.
- **Weak spot:** after each snap it hangs open for a moment. Hit it then.
- **Stats:** 40 health; weak to blunt and fire; resists pierce and frost; immune to poison. Undead: crypt skeletons
  and draugr leave it alone.
- **Loot:** a fresh roll of the replaced chest's loot. Epic Loot needs an `ECP_CryptMimic` entry to give it magic
  items. With EliteCrafting it drops runes and magic gear at a crypt chest's odds (30% and 10% in a burial chamber).

### Settings: `2 - Crypt Mimic`

| Key | Default | Meaning |
| --- | --- | --- |
| Enabled | true | Mimics can appear |
| Chance | 15 | Percent of listed chests that are mimics |
| Chests | TreasureChest_forestcrypt, TreasureChest_sunkencrypt | Chests that may be mimics |
| Bite Cooldown | 3 | Seconds between bites |
| Bite Damage | 20 | Slash damage of the ambush and lunge |
| Run Speed | 5 | Chase speed (a skeleton: 4) |

## Greydwarf Slinger

A greydwarf with a slingshot. It stands and shoots like a skeleton archer and never claws.

- **Where:** 10% of Greydwarfs spawning in the Black Forest, in the wild and from greydwarf nests. Greydwarf camps
  keep their greydwarfs.
- **The fight:** it walks into range (22 m) and sight, then stands and shoots a stone every 3.5 seconds at most,
  down to point blank: 12 blunt on a low arc at where you stood. It never melees.
- **Beating it:** it does not lead a moving target. Run across its line, roll or block. Up close it still shoots but
  has no claws.
- **Stats:** a Greydwarf's (40 health, same resistances).
- **Loot:** a Greydwarf's drops plus 2-3 Stone. With EliteCrafting, runes and magic gear as from any Black Forest creature.

### Settings: `3 - Greydwarf Slinger`

| Key | Default | Meaning |
| --- | --- | --- |
| Enabled | true | Slingers can appear |
| Share | 10 | Percent of greydwarfs that are slingers |
| Nests | true | Greydwarf nests spawn them too |
| Biomes | BlackForest | Biomes where they can appear |
| Shot Interval | 3.5 | Least seconds between shots |
| Stone Damage | 12 | Blunt damage of a stone |
| Stone Speed | 16 | Stone speed in m/s (a greydwarf's thrown rock: 12) |
| Range | 22 | Shooting range in metres |
