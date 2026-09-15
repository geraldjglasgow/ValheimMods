using System.Collections.Generic;

namespace OpenKeep.Signs
{
    /// <summary>One <see cref="SignState"/> per loaded container; entries of destroyed containers are pruned lazily.</summary>
    public static class SignStates
    {
        private const int PruneAbove = 64;
        private static readonly Dictionary<Container, SignState> states = new Dictionary<Container, SignState>();

        public static SignState For(Container container)
        {
            if (states.TryGetValue(container, out SignState state))
                return state;
            state = new SignState();
            states[container] = state;
            if (states.Count > PruneAbove)
                Prune();
            return state;
        }

        private static void Prune()
        {
            List<Container> gone = new List<Container>();
            foreach (Container key in states.Keys)
            {
                if (key == null)
                    gone.Add(key);
            }
            foreach (Container key in gone)
                states.Remove(key);
        }
    }
}
