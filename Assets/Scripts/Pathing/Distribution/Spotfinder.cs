using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Spotfinder : MonoBehaviour
{
    public Maze maze;
    public PhysicsSim simulation;
    public Pathfinder pathfinder;
    public AreaFinder areafinder;
    public GameObject layout;

    [SerializeField] int placeCount = 1;
    List<MazeCell> availableSpots = new List<MazeCell>();
    List<MazeCell> placedSpots = new List<MazeCell>();

    public List<Tile> activeTileSet = new List<Tile>();

    float spotHeight = 3f; // used to scale height (in z axis) for placed tiles

    public void DeterminePlacement()
    {
        FindAvailableSpots();
        PickSpotsRandom();
        DetermineTilePlacement();
    }

    void FindAvailableSpots()
    {
        availableSpots.Clear();

        for (int i = 0; i < maze.size.x; i++)
        {
            for (int j = 0; j < maze.size.y; j++)
            {
                if ((i == pathfinder.startPos.x && j == pathfinder.startPos.y) || (i == pathfinder.endPos.x && j == pathfinder.endPos.y))
                    continue;

                if (maze.cells[i, j].connectedCells.Count <= 1)
                {
                    maze.cells[i, j].isPlaceable = true;
                    if (!availableSpots.Contains(maze.cells[i, j]))
                        availableSpots.Add(maze.cells[i, j]);
                    continue;
                }

                int connectedCount = 0;

                foreach (var cell in maze.cells[i, j].connectedCells)
                {
                    if(cell.pos.y > maze.cells[i, j].pos.y)
                    {
                        if(i + 1 < maze.size.x
                           && cell.connectedCells.Contains(maze.cells[cell.pos.x + 1, cell.pos.y])
                           && maze.cells[cell.pos.x + 1, cell.pos.y].connectedCells.Contains(maze.cells[i + 1, j])
                           && maze.cells[i + 1, j].connectedCells.Contains(maze.cells[i,j]))
                        {
                            connectedCount++;
                        }
                    }
                    else if (cell.pos.x > maze.cells[i, j].pos.x)
                    {
                        if (j - 1 >= 0
                            && cell.connectedCells.Contains(maze.cells[cell.pos.x, cell.pos.y - 1])
                            && maze.cells[cell.pos.x, cell.pos.y - 1].connectedCells.Contains(maze.cells[i, j - 1])
                            && maze.cells[i, j - 1].connectedCells.Contains(maze.cells[i, j]))
                        {
                            connectedCount++;
                        }
                    }
                    else if (cell.pos.y < maze.cells[i, j].pos.y)
                    {
                        if (i - 1 >= 0
                            && cell.connectedCells.Contains(maze.cells[cell.pos.x - 1, cell.pos.y])
                            && maze.cells[cell.pos.x - 1, cell.pos.y].connectedCells.Contains(maze.cells[i - 1, j])
                            && maze.cells[i - 1, j].connectedCells.Contains(maze.cells[i, j]))
                        {
                            connectedCount++;
                        }
                    }
                    else if (cell.pos.x < maze.cells[i, j].pos.x)
                    {
                        if (j + 1 < maze.size.y
                            && cell.connectedCells.Contains(maze.cells[cell.pos.x, cell.pos.y + 1])
                            && maze.cells[cell.pos.x, cell.pos.y + 1].connectedCells.Contains(maze.cells[i, j + 1])
                            && maze.cells[i, j + 1].connectedCells.Contains(maze.cells[i, j]))
                        {
                            connectedCount++;
                        }
                    }
                }

                if (connectedCount >= 1)
                {
                    maze.cells[i, j].isPlaceable = true;
                    if (!availableSpots.Contains(maze.cells[i, j]))
                        availableSpots.Add(maze.cells[i, j]);
                }
                else
                {
                    maze.cells[i, j].isPlaceable = false;
                    if (availableSpots.Contains(maze.cells[i, j]))
                        availableSpots.Remove(maze.cells[i, j]);
                }
            }
        }

        for (int i = 0; i < maze.size.x; i++)
        {
            for (int j = 0; j < maze.size.y; j++)
            {
                maze.cells[i, j].placeableNeighbourCount = 0;

                foreach (var cell in maze.cells[i, j].connectedCells)
                {
                    if (cell.isPlaceable)
                        maze.cells[i, j].placeableNeighbourCount++;
                }
            }
        }
    }

    void PickSpotsRandom()
    {
        var count = placeCount;

        placedSpots.Clear();

        while(availableSpots.Count > 0 && count > 0)
        {
            int random = Random.Range(0, availableSpots.Count);            

            if(pathfinder.TryNeighbourPaths(availableSpots[random]) && !HasDiagonalDisconnect(availableSpots[random]))
            {
                availableSpots[random].state = 2;

                placedSpots.Add(availableSpots[random]);

                foreach (var cell in availableSpots[random].connectedCells)
                {
                    cell.connectedCells.Remove(availableSpots[random]);
                    cell.placedConnectedCells.Add(availableSpots[random]);
                }

                foreach (var cell in availableSpots[random].placedConnectedCells)
                {
                    cell.connectedCells.Remove(availableSpots[random]);
                    cell.placedConnectedCells.Add(availableSpots[random]);
                }

                count--;
            }

            availableSpots[random].isPlaceable = false;
            availableSpots.Remove(availableSpots[random]);
        }

        placedSpots = placedSpots.OrderByDescending(x => x.cardinalBits + x.diagonalBits).ToList();
    }

    private bool HasDiagonalDisconnect(MazeCell cell)
    {
        var length = MazeDirections.allVectors.Length;
        for (int i = 0; i < length; i += 2)
        {
            var xpos = cell.pos.x + MazeDirections.allVectors[i].x;
            var ypos = cell.pos.y + MazeDirections.allVectors[i].y;

            if (xpos < 0 || ypos < 0 || xpos > maze.size.x - 1 || ypos > maze.size.y - 1)
                continue;

            var xdiagonal = cell.pos.x + MazeDirections.allVectors[(i + 1) % length].x;
            var ydiagonal = cell.pos.y + MazeDirections.allVectors[(i + 1) % length].y;

            if (xdiagonal < 0 || ydiagonal < 0 || xdiagonal > maze.size.x - 1 || ydiagonal > maze.size.y - 1)
                continue;

            var xnext = cell.pos.x + MazeDirections.allVectors[(i + 2) % length].x;
            var ynext = cell.pos.y + MazeDirections.allVectors[(i + 2) % length].y;

            if (xnext < 0 || ynext < 0 || xnext > maze.size.x - 1 || ynext > maze.size.y - 1)
                continue;

            if (!cell.connectedCells.Contains(maze.cells[xpos, ypos]) || !cell.connectedCells.Contains(maze.cells[xnext, ynext]))
                continue;

            bool areConnected = maze.cells[xpos, ypos].connectedCells.Contains(maze.cells[xdiagonal, ydiagonal])
                             && maze.cells[xnext, ynext].connectedCells.Contains(maze.cells[xdiagonal, ydiagonal]);

            if (!areConnected)
                return true;
        }

        return false;
    }

    private void DetermineTilePlacement()
    {
        DetermineNeighbourBits();

        foreach(var spot in placedSpots)
        {
            Tile tileToPlace = SetTile(spot);

            if (tileToPlace != null)
            {
                var go = Instantiate(tileToPlace.tile);
                go.transform.position = new Vector3(spot.transform.position.x, spot.transform.position.y,
                                                    spot.transform.position.z - spotHeight / 2);
                go.transform.rotation = Quaternion.Euler(go.transform.rotation.x, go.transform.rotation.y, go.transform.rotation.z + tileToPlace.selectedRule.rotation);
                Vector3 scale = tileToPlace.selectedRule.rotation % 180f == 0 ?
                                new Vector3(maze.cellScaleX, maze.cellScaleY, 1f) : new Vector3(maze.cellScaleY, maze.cellScaleX, 1f);
                go.transform.localScale = scale;
                go.transform.parent = layout.transform;

                maze.placementInScene.Add(go); // adding placement to maze to then be copied over to physics sim
            }
        }
    }

    private Tile SetTile(MazeCell spot)
    {
        List<Tile> candidates = new List<Tile>();

        foreach(var potentialTile in activeTileSet)
        {
            foreach(var rule in potentialTile.rules)
            {
                if (rule.cardinal == spot.cardinalBits)
                {
                    candidates.Add(potentialTile);
                    potentialTile.selectedRule = rule;
                    break;
                }
            }
        }

        foreach(var candidate in candidates)
        {
            bool test = (candidate.selectedRule.diagonal & spot.diagonalBits) == candidate.selectedRule.diagonal;

            if (test)
                return candidate;
        }

        Debug.Log("No candidate found for: " + spot.gameObject.name + " with candidate count of: " + candidates.Count);
        return null;
    }

    private void DetermineNeighbourBits()
    {
        for (int i = 0; i < maze.size.x; i++)
        {
            for (int j = 0; j < maze.size.y; j++)
            {
                var cell = maze.cells[i, j];

                cell.cardinalBits = 0;
                cell.diagonalBits = 0;

                for (int k = 0; k < MazeDirections.cardinalVectors.Length; k++)
                {
                    var xpos = cell.pos.x + MazeDirections.cardinalVectors[k].x;
                    var ypos = cell.pos.y + MazeDirections.cardinalVectors[k].y;

                    if (xpos < 0 || ypos < 0 || xpos > maze.size.x - 1 || ypos > maze.size.y - 1)
                        continue;

                    var neighbour = maze.cells[xpos, ypos];

                    if (cell.connectedCells.Contains(neighbour) || cell.placedConnectedCells.Contains(neighbour))
                    //if (cell.connectedCells.Contains(neighbour))
                    {
                        if (cell.connectedCells.Contains(neighbour))
                            cell.cardinalBits |= 1 << k;
                        //Debug.Log($"{cell.gameObject.name} has {1 << k} added to cardinal bits for neighbour at ({xpos},{ypos})");

                        var xdiagonal = cell.pos.x + MazeDirections.diagonalVectors[k].x;
                        var ydiagonal = cell.pos.y + MazeDirections.diagonalVectors[k].y;

                        if (xdiagonal < 0 || ydiagonal < 0 || xdiagonal > maze.size.x - 1 || ydiagonal > maze.size.y - 1)
                            continue;

                        var diagonal = maze.cells[xdiagonal, ydiagonal];

                        if (diagonal.state > 1)
                            continue;

                        var xnext = cell.pos.x + MazeDirections.cardinalVectors[(k + 1) % MazeDirections.cardinalVectors.Length].x;
                        var ynext = cell.pos.y + MazeDirections.cardinalVectors[(k + 1) % MazeDirections.cardinalVectors.Length].y;
                        var next = maze.cells[xnext, ynext];

                        if (neighbour.connectedCells.Contains(diagonal) && next.connectedCells.Contains(diagonal)
                            && (cell.connectedCells.Contains(next) || cell.placedConnectedCells.Contains(next)))
                        {
                            cell.diagonalBits |= 1 << k;
                            //Debug.Log($"{cell.gameObject.name} has {1 << k} added to diagonal bits for neighbour at ({xdiagonal},{ydiagonal})");
                        }
                    }
                }
            }
        }
    }
}
