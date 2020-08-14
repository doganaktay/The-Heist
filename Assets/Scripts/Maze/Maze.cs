using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maze : MonoBehaviour
{
    public IntVector2 size;

    public MazeCell cellPrefab;
    public float cellScaleX, cellScaleY;

    public MazeCell[,] cells;

    public MazeCellPassage passagePrefab;
    public MazeCellWall wallPrefab;

    public void Generate()
    {
        cells = new MazeCell[size.x, size.y];

        List<MazeCell> activeCells = new List<MazeCell>();
        DoFirstGenerationStep(activeCells);
        while(activeCells.Count > 0)
        {
            DoNextGenerationStep(activeCells);
        }
    }

    MazeCell CreateCell(IntVector2 location)
    {
        MazeCell newCell = Instantiate(cellPrefab);
        cells[location.x, location.y] = newCell;
        newCell.name = "Maze Cell " + location.x + ", " + location.y;
        newCell.pos = location;
        newCell.transform.parent = transform;
        float xPos = (location.x - size.x * 0.5f) * cellScaleX + cellScaleX / 2;
        float yPos = (location.y - size.y * 0.5f) * cellScaleY + cellScaleY / 2;
        newCell.transform.position = new Vector3(xPos, yPos, 0f);

        newCell.row = location.x;
        newCell.col = location.y;

        return newCell;
    }

    public IntVector2 RandomCoordinates
    {
        get
        {
            return new IntVector2(UnityEngine.Random.Range(0, size.x), UnityEngine.Random.Range(0, size.y));
        }
    }

    public bool ContainsPos(IntVector2 pos)
    {
        return pos.x >= 0 && pos.x < size.x && pos.y >= 0 && pos.y < size.y;
    }

    public MazeCell GetCell(IntVector2 pos)
    {
        return cells[pos.x, pos.y];
    }

    private void DoFirstGenerationStep(List<MazeCell> activeCells)
    {
        activeCells.Add(CreateCell(RandomCoordinates));
    }

    private void DoNextGenerationStep(List<MazeCell> activeCells)
    {
        int currentIndex = activeCells.Count - 1;
        MazeCell currentCell = activeCells[currentIndex];
        if (currentCell.IsFullyInitialized)
        {
            activeCells.RemoveAt(currentIndex);
            return;
        }
        MazeDirection direction = currentCell.RandomUninitializedDirection;
        IntVector2 newPos = currentCell.pos + direction.ToIntVector2();
        if (ContainsPos(newPos))
        {
            MazeCell neighbor = GetCell(newPos);
            if (neighbor == null)
            {
                neighbor = CreateCell(newPos);
                CreatePassage(currentCell, neighbor, direction);
                activeCells.Add(neighbor);

                currentCell.connectedCells.Add(neighbor);
                neighbor.connectedCells.Add(currentCell);
            }
            else
            {
                CreateWall(currentCell, neighbor, direction);
 
                currentCell.connectedCells.Remove(neighbor);
                neighbor.connectedCells.Remove(currentCell);
            }
        }
        else
        {
            CreateWall(currentCell, null, direction);
        }
    }

    private void CreatePassage(MazeCell cell, MazeCell otherCell, MazeDirection direction)
    {
        MazeCellPassage passage = Instantiate(passagePrefab);
        passage.Initialize(cell, otherCell, direction);
        passage = Instantiate(passagePrefab);
        passage.Initialize(otherCell, cell, direction.GetOpposite());
    }

    private void CreateWall(MazeCell cell, MazeCell otherCell, MazeDirection direction)
    {
        MazeCellWall wall = Instantiate(wallPrefab);
        wall.Initialize(cell, otherCell, direction);
        if (otherCell != null)
        {
            wall = Instantiate(wallPrefab);
            wall.Initialize(otherCell, cell, direction.GetOpposite());
        }
    }

    // for A* through interface
    public float Cost(MazeCell a, MazeCell b)
    {
        return b.travelCost;
    }
}
