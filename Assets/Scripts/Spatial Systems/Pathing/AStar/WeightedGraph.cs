using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface WeightedGraph //<L>
{
    float Cost(MazeCell a, MazeCell b);
    //IEnumerable<IntVector2> Neighbours(IntVector2 pos);
}
