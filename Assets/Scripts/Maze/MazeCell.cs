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

	#region Graph

	// for use in identifying corridors and islands
	public bool IsGraphConnection { get; private set; }
	public bool IsLockedConnection { get; set; } = false;
	public Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)> graphAreas;
	public List<int> GetGraphAreaIndices() => new List<int>(graphAreas.Keys);
	public int GraphAreaCount => graphAreas.Keys.Count;
	public bool IsDeadEnd => connectedCells.Count == 1;
	public bool IsJunction => IsGraphConnection && graphAreas.Keys.Count > 1;

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
		foreach(var g in graphAreas.Values)
        {
			foreach (var end in g.ends)
            {
				if (end == target)
					return true;
            }
        }

		return false;
    }

	public void SetGraphArea(int index, List<MazeCell> all, List<MazeCell> ends)
    {
		if (graphAreas == null)
			graphAreas = new Dictionary<int, (List<MazeCell> all, List<MazeCell> ends)>();

		if (ends.Contains(this))
			IsGraphConnection = true;

		if (!graphAreas.ContainsKey(index))
			graphAreas.Add(index, (new List<MazeCell>(all), new List<MazeCell>(ends)));
		else
			graphAreas[index] = (new List<MazeCell>(all), new List<MazeCell>(ends));
	}

	public void AddToGraphArea(int index, List<MazeCell> area, List<MazeCell> ends = null)
    {
        if (!graphAreas.ContainsKey(index))
        {
			Debug.Log($"{gameObject.name} does not have a graph key for {index}");
			return;
        }

		graphAreas[index].all.AddRange(area);

		if(ends != null)
			graphAreas[index].ends.AddRange(ends);
    }

	public void RemoveGraphArea(int index)
    {
		if (graphAreas.ContainsKey(index))
			graphAreas.Remove(index);
    }

    public void RemoveJunction(int index, MazeCell cell)
    {
		graphAreas[index].ends.Remove(cell);
    }

    public int GetSmallestAreaIndex(int indexToIgnore = -1)
    {
		int lowestIndex = -1;

		// exaggerating value for min check
		int areaCount = 1000;

		foreach(var part in graphAreas)
        {
			if (indexToIgnore > -1 && part.Key == indexToIgnore)
				continue;

			if(part.Value.all.Count < areaCount)
            {
				areaCount = part.Value.all.Count;
				lowestIndex = part.Key;
            }
        }

		if(lowestIndex < 0)
        {
			Debug.Log($"Smallest area index not found for {gameObject.name}");
			return -1;
        }

		return lowestIndex;
    }

	public int GetLargestAreaIndex(int indexToIgnore = -1)
	{
		int highestIndex = -1;

		// exaggerating value for min check
		int areaCount = 0;

		foreach (var part in graphAreas)
		{
			if (indexToIgnore > -1 && part.Key == indexToIgnore)
				continue;

			if (part.Value.all.Count > areaCount)
			{
				areaCount = part.Value.all.Count;
				highestIndex = part.Key;
			}
		}

		if (highestIndex < 0)
		{
			Debug.Log($"Largest area index not found for {gameObject.name}");
			return -1;
		}

		return highestIndex;
	}

	public int GetRandomAreaIndex()
    {
		var indices = new List<int>(graphAreas.Keys);
		return indices[UnityEngine.Random.Range(0, indices.Count)];
    }

	public List<MazeCell> GetConnections(bool includeSelf = false)
    {
		if (!IsGraphConnection)
			return new List<MazeCell>(graphAreas[GetGraphAreaIndices()[0]].ends);
        else
        {
			var connections = new List<MazeCell>();

			foreach(var set in graphAreas)
            {
				foreach(var item in set.Value.ends)
                {
					if (!includeSelf && item == this)
						continue;

					connections.Add(item);
                }
            }

			return connections;
        }
    }

	public List<MazeCell> GetConnections(int index, bool includeSelf = false)
    {
		if (!IsGraphConnection)
        {
			var existingIndex = GetGraphAreaIndices()[0];

			if (index != existingIndex)
				Debug.Log($"{gameObject.name} does not belong to {index} returning only available index at {existingIndex}");

			return new List<MazeCell>(graphAreas[existingIndex].ends);
        }
		else
		{
			var connections = new List<MazeCell>();

			foreach (var item in graphAreas[index].ends)
			{
				if (!includeSelf && item == this)
					continue;

				connections.Add(item);
			}

			return connections;
		}
	}

	public int GetOtherConnectionCount(int index) => GetOtherConnections(index).Count;

	public List<MazeCell> GetOtherConnections(int index)
    {
		if (!IsGraphConnection)
		{
			Debug.LogError($"{gameObject.name} is not a connection point. Returning ends of area");

			return new List<MazeCell>(graphAreas[GetGraphAreaIndices()[0]].ends);
		}
		else
		{
			var connections = new List<MazeCell>();

			foreach (var set in graphAreas)
			{
				if (index == set.Key)
					continue;

				foreach (var item in set.Value.ends)
				{
					if (item == this)
						continue;

					connections.Add(item);
				}
			}

			return connections;
		}
	}

	public int GetAreaCellCount(int index = -1) => GetAreaCells(index).Count;

	public List<MazeCell> GetAreaCells(int index = -1)
    {
		if(index == -1)
        {
			if (IsJunction)
            {
				Debug.LogError($"Junction {gameObject.name} received an area cell request without an index, returning null");
				return null;
            }

			return new List<MazeCell>(graphAreas[GetGraphAreaIndices()[0]].all);
        }
        else
        {
			if (!graphAreas.ContainsKey(index))
            {
				Debug.Log($"{gameObject.name} received area cell request for {index} but does not contain the key, returning null");
				return null;
            }

			return new List<MazeCell>(graphAreas[index].all);
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

	public bool IsFullyInitialized => initializedEdgeCount == MazeDirections.Count;

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

	public void DisplayText(string text, Color color)
    {
		cellText.color = color;
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
