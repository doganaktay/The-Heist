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

    float stepTime;
    [Tooltip("Multiplied with the percentage of cells that are part of the path to get a lerp step ratio")]
    public float stepMultiplier = 1f;

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

    public void NewPath()
    {
        if(followPath != null)
            StopCoroutine(followPath);
        transform.position = new Vector3(maze.cells[startPos.x, startPos.y].transform.position.x,
                              maze.cells[startPos.x, startPos.y].transform.position.y, -1f);
        currentPath = pathfinder.SetNewPath(startPos, endPos);
        stepTime = Mathf.Max(0.05f, stepMultiplier * maze.cells[endPos.x, endPos.y].distanceFromStart[0] / (maze.size.x * maze.size.y));
        followPath = StartCoroutine(FollowPath());
    }

    void UpdatePath()
    {
        if (followPath != null)
            StopCoroutine(followPath);
        currentPath = pathfinder.SetNewPath(new IntVector2(currentCell.pos.x, currentCell.pos.y), endPos);
        stepTime = Mathf.Max(0.05f, stepMultiplier * maze.cells[endPos.x, endPos.y].distanceFromStart[0] / (maze.size.x * maze.size.y));
        followPath = StartCoroutine(FollowPath());
    }

    public void FlagAndDestroy()
    {
        StopCoroutine(followPath);
        Destroy(gameObject);
    }

    IEnumerator FollowPath()
    {
        var endPos = pathfinder.GetDestination(0);

        for(int i = 0; i < currentPath.Count; i++)
        {
            if (currentPath == null) yield break;

            if (i + 1 < currentPath.Count && Vector2.Dot(currentPath[i + 1].transform.position - transform.position,
                                             currentPath[i].transform.position - transform.position) < 0)
                continue;

            while (rb.position != (Vector2)currentPath[i].transform.position)
            {
                rb.position = Vector2.MoveTowards(rb.position, currentPath[i].transform.position, stepTime);
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
