using System.Collections.Generic;
using UnityEngine;

public enum MazeDirection
{
	Up,
	Right,
	Down,
	Left
}

public static class MazeDirections
{

	public const int Count = 4;

	public static MazeDirection RandomValue
	{
		get
		{
			return (MazeDirection)Random.Range(0, Count);
		}
	}

	// clockwise from top
	public static IntVector2[] cardinalVectors = {
		new IntVector2(0, 1),
		new IntVector2(1, 0),
		new IntVector2(0, -1),
		new IntVector2(-1, 0)
	};

	// clockwise from top right
	public static IntVector2[] diagonalVectors = {
		new IntVector2(1, 1),
		new IntVector2(1, -1),
		new IntVector2(-1, -1),
		new IntVector2(-1, 1)
	};

	// clockwise from top
	public static IntVector2[] allVectors = {
		new IntVector2(0,1),
		new IntVector2(1,1),
		new IntVector2(1,0),
		new IntVector2(1,-1),
		new IntVector2(0,-1),
		new IntVector2(-1,-1),
		new IntVector2(-1,0),
		new IntVector2(-1,1)
    };

	public static Dictionary<IntVector2, int> cardinalBitmasks = new Dictionary<IntVector2, int>
    {
		{ new IntVector2(0, 1), 1 << 0 },
		{ new IntVector2(1, 0), 1 << 1 },
		{ new IntVector2(0, -1), 1 << 2 },
		{ new IntVector2(-1, 0), 1 << 3 }
	};

	public static Dictionary<IntVector2, int> diagonalBitmasks = new Dictionary<IntVector2, int>
	{
		{ new IntVector2(1, 1), 1 << 0 },
		{ new IntVector2(1, -1), 1 << 1 },
		{ new IntVector2(-1, -1), 1 << 2 },
		{ new IntVector2(-1, 1), 1 << 3 }
	};

	public static Dictionary<int, IntVector2> postDirections = new Dictionary<int, IntVector2>
	{
		{ 1 << 0, cardinalVectors[0]},
		{ 1 << 1, cardinalVectors[1]},
		{ 1 << 2, cardinalVectors[2]},
		{ 1 << 3, cardinalVectors[3]},
		{ 1 << 0 | 1 << 1, diagonalVectors[0]},
		{ 1 << 1 | 1 << 2, diagonalVectors[1]},
		{ 1 << 2 | 1 << 3, diagonalVectors[2]},
		{ 1 << 3 | 1 << 0, diagonalVectors[3]}
	};

	public static IntVector2 ToIntVector2(this MazeDirection direction)
	{
		return cardinalVectors[(int)direction];
	}

	private static MazeDirection[] opposites = {
		MazeDirection.Down,
		MazeDirection.Left,
		MazeDirection.Up,
		MazeDirection.Right
	};

	public static MazeDirection GetOpposite(this MazeDirection direction)
	{
		return opposites[(int)direction];
	}

	private static Quaternion[] rotations = {
		Quaternion.identity,
		Quaternion.Euler(0f, 0f, 270f),
		Quaternion.Euler(0f, 0f, 180f),
		Quaternion.Euler(0f, 0f, 90f)
	};

	public static Quaternion ToRotation(this MazeDirection direction)
	{
		return rotations[(int)direction];
	}

	public static int RotatePattern(this int pattern, int bitCount, int rotationCount)
    {
		int rotatedPattern = pattern;

		for(int i = 0; i < rotationCount; i++)
        {
			int rshift = rotatedPattern >> (bitCount - 1);
			int lshift = rotatedPattern << 1;

			int mask = 0;
			for(int j = 0; j < bitCount; j++)
            {
				mask |= 1 << j;
            }

			rotatedPattern = (rshift | lshift) & mask;
		}

		return rotatedPattern;
    }

	public static List<IntVector2> ConstructPostDirections(int bitfield)
    {
		var directions = new List<IntVector2>();

		bitfield = ~bitfield;

		if(bitfield == 0)
        {
			directions.Add(new IntVector2(0, 0));
			return directions;
        }

		foreach (var dir in postDirections)
			if ((dir.Key & bitfield) == dir.Key)
				directions.Add(dir.Value);

		return directions;
    }

	public static void RemoveWall(this MazeCellWall wall)
	{
		wall.cellA.connectedCells.Add(wall.cellB);
		wall.cellB.connectedCells.Add(wall.cellA);

		wall.gameObject.SetActive(false);
	}

	public static IntVector2 GetDirection(MazeCell from, MazeCell to) => new IntVector2(to.pos.x - from.pos.x, to.pos.y - from.pos.y);

	public static bool CheckAhead(MazeCell current, MazeCell next)
    {
		// we never check diagonals with this method
		// otherwise the condition for the return ternary could fail
		// if the direction was diagoal: eg. (1, -1)

		var dir = GetDirection(current, next);
		return dir.x + dir.y == 0 ? false : (next.cardinalBits & cardinalBitmasks[dir]) != 0;
    }

	public static Vector2 GetDirectionBiasVector(MazeCell currentPos, MazeCell currentTarget, MazeCell nextTarget)
    {
		IntVector2 first = new IntVector2(0, 0);
		IntVector2 second = new IntVector2(0, 0);

		if (currentPos != currentTarget && currentTarget != nextTarget)
        {
			first = GetDirection(currentPos, currentTarget);
			second = GetDirection(currentTarget, nextTarget);
        }

		var vector = new Vector2(-first.x, -first.y) + new Vector2(second.x, second.y);
		var result = first.x == second.x ? new Vector2(0, 0) : vector;

		return result;
    }
}