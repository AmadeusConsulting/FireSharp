using System.Collections.Generic;
using System.Linq;

namespace FireSharp.GeoFire
{
    internal class StringArrayEqualityComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[] x, string[] y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            if(x.Length != y.Length)
            {
                return false;
            }

            return !x.Where((t, i) => t != y[i]).Any();
        }

        public int GetHashCode(string[] obj)
        {
            if (obj != null)
            {
                return obj.GetHashCode();
            }
            return 0;
        }
    }
}