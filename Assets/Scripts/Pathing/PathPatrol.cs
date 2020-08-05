using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PathPatrol : MonoBehaviour
{
    public List<MazeCell> patrolArea = new List<MazeCell>();
    public MazeCell currentCell;
    public MazeCell nextCell;

    [SerializeField]
    bool patrolRandom = false;

    Coroutine patrol;

    void Start()
    {
        if(!patrolRandom)
            patrol = StartCoroutine(PatrolPath());
        else
            patrol = StartCoroutine(PatrolRandom());
    }

    void ResetCoroutine()
    {
        if (patrol != null)
            StopCoroutine(patrol);

        if (!patrolRandom)
            patrol = StartCoroutine(PatrolPath());
        else
            patrol = StartCoroutine(PatrolRandom());

    }

    IEnumerator PatrolPath()
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
            { patrolArea.Reverse(); ResetCoroutine(); }
        }

    }

    IEnumerator PatrolRandom()
    {
        var list = currentCell.connectedCells.ToList();
        list.Shuffle();

        foreach (var cell in list)
        {
            if (cell.state == 0)
            { nextCell = cell; break; }
        }

        if (nextCell == null) yield break;

        while(transform.position != nextCell.transform.position)
        {
            transform.position = Vector2.MoveTowards(transform.position, nextCell.transform.position, 0.2f);
            yield return null;
        }

        currentCell = nextCell;

        ResetCoroutine();
    }
}
