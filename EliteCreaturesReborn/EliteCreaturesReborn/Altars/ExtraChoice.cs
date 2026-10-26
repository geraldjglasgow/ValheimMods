using System.Collections.Generic;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Altars
{
    /// <summary>
    /// The extra aspects a player picks beside a chosen Bountiful in the altar window, in the order they were picked: a
    /// pick past `extra aspects` lets go of the oldest, and an aspect that brings more bodies (Twin, Tethered, Phantom)
    /// lets go of the one already picked, since a boss carries only one of them. The altar's owner checks the picks again
    /// (<see cref="Aspects.AltarPicks.TrimExtras"/>).
    /// </summary>
    internal sealed class ExtraChoice
    {
        private readonly List<Aspect> _order = new List<Aspect>();

        /// <summary>The picks, one bit per aspect value; 0 while they all roll.</summary>
        public int Mask { get; private set; }

        public int Count => _order.Count;

        public bool Holds(Aspect aspect) => (Mask & (1 << (int)aspect)) != 0;

        public void Clear()
        {
            _order.Clear();
            Mask = 0;
        }

        /// <summary>Picks the aspect, or lets it go when it is picked already; at most <paramref name="limit"/> are kept.</summary>
        public void Toggle(Aspect aspect, int limit)
        {
            if (Holds(aspect))
            {
                _order.Remove(aspect);
            }
            else
            {
                if (AspectRoller.BringsBodies(aspect))
                {
                    _order.RemoveAll(AspectRoller.BringsBodies);
                }
                _order.Add(aspect);
                while (_order.Count > limit && _order.Count > 1)
                {
                    _order.RemoveAt(0);
                }
            }
            Mask = Bits();
        }

        /// <summary>The picks the altar holds taken up: those still picked keep their order, new ones follow in catalog
        /// order.</summary>
        public void Adopt(int mask)
        {
            _order.RemoveAll(aspect => (mask & (1 << (int)aspect)) == 0);
            foreach (Aspect aspect in AspectCatalog.InOrder)
            {
                if ((mask & (1 << (int)aspect)) != 0 && !_order.Contains(aspect))
                {
                    _order.Add(aspect);
                }
            }
            Mask = Bits();
        }

        private int Bits()
        {
            int mask = 0;
            foreach (Aspect aspect in _order)
            {
                mask |= 1 << (int)aspect;
            }
            return mask;
        }
    }
}
