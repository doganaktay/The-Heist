﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class AreaFinder : MonoBehaviour
{
    MazeCell[,] grid;
    public Maze maze;

    List<List<MazeCell>> lowAreas = new List<List<MazeCell>>();
    int lowAreaCount;
    List<List<MazeCell>> highAreas = new List<List<MazeCell>>();
    int highAreaCount;
    bool allDropped = false;

    Dictionary<int, List<MazeCell>> lowCellAreas = new Dictionary<int, List<MazeCell>>();
    Dictionary<int, List<MazeCell>> highCellAreas = new Dictionary<int, List<MazeCell>>();

    Dictionary<int, List<MazeCell>> lowCellConnected = new Dictionary<int, List<MazeCell>>();
    Dictionary<int, List<MazeCell>> highCellConnected = new Dictionary<int, List<MazeCell>>();

    public void FindAreas()
    {
        ResetGrid();
        NewDetermineAreas();
    }

    public void ResetGrid()
    {
        if(grid == null)
            grid = new MazeCell[maze.size.x, maze.size.y];

        for (int i = 0; i < maze.size.x; i++)
        {
            for (int j = 0; j < maze.size.y; j++)
            {
                grid[i, j] = maze.cells[i, j];
            }
        }
    }

    // GRID SEARCH IMPLEMENTING CCL

    public void NewDetermineAreas()
    {

        lowCellAreas.Clear();
        highCellAreas.Clear();
        lowCellConnected.Clear();
        highCellConnected.Clear();

        int[,] labels = new int[maze.size.x, maze.size.y];
        List<HashSet<int>> linkedLow = new List<HashSet<int>>();
        int nextLabelLow = 0;
        List<HashSet<int>> linkedHigh = new List<HashSet<int>>();
        int nextLabelHigh = 0;

        for (int j = 0; j < maze.size.x; j++)
        {
            for (int i = 0; i < maze.size.y; i++)
            {
                grid[j, i].searched = false;

                int k = i - 1;
                int h = j - 1;

                if (grid[j, i].state == 0)
                {
                    HashSet<int> neighbors = new HashSet<int>();

                    if (k >= 0 && grid[j, k].state == 0 && grid[j,k].connectedCells.Contains(grid[j,i]))
                        neighbors.Add(labels[j, k]);
                    if (h >= 0 && grid[h, i].state == 0 && grid[h, i].connectedCells.Contains(grid[j, i]))
                        neighbors.Add(labels[h, i]);

                    if (neighbors.Count == 0)
                    {
                        linkedLow.Add(new HashSet<int> { nextLabelLow });
                        labels[j, i] = nextLabelLow;
                        nextLabelLow++;
                    }
                    else
                    {
                        HashSet<int> neighborLabels = new HashSet<int>();
                        foreach (int n in neighbors)
                        {
                            foreach (int a in linkedLow[n])
                            { neighborLabels.Add(a); }
                        }

                        labels[j, i] = neighborLabels.Min();
                        foreach (int label in neighborLabels)
                        {
                            linkedLow[label].UnionWith(neighborLabels);
                        }
                    }

                }
                else
                {
                    HashSet<int> neighbors = new HashSet<int>();

                    if (k >= 0 && grid[j, k].state == 1 && grid[j, k].connectedCells.Contains(grid[j, i]))
                        neighbors.Add(labels[j, k]);
                    if (h >= 0 && grid[h, i].state == 1 && grid[h, i].connectedCells.Contains(grid[j, i]))
                        neighbors.Add(labels[h, i]);

                    if (neighbors.Count == 0)
                    {
                        linkedHigh.Add(new HashSet<int> { nextLabelHigh });
                        labels[j, i] = nextLabelHigh;
                        nextLabelHigh++;
                    }
                    else
                    {
                        HashSet<int> neighborLabels = new HashSet<int>();
                        foreach (int n in neighbors)
                        {
                            foreach (int a in linkedHigh[n])
                            { neighborLabels.Add(a); }
                        }

                        labels[j, i] = neighborLabels.Min();
                        foreach (int label in neighborLabels)
                        {
                            linkedHigh[label].UnionWith(neighborLabels);
                        }
                    }

                }
            }
        }

        for (int j = 0; j < maze.size.x; j++)
        {
            for (int i = 0; i < maze.size.y; i++)
            {
                if (grid[j, i].state == 0)
                {
                    labels[j, i] = linkedLow[labels[j, i]].Min();
                    grid[j, i].areaIndex = labels[j, i];
                    grid[j, i].cellText.text = labels[j, i].ToString();

                    if (!lowCellAreas.ContainsKey(labels[j, i]))
                    {
                        lowCellAreas.Add(labels[j, i], new List<MazeCell>());
                        lowCellAreas[labels[j, i]].Add(grid[j, i]);
                    }
                    else
                        lowCellAreas[labels[j, i]].Add(grid[j, i]);
                }
                else
                {
                    labels[j, i] = linkedHigh[labels[j, i]].Min();
                    grid[j, i].areaIndex = labels[j, i];
                    grid[j, i].cellText.text = labels[j, i].ToString();

                    if (!highCellAreas.ContainsKey(labels[j, i]))
                    {
                        highCellAreas.Add(labels[j, i], new List<MazeCell>());
                        highCellAreas[labels[j, i]].Add(grid[j, i]);
                    }
                    else
                        highCellAreas[labels[j, i]].Add(grid[j, i]);
                }
            }
        }

        DeterminePaths();
        //SetPaths();
    }

    void DeterminePaths()
    {
        foreach (var lowCellArea in lowCellAreas)
        {
            lowCellConnected.Add(lowCellArea.Key, new List<MazeCell>());
            //SearchNeighbours(lowCellConnected[lowCellArea.Key], lowCellArea.Value[0]);
        }

        foreach (var highCellArea in highCellAreas)
        {
            highCellConnected.Add(highCellArea.Key, new List<MazeCell>());
            SearchNeighbours(highCellConnected[highCellArea.Key], highCellArea.Value[0]);
        }

        SetPaths();

        foreach (var connectionCellBlock in highCellConnected)
        {
            foreach (var connectionCell in connectionCellBlock.Value)
            {
                connectionCell.cellText.color = Color.magenta;
            }
        }

        foreach(var connectionCellBlock in lowCellConnected)
        {
            foreach(var connectionCell in connectionCellBlock.Value)
            {
                connectionCell.cellText.color = Color.yellow;
            }
        }


        Debug.Log("Low cell area count: " + lowCellConnected.Count + " High cell area count: " + highCellConnected.Count);
    }

    void SetPaths()
    {
        for (int j = 0; j < maze.size.x; j++)
        {
            for (int i = 0; i < maze.size.y; i++)
            {
                if (grid[j, i].state == 0)
                {
                    grid[j, i].cellText.color = Color.red;
                }
                else
                {
                    grid[j, i].cellText.color = Color.white;
                    grid[j, i].cellText.text = grid[j, i].distanceFromStart.ToString();
                }   
            }
        }
    }

    void SearchNeighbours(List<MazeCell> connectedCells, MazeCell point)
    {
        int k = point.row - 1;
        int h = point.col - 1;
        int l = point.row + 1;
        int m = point.col + 1;

        grid[point.row, point.col].searched = true;

        if (m < maze.size.y && !grid[point.row, m].searched && grid[point.row, point.col].connectedCells.Contains(grid[point.row, m]))
        {
            if (grid[point.row, point.col].state == grid[point.row, m].state)
                SearchNeighbours(connectedCells, grid[point.row, m]);
            else
            { connectedCells.Add(grid[point.row, m]); lowCellConnected[grid[point.row, m].areaIndex].Add(grid[point.row, point.col]); }
        }

        if (l < maze.size.x && !grid[l, point.col].searched && grid[point.row, point.col].connectedCells.Contains(grid[l, point.col]))
        {
            if (grid[point.row, point.col].state == grid[l, point.col].state)
                SearchNeighbours(connectedCells, grid[l, point.col]);
            else
            { connectedCells.Add(grid[l, point.col]); lowCellConnected[grid[l, point.col].areaIndex].Add(grid[point.row, point.col]); }
        }

        if (h >= 0 && !grid[point.row, h].searched && grid[point.row, point.col].connectedCells.Contains(grid[point.row, h]))
        {
            if (grid[point.row, point.col].state == grid[point.row, h].state)
                SearchNeighbours(connectedCells, grid[point.row, h]);
            else
            { connectedCells.Add(grid[point.row, h]); lowCellConnected[grid[point.row, h].areaIndex].Add(grid[point.row, point.col]); }
        }
        if (k >= 0 && !grid[k, point.col].searched && grid[point.row, point.col].connectedCells.Contains(grid[k, point.col]))
        {

            if (grid[point.row, point.col].state == grid[k, point.col].state)
                SearchNeighbours(connectedCells, grid[k, point.col]);
            else
            { connectedCells.Add(grid[k, point.col]); lowCellConnected[grid[k, point.col].areaIndex].Add(grid[point.row, point.col]); }
        }
    }


    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 130, 80, 60), "Find Areas"))
            FindAreas();
    }
    //    void DropLongestHighAreaCCL()
    //    {
    //        if (allDropped) return;

    //        int highestCount = 0;
    //        int highestIndex = 0;

    //        foreach (var highCellArea in highCellAreas)
    //        {
    //            if(highCellArea.Value.Count > highestCount)
    //            {
    //                highestCount = highCellArea.Value.Count;
    //                highestIndex = highCellArea.Key;
    //            }
    //        }

    //        foreach (MazeCell point in highCellAreas[highestIndex])
    //        {
    //            StartCoroutine(MoveCellDown(point.cell));
    //        }

    //        highCellAreas.Remove(highestIndex);

    //        if (highCellAreas.Count == 0)
    //        {
    //            allDropped = true;
    //        }
    //    }

    //    void DropShortestHighAreaCCL()
    //    {
    //        if (allDropped) return;

    //        int lowestCount = 10000;
    //        int lowestIndex = 0;

    //        foreach (var highCellArea in highCellAreas)
    //        {
    //            if (highCellArea.Value.Count < lowestCount)
    //            {
    //                lowestCount = highCellArea.Value.Count;
    //                lowestIndex = highCellArea.Key;
    //            }
    //        }

    //        foreach (MazeCell point in highCellAreas[lowestIndex])
    //        {
    //            StartCoroutine(MoveCellDown(point.cell));
    //        }

    //        highCellAreas.Remove(lowestIndex);

    //        if (highCellAreas.Count == 0)
    //        {
    //            allDropped = true;
    //        }
    //    }

    //    void RaiseAreas()
    //    {
    //        foreach(var highCellArea in highCellAreas)
    //        {
    //            foreach (MazeCell point in highCellArea.Value)
    //            {
    //                StartCoroutine(MoveCell(point.cell));
    //            }
    //        }
    //    }


}
