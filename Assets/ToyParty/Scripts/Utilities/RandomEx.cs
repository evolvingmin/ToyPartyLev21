using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyParty.Utilities
{
    public static class RandomEx
    {
        /// <summary>
        /// IEnumerable 에서 랜덤하게 하나를 선택 해 반환 합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static T RandomSelect<T>(this IEnumerable<T> enumerable)
        {
            int randomIndex = UnityEngine.Random.Range(0, enumerable.Count());

            return enumerable.ElementAt(randomIndex);
        }
    }
}
