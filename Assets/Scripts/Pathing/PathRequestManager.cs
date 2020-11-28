﻿using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathRequestManager : MonoBehaviour
{
    Queue<PathResult> results = new Queue<PathResult>();

    static PathRequestManager instance;
    Pathfinder pathfinder;

    void Awake()
    {
        instance = this;
        pathfinder = GetComponent<Pathfinder>();
    }

    void Update()
    {
        if (results.Count > 0)
        {
            int itemsInQueue = results.Count;
            lock (results)
            {
                for (int i = 0; i < itemsInQueue; i++)
                {
                    PathResult result = results.Dequeue();
                    result.callback(result.path);
                }
            }
        }
    }

    public static void RequestPath(PathRequest request)
    {
        ThreadStart threadStart = delegate
        {
            instance.pathfinder.FindPath(request, instance.FinishedProcessingPath);
        };
        threadStart.Invoke();
    }

    public void FinishedProcessingPath(PathResult result)
    {
        lock (results)
        {
            results.Enqueue(result);
        }
    }
}

public struct PathResult
{
    public List<MazeCell> path;
    public Action<List<MazeCell>> callback;

    public PathResult(List<MazeCell> path, Action<List<MazeCell>> callback)
    {
        this.path = path;
        this.callback = callback;
    }
}

public struct PathRequest
{
    public MazeCell start;
    public MazeCell end;
    public Action<List<MazeCell>> callback;

    public PathRequest(Action<List<MazeCell>> callback, MazeCell start, MazeCell end = null )
    {
        this.start = start;
        this.end = end;
        this.callback = callback;
    }
}