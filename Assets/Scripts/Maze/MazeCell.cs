using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class MazeCell : FastPriorityQueueNode
{
    #region Data

	// INSTANCE

	// PUBLIC
    public IntVector2 pos;
	public int areaIndex;
	public int state = 0;
	public int row, col;
	public TextMeshPro cellText;
	public Material mat;
	public bool[] visited;
	public bool searched = false;
    public MazeCell[] exploredFrom;
	public int[] distanceFromStart;
	public int searchSize = 10; // should be same as search size in pathfinder script
	public bool isPlaceable = false; // used by spotfinder to find available placement spots
	public int placeableNeighbourCount = 0;
	public int travelCost; // for A*
	public bool HasCCTVCoverage { get; set; } = false;
	public HashSet<MazeCell> IsolatedEntryPoints { get; set; }
	public float VantageScore { get; private set; }
	public float PlacementScore { get; set; } = -1;
	public bool Occupied { get; set; } = false;

	// used for bit ops for spot placement and propagation
	public int cardinalBits = 0;
	public int diagonalBits = 0;
	public int allNeighbourBits = 0;

	// actually connected cells
	public HashSet<MazeCell> connectedCells = new HashSet<MazeCell>();
	// connected cells with placement on them
	public HashSet<MazeCell> placedConnectedCells = new HashSet<MazeCell>();
	// cells only connected through special connections through walls (like grates)
	public HashSet<MazeCell> specialConnectedCells = new HashSet<MazeCell>();
	// for keeping track of items placed on cell
	public Dictionary<PlaceableItemType, PlaceableItem> placedItems = new Dictionary<PlaceableItemType, PlaceableItem>();

	// Graph data
	public bool IsGraphConnection { get; set; }
	public bool IsLockedConnection { get; set; } = false;
	public bool IsDeadEnd => connectedCells.Count == 1;
	public List<int> GraphAreaIndices = new List<int>();
	public List<int> GetGraphAreaIndices() => GraphAreaIndices;
	public int GraphAreaCount => GraphAreaIndices.Count;
	public bool IsJunction => IsGraphConnection && GraphAreaIndices.Count > 1;
	public int EndIndex { get; set; } = -1;
	public int DeadConnectionCount { get; set; } = 0;
	public bool IsUnloopable { get; set; } = false;
	public int LastIndexAddedToQueue { get; set; } = -1;
	public List<KeyValuePair<MazeCell, int>> MeasuredEnds = new List<KeyValuePair<MazeCell, int>>();

	// SERIALIZED
	[SerializeField] PerObjectMaterialProperties props;

	// PRIVATE
	Color initialColor;
	List<Color> requestedIndicatorColors = new List<Color>();
	private MazeCellEdge[] edges = new MazeCellEdge[MazeDirections.Count];
	int unexploredDirectionCount = -1;

    #endregion

    #region MonoBehaviour

    void Awake()
    {
        visited = new bool[searchSize];
		exploredFrom = new MazeCell[searchSize];
		distanceFromStart = new int[searchSize];
    }

	#endregion

	#region Graph

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

			// currently ignores interal corners formed by 3 connection cells
			// needs to do an or operation besides the pattern to check
			// for other connnections to not invalidate the corner pattern

			if ((cardinalBits & CornerBitPatterns[i].cardinal) == CornerBitPatterns[i].cardinal
				&& (diagonalBits & CornerBitPatterns[i].diagonal) == CornerBitPatterns[i].diagonal)
				return true;
		}

		return false;
	}

	static (int cardinal, int diagonal)[] CornerBitPatterns = new (int cardinal, int diagonal)[]
	{
		(1 << 0 | 1 << 1, 1 << 0),
		(1 << 1 | 1 << 2, 1 << 1),
		(1 << 2 | 1 << 3, 1 << 2),
		(1 << 3 | 1 << 0, 1 << 3)

	};

	public void SetGraphArea(int index)
	{
		if (!GraphAreaIndices.Contains(index))
			GraphAreaIndices.Add(index);
	}


	public void RemoveGraphArea(int index)
	{
		if (GraphAreaIndices.Contains(index))
			GraphAreaIndices.Remove(index);
	}

	public void SetDistanceToJunctions(List<MazeCell> ends)
	{
		foreach (var end in ends)
		{
			if (end == this)
				continue;

			// -1 because path includes asking cell
			var distance = PathRequestManager.RequestPathImmediate(this, end).Count - 1;

			MeasuredEnds.Add(new KeyValuePair<MazeCell, int>(end, distance));
		}

		MeasuredEnds = MeasuredEnds.OrderBy(x => x.Value).ToList();
	}

	public MazeCell GetClosestJunction() => MeasuredEnds[0].Key;
	public MazeCell GetClosestJunction(MazeCell junctionToAvoid)
	{
		for (int i = 0; i < MeasuredEnds.Count; i++)
		{
			if (MeasuredEnds[i].Key != junctionToAvoid)
				return MeasuredEnds[i].Key;
		}

		return null;
	}
	public MazeCell GetFarthestJunction() => MeasuredEnds[MeasuredEnds.Count - 1].Key;
	public MazeCell GetFarthestJunction(MazeCell junctionToAvoid)
	{
		for (int i = MeasuredEnds.Count - 1; i >= 0; i--)
		{
			if (MeasuredEnds[i].Key != junctionToAvoid)
				return MeasuredEnds[i].Key;
		}

		return null;
	}

	public bool HasGraphIndex(int index)
	{
		foreach (var graphIndex in GetGraphAreaIndices())
		{
			if (graphIndex == index)
				return true;
		}

		return false;
	}

	public int GetJunctionDistance(MazeCell other)
	{
		for (int i = 0; i < MeasuredEnds.Count; i++)
		{
			if (MeasuredEnds[i].Key == other)
				return MeasuredEnds[i].Value;
		}

		return -1;
	}

	public float GetJunctionDistanceAverage()
	{
		int total = 0;

		for (int i = 0; i < MeasuredEnds.Count; i++)
			total += MeasuredEnds[i].Value;

		return (float)total / MeasuredEnds.Count;
	}

	public float GetPlacementScore()
    {
		if (PlacementScore != -1)
			return PlacementScore;

		int total = 0;
		int count = 0;

		foreach(var neighbor in connectedCells)
        {
			if (neighbor.IsolatedEntryPoints != null)
			{
				foreach(var point in neighbor.IsolatedEntryPoints)
                {
					total += PathRequestManager.RequestPathImmediate(neighbor, point).Count;
					count++;
                }

				continue;
			}

			for (int i = 0; i < neighbor.MeasuredEnds.Count; i++)
            {
				total += neighbor.MeasuredEnds[i].Value;
				count++;
			}
        }

		return PlacementScore = (float)total / count;
	}

	public void CalculateVantageScore()
    {
		// hardcoded obstacle layermask is ugly
		// but didn't want to promote to class variable
		// on MazeCell
		int layerMask = 1 << 9 | 1 << 13;

		int stepCount = 8;
		float iterationAngle = 360f / stepCount;
		float currentAngle = 0f;

		float accumulatedVisibilityDistance = 0;

		for(int i = 0; i < stepCount; i++, currentAngle += iterationAngle)
        {
			var dir = currentAngle.DirectionFromAngle();
			var hit = Physics2D.Raycast(transform.position, dir, Mathf.Infinity, layerMask);

			if(hit.collider != null)
            {
				accumulatedVisibilityDistance += (hit.point - (Vector2)transform.position).magnitude;
            }
        }

		VantageScore = accumulatedVisibilityDistance / stepCount;
    }

	#endregion

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
			int skips = GameManager.rngSeeded.Range(0, MazeDirections.Count - initializedEdgeCount);
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

	public float GetLookRotationAngle()
    {
		var result = new List<float>();
		var cardinalAngleStep = -90f;
		for(int i = 0; i < MazeDirections.cardinalVectors.Length; i++)
		{
			if ((cardinalBits & 1 << i) != 0)
				result.Add((i * cardinalAngleStep + 360f) % 360f);

			if ((diagonalBits & 1 << i) != 0)
				result.Add((i * cardinalAngleStep - 45f + 360f) % 360f);
		}

        return result[GameManager.rngFree.Range(0, result.Count)];
    }

	public List<IntVector2> GetPostDirections() => MazeDirections.ConstructPostDirections(cardinalBits);

	public HashSet<MazeCell> GetNeighbours() => connectedCells;

	public MazeCell GetNeighbour(IntVector2 direction)
    {
		foreach(var cell  in connectedCells)
        {
			if (cell.pos.x == pos.x + direction.x && cell.pos.y == pos.y + direction.y)
				return cell;
        }

		return null;
    }

    #region Item Placemment

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

    #endregion

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
