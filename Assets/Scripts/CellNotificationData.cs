using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CellNotificationData
{
    public int priority;
    public float signalStrength;
    public MazeCell signalCenter;

    public CellNotificationData(int priority, float signalStrength, MazeCell signalCenter)
    {
        this.priority = priority;
        this.signalStrength = signalStrength;
        this.signalCenter = signalCenter;
    }
}
