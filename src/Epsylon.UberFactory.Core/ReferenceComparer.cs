using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    public class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        static ReferenceComparer() { }

        private ReferenceComparer() { }

        private static ReferenceComparer<T> _Instance = new ReferenceComparer<T>();

        public static ReferenceComparer<T> GetInstance() => _Instance;

        public bool Equals(T x, T y) { return Object.ReferenceEquals(x, y); }

        public int GetHashCode(T obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
