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

	// MazeCellWall extension method, in here because this is currently the only static class
	public static void RemoveWall(this MazeCellWall wall)
	{
		wall.cellA.connectedCells.Add(wall.cellB);
		wall.cellB.connectedCells.Add(wall.cellA);

		wall.gameObject.SetActive(false);
	}

	public static bool IsConnectedTo(this MazeCell current, MazeCell next, bool isCardinal)
	{
		if (isCardinal)
		{
			if (current.connectedCells.Contains(next) || current.placedConnectedCells.Contains(next))
				return true;
		}
		else
		{
			int connected = 0;

			foreach (var cell in current.connectedCells)
			{
				if (cell.connectedCells.Contains(next) || cell.placedConnectedCells.Contains(next))
					connected++;
			}
			foreach (var cell in current.placedConnectedCells)
			{
				if (cell.connectedCells.Contains(next) || cell.placedConnectedCells.Contains(next))
					connected++;
			}

			if (connected == 2)
				return true;
		}

		return false;
	}
}