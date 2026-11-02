using OpenKeep.Core;

namespace OpenKeep.Store
{
    /// <summary>
    /// What moved ("Moved n stacks", "Sent x to chest", "Stored one x", "Topped up n items", "Sorted n stacks"), shown
    /// only while Move Messages is on. Refusals and "Nothing to move" go through <see cref="Messages"/> and always show.
    /// </summary>
    internal static class MoveMessages
    {
        public static void Center(string text)
        {
            if (StoreSettings.MoveMessages.Value)
                Messages.Center(text);
        }

        public static void TopLeft(string text)
        {
            if (StoreSettings.MoveMessages.Value)
                Messages.TopLeft(text);
        }
    }
}
