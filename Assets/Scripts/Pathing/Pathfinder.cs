using System;
using System.Collections;
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
    int searchSize = 10;

    // track distance
    public bool initialized = false;
    public int startDistance;
    public int currentDistance;
    public int distanceTravelled;
    int distanceCut = 0;

    // last level reset time
    public float lastRestartTime;

    // astar
    public AStar aStar;

    #region MonoBehaviour

    private void Start()
    {
        Player.MazeChange += NewPath;

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
            if (player.currentPlayerCell.state == 0)
                SetHighlightPath(player.currentPlayerCell.pos, player.areaIndex);
            else
            {
                if (highestIndex < 1000)
                    ResetCells(explored[highestIndex], highestIndex, true);
            }

            player.cellChanged = false;
        }
    }

    private void OnDestroy()
    {
        //GameManager.MazeGenFinished -= NewPath;
        Player.MazeChange -= NewPath;
    }

    #endregion

    // For setting maze path from startPos to endPos
    public void NewPath()
    {
        SetNewPath(startPos, endPos);
    }

    // get aStar path
    public List<MazeCell> GetRandomAStarPath(MazeCell start)
    {
        // random endpoint assignment
        var end = areafinder.WalkableArea[UnityEngine.Random.Range(0, areafinder.WalkableArea.Count - 1)];

        return new List<MazeCell>(aStar.GetPath(start, end));
    }

    public List<MazeCell> GetAStarPath(MazeCell start, MazeCell end)
    {
        return new List<MazeCell>(aStar.GetPath(start, end));
    }

    public void FindPath(PathRequest request, Action<PathResult> callback)
    {
        List<MazeCell> newPath;

        if (request.end != null)
            newPath = GetAStarPath(request.start, request.end);
        else
            newPath = GetRandomAStarPath(request.start);

        callback(new PathResult(newPath, request.callback));
    }

    // check neighbours for placement
    public bool TryNeighbourPaths(MazeCell candidate)
    {
        // place if it is a low cell area with count of 1
        if (candidate.state == 0 && areafinder.GetLowCellArea(candidate.areaIndex).Count == 1)
            return true;

        foreach (var connected in candidate.connectedCells)
        {
            if (connected.connectedCells.Count == 1)
                return false;
            if (connected.state > 1)
                continue;

            int pathIndex = 1;

            if (candidate.state == 1)
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

                startCell[pathIndex] = maze.cells[startPos.x, startPos.y];
                queue[pathIndex].Enqueue(startCell[pathIndex]);
                startCell[pathIndex].visited[pathIndex] = true;
                endCell[pathIndex] = maze.cells[connected.pos.x, connected.pos.y];

                MazeCell currentCell;

                while (queue[pathIndex].Count > 0)
                {
                    currentCell = queue[pathIndex].Dequeue();
                    explored[pathIndex].Add(currentCell);

                    foreach (MazeCell cell in currentCell.connectedCells)
                    {
                        if (cell == candidate || cell.state > 1) { continue; }

                        if (!cell.visited[pathIndex] && cell.state == 1)
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
                { return false; }
            }
            else
            {
                var connectionPoints = areafinder.GetLowConnectionPoints(connected.areaIndex);

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
                    endCell[pathIndex] = maze.cells[connected.pos.x, connected.pos.y];

                    MazeCell currentCell;

                    while (queue[pathIndex].Count > 0)
                    {
                        currentCell = queue[pathIndex].Dequeue();
                        explored[pathIndex].Add(currentCell);

                        foreach (MazeCell cell in currentCell.connectedCells)
                        {
                            if (cell == candidate || cell.state > 1) { continue; }

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
                    { return false; }
                }
            }
        }

        return true;
    }

    // keeps track of the used indices of secondary paths
    int highestIndex = 1000;

    // get path with start and end points supplied
    public List<MazeCell> SetHighlightPath(IntVector2 pos, int areaIndex)
    {
        int pathIndex = 1;
        int shortestIndex = 1000;
        int shortestCount = 1000;

        var connectionPoints = areafinder.GetLowConnectionPoints(areaIndex);

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

            highestIndex = pathIndex;
        }

        //DisplayPath(path[shortestIndex], 2, shortestIndex, true);

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
                if (!cell.visited[pathIndex] && cell.state < 2)
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
        }

        //DisplayPath(path[pathIndex], 1, pathIndex);

        return path[pathIndex];
    }

    public List<MazeCell> GetPatrolPath(IntVector2 start, IntVector2 end, int areaIndex)
    {
        var linkPoints = areafinder.GetLowConnectionPoints(areaIndex);
        var patrolArea = areafinder.GetPatrolAreaByIndex(areaIndex);

        return patrolArea;
    }

    public List<MazeCell> GetCurrentPath(int index) { return path[index]; }

    public MazeCell GetDestination(int pathIndex)
    {
        return endCell[pathIndex];
    }

    // tracking last start and end cell for timer reset
    MazeCell lastStartCell, lastEndCell;
    // Display path
    void DisplayPath(List<MazeCell> path, int colorIndex, int pathIndex, bool resetTime = false)
    {
        bool reset = player.hitIndexChanged || lastStartCell != startCell[pathIndex] || !endCell[pathIndex].connectedCells.Contains(lastEndCell);

        foreach (MazeCell cell in path)
        {
            cell.mat.SetColorIndex(colorIndex);
            cell.mat.SetPathIndex(cell.distanceFromStart[pathIndex]);
            cell.mat.SetPathCount(endCell[pathIndex].distanceFromStart[pathIndex]);

            if (resetTime && reset)
            { cell.mat.SetRestartTime(Time.time); }
        }

        if (!reset)
            path.ElementAt(path.Count - 1).mat.SetRestartTime(lastEndCell.mat.GetRestartTime());

        lastStartCell = startCell[pathIndex];
        lastEndCell = endCell[pathIndex];
        player.hitIndexChanged = false;
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

            if (cell.state < 2)
                cell.state = 0;

            // setting material properties
            //cell.mat.SetColorIndex(0);
            //cell.mat.SetPathIndex(0);
            //cell.mat.SetPathCount(0);
            //cell.mat.SetRestartTime(Time.time);

            // manually resetting secondary path variables
            if (highestIndex > 10) continue; // this is in case we haven't searched any secondary paths yet
            for (int i = 1; i <= highestIndex; i++)
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
                for (int i = 1; i <= index; i++)
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

            //if (state)
            //{
            //    cell.mat.SetPathIndex(cell.distanceFromStart[0]);
            //    cell.mat.SetColorIndex(1);
            //    cell.mat.SetPathCount(endCell[0].distanceFromStart[0]);
            //    cell.mat.SetRestartTime(lastRestartTime);
            //}
            //else
            //{
            //    cell.mat.SetColorIndex(0);
            //}

        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (maze != null)
        {
            foreach (var cell in maze.cells)
            {
                foreach (var connected in cell.connectedCells)
                {
                    if (connected.pos.y > cell.pos.y)
                        Debug.DrawLine(cell.transform.position + Vector3.right * 2f, connected.transform.position + Vector3.right * 2f, Color.green);
                    else if (connected.pos.y < cell.pos.y)
                        Debug.DrawLine(cell.transform.position + Vector3.left * 2f, connected.transform.position + Vector3.left * 2f, Color.blue);

                    if (connected.pos.x > cell.pos.x)
                        Debug.DrawLine(cell.transform.position + Vector3.down * 2f, connected.transform.position + Vector3.down * 2f, Color.cyan);
                    else if (connected.pos.x < cell.pos.x)
                        Debug.DrawLine(cell.transform.position + Vector3.up * 2f, connected.transform.position + Vector3.up * 2f, Color.red);
                }

                //foreach(var placed in cell.placedConnectedCells)
                //{
                //    Debug.DrawLine(cell.transform.position, placed.transform.position, Color.red);
                //}
            }
        }
    }
#endif

}
