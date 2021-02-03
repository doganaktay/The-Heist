using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GraphFinder : MonoBehaviour
{
    public Maze maze;

    int index;
    Queue<MazeCell> frontier = new Queue<MazeCell>();
    List<MazeCell> currentArea = new List<MazeCell>();
    List<MazeCell> ends = new List<MazeCell>();
    bool[,] visited;
    static Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)> GraphAreas;

    [SerializeField]
    bool showDebugDisplay = false;

    private void Awake()
    {
        GameManager.MazeGenFinished += CreateGraph;
    }

    private void OnDisable()
    {
        GameManager.MazeGenFinished -= CreateGraph;
    }

    public void CreateGraph()
    {
        GraphAreas = new Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)>();
        visited = new bool[maze.size.x, maze.size.y];
        frontier.Enqueue(GameManager.StartCell);

        index = 0;

        // PASS 1
        //Debug.Log("Pass 1:");

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

        // PASS 2

        //Debug.Log($"Pass 2: {GraphAreas.Count} partitions");

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

                if (i == 1 || (i == 2 && GraphAreas[coveredIndex].all.Count >= 3))
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
                    GraphAreas.Remove(indexToMerge);
            }

            if (!GraphAreas.ContainsKey(index))
            {
                GraphAreas.Add(index, (new List<MazeCell>(currentArea), new List<MazeCell>(ends)));
            }

            index++;
        }

        //partitionKeys = "Pass 2 Keys: ";

        //foreach (var key in GraphAreas.Keys)
        //    partitionKeys += key.ToString() + ", ";

        //Debug.Log(partitionKeys);

        // PASS 3

        List<int> coveredIndices = new List<int>();
        List<int> internalCornerIndices = new List<int>();

        for(int i = 0; i < maze.size.x; i++)
        {
            for(int j = 0; j < maze.size.y; j++)
            {
                var cell = maze.cells[i, j];

                if (cell.state > 1 || cell.graphAreas.Keys.Count > 1)
                    continue;

                var index = cell.GetGraphAreaIndices()[0];

                if (coveredIndices.Contains(index))
                    continue;

                var junctions = GetJunctionCells(index);
                var junctionCount = junctions.Count;

                if (junctionCount == 1 && GraphAreas[index].all.Count == 2)
                {
                    bool isConnectedToSingleArea = true;

                    foreach (var end in GraphAreas[index].ends)
                        if (end.GraphAreaCount > 2)
                            isConnectedToSingleArea = false;

                    var connectedIndices = GetConnectedIndices(index);

                    if (isConnectedToSingleArea)
                        MergeAreas(index, connectedIndices[0]);
                    else
                    {
                        var smallestAreaIndex = junctions[0].GetSmallestAreaIndex(index);
                        MergeAreas(index, smallestAreaIndex);
                    }
                }
                else if (junctionCount == 2 && GraphAreas[index].all.Count == 3)
                {
                    if (cell.IsInternalCorner() && !internalCornerIndices.Contains(index))
                    {
                        internalCornerIndices.Add(index);

                        for(int k = 0; k < MazeDirections.diagonalVectors.Length; k++)
                        {
                            var newX = i + MazeDirections.diagonalVectors[k].x;
                            var newY = j + MazeDirections.diagonalVectors[k].y;

                            if (newX < 0 || newY < 0 || newX >= maze.size.x || newY >= maze.size.y)
                                continue;

                            var otherCell = maze.cells[newX, newY];

                            bool hasConnection = (cell.diagonalBits & 1 << k) != 0;

                            if(hasConnection && otherCell.IsInternalCorner())
                            {
                                var otherIndex = otherCell.GetGraphAreaIndices()[0];
                                MergeAreas(index, otherIndex);
                                coveredIndices.Add(otherIndex);

                                if (!internalCornerIndices.Contains(otherIndex))
                                    internalCornerIndices.Add(otherIndex);
                            }
                        }

                    }
                }

                coveredIndices.Add(index);
            }
        }

        // DISPLAY

        if (showDebugDisplay)
        {
            Color junctionColor = new Color(1f, 0f, 0.1f);

            foreach(var area in GraphAreas)
            {
                var randomColor = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));

                foreach(var cell in area.Value.all)
                {
                    string indices = "";

                    foreach (var key in cell.graphAreas.Keys)
                        indices += key.ToString() + " ";

                    if (!cell.IsLockedConnection || cell.graphAreas.Count == 1)
                        cell.DisplayText(indices, randomColor);
                    else
                        cell.DisplayText(indices, junctionColor);
                }
            }
        }

    }


    void SearchCell(MazeCell currentCell, MazeCell cameFrom = null)
    {
        if (visited[currentCell.pos.x, currentCell.pos.y])
            return;

        //Debug.Log($"Searching {currentCell.gameObject.name}");

        if (currentCell.connectedCells.Count < 3 || (currentCell.connectedCells.Count >= 3 && currentCell.UnexploredDirectionCount == 1))
            visited[currentCell.pos.x, currentCell.pos.y] = true;

        if(!currentArea.Contains(currentCell))
            currentArea.Add(currentCell);

        int count = currentCell.connectedCells.Count;

        if (count == 1)
        {
            if(!ends.Contains(currentCell))
                ends.Add(currentCell);

            if (currentArea.Count == 1)
                foreach (var cell in currentCell.connectedCells)
                    SearchCell(cell, currentCell);
                
        }
        else if (count == 2)
        {
            foreach(var cell in currentCell.connectedCells)
            {
                if (cameFrom != null && cell == cameFrom)
                    continue;

                SearchCell(cell, currentCell);
            }
        }
        else
        {
            if (!ends.Contains(currentCell))
                ends.Add(currentCell);

            if(currentArea.Count == 1)
            {
                foreach(var cell in currentCell.connectedCells)
                {
                    if ((cell.connectedCells.Count < 3 && !visited[cell.pos.x, cell.pos.y]) || (cell.connectedCells.Count >= 3 && !currentCell.HasMadeConnection(cell)))
                    {
                        SearchCell(cell, currentCell);
                        break;
                    }
                }
            }

            if(currentCell.UnexploredDirectionCount > 0)
                currentCell.UnexploredDirectionCount--;

            if (currentCell.UnexploredDirectionCount > 0 && currentCell.LastIndexAddedToQueue != index)
            {
                frontier.Enqueue(currentCell);
                currentCell.LastIndexAddedToQueue = index;
            }
        }
    }

    void SearchCellForMerge(MazeCell currentCell, HashSet<int> coveredIndices)
    {
        if (visited[currentCell.pos.x, currentCell.pos.y])
            return;

        visited[currentCell.pos.x, currentCell.pos.y] = true;

        if(!currentArea.Contains(currentCell))
            currentArea.Add(currentCell);

        if (currentCell.IsLockedConnection && !ends.Contains(currentCell))
            ends.Add(currentCell);

        foreach (var key in currentCell.graphAreas.Keys)
            coveredIndices.Add(key);

        var currentConnectedCount = currentCell.connectedCells.Count;

        foreach (var cell in currentCell.connectedCells)
        {
            //if(currentConnectedCount == 3 && cell.connectedCells.Count == 3)
            //{
            //    var bitPatternToCheck = currentCell.cardinalBits.RotatePattern(4, 2);

            //    if ((bitPatternToCheck ^ cell.cardinalBits) == 0)
            //        continue;
            //}

            SearchCellForMerge(cell, coveredIndices);
        }

    }

    void MergeAreas(int from, int to)
    {
        var junctionCells = GetJunctionCells(from, to);

        var endsToMerge = new List<MazeCell>(GraphAreas[from].ends);
        var cellsToMerge = new List<MazeCell>(GraphAreas[from].all);

        foreach (var cell in junctionCells)
        {
            endsToMerge.Remove(cell);
            cellsToMerge.Remove(cell);
        }

        foreach(var cell in GraphAreas[to].all.ToList())
            cell.AddToGraphArea(to, cellsToMerge, endsToMerge);

        GraphAreas[to].all.AddRange(cellsToMerge);
        GraphAreas[to].ends.AddRange(endsToMerge);

        foreach(var cell in GraphAreas[from].all.ToList())
        {
            if(!cell.graphAreas.ContainsKey(to))
                cell.graphAreas.Add(to, (GraphAreas[to].all, GraphAreas[to].ends));

            cell.graphAreas.Remove(from);
        }

        GraphAreas.Remove(from);
    }

    int GetJunctionCellCount(int from, int to)
    {
        return GetJunctionCells(from, to).Count;
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

    int GetJunctionCellCount(int index)
    {
        return GetJunctionCells(index).Count;
    }

    List<MazeCell> GetJunctionCells(int index)
    {
        var junctionCells = new List<MazeCell>();

        foreach (var end in GraphAreas[index].ends)
            if (end.graphAreas.Count > 1)
                junctionCells.Add(end);

        return junctionCells;
    }

    int GetConnectedIndexCount(int index)
    {
        return GetConnectedIndices(index).Count;
    }

    List<int> GetConnectedIndices(int index)
    {
        List<int> indices = new List<int>();

        foreach(var cell in GraphAreas[index].ends)
        {
            //Debug.Log($"Searching {cell.gameObject.name} for indices connected except {index}");

            foreach (var key in cell.graphAreas.Keys)
            {
                if (key != index && !indices.Contains(key))
                    indices.Add(key);
            }
        }

        return indices;
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 190, 80, 60), "Find Paths"))
            CreateGraph();
    }
}
