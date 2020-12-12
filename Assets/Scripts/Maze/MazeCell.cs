using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MazeCell : FastPriorityQueueNode
{
    public IntVector2 pos;
	public int areaIndex;
	public int state = 0;
	public int row, col;

	public TextMeshPro cellText;
	public Material mat;
	Color initialColor;

	public bool[] visited;
	public bool searched = false;

	public HashSet<MazeCell> connectedCells = new HashSet<MazeCell>();
	public HashSet<MazeCell> placedConnectedCells = new HashSet<MazeCell>();
	public MazeCell[] exploredFrom;
	public int[] distanceFromStart;

	public int searchSize = 10; // should be same as search size in pathfinder script

	public bool isPlaceable = false; // used by spotfinder to find available placement spots
	public int placeableNeighbourCount = 0;

	// used for bit ops for spot placement and propagation
	public int cardinalBits = 0;
	public int diagonalBits = 0;
	public int allNeighbourBits = 0;

	List<Color> requestedIndicatorColors = new List<Color>();

	// for keeping track of items placed on cell
	public Dictionary<PlaceableItemType, PlaceableItem> placedItems = new Dictionary<PlaceableItemType, PlaceableItem>();

	// for A*
	public int travelCost;

	private MazeCellEdge[] edges = new MazeCellEdge[MazeDirections.Count];

    void Awake()
    {
        mat = transform.GetChild(0).GetComponent<Renderer>().material;
		initialColor = mat.color;

        visited = new bool[searchSize];
		//for(int i = 0; i < visited.Length; i++)
		//{ visited[i] = false; }
		exploredFrom = new MazeCell[searchSize];
		distanceFromStart = new int[searchSize];
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
			int skips = UnityEngine.Random.Range(0, MazeDirections.Count - initializedEdgeCount);
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

	public HashSet<MazeCell> GetNeighbours()
    {
		return connectedCells;
    }

	public void PlaceItem(PlaceableItemType type, PlaceableItem item)
    {
		if (!placedItems.ContainsKey(type))
			placedItems.Add(type, item);
		else
			placedItems[type] = item;
    }

	public void RemoveItem(PlaceableItemType type)
    {
		if (placedItems.ContainsKey(type))
			placedItems[type] = null;
		else
			Debug.Log("Item key not found in cell: " + gameObject.name);
    }

	public bool HasPlacedItem(PlaceableItemType type)
	{
		if (placedItems.ContainsKey(type) && placedItems[type])
			return true;

		return false;
	}

	public bool HasPlacedItem()
	{
		foreach(var item in placedItems.Values)
        {
			if (item != null)
				return true;
        }

		return false;
	}

	public void DisplayText(string text)
    {
		cellText.text = text;
    }

	public void IndicateAOE(Color aoeColor)
    {
		requestedIndicatorColors.Add(aoeColor);
		mat.color = MixIndicatorColors();
    }

	public void ClearAOE(Color aoeColor)
    {
		requestedIndicatorColors.Remove(aoeColor);
		mat.color = MixIndicatorColors();
    }

	private Color MixIndicatorColors()
    {
		if (requestedIndicatorColors.Count == 0)
			return initialColor;

		float tempR = 0;
		float tempG = 0;
		float tempB = 0;
		foreach(var color in requestedIndicatorColors)
        {
			tempR += color.r;
			tempG += color.g;
			tempB += color.b;
        }

		return new Color(tempR / requestedIndicatorColors.Count,
						 tempG / requestedIndicatorColors.Count,
						 tempB / requestedIndicatorColors.Count);
    }
}
