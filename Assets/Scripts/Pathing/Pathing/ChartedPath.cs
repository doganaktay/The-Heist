using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ChartedPath
{
    public MazeCell[] cells;
    public int[] indices;
    public int travelIndex;

    public ChartedPath(MazeCell[] cells, int[] indices)
    {
        this.cells = cells;
        this.indices = indices;
        travelIndex = -1;

#if UNITY_EDITOR
        if (cells.Length != indices.Length)
            Debug.LogError("Created incomplete charted path");
#endif
    }

    public (MazeCell cell, int index, bool endOfLoop) GetNext()
    {
        if (travelIndex == -1)
            return (cells[++travelIndex], -1, false);
        else if (travelIndex < cells.Length - 1)
            return (cells[travelIndex], indices[travelIndex++], false);
        else
        {
            var next = travelIndex;
            travelIndex = 0;
            return (cells[next], indices[next], true);
        }
    }

    public int GetIndex(MazeCell start, MazeCell end)
    {
        for(int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == start && cells[(i + 1) % cells.Length] == end)
                    return indices[i];
        }

        return -1;
    }

    public void ReversePath()
    {
        var temp = new MazeCell[cells.Length];
        for(int i = 0; i < cells.Length; i++)
            temp[i] = cells[cells.Length - 1 - i];

        cells = temp;

        var tempInt = new int[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            if (i < indices.Length - 1)
                tempInt[i] = indices[indices.Length - 2 - i];
            else
                tempInt[i] = indices[i];

        }

        indices = tempInt;

        travelIndex = 0;
    }

#if UNITY_EDITOR

    public void DebugPath()
    {
        string str = "Charted Path: ";

        for(int i = 0; i < cells.Length; i++)
        {
            str += cells[i].gameObject.name + " - " + indices[i] + " - ";
        }

        Debug.Log(str);
    }

#endif
}
