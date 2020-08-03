using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollow : MonoBehaviour
{
    GameManager gameManager;
    public Maze maze;
    Rigidbody2D rb;
    public Pathfinder pathfinder;
    Player player;
    List<MazeCell> currentPath = new List<MazeCell>();
    MazeCell currentCell;

    Coroutine followPath;

    public IntVector2 startPos, endPos;

    public float stepTime = 0.5f;

    //bool readyToDestroy = false;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        gameManager.MazeGenFinished += NewPath;
        player = FindObjectOfType<Player>();
        player.MazeChange += UpdatePath;
        rb = GetComponent<Rigidbody2D>();
        pathfinder = FindObjectOfType<Pathfinder>();
    }

    //void Start()
    //{
    //    currentPath = pathfinder.SetNewPath(startPos, endPos);
    //    followPath = StartCoroutine(FollowPath());
    //}

    public void NewPath()
    {
        if(followPath != null)
            StopCoroutine(followPath);
        transform.position = new Vector3(maze.cells[0, 0].transform.position.x,
                              maze.cells[0, 0].transform.position.y, -1f);
        currentPath = pathfinder.SetNewPath(startPos, endPos);
        followPath = StartCoroutine(FollowPath());
    }

    void UpdatePath()
    {
        if (followPath != null)
            StopCoroutine(followPath);
        currentPath = pathfinder.SetNewPath(new IntVector2(currentCell.pos.x, currentCell.pos.y), endPos);
        followPath = StartCoroutine(FollowPath());

        pathfinder.areafinder.FindAreas();

    }

    public void FlagAndDestroy()
    {
        StopCoroutine(followPath);
        Destroy(gameObject);
    }

    IEnumerator FollowPath()
    {
        var endPos = pathfinder.GetDestination();

        for(int i = 0; i < currentPath.Count; i++)
        {
            if (currentPath == null) yield break;

            if (i + 1 < currentPath.Count && Vector2.Dot(currentPath[i + 1].transform.position - transform.position,
                                             currentPath[i].transform.position - transform.position) < 0)
                continue;

            while (rb.position != (Vector2)currentPath[i].transform.position)
            {
                rb.position = Vector2.MoveTowards(rb.position, currentPath[i].transform.position, 1f);
                yield return null;
            }

            currentCell = currentPath[i];

            if (currentPath[i] == endPos)
                Debug.Log("path finished");
        }

    }

    private void OnDestroy()
    {
        gameManager.MazeGenFinished -= NewPath;
        player.MazeChange -= UpdatePath;
    }
}
