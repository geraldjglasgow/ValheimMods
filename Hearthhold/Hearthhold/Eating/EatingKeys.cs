namespace Hearthhold
{
    /// <summary>The names the Eating feature stores. Every one starts with "hearthhold_".</summary>
    public static class EatingKeys
    {
        /// <summary>
        /// Player custom data key prefix: plus an active food's prefab name, the stars of the dish eaten for it
        /// (<see cref="FoodStars"/>).
        /// </summary>
        public const string ActiveFood = "hearthhold_food_";
    }
}
