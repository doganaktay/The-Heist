using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeCellWall : MazeCellEdge
{
    public bool IsPassable { get; private set; }

    public void AddSpecialPassage()
    {
        cellA.specialConnectedCells.Add(cellB);
        cellB.specialConnectedCells.Add(cellA);
        IsPassable = true;
    }
}
