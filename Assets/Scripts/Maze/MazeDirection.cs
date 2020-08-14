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

	public static IntVector2[] vectors = {
		new IntVector2(0, 1),
		new IntVector2(1, 0),
		new IntVector2(0, -1),
		new IntVector2(-1, 0)
	};

	public static IntVector2 ToIntVector2(this MazeDirection direction)
	{
		return vectors[(int)direction];
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

	// MazeCellWall extension method, in here because this is currently the only static class
	public static void RemoveWall(this MazeCellWall wall)
	{
		wall.cell.connectedCells.Add(wall.otherCell);
		wall.otherCell.connectedCells.Add(wall.cell);

		wall.gameObject.SetActive(false);
	}
}