using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorridorFinder : MonoBehaviour
{
    public Maze maze;

    int index;
    Queue<MazeCell> frontier = new Queue<MazeCell>();
    List<MazeCell> currentArea = new List<MazeCell>();
    List<MazeCell> ends = new List<MazeCell>();
    bool[,] visited;
    Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)> partitions;

    public void DetermineCorridorsAndRooms()
    {
        partitions = new Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)>();
        visited = new bool[maze.size.x, maze.size.y];
        frontier.Enqueue(GameManager.StartCell);

        index = 0;

        // PASS 1
        Debug.Log("Pass 1:");

        while(frontier.Count > 0)
        {
            currentArea.Clear();
            ends.Clear();

            Debug.Log($"Starting new search at {frontier.Peek().gameObject.name} with index {index}");  

            var nextCell = frontier.Dequeue();
            SearchCell(nextCell);

            if (currentArea.Count == 0)
                continue;

            foreach (var cell in currentArea)
            {
                cell.SetGraphConnections(index, ends);
                Debug.Log($"{cell.gameObject.name} Adding graph connection at {index} with count {ends.Count}. Search started at {nextCell.gameObject.name}");
            }

            partitions.Add(index, (new List<MazeCell>(currentArea), new List<MazeCell>(ends)));

            index++;
        }

        // PASS 2

        Debug.Log($"Pass 2: {partitions.Count} partitions");

        List<MazeCell> cellsToTest = new List<MazeCell>();

        foreach (var part in partitions)
        {
            //if (part.Value.all.Count > 3)
            //    continue;

            // if any partitions only have 1 end with 3 or more connections, that end has to remain a node
            // if partition cell count is greater than 3 OR (cell count is 3 AND cell with two connections cardinal bits modulo 3 != 0) both ends remain as nodes 
            var count = part.Value.all.Count;

            if(count > 3)
            {
                foreach (var cell in part.Value.ends)
                    if (cell.connectedCells.Count >= 3)
                    {
                        cell.IsLockedConnection = true;
                        visited[cell.pos.x, cell.pos.y] = false;
                    }
            }
            else if(count == 3)
            {
                MazeCell middleCell = null;
                foreach(var cell in part.Value.all)
                {
                    if (cell.connectedCells.Count == 2)
                    { middleCell = cell; break; }
                }

                if (!middleCell.IsInternalCorner())
                {
                    foreach (var cell in part.Value.ends)
                        if (cell.connectedCells.Count >= 3)
                        {
                            cell.IsLockedConnection = true;
                            visited[cell.pos.x, cell.pos.y] = false;
                        }
                }
                else
                {
                    foreach(var cell in part.Value.all)
                        if(cell.connectedCells.Count >= 2)
                            visited[cell.pos.x, cell.pos.y] = false;
                        
                }
            }
            else if (count == 2)
            {
                bool isSingle = false;
                MazeCell mainNode = null;

                foreach(var end in part.Value.ends)
                {
                    if(end.connectedCells.Count == 1)
                    {
                        isSingle = true;
                    }
                    else
                    {
                        mainNode = end;
                        visited[end.pos.x, end.pos.y] = false;
                    }
                }

                if (isSingle)
                    mainNode.IsLockedConnection = true;
                else
                    foreach (var end in part.Value.ends)
                        cellsToTest.Add(end);
            }

            //Debug.Log($"Partition Index: {part.Key} with one end at ({part.Value.ends[0].pos.x},{part.Value.ends[0].pos.y})");
            //Debug.Log($"index: {part.Key} cells: {part.Value.all.Count} ends: {part.Value.ends.Count} one end at ({part.Value.ends[0].pos.x},{part.Value.ends[0].pos.y})");
        }

        HashSet<int> coveredPartitions = new HashSet<int>();

        foreach(var testCell in cellsToTest)
        {
            currentArea.Clear();
            ends.Clear();
            coveredPartitions.Clear();

            SearchCellForMerge(testCell, coveredPartitions);

            if (currentArea.Count == 0)
                continue;

            var temp = new HashSet<int>();
            foreach (var coveredIndex in coveredPartitions)
            {
                int i = 0;

                foreach (var cell in currentArea)
                    if (cell.graphConnections.ContainsKey(coveredIndex))
                        i++;

                if (i == 1)
                    temp.Add(coveredIndex);
            }

            foreach(var t in temp)
            {
                coveredPartitions.Remove(t);
            }

            foreach(var cell in currentArea)
            {
                if (cell.IsLockedConnection)
                {
                    foreach(var index in coveredPartitions)
                    {
                        if (cell.graphConnections.ContainsKey(index))
                            cell.graphConnections.Remove(index);
                    }
                }
                else
                {
                    cell.graphConnections.Clear();
                }

                cell.SetGraphConnections(index, ends);
            }

            index++;
        }

        foreach (var cell in maze.cells)
        {
            if (cell.IsLockedConnection)
                Debug.Log($"{cell.gameObject.name} is a locked connection");
        }

        // DISPLAY

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

    void SearchCellForMerge(MazeCell currentCell, HashSet<int> coveredIndices)
    {
        if (visited[currentCell.pos.x, currentCell.pos.y])
            return;

        visited[currentCell.pos.x, currentCell.pos.y] = true;

        currentArea.Add(currentCell);

        foreach (var key in currentCell.graphConnections.Keys)
            coveredIndices.Add(key);

        foreach (var cell in currentCell.connectedCells)
            SearchCellForMerge(cell, coveredIndices);
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 190, 80, 60), "Find Paths"))
            DetermineCorridorsAndRooms();
    }
}
