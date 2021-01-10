using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeCellWall : MazeCellEdge
{
    public bool IsPassable { get; private set; }

    [SerializeField]
    PerObjectMaterialProperties props;

    public void SetSpecialColor()
    {
        props.SetSecondaryColor();
    }

    public void AddSpecialPassage()
    {
        cellA.specialConnectedCells.Add(cellB);
        cellB.specialConnectedCells.Add(cellA);
        IsPassable = true;
        SetSpecialColor();
    }

    public MazeCell CheckCell(MazeCell cell)
    {
        if (cell == cellA)
            return cellB;
        else if (cell == cellB)
            return cellA;
        else
            return null;
    }
}
