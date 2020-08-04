using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathPatrol : MonoBehaviour
{
    public List<MazeCell> patrolArea = new List<MazeCell>();
    public MazeCell currentCell;

    void Start()
    {
        var cell = patrolArea[Random.Range(0, patrolArea.Count - 1)];
        transform.position = cell.transform.position;
        currentCell = cell;

        StartCoroutine(FollowPath());
    }
    
    void Update()
    {
        
    }

    IEnumerator FollowPath()
    {
        var endPos = patrolArea[patrolArea.Count - 1];

        for (int i = 0; i < patrolArea.Count; i++)
        {
            if (patrolArea == null) yield break;

            if (i + 1 < patrolArea.Count && Vector2.Dot(patrolArea[i + 1].transform.position - transform.position,
                                             patrolArea[i].transform.position - transform.position) < 0)
                continue;

            while (transform.position != patrolArea[i].transform.position)
            {
                transform.position = Vector2.MoveTowards(transform.position, patrolArea[i].transform.position, 0.2f);
                yield return null;
            }

            currentCell = patrolArea[i];

            if (patrolArea[i] == endPos)
            { patrolArea.Reverse(); StartCoroutine(FollowPath()); }
        }

    }

    void PickRandomDirection()
    {

    }
}
