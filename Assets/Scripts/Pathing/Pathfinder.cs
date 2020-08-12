﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Pathfinder : MonoBehaviour
{
    public Player player;
    public AreaFinder areafinder;
    public PatrolManager patrolManager;
    public Maze maze;
    public IntVector2 startPos, endPos;

    // path
    MazeCell[] startCell;
    MazeCell[] endCell;
    Queue<MazeCell>[] queue;
    List<MazeCell>[] path;
    List<MazeCell>[] explored;
    bool[] pathFound;
    int searchSize = 5;

    // track distance
    public bool initialized = false;
    public int startDistance;
    public int currentDistance;
    public int distanceTravelled;
    int distanceCut = 0;

    // last level reset time
    public float lastRestartTime;

    private void Start()
    {
        pathFound = new bool[searchSize];
        queue = new Queue<MazeCell>[searchSize];
        path = new List<MazeCell>[searchSize];
        startCell = new MazeCell[searchSize];
        endCell = new MazeCell[searchSize];
        explored = new List<MazeCell>[searchSize];
    }

    void Update()
    {
        if (player.cellChanged)
        {
            if (player.currentCell.state == 0)
                SetHighlightPath(player.currentCell.pos, player.areaIndex);
            else
            {
                ResetCells(explored[shortestIndex], shortestIndex, true);
            }

            player.cellChanged = false;
        }
    }

    // keeps track of the used indices of secondary paths
    int shortestIndex = 1000;
    // get path with start and end points supplied
    public List<MazeCell> SetHighlightPath(IntVector2 pos, int areaIndex)
    {
        int pathIndex = 1;
        int shortestCount = 1000;

        var connectionPoints = areafinder.GetConnectionPoints(areaIndex);

        for (int j = 0; j < connectionPoints.Count; j++)
        {
            if (path[pathIndex] == null)
                path[pathIndex] = new List<MazeCell>();

            if (queue[pathIndex] == null)
                queue[pathIndex] = new Queue<MazeCell>();

            if (explored[pathIndex] == null)
                explored[pathIndex] = new List<MazeCell>();
            else
                ResetCells(explored[pathIndex], pathIndex);

            explored[pathIndex].Clear();

            path[pathIndex].Clear();
            pathFound[pathIndex] = false;

            startCell[pathIndex] = connectionPoints[j];
            queue[pathIndex].Enqueue(startCell[pathIndex]);
            startCell[pathIndex].visited[pathIndex] = true;
            endCell[pathIndex] = maze.cells[pos.x, pos.y];

            MazeCell currentCell;

            while (queue[pathIndex].Count > 0)
            {
                currentCell = queue[pathIndex].Dequeue();
                explored[pathIndex].Add(currentCell);

                foreach (MazeCell cell in currentCell.connectedCells)
                {
                    if (!cell.visited[pathIndex] && cell.state == 0)
                    {
                        queue[pathIndex].Enqueue(cell);
                        cell.visited[pathIndex] = true;
                        cell.exploredFrom[pathIndex] = currentCell;
                        cell.distanceFromStart[pathIndex] = 1 + cell.exploredFrom[pathIndex].distanceFromStart[pathIndex];

                        explored[pathIndex].Add(cell);

                        if (cell == endCell[pathIndex])
                        {
                            pathFound[pathIndex] = true;
                            break;
                        }
                    }
                }
            }

            if (!pathFound[pathIndex])
            { Debug.Log("no available path for: " + pathIndex); continue; }

            path[pathIndex].Add(endCell[pathIndex]);
            var previous = endCell[pathIndex].exploredFrom[pathIndex];
            while (previous != startCell[pathIndex])
            {
                path[pathIndex].Add(previous);
                previous = previous.exploredFrom[pathIndex];
            }
            path[pathIndex].Add(startCell[pathIndex]);
            path[pathIndex].Reverse();

            if (path[pathIndex].Count < shortestCount)
            {
                shortestIndex = pathIndex;
                shortestCount = path[pathIndex].Count;
            }

            if (j < connectionPoints.Count - 1)
                pathIndex++;
        }

        DisplayPath(path[shortestIndex], 2, shortestIndex);

        return path[pathIndex];
    }

    // get path with start and end points supplied
    public List<MazeCell> SetNewPath(IntVector2 start, IntVector2 end, int pathIndex = 0)
    {
        ResetCells(pathIndex);

        if (path[pathIndex] == null)
            path[pathIndex] = new List<MazeCell>();
        if (queue[pathIndex] == null)
            queue[pathIndex] = new Queue<MazeCell>();

        path[pathIndex].Clear();
        pathFound[pathIndex] = false;

        startCell[pathIndex] = maze.cells[start.x, start.y];
        queue[pathIndex].Enqueue(startCell[pathIndex]);
        startCell[pathIndex].visited[pathIndex] = true;
        endCell[pathIndex] = maze.cells[end.x, end.y];

        MazeCell currentCell;
        while (queue[pathIndex].Count > 0)
        {
            currentCell = queue[pathIndex].Dequeue();

            foreach (MazeCell cell in currentCell.connectedCells)
            {
                if (!cell.visited[pathIndex])
                {
                    queue[pathIndex].Enqueue(cell);
                    cell.visited[pathIndex] = true;
                    cell.exploredFrom[pathIndex] = currentCell;
                    cell.distanceFromStart[pathIndex] = 1 + cell.exploredFrom[pathIndex].distanceFromStart[pathIndex];

                    if (cell == endCell[pathIndex])
                    { pathFound[pathIndex] = true; break; }
                }
            }
        }

        if (!pathFound[pathIndex])
        { Debug.Log("no available path"); return null; }

        maze.cells[endPos.x, endPos.y].state = 1;
        path[pathIndex].Add(maze.cells[endPos.x, endPos.y]);
        var previous = maze.cells[endPos.x, endPos.y].exploredFrom[pathIndex];
        while (previous != startCell[pathIndex])
        {
            previous.state = 1;
            path[pathIndex].Add(previous);
            previous = previous.exploredFrom[pathIndex];
        }
        startCell[pathIndex].state = 1;
        path[pathIndex].Add(startCell[pathIndex]);
        path[pathIndex].Reverse();

        // distance tracking
        if (!initialized)
        {
            startDistance = maze.cells[endPos.x, endPos.y].distanceFromStart[0];
            currentDistance = startDistance;
            distanceCut = 0;
            distanceTravelled = 0;
            initialized = true;
        }
        else
        {
            var newDist = maze.cells[endPos.x, endPos.y].distanceFromStart[0];
            distanceCut += currentDistance - newDist - distanceTravelled;
            currentDistance = newDist;
            distanceTravelled = 0;
            Debug.Log("Start distance: " + startDistance + " Current distance: " + currentDistance + " Distance cut: " + distanceCut);
        }

        DisplayPath(path[pathIndex], 1, pathIndex);

        areafinder.FindAreas();

        return path[pathIndex];
    }

    public List<MazeCell> GetPatrolPath(IntVector2 start, IntVector2 end, int areaIndex)
    {
        var linkPoints = areafinder.GetConnectionPoints(areaIndex);
        var patrolArea = areafinder.GetPatrolAreaByIndex(areaIndex);

        return patrolArea;
    }

    public List<MazeCell> GetCurrentPath(int index) { return path[index]; }

    public MazeCell GetDestination(int pathIndex)
    {
        return endCell[pathIndex];
    }

    void DisplayPath(List<MazeCell> path, int colorIndex, int pathIndex)
    {
        foreach(MazeCell cell in path)
        {
            cell.mat.SetInt(GameManager.colorIndex, colorIndex);
            cell.mat.SetFloat(GameManager.pathIndex, cell.distanceFromStart[pathIndex]);
            cell.mat.SetFloat(GameManager.pathCount, endCell[pathIndex].distanceFromStart[pathIndex]);
        }
    }

    void ResetCells(int index)
    {
        foreach (MazeCell cell in maze.cells)
        {
            cell.visited[index] = false;
            cell.exploredFrom[index] = null;
            cell.distanceFromStart[index] = 0;
            cell.cellText.color = Color.red;
            cell.searched = false;
            cell.state = 0;

            // setting material properties
            cell.mat.SetInt(GameManager.colorIndex, 0);
            cell.mat.SetFloat(GameManager.pathIndex, 0);
            cell.mat.SetFloat(GameManager.pathCount, 0);
            cell.mat.SetFloat(GameManager.restartTime, Time.time);

            // manually resetting secondary path variables
            if (shortestIndex > 10) continue; // this is in case we haven't searched any secondary paths yet
            for (int i = 1; i <= shortestIndex; i++)
            {
                cell.visited[i] = false;
                cell.exploredFrom[i] = null;
                cell.distanceFromStart[i] = 0;
            }
        }

        lastRestartTime = Time.time;
    }

    void ResetCells(List<MazeCell> explored, int index, bool resetAll = false)
    {
            foreach (MazeCell cell in explored)
            {
                if (resetAll)
                {
                    for(int i=1; i<=index; i++)
                    {
                        cell.visited[i] = false;
                        cell.exploredFrom[i] = null;
                        cell.distanceFromStart[i] = 0;
                    }
                }
                else
                {
                    cell.visited[index] = false;
                    cell.exploredFrom[index] = null;
                    cell.distanceFromStart[index] = 0;
                }

                var state = cell.state == 1;

                if (state)
                {
                    cell.mat.SetFloat(GameManager.pathIndex, cell.distanceFromStart[0]);
                    cell.mat.SetInt(GameManager.colorIndex, 1);
                    cell.mat.SetFloat(GameManager.pathCount, endCell[0].distanceFromStart[0]);
                    cell.mat.SetFloat(GameManager.restartTime, lastRestartTime);
                }
                else
                {
                    cell.mat.SetInt(GameManager.colorIndex, 0);

                    if (!path[index].Contains(player.currentCell) && !player.currentCell.connectedCells.Contains(endCell[index]))
                    {
                        cell.mat.SetFloat(GameManager.restartTime, Time.time);
                    }
                }

            }  
    }

}
