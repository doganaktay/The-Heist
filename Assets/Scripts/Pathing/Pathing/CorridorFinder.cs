using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorridorFinder : MonoBehaviour
{
    public Maze maze;

    int index;
    int connectionCount = 0;
    Queue<MazeCell> frontier = new Queue<MazeCell>();
    List<MazeCell> currentArea = new List<MazeCell>();
    List<MazeCell> ends = new List<MazeCell>();
    bool[,] visited;

    public void DetermineCorridorsAndRooms()
    {
        visited = new bool[maze.size.x, maze.size.y];
        frontier.Enqueue(GameManager.StartCell);

        int counterLimit = maze.size.x * maze.size.y * 4;
        int counter = 0;

        index = 0;

        while(frontier.Count > 0)
        {
            currentArea.Clear();
            ends.Clear();

            Debug.Log($"Starting new search at {frontier.Peek().gameObject.name} with index {index}");  

            var nextCell = frontier.Dequeue();
            SearchCell(nextCell);

            foreach (var cell in currentArea)
            {
                cell.SetGraphConnections(index, ends);
                Debug.Log($"{cell.gameObject.name} Adding graph connection at {index} with count {ends.Count}. Search started at {nextCell.gameObject.name}");
                foreach(var end in ends)
                {
                    Debug.Log($"{end.gameObject.name} added");
                }
            }

            index++;
            connectionCount = 0;

            if (counter >= counterLimit)
            { Debug.Log("exceeded search count, breaking while loop"); break; }

            counter++;
        }

        foreach (var cell in maze.cells)
        {
            if (cell.state > 1)
                continue;

            if(cell.graphConnections == null || cell.graphConnections.Count == 0)
            {
                Debug.Log($"{cell.gameObject.name} connection graph is null or empty");
                continue;
            }

            string indices = "";

            foreach (var key in cell.graphConnections.Keys)
                indices += key.ToString() + " ";

            cell.DisplayText(indices);
        }
    }

    void SearchCell(MazeCell currentCell, MazeCell cameFrom = null)
    {
        if (visited[currentCell.pos.x, currentCell.pos.y])
            return;

        Debug.Log($"Searching {currentCell.gameObject.name}");

        if (currentCell.connectedCells.Count < 3 || (currentCell.connectedCells.Count >= 3 && currentCell.UnexploredDirectionCount == 1))
            visited[currentCell.pos.x, currentCell.pos.y] = true;

        currentArea.Add(currentCell);

        int count = currentCell.connectedCells.Count;

        if (count == 1)
        {
            ends.Add(currentCell);
            connectionCount++;

            Debug.Log($"{currentCell.gameObject.name} added as an end with {count} connection");

            if (currentArea.Count == 1)
                foreach (var cell in currentCell.connectedCells)
                {
                    Debug.Log($"{currentCell.gameObject.name} triggering first search on {cell.gameObject.name}");
                    SearchCell(cell, currentCell);
                }
        }
        else if (count == 2)
        {
            foreach(var cell in currentCell.connectedCells)
            {
                if (cameFrom != null && cell == cameFrom)
                    continue;

                Debug.Log($"{currentCell.gameObject.name} triggering recursive search on {cell.gameObject.name}");
                SearchCell(cell, currentCell);
            }
        }
        else
        {
            if (!ends.Contains(currentCell))
            {
                ends.Add(currentCell);
                connectionCount++;

                Debug.Log($"{currentCell.gameObject.name} added as an end with {count} connection");
            }


            if(currentArea.Count == 1)
            {
                foreach(var cell in currentCell.connectedCells)
                {
                    if ((cell.connectedCells.Count < 3 && !visited[cell.pos.x, cell.pos.y]) || (cell.connectedCells.Count >= 3 && !currentCell.HasMadeConnection(cell)))
                    {
                        Debug.Log($"{currentCell.gameObject.name} triggering first search on {cell.gameObject.name}");
                        SearchCell(cell, currentCell);
                        break;
                    }
                }
            }

            if(currentCell.UnexploredDirectionCount > 0)
                currentCell.UnexploredDirectionCount--;
            Debug.Log($"{currentCell.gameObject.name} has one direction removed, down from {currentCell.UnexploredDirectionCount + 1} to {currentCell.UnexploredDirectionCount}");

            if (currentCell.UnexploredDirectionCount > 0 && currentCell.LastIndexAddedToQueue != index)
            {
                Debug.Log($"{currentCell.gameObject.name} has {currentCell.UnexploredDirectionCount} unexplored directions left. Adding to queue");

                frontier.Enqueue(currentCell);
                currentCell.LastIndexAddedToQueue = index;
            }
            else
            {
                Debug.Log($"{currentCell.gameObject.name} has no unexplored directions left or was already added at this index. Not added to queue");
            }
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 190, 80, 60), "Find Paths"))
            DetermineCorridorsAndRooms();
    }
}
