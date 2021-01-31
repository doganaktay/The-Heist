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

	[SerializeField]
	PerObjectMaterialProperties props;

	// actually connected cells
	public HashSet<MazeCell> connectedCells = new HashSet<MazeCell>();
	// connected cells with placement on them
	public HashSet<MazeCell> placedConnectedCells = new HashSet<MazeCell>();
	// cells only connected through special connections through walls (like grates)
	public HashSet<MazeCell> specialConnectedCells = new HashSet<MazeCell>();

	#region Corridors and Rooms

	// for use in identifying corridors and islands
	public bool IsGraphConnection { get; private set; }
	public bool IsLockedConnection { get; set; } = false;
	public Dictionary<int, List<MazeCell>> graphConnections;
	public List<int> GetAreaIndices() => new List<int>(graphConnections.Keys);
	public int AreaCount { get => graphConnections.Keys.Count; }

	public int LastIndexAddedToQueue { get; set; } = -1;

	int unexploredDirectionCount = -1;
	public int UnexploredDirectionCount
	{
		get
		{
			if (unexploredDirectionCount < 0)
				return connectedCells.Count;
			else
				return unexploredDirectionCount;
		}

		set => unexploredDirectionCount = value;
	}

	public bool IsInternalCorner()
	{
		for (int i = 0; i < CornerBitPatterns.Length; i++)
		{
			Debug.Log($"{gameObject.name} Checking {Convert.ToString(cardinalBits, 2)} & {Convert.ToString(CornerBitPatterns[i].cardinal << i, 2)} " +
				$"and {Convert.ToString(diagonalBits, 2)} & {Convert.ToString(CornerBitPatterns[i].diagonal << i, 2)}");

			if (diagonalBits == 0)
				continue;

			if ((cardinalBits & CornerBitPatterns[i].cardinal) == CornerBitPatterns[i].cardinal
				&& (diagonalBits & CornerBitPatterns[i].diagonal) == CornerBitPatterns[i].diagonal)
				return true;
		}

		return false;
	}

	static (int cardinal, int diagonal)[] CornerBitPatterns = new(int cardinal, int diagonal)[]
	{
		(1 << 0 | 1 << 1, 1 << 0),
		(1 << 1 | 1 << 2, 1 << 1),
		(1 << 2 | 1 << 3, 1 << 2),
		(1 << 3 | 1 << 0, 1 << 3)

	};

	public bool HasMadeConnection(MazeCell target)
    {
		foreach(var g in graphConnections.Values)
        {
			foreach (var end in g)
            {
				//Debug.Log($"{gameObject.name} checking {target.gameObject.name} against {end.gameObject.name} for a connection");

				if (end == target)
					return true;
            }
        }

		//Debug.Log($"{gameObject.name} has no connection to {target.gameObject.name}");

		return false;
    }

	public void SetGraphConnections(int index, List<MazeCell> cells)
    {
		if(graphConnections == null)
			graphConnections = new Dictionary<int, List<MazeCell>>();

		if (cells.Contains(this))
			IsGraphConnection = true;

		if (!graphConnections.ContainsKey(index))
			graphConnections.Add(index, new List<MazeCell>(cells));
		else
			graphConnections[index] = new List<MazeCell>(cells);

		//UnexploredDirectionCount = AreaCount;
	}
	public void AddGraphConnection(int index, MazeCell cell)
	{
		if (!graphConnections.ContainsKey(index))
        {
			Debug.Log($"{gameObject.name} does not belong to area {index}");
        }
        else
        {
			if(!graphConnections[index].Contains(cell))
				graphConnections[index].Add(cell);
        }
	}

	public void RemoveGraphConnections(int index)
    {
		if (graphConnections.ContainsKey(index))
			graphConnections.Remove(index);
    }

	public List<MazeCell> GetConnections()
    {
		if (!IsGraphConnection)
			return graphConnections[GetAreaIndices()[0]];
        else
        {
			var connections = new List<MazeCell>();

			foreach(var set in graphConnections)
            {
				foreach(var item in set.Value)
                {
					connections.Add(item);
                }
            }

			return connections;
        }
    }
	public List<MazeCell> GetConnections(int index, bool avoid = true)
    {
		if (!IsGraphConnection)
        {
			Debug.Log($"{gameObject.name} is not a connection point, returning the cell's end nodes");
			return graphConnections[GetAreaIndices()[0]];
		}
        else
        {
			var connections = new List<MazeCell>();

			foreach (var set in graphConnections)
			{
                if ((avoid && set.Key == index) || (!avoid && set.Key != index))
					continue;
                

				foreach (var item in set.Value)
				{
					connections.Add(item);
				}
			}

			return connections;
		}
    }

    #endregion

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

	[HideInInspector]
	public bool HasCCTVCoverage { get; set; } = false;

	// for keeping track of items placed on cell
	public Dictionary<PlaceableItemType, PlaceableItem> placedItems = new Dictionary<PlaceableItemType, PlaceableItem>();

	// for A*
	public int travelCost;

	private MazeCellEdge[] edges = new MazeCellEdge[MazeDirections.Count];

    void Awake()
    {
        visited = new bool[searchSize];
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

		//requestedIndicatorColors.Add(aoeColor);
		props.SetSecondaryColor();
    }

	public void ClearAOE(Color aoeColor)
    {
		//requestedIndicatorColors.Remove(aoeColor);
		props.SetBaseColor();
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

#if UNITY_EDITOR
	public void PrintCellInfo()
    {
		Debug.Log($"{gameObject.name} State: {state} Neighbors - Connected: {connectedCells.Count} Placed: {placedConnectedCells.Count} Special: {specialConnectedCells.Count}");
    }
#endif
}
