using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public AreaFinder areafinder;
    public PatrolManager patrolManager;
    public Maze maze;
    public IntVector2 startPos, endPos;
    MazeCell startCell, endCell;

    Queue<MazeCell> queue = new Queue<MazeCell>();
    List<MazeCell> path = new List<MazeCell>();
    bool pathFound;


    // get path with start and end points supplied
    public List<MazeCell> SetNewPath(IntVector2 start, IntVector2 end)
    {
        if (maze.cells != null)
            ResetPathfinding();
        path.Clear();
        pathFound = false;

        startCell = maze.cells[start.x, start.y];
        queue.Enqueue(startCell);
        startCell.visited = true;
        endCell = maze.cells[end.x, end.y];

        MazeCell currentCell;
        while (queue.Count > 0)
        {
            currentCell = queue.Dequeue();

            foreach (MazeCell cell in currentCell.connectedCells)
            {
                if (!cell.visited)
                {
                    queue.Enqueue(cell);
                    cell.visited = true;
                    cell.exploredFrom = currentCell;
                    cell.distanceFromStart = 1 + cell.exploredFrom.distanceFromStart;

                    if (cell == endCell)
                    { pathFound = true; break; }
                }
            }
        }

        if (!pathFound)
        { Debug.Log("no available path"); return null; }

        maze.cells[endPos.x, endPos.y].state = 1;
        path.Add(maze.cells[endPos.x, endPos.y]);
        var previous = maze.cells[endPos.x, endPos.y].exploredFrom;
        while (previous != startCell)
        {
            previous.state = 1;
            path.Add(previous);
            previous = previous.exploredFrom;
        }
        startCell.state = 1;
        path.Add(startCell);
        path.Reverse();

        DisplayPath(path, Color.red);

        areafinder.FindAreas();

        return path;
    }

    public List<MazeCell> GetCurrentPath() { return path; }

    public MazeCell GetDestination()
    {
        return endCell;
    }

    // get path with indices stored as public variables in pathfinder
    public List<MazeCell> GetPath()
    {
        if(maze.cells != null)
            ResetPathfinding();
        path.Clear();
        pathFound = false;

        startCell = maze.cells[startPos.x, startPos.y];
        queue.Enqueue(startCell);
        startCell.visited = true;
        endCell = maze.cells[endPos.x, endPos.y];

        MazeCell currentCell;
        while(queue.Count > 0)
        {
            currentCell = queue.Dequeue();

            foreach (MazeCell cell in currentCell.connectedCells)
            {
                if (!cell.visited)
                {
                    queue.Enqueue(cell);
                    cell.visited = true;
                    cell.exploredFrom = currentCell;
                    cell.distanceFromStart = 1 + cell.exploredFrom.distanceFromStart;

                    if (cell == endCell)
                    { pathFound = true; break; }
                }
            }
        }

        if(!pathFound)
        { Debug.Log("no available path"); return null; }

        maze.cells[endPos.x, endPos.y].state = 1;
        path.Add(maze.cells[endPos.x, endPos.y]);
        var previous = maze.cells[endPos.x, endPos.y].exploredFrom;
        while (previous != startCell)
        {
            previous.state = 1;
            path.Add(previous);
            previous = previous.exploredFrom;
        }
        startCell.state = 1;
        path.Add(startCell);
        path.Reverse();

        areafinder.FindAreas();

        DisplayPath(path, Color.red);

        return path;
    }

    void DisplayPath(List<MazeCell> path, Color color)
    {
        foreach(MazeCell cell in path)
        {
            if(cell == startCell || cell == endCell)
            { cell.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.green; continue; }
            cell.transform.GetChild(0).GetComponent<Renderer>().material.color = color;
        }
    }

    void ResetPathfinding()
    {
        foreach(MazeCell cell in maze.cells)
        {
            cell.visited = false;
            cell.searched = false;
            cell.exploredFrom = null;
            cell.distanceFromStart = 0;
            cell.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.white;
            cell.cellText.color = Color.red;
            cell.state = 0;
        }
    }

    

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 70, 80, 60), "Find Path"))
            GetPath();
    }
    //int pathIndex = 0;

    //private void Update()
    //{
    //    RaycastHit2D[] hits = new RaycastHit2D[10];
    //    ContactFilter2D filter = new ContactFilter2D();
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        if (Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector3.forward, filter, results: hits) > 0 && pathIndex % 2 == 0)
    //        { startCell = hits[0].collider.GetComponent<MazeCell>(); pathIndex++; }
    //        else if (Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector3.forward, filter, results: hits) > 0 && pathIndex % 2 == 1)
    //        { endCell = hits[0].collider.GetComponent<MazeCell>(); pathIndex++; }
    //    }
    //}
}
