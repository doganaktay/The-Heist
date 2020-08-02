using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface WeightedGraph<L>
{
    double Cost(IntVector2 a, IntVector2 b);
    IEnumerable<IntVector2> Neighbors(IntVector2 pos);
}
