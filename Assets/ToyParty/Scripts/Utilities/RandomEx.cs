using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyParty.Utilities
{
    public static class RandomEx
    {
        public static T RandomSelect<T>(this IEnumerable<T> enumerable)
        {
            int randomIndex = UnityEngine.Random.Range(0, enumerable.Count());

            return enumerable.ElementAt(randomIndex);
        }
    }
}
