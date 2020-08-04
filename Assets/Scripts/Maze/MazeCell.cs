using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MazeCell : MonoBehaviour
{
    public IntVector2 pos;
	public int areaIndex;
	public int state = 0;
	public int row, col;

	public TextMeshPro cellText;
	public Material mat;
	public Color startColor;

	public bool[] visited;
	public bool searched = false;

	public HashSet<MazeCell> connectedCells = new HashSet<MazeCell>();
	public MazeCell[] exploredFrom;
	public int[] distanceFromStart;
	

	private MazeCellEdge[] edges = new MazeCellEdge[MazeDirections.Count];

    void Awake()
    {
		mat = transform.GetChild(0).GetComponent<Renderer>().material;
		startColor = mat.color;

		visited = new bool[5];
		for(int i = 0; i < visited.Length; i++)
        { visited[i] = false; }
		exploredFrom = new MazeCell[5];
		distanceFromStart = new int[5];
    }

    public MazeCellEdge GetEdge(MazeDirection direction)
	{
		return edges[(int)direction];
	}

	public void SetEdge(MazeDirection direction, MazeCellEdge edge)
	{
		edges[(int)direction] = edge;
		initializedEdgeCount += 1;
	}

	private int initializedEdgeCount;

	public bool IsFullyInitialized
	{
		get
		{
			return initializedEdgeCount == MazeDirections.Count;
		}
	}

	public MazeDirection RandomUninitializedDirection
	{
		get
		{
			int skips = Random.Range(0, MazeDirections.Count - initializedEdgeCount);
			for (int i = 0; i < MazeDirections.Count; i++)
			{
				if (edges[i] == null)
				{
					if (skips == 0)
					{
						return (MazeDirection)i;
					}
					skips -= 1;
				}
			}

			throw new System.InvalidOperationException("MazeCell has no uninitialized directions left.");
		}
	}
}
