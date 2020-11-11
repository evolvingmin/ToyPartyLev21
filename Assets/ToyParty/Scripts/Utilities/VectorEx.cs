using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToyParty.Utilities
{
    public static class VectorEx
    {
        // https://stackoverflow.com/questions/13640931/how-to-determine-if-a-vector-is-between-two-other-vectors
        public static bool IsWithinAB(this Vector3 c, Vector3 a, Vector3 b)
        {
            return (Vector3.Dot(Vector3.Cross(a, c),Vector3.Cross(a, b)) >= 0 && Vector3.Dot(Vector3.Cross(b, c),Vector3.Cross(b, a)) >= 0);
        }
    }
}
