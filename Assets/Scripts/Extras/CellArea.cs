using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellArea
{
    public List<MazeCell> cellArea;
    public List<MazeCell> connections;

    public CellArea()
    {
        cellArea = new List<MazeCell>();
        connections = new List<MazeCell>();
    }

    public void AddToArea(MazeCell point)
    {
        cellArea.Add(point);
    }

    public void AddToConnections(MazeCell point)
    {
        connections.Add(point);
    }
}
