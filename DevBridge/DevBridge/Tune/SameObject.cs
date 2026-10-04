using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DevBridge.Tune
{
    /// <summary>Compares by reference, so two copies holding equal values still count as two objects.</summary>
    internal sealed class SameObject : IEqualityComparer<object>
    {
        internal static readonly SameObject Instance = new SameObject();

        bool IEqualityComparer<object>.Equals(object a, object b) => ReferenceEquals(a, b);

        int IEqualityComparer<object>.GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
    }
}
