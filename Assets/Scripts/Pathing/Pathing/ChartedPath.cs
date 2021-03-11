using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ChartedPath
{
    public MazeCell[] cells;
    public int[] indices;
    public int travelIndex;
    public bool isLoop;

    public ChartedPath(MazeCell[] cells, int[] indices)
    {
        this.cells = cells;
        this.indices = indices;
        isLoop = indices.Length == cells.Length;
        travelIndex = -1;
    }

    public (MazeCell cell, int index) GetNext(MazeCell cell, bool isEntry = false)
    {
        if (cells == null || indices == null)
            return (null, -1);

        bool found = false;

        if (!isEntry)
        {
            if (cell == cells[travelIndex + 1])
                travelIndex++;

            found = true;
        }
        else
        {

            for (int i = 0; i < cells.Length; i++)
                if (cell == cells[i])
                {
                    travelIndex = i;
                    found = true;
                }

            if (!found)
            {
                foreach(var index in cell.GetGraphAreaIndices())
                {
                    for(int i = 0; i < indices.Length; i++)
                        if(index == indices[i])
                        {
                            travelIndex = i;
                            found = true;
                            break;
                        }

                    if (found)
                        break;
                }
            }
        }

        if (travelIndex == cells.Length - 1)
        {
            if (!isLoop)
            {
                travelIndex = -1;
                return (null, -1);
            }
            else
            {
                var next = travelIndex;
                travelIndex = -1;
                return (cells[0], found ? indices[next] : -1);
            }
        }

        return (cells[travelIndex + 1], travelIndex == -1 || !found ? -1 : indices[travelIndex]);
    }

    public MazeCell Start => cells[0];
    public MazeCell End => cells[cells.Length - 1];
    
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

    public void Clear()
    {
        cells = null;
        indices = null;
        travelIndex = -1;
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
