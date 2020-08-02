using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public Maze maze;
    public IntVector2 startPos, endPos;
    MazeCell startCell, endCell;

    Queue<MazeCell> queue = new Queue<MazeCell>();
    List<MazeCell> path = new List<MazeCell>();
    bool pathFound;

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

    void FindPath()
    {
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
                        pathFound = true;
                }
            }   
        }

        if(!pathFound)
        { Debug.Log("no available path"); return; }

        path.Add(maze.cells[endPos.x, endPos.y]);
        var previous = maze.cells[endPos.x, endPos.y].exploredFrom;
        while (previous != startCell)
        {
            path.Add(previous);
            previous = previous.exploredFrom;
        }
        path.Add(startCell);
        path.Reverse();

        DisplayPath(path);

        path.Clear();

        ResetPathfinding();
    }

    void DisplayPath(List<MazeCell> path)
    {
        foreach(MazeCell cell in path)
        {
            cell.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.red;
        }
    }

    void ResetPathfinding()
    {
        foreach(MazeCell cell in maze.cells)
        {
            cell.visited = false;
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 70, 50, 30), "Find Path"))
            FindPath();
    }
}
