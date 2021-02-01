using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CorridorFinder : MonoBehaviour
{
    public Maze maze;

    int index;
    Queue<MazeCell> frontier = new Queue<MazeCell>();
    List<MazeCell> currentArea = new List<MazeCell>();
    List<MazeCell> ends = new List<MazeCell>();
    bool[,] visited;
    static Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)> GraphAreas;

    public void CreateGraph()
    {
        GraphAreas = new Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)>();
        visited = new bool[maze.size.x, maze.size.y];
        frontier.Enqueue(GameManager.StartCell);

        index = 0;

        // PASS 1
        Debug.Log("Pass 1:");

        while(frontier.Count > 0)
        {
            currentArea.Clear();
            ends.Clear();

            var nextCell = frontier.Dequeue();
            SearchCell(nextCell);

            if (currentArea.Count <= 1)
                continue;

            foreach (var cell in currentArea)
                cell.SetGraphArea(index, currentArea, ends);
            
            GraphAreas.Add(index, (new List<MazeCell>(currentArea), new List<MazeCell>(ends)));

            index++;
        }

        string partitionKeys = "Pass 1 Keys: ";

        foreach (var key in GraphAreas.Keys)
            partitionKeys += key.ToString() + ", ";

        Debug.Log(partitionKeys);

        // PASS 2

        Debug.Log($"Pass 2: {GraphAreas.Count} partitions");

        List<MazeCell> cellsToTest = new List<MazeCell>();

        foreach (var part in GraphAreas)
        {
            var count = part.Value.all.Count;

            if(count > 3)
            {

                foreach (var cell in part.Value.ends)
                    if (cell.connectedCells.Count >= 3)
                    {
                        cell.IsLockedConnection = true;
                        visited[cell.pos.x, cell.pos.y] = false;
                    }

                if (part.Value.ends.Count != 2)
                    continue;

                if (part.Value.ends[0].connectedCells.Contains(part.Value.ends[1]))
                    foreach (var cell in part.Value.all)
                        visited[cell.pos.x, cell.pos.y] = false;
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

                    //cellsToTest.Add(middleCell);
                        
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

        HashSet<int> indicesToMerge = new HashSet<int>();

        foreach(var testCell in cellsToTest)
        {
            currentArea.Clear();
            ends.Clear();
            indicesToMerge.Clear();

            SearchCellForMerge(testCell, indicesToMerge);

            if (currentArea.Count <= 1)
                continue;

            var temp = new HashSet<int>();
            foreach (var coveredIndex in indicesToMerge)
            {
                int i = 0;
                
                foreach (var cell in currentArea)
                {
                    if (cell.graphAreas.ContainsKey(coveredIndex))
                        i++;
                }

                if (i == 1 || (i == 2 && GraphAreas[coveredIndex].all.Count > 3))
                {
                    temp.Add(coveredIndex);
                }
            }

            foreach(var t in temp)
            {
                indicesToMerge.Remove(t);
            }

            foreach(var cell in currentArea)
            {
                if (cell.IsLockedConnection)
                {
                    foreach(var indexToMerge in indicesToMerge)
                    {
                        cell.RemoveGraphArea(indexToMerge);
                    }
                }
                else
                {
                    cell.graphAreas.Clear();
                }

                cell.SetGraphArea(index, currentArea, ends);
            }

            foreach(var indexToMerge in indicesToMerge)
            {
                if (GraphAreas.ContainsKey(indexToMerge))
                {
                    GraphAreas.Remove(indexToMerge);
                    Debug.Log($"Removing {indexToMerge} from partitions");
                }
            }

            if (!GraphAreas.ContainsKey(index))
            {
                GraphAreas.Add(index, (currentArea, ends));
                Debug.Log($"Adding {index} to partitions");
            }

            index++;
        }

        partitionKeys = "Pass 2 Keys: ";

        foreach (var key in GraphAreas.Keys)
            partitionKeys += key.ToString() + ", ";

        Debug.Log(partitionKeys);

        // PASS 3

        //var endsToSearch = new List<MazeCell>();

        //foreach(var cell in maze.cells)
        //{
        //    if (cell.state > 1 || cell.graphAreas.Count == 1)
        //        continue;

        //    endsToSearch.Add(cell);
        //}

        foreach(var area in GraphAreas)
        {
            var junctionCount = GetJunctionCount(area.Key);

            if(junctionCount == 1)
            {
                var connectedIndices = GetConnectedIndices(area.Key);
                MergeAreas(area.Key, connectedIndices[0]);

                Debug.Log($"Merging area {area.Key} to {connectedIndices[0]}");
            }
        }

        // DISPLAY

        foreach (var cell in maze.cells)
        {
            if (cell.state > 1)
                continue;

            if(cell.graphAreas == null || cell.graphAreas.Count == 0)
            {
                Debug.Log($"{cell.gameObject.name} connection graph is null or empty");
                continue;
            }

            string indices = "";

            foreach (var key in cell.graphAreas.Keys)
                indices += key.ToString() + " ";

            if (!cell.IsLockedConnection || cell.graphAreas.Count == 1)
                cell.DisplayText(indices);
            else
                cell.DisplayText(indices, Color.blue);
        }
    }


    void SearchCell(MazeCell currentCell, MazeCell cameFrom = null)
    {
        if (visited[currentCell.pos.x, currentCell.pos.y])
            return;

        //Debug.Log($"Searching {currentCell.gameObject.name}");

        if (currentCell.connectedCells.Count < 3 || (currentCell.connectedCells.Count >= 3 && currentCell.UnexploredDirectionCount == 1))
            visited[currentCell.pos.x, currentCell.pos.y] = true;

        currentArea.Add(currentCell);

        int count = currentCell.connectedCells.Count;

        if (count == 1)
        {
            ends.Add(currentCell);

            //Debug.Log($"{currentCell.gameObject.name} added as an end with {count} connection");

            if (currentArea.Count == 1)
                foreach (var cell in currentCell.connectedCells)
                {
                    //Debug.Log($"{currentCell.gameObject.name} triggering first search on {cell.gameObject.name}");
                    SearchCell(cell, currentCell);
                }
        }
        else if (count == 2)
        {
            foreach(var cell in currentCell.connectedCells)
            {
                if (cameFrom != null && cell == cameFrom)
                    continue;

                //Debug.Log($"{currentCell.gameObject.name} triggering recursive search on {cell.gameObject.name}");
                SearchCell(cell, currentCell);
            }
        }
        else
        {
            if (!ends.Contains(currentCell))
            {
                ends.Add(currentCell);

                //Debug.Log($"{currentCell.gameObject.name} added as an end with {count} connection");
            }


            if(currentArea.Count == 1)
            {
                foreach(var cell in currentCell.connectedCells)
                {
                    if ((cell.connectedCells.Count < 3 && !visited[cell.pos.x, cell.pos.y]) || (cell.connectedCells.Count >= 3 && !currentCell.HasMadeConnection(cell)))
                    {
                        //Debug.Log($"{currentCell.gameObject.name} triggering first search on {cell.gameObject.name}");
                        SearchCell(cell, currentCell);
                        break;
                    }
                }
            }

            if(currentCell.UnexploredDirectionCount > 0)
                currentCell.UnexploredDirectionCount--;
            //Debug.Log($"{currentCell.gameObject.name} has one direction removed, down from {currentCell.UnexploredDirectionCount + 1} to {currentCell.UnexploredDirectionCount}");

            if (currentCell.UnexploredDirectionCount > 0 && currentCell.LastIndexAddedToQueue != index)
            {
                //Debug.Log($"{currentCell.gameObject.name} has {currentCell.UnexploredDirectionCount} unexplored directions left. Adding to queue");

                frontier.Enqueue(currentCell);
                currentCell.LastIndexAddedToQueue = index;
            }
            else
            {
                //Debug.Log($"{currentCell.gameObject.name} has no unexplored directions left or was already added at this index. Not added to queue");
            }
        }
    }

    void SearchCellForMerge(MazeCell currentCell, HashSet<int> coveredIndices)
    {
        if (visited[currentCell.pos.x, currentCell.pos.y])
            return;

        visited[currentCell.pos.x, currentCell.pos.y] = true;

        currentArea.Add(currentCell);

        if (currentCell.IsLockedConnection)
            ends.Add(currentCell);

        foreach (var key in currentCell.graphAreas.Keys)
            coveredIndices.Add(key);

        foreach (var cell in currentCell.connectedCells)
            SearchCellForMerge(cell, coveredIndices);
    }

    void MergeAreas(int from, int to)
    {
        var junctionCells = GetJunctionCells(from, to);

        var endsToMerge = GraphAreas[from].ends;
        var cellsToMerge = GraphAreas[from].all;

        foreach (var cell in junctionCells)
        {
            endsToMerge.Remove(cell);
            cellsToMerge.Remove(cell);
        }

        foreach(var cell in GraphAreas[to].all)
            cell.AddToGraphArea(to, GraphAreas[from].all, endsToMerge);

        GraphAreas[to].all.AddRange(cellsToMerge);
        GraphAreas[to].ends.AddRange(endsToMerge);

        foreach(var cell in GraphAreas[from].all)
        {
            cell.graphAreas.Add(to, (GraphAreas[to].all, GraphAreas[to].ends));
            cell.graphAreas.Remove(from);
        }

        GraphAreas.Remove(from);
        Debug.Log($"index {from} removed from partitions");
    }

    List<MazeCell> GetJunctionCells(int from, int to)
    {
        var junctionCells = new List<MazeCell>();

        foreach(var endFrom in GraphAreas[from].ends)
            foreach(var endTo in GraphAreas[to].ends)
                if(endFrom == endTo && !junctionCells.Contains(endFrom))
                {
                    junctionCells.Add(endFrom);
                }

        return junctionCells;
    }

    int GetJunctionCount(int index)
    {
        int count = 0;

        foreach (var cell in GraphAreas[index].ends)
            if (cell.graphAreas.Count > 1)
                count++;

        return count;
    }

    List<int> GetConnectedIndices(int index)
    {
        List<int> indices = new List<int>();

        foreach(var cell in GraphAreas[index].ends)
        {
            foreach (var key in cell.graphAreas.Keys)
                if (key != index)
                    indices.Add(key);
        }

        return indices;
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 190, 80, 60), "Find Paths"))
            CreateGraph();
    }
}
