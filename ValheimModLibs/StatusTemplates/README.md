# StatusTemplates

A mod's own status effects, written one way: the template made once and kept loaded, the copy on a character found,
held or dropped, icons borrowed from the game, and the effects a hit carries put in every object database.

- `StatusTemplate<T>(name, setup, icon)`: one effect. `Get()` makes the template on first use (named, kept loaded,
  `setup` run once) and asks `icon` again while the template has none. `On(character)`, `Has`, `Add(character,
  resetTime)`, `Hold` (the copy there or a new one), `Drop` (quiet). `Name`, `Hash`.
- `StatusTemplate.Make<T>(name)`: a bare effect object, named and kept loaded, for a mod that builds its own set.
- `StatusTable.Register(harmony, effects...)`: effects that travel by name hash (a hit's `m_statusEffectHash`, an attack
  item's status effect) in the live `ObjectDB` now and every one the game wakes or copies later (postfixes on
  `ObjectDB.Awake` and `CopyOtherDB`, once per merged copy). Registering the same effect again changes nothing.
  `StatusTable.Put(list, effect)` puts one in a list: a stale entry of its name is replaced, never added twice.
- `GameIcons.Item(prefabNames...)`, `GameIcons.Effect(hashes...)`, `GameIcons.Effect(names...)`: the first icon found
  in the live object database, null while there is none.

Why the template is kept loaded: `SEMan.AddStatusEffect(effect)` adds `effect.Clone()`, a MemberwiseClone that shares
the template's native object, so the template must outlive every copy (`HideFlags.DontUnloadUnusedAsset`). The game
refuses a second copy of a name (`Add` returns null, and with `resetTime` the one there starts again). An effect only
handed to a character as an object needs no place in `ObjectDB`; the game's `ObjectDB.GetStatusEffect` walks the list
and `Awake` indexes nothing, so a postfix registers in time.

```csharp
static readonly StatusTemplate<StatusEffect> Ringing = new StatusTemplate<StatusEffect>("mymod_ringing",
    se => (se.m_name, se.m_tooltip) = ("$mymod_ringing", "$mymod_ringing_tip"),
    () => GameIcons.Item("Bell", "Wishbone"));

Ringing.Hold(player);        // local: on this machine's player, never registered
StatusTable.Register(harmony, Slow.Get);   // carried by hits: in every ObjectDB on every peer
```

Merged into the consuming mod with ILRepack like every library here. Consumers: EliteCrafting (the aggregate, its
indicators, Hamstring, Numbing Blow), EliteCreaturesReborn (Chilled, Ringing ears, the trails, the Bat-fiend's two),
EliteCreaturesPack (the kraken's ink), GrindstoneSkills (Defense's icons, Breath), EliteEquipment (`Make` and `Put` for
the casters' set, registered in its own `ObjectDB.Awake` prefix beside its items).
