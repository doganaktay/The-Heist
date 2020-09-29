using UnityEngine;

public abstract class MazeCellEdge : MonoBehaviour
{
	public MazeCell cellA, cellB;

	public MazeDirection directionA;

	public void Initialize(MazeCell cellA, MazeCell cellB, MazeDirection directionA)
	{
		this.cellA = cellA;
		this.cellB = cellB;
		this.directionA = directionA;
		cellA.SetEdge(directionA, this);

		if(cellB != null)
			cellB.SetEdge(directionA.GetOpposite(), this);

		transform.parent = cellA.transform;
		transform.localPosition = Vector3.zero;
        transform.localRotation = directionA.ToRotation();
    }

	public MazeCell GetOppositeNeighbour(MazeCell cell)
    {
		if (cell == cellA && cellB != null)
			return cellB;
		else if (cell == cellB && cellA != null)
			return cellA;

		return null;
    }
}