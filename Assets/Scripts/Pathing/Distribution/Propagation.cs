using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Propagation : MonoBehaviour
{
    public static Maze maze;
    public static Propagation instance;

    [Tooltip("1 / this value is used to scale strength attenuation after each iteration of propagation")]
    [SerializeField] private float attenuationScalar = 1f;

    public int[,] connectivityGrid;
    public bool[,] isSearched;
    Queue<MazeCell> queue;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            UnityEngine.Debug.LogError("Trying to create second instance of Propagation module");
    }

    public void BuildConnectivityGrid()
    {
        if(connectivityGrid == null)
        {
            connectivityGrid = new int[maze.size.x, maze.size.y];
            isSearched = new bool[maze.size.x, maze.size.y];
            queue = new Queue<MazeCell>();
        }

        for (int i = 0; i < maze.size.x; i++)
        {
            for(int j = 0; j < maze.size.y; j++)
            {
                for(int k = 0; k < MazeDirections.allVectors.Length; k++)
                {
                    var xpos = i + MazeDirections.allVectors[k].x;
                    var ypos = j + MazeDirections.allVectors[k].y;

                    if (!IsInBounds(xpos, ypos))
                        continue;

                    if (maze.cells[i, j].IsConnectedTo(maze.cells[xpos, ypos], k % 2 == 0))
                        maze.cells[i, j].allNeighbourBits |= 1 << k;

                    //maze.cells[i, j].DisplayText(maze.cells[i, j].allNeighbourBits.ToString());
                }
            }
        }
    }

    public List<MazeCell> Propagate(MazeCell center, float initialStrength, float minViableStrength)
    {
        List<MazeCell> requestedAreaOfEffect = new List<MazeCell>();

        Array.Clear(isSearched, 0, isSearched.Length);
        queue.Clear();

        queue.Enqueue(center);
        isSearched[center.pos.x, center.pos.y] = true;

        var strength = initialStrength;

        int currentRingCellCount = 1;
        int nextRingCellCount = 0;
        int totalRingCount = 0;

        List<MazeCell> currentRing = new List<MazeCell>();

        while(queue.Count > 0)
        {
            var current = queue.Dequeue();
            currentRingCellCount--;
            currentRing.Add(current);

            for (int k = 0; k < MazeDirections.allVectors.Length; k++)
            {
                var xpos = current.pos.x + MazeDirections.allVectors[k].x;
                var ypos = current.pos.y + MazeDirections.allVectors[k].y;

                if ((current.allNeighbourBits & 1<<k) != 0 && !isSearched[xpos, ypos])
                {
                    isSearched[xpos, ypos] = true;
                    queue.Enqueue(maze.cells[xpos, ypos]);
                    nextRingCellCount++;
                }
            }

            if(currentRingCellCount == 0)
            {
                var strengthPerCell = strength / currentRing.Count;

                if (strengthPerCell < minViableStrength)
                { break; }

                foreach(var cell in currentRing)
                {
                    if (cell.state < 2)
                        requestedAreaOfEffect.Add(cell);
                }

                currentRing.Clear();

                currentRingCellCount = nextRingCellCount;
                nextRingCellCount = 0;

                totalRingCount++;

                strength = Attenuate(strength, totalRingCount, currentRingCellCount);

            }
        }


        return requestedAreaOfEffect;
    }

    private float Attenuate(float strength, float iteration, float ringCount)
    {
        var d = iteration * ringCount / attenuationScalar;

        return strength / (1 + d * d);
    }

    private bool IsInBounds(int x, int y) => x >= 0 && y >= 0 && x < maze.size.x && y < maze.size.y;
}
