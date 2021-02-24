using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChartedPath
{
    public MazeCell[] cells;
    public int[] indices;
    public int travelIndex;
    public bool isLoop;

    public ChartedPath(MazeCell[] cells, int[] indices)
    {
        this.cells = cells;
        this.indices = indices;
        isLoop = cells.Length == indices.Length;
        travelIndex = -1;

#if UNITY_EDITOR
        if (isLoop && cells.Length != indices.Length)
            Debug.LogError("Created incomplete charted path");
#endif
    }

    public (MazeCell cell, int index, bool endOfLoop) GetNext()
    {
        if (travelIndex == -1)
        {
            travelIndex++;
            return (cells[0], -1, false);
        }
        else if (travelIndex < cells.Length - 1)
        {
            var next = travelIndex;
            travelIndex++;
            return (cells[next + 1], indices[next], false);
        }
        else
        {
            var next = travelIndex;
            travelIndex = 0;
            return (cells[0], indices[next], true);
        }
    }

    public MazeCell GetStart() => cells[0];
    
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
        if(indices.Length == cells.Length)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                if (i < indices.Length - 1)
                    tempInt[i] = indices[indices.Length - 2 - i];
                else
                    tempInt[i] = indices[i];

            }
        }
        else
            for (int i = 0; i < indices.Length; i++)
                tempInt[i] = indices[indices.Length - 1 - i];
        


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
