using System;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using PatchGuard;

namespace EliteCreaturesReborn.Breeding
{
    /// <summary>
    /// Carries a newborn's traits across the one game call that creates it: a birth, a hatching, or a young creature
    /// growing up. The breeding patches open it just before the game's call and close it just after. A creature that
    /// wakes inside the call (Awake runs within the instantiate) claims the traits before its controller rolls, and an
    /// egg that wakes inside a birth is held so the birth can write onto it. The traits are worked out only when
    /// something claims them, so a call that creates nothing leaves the parents' records alone. Single-threaded and
    /// scoped to one call, the same shape as the altar's aspect hand-over.
    /// </summary>
    internal static class Lineage
    {
        /// <summary>What a newborn is: its traits, and the biome whose rules it follows - its parent's, not where it stands.</summary>
        public sealed class Newborn
        {
            public readonly CreatureTraits Traits;
            public readonly Heightmap.Biome Biome;

            public Newborn(CreatureTraits traits, Heightmap.Biome biome)
            {
                Traits = traits;
                Biome = biome;
            }
        }

        private static Func<Newborn?>? _source;
        private static bool _watchEggs;

        /// <summary>The egg a birth laid, once it has woken inside the birth; null otherwise.</summary>
        public static ItemDrop? Egg { get; private set; }

        public static void Begin(Func<Newborn?> source, bool watchEggs = false)
        {
            _source = source;
            _watchEggs = watchEggs;
            Egg = null;
        }

        public static void End()
        {
            _source = null;
            _watchEggs = false;
            Egg = null;
        }

        /// <summary>A creature waking inside the call takes the newborn's traits. A failure leaves it to roll as wild.</summary>
        public static void Claim(EliteController controller)
        {
            if (_source == null)
            {
                return;
            }
            try
            {
                Newborn? born = Take();
                if (born != null)
                {
                    controller.ForceTraits(born.Traits, born.Biome);
                }
            }
            catch (Exception e)
            {
                Guard.Report(e, "breeding: newborn traits");
            }
        }

        /// <summary>An item waking inside a birth is the egg the birth is laying.</summary>
        public static void Notice(ItemDrop item)
        {
            if (_watchEggs && _source != null && Egg == null)
            {
                Egg = item;
            }
        }

        /// <summary>Works the newborn's traits out, once; a second call returns null.</summary>
        public static Newborn? Take()
        {
            Func<Newborn?>? source = _source;
            _source = null;
            return source?.Invoke();
        }
    }
}
