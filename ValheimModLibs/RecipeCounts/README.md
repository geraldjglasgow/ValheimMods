# RecipeCounts

Starred eggs, and any other item of a single quality, count for recipes.

- `AnyQuality.Install(harmony)`: once in the mod's `Awake`. Installs the patches once per merged copy.

The game's private `Player.HaveRequirementItems` counts each ingredient at every quality from 1 to the item's maximum
quality and keeps the largest. An egg's maximum quality is 1, but it carries its hen's level as its quality (a one-star
hen lays quality 2 eggs, which hatch one-star chicks), so a starred egg never counted for a recipe, although crafting
takes eggs of any quality. While that check runs for a recipe that needs every ingredient, `Inventory.CountItems(name, 1)`
for an item whose maximum quality is 1 counts every unit, in any inventory (a mod that counts chest contents through
`CountItems` gets them too). Recipes that need only one of their ingredients keep the game's count: they pay from one
quality the game picks.

```csharp
AnyQuality.Install(harmony);
```

Merged into the consuming mod with ILRepack like every library here. Consumers: EliteCreaturesReborn (its eggs carry
the stars its hens have), GrindstoneSkills (starred eggs, and Hearthhold's starred food through its stars).
