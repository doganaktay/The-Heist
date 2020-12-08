using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NotificationModule
{
    public static Maze Maze;
    public static Action<CellNotificationData>[,] NotificationGrid;

    public static void Create(Maze maze)
    {
        Maze = maze;
        NotificationGrid = new Action<CellNotificationData>[maze.size.x, maze.size.y];

    }

    public static void AddListener(IntVector2 pos, Action<CellNotificationData> callback)
    {
        NotificationGrid[pos.x, pos.y] += callback;
    }

    public static void RemoveListener(IntVector2 pos, Action<CellNotificationData> callback)
    {
        NotificationGrid[pos.x, pos.y] -= callback;
    }

    public static void MakeNotification(MazeCell cell, CellNotificationData data)
    {
        NotificationGrid[cell.pos.x, cell.pos.y]?.Invoke(data);
    }
}
