using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Signs
{
    /// <summary>
    /// One <see cref="SignState"/> per loaded container; entries of destroyed containers are pruned lazily, when a new
    /// entry finds the table doubled since the last prune (<see cref="PruneMark"/>), so a big base is not walked per entry.
    /// </summary>
    public static class SignStates
    {
        private static readonly Dictionary<Container, SignState> states = new Dictionary<Container, SignState>();
        private static readonly PruneMark pruneMark = new PruneMark(64);

        public static SignState For(Container container)
        {
            if (states.TryGetValue(container, out SignState state))
                return state;
            if (pruneMark.Due(states.Count))
                Prune();
            state = new SignState();
            states[container] = state;
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
            pruneMark.Pruned(states.Count);
        }
    }
}
