using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextOverlay : MonoBehaviour
{
    public Maze maze;
    public TextMeshPro cellText; // assigned in inspector

    public TextMeshPro[,] cellTexts;

    public void InitializeDisplay()
    {
        if(cellTexts == null)
            cellTexts = new TextMeshPro[maze.size.x, maze.size.y];

        for(int j=0; j<maze.size.y; j++)
        {
            for(int k=0; k<maze.size.x; k++)
            {
                if (cellTexts[k, j] == null)
                {
                    cellTexts[k, j] = Instantiate(cellText, maze.cells[k, j].transform.position, Quaternion.identity);
                    cellTexts[k, j].transform.SetParent(transform);
                    cellTexts[k, j].name = "cellText " + k + "," + j;
                    var rect = cellTexts[k, j].GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(maze.cellScaleX, maze.cellScaleY);
                    rect.position = new Vector3(rect.position.x, rect.position.y - 1f, rect.position.z);
                }
                maze.cells[k, j].cellText = cellTexts[k, j];
            }
        }
    }
}
