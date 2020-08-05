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

    float moveLerpSpeed;
    float currentLerpTime = 0f;
    bool reachedTarget = true;

    void Start()
    {
        moveLerpSpeed = Random.Range(0.1f, 1f);
    }

    void Update()
    {
        if (currentCell.state == 1)
            Destroy(gameObject);

        if (patrolRandom)
            PatrolRandom();
    }

    void PatrolRandom()
    {
        if (reachedTarget)
        {
            var list = currentCell.connectedCells.ToList();
            list.Shuffle();

            foreach (var cell in list)
            {
                if (cell.state == 0)
                { nextCell = cell; break; }
            }

            reachedTarget = false;
        }

        if (nextCell == null) return;

        currentLerpTime += Time.deltaTime;
        if (currentLerpTime > moveLerpSpeed)
            currentLerpTime = moveLerpSpeed;

        transform.position = Vector2.MoveTowards(transform.position, nextCell.transform.position, moveLerpSpeed);

        if (transform.position == nextCell.transform.position)
        {
            currentCell = nextCell;
            currentLerpTime = 0f;
            reachedTarget = true;
        }
    }

    //void Start()
    //{
    //    if(!patrolRandom)
    //        patrol = StartCoroutine(PatrolPath());
    //    else
    //        patrol = StartCoroutine(PatrolRandom());
    //}

    //public void ResetCoroutine()
    //{
    //    if (patrol != null)
    //        StopCoroutine(patrol);

    //    if (!patrolRandom)
    //        patrol = StartCoroutine(PatrolPath());
    //    else
    //        patrol = StartCoroutine(PatrolRandom());

    //}

    //public void StopAndDestroy()
    //{
    //    StopCoroutine(patrol);
    //    Destroy(gameObject);
    //}

    //IEnumerator PatrolPath()
    //{
    //    var endPos = patrolArea[patrolArea.Count - 1];

    //    for (int i = 0; i < patrolArea.Count; i++)
    //    {
    //        if (patrolArea == null) yield break;

    //        if (i + 1 < patrolArea.Count && Vector2.Dot(patrolArea[i + 1].transform.position - transform.position,
    //                                         patrolArea[i].transform.position - transform.position) < 0)
    //            continue;

    //        while (transform.position != patrolArea[i].transform.position)
    //        {
    //            transform.position = Vector2.MoveTowards(transform.position, patrolArea[i].transform.position, 0.2f);
    //            yield return null;
    //        }

    //        currentCell = patrolArea[i];

    //        if (patrolArea[i] == endPos)
    //        { patrolArea.Reverse(); ResetCoroutine(); }
    //    }

    //}

    //IEnumerator PatrolRandom()
    //{
    //    var list = currentCell.connectedCells.ToList();
    //    list.Shuffle();

    //    foreach (var cell in list)
    //    {
    //        if (cell.state == 0)
    //        { nextCell = cell; break; }
    //    }

    //    if (nextCell == null) yield break;

    //    while(transform.position != nextCell.transform.position)
    //    {
    //        transform.position = Vector2.MoveTowards(transform.position, nextCell.transform.position, 0.2f);
    //        yield return null;
    //    }

    //    currentCell = nextCell;

    //    ResetCoroutine();
    //}
}
