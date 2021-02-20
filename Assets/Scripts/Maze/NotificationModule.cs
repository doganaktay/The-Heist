using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NotificationModule
{
    public static Maze Maze;
    public static Action<MazeCell, CellNotificationData> Notification;

    public static void Create(Maze maze)
    {
        Maze = maze;
    }

    public static void AddListener(Action<MazeCell, CellNotificationData> callback)
    {
        Notification += callback;
    }

    public static void RemoveListener(Action<MazeCell, CellNotificationData> callback)
    {
        Notification -= callback;
    }

    public static void MakeNotification(MazeCell cell, CellNotificationData data)
    {
        Notification?.Invoke(cell, data);
    }
}
