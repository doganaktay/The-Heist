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
    List<MazeCell> explored;
    bool[] pathFound;

    public bool gameHasReset = false;

    int searchSize = 5;
    int currentSearchIndex = 0;

    private void Start()
    {
        pathFound = new bool[searchSize];
        queue = new Queue<MazeCell>[searchSize];
        path = new List<MazeCell>[searchSize];
        startCell = new MazeCell[searchSize];
        endCell = new MazeCell[searchSize];

    }

    void Update()
    {
        if (player.cellChanged)
        {
            if (player.currentCell.state == 0)
                SetNewSecondaryPath(player.currentCell.pos, player.areaIndex);
            else
            {
                if(!gameHasReset)
                    ResetCells(explored, 1);
            }

        player.cellChanged = false;
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 70, 80, 60), "get path"))
            Update();
    }

    int GetSearchIndex()
    {
        currentSearchIndex++;

        if (currentSearchIndex == searchSize)
            currentSearchIndex = 1;

        return currentSearchIndex;
    }

    // get path with start and end points supplied
    public List<MazeCell> SetNewSecondaryPath(IntVector2 pos, int areaIndex)
    {
        int pathIndex = 1;

        if (path[pathIndex] == null)
            path[pathIndex] = new List<MazeCell>();

        if (queue[pathIndex] == null)
            queue[pathIndex] = new Queue<MazeCell>();

        if (explored == null)
            explored = new List<MazeCell>();

        if (!gameHasReset)
            ResetCells(explored, pathIndex);
        else
            gameHasReset = false;

        explored.Clear();

        path[pathIndex].Clear();
        pathFound[pathIndex] = false;

        var connectionPoints = areafinder.GetConnectionPoints(areaIndex);

        List<MazeCell>[] alternatePaths = new List<MazeCell>[connectionPoints.Count];

        for (int j = 0; j < connectionPoints.Count; j++)
        {
            if (alternatePaths[j] == null)
                alternatePaths[j] = new List<MazeCell>();

            startCell[pathIndex] = connectionPoints[j];
            queue[pathIndex].Enqueue(startCell[pathIndex]);
            startCell[pathIndex].visited[pathIndex] = true;
            endCell[pathIndex] = maze.cells[pos.x, pos.y];

            MazeCell currentCell;

            while (queue[pathIndex].Count > 0)
            {
                currentCell = queue[pathIndex].Dequeue();

                foreach (MazeCell cell in currentCell.connectedCells)
                {
                    if (!cell.visited[pathIndex] && cell.state == 0)
                    {
                        queue[pathIndex].Enqueue(cell);
                        cell.visited[pathIndex] = true;
                        cell.exploredFrom[pathIndex] = currentCell;
                        cell.distanceFromStart[pathIndex] = 1 + cell.exploredFrom[pathIndex].distanceFromStart[pathIndex];

                        explored.Add(cell);

                        if (cell == endCell[pathIndex])
                        {
                            pathFound[pathIndex] = true;
                            break;
                        }
                    }
                }
            }

            if (!pathFound[pathIndex])
            { Debug.Log("no available path"); continue; }

            alternatePaths[j].Add(endCell[pathIndex]);
            var previous = endCell[pathIndex].exploredFrom[pathIndex];
            while (previous != startCell[pathIndex])
            {
                alternatePaths[j].Add(previous);
                previous = previous.exploredFrom[pathIndex];
            }
            alternatePaths[j].Add(startCell[pathIndex]);
            alternatePaths[j].Reverse();

            if(connectionPoints.Count > 1 && j < connectionPoints.Count - 1)
                ResetCells(explored, pathIndex);
        }

        var ordered = alternatePaths.OrderBy(x => x.Count).ToArray();

        path[pathIndex].AddRange(ordered[0]);


        DisplayPath(path[pathIndex], new Color(0.3645f, 0.6643f, 0.9360f), pathIndex, false);

        return path[pathIndex];
    }

    // get path with start and end points supplied
    public List<MazeCell> SetNewPath(IntVector2 start, IntVector2 end, int pathIndex = 0)
    {
        ResetCells(pathIndex, true);

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

        //if (!pathFound[pathIndex])
        //{ Debug.Log("no available path"); return null; }

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

        DisplayPath(path[pathIndex], Color.red, pathIndex);

        areafinder.FindAreas();

        return path[pathIndex];
    }

    public List<MazeCell> GetPatrolPath(IntVector2 start, IntVector2 end, int areaIndex)
    {
        var linkPoints = areafinder.GetConnectionPoints(areaIndex);
        var patrolArea = areafinder.GetPatrolAreaIndex(areaIndex);

        return patrolArea;
    }

    public List<MazeCell> GetCurrentPath(int index) { return path[index]; }

    public MazeCell GetDestination(int pathIndex)
    {
        return endCell[pathIndex];
    }

    void DisplayPath(List<MazeCell> path, Color color, int pathIndex, bool setEndPoints = true)
    {
        foreach(MazeCell cell in path)
        {
            if(setEndPoints && (cell == startCell[pathIndex] || cell == endCell[pathIndex]))
            { cell.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.green; continue; }
            cell.transform.GetChild(0).GetComponent<Renderer>().material.color = color;
        }
    }

    void ResetCells(int index, bool resetAll = false)
    {
        foreach (MazeCell cell in maze.cells)
        {
            cell.visited[index] = false;
            cell.exploredFrom[index] = null;
            cell.distanceFromStart[index] = 0;
            cell.cellText.color = Color.red;
            cell.transform.GetChild(0).GetComponent<Renderer>().material.color = cell.startColor;
            cell.searched = false;
            cell.state = 0;
        }
    }

    void ResetCells(List<MazeCell> explored, int index)
    {
            if (explored.Count == 0) return;

            foreach (MazeCell cell in explored)
            {
                cell.visited[index] = false;
                cell.exploredFrom[index] = null;
                cell.distanceFromStart[index] = 0;
                cell.transform.GetChild(0).GetComponent<Renderer>().material.color = cell.startColor;
            }  
    }

}
