using PackPanel.Core;

namespace PackPanel.Consume
{
    /// <summary>The $packpanel_ words of the Food Key and the Mead Key.</summary>
    public static class ConsumeWords
    {
        public static string NothingToEat { get; private set; }
        public static string NothingToDrink { get; private set; }

        public static void Register()
        {
            NothingToEat = Language.Add("packpanel_nothingtoeat", "Nothing in your food slots can be eaten now");
            NothingToDrink = Language.Add("packpanel_nothingtodrink", "Nothing in your mead slots can be drunk now");
        }
    }
}
