using System.Collections.Generic;

namespace EarthWright.Actions
{
    /// <summary>
    /// A menu entry whose click is not a brush stroke. The handler runs instead of the game's placement: no piece is
    /// placed and the game charges nothing (no stamina, durability or resources); the handler charges its own costs
    /// through the Costs module when it commits work (see <c>Costs.CostApi</c>).
    /// </summary>
    public interface ISpecialAction
    {
        /// <summary>The primary click with the entry selected. <paramref name="ghostPosition"/> is where the game's ghost stands.</summary>
        void OnClick(Player player, ToolAction action, UnityEngine.Vector3 ghostPosition);
    }

    /// <summary>Special action handlers by their <see cref="ToolAction.Special"/> key ("ramp", "road", "clear", "uproot", "custom").</summary>
    public static class SpecialActions
    {
        private static readonly Dictionary<string, ISpecialAction> handlers = new Dictionary<string, ISpecialAction>();

        /// <summary>Registers the handler for a key. For "custom:&lt;id&gt;" keys the handler registered as "custom" is used.</summary>
        public static void Register(string key, ISpecialAction handler) => handlers[key] = handler;

        public static ISpecialAction For(ToolAction action)
        {
            if (action == null || !action.IsSpecial)
                return null;
            if (handlers.TryGetValue(action.Special, out ISpecialAction exact))
                return exact;
            int colon = action.Special.IndexOf(':');
            if (colon > 0 && handlers.TryGetValue(action.Special.Substring(0, colon), out ISpecialAction family))
                return family;
            return null;
        }
    }
}
