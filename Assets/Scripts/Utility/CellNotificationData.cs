using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CellNotificationData
{
    public CellNotificationType type;
    public float attenuatedSignalRatio;
    public MazeCell signalCenter;

    public CellNotificationData(CellNotificationType type, float attenuatedSignalRatio, MazeCell signalCenter)
    {
        this.type = type;
        this.attenuatedSignalRatio = attenuatedSignalRatio;
        this.signalCenter = signalCenter;
    }
}

public enum CellNotificationType
{
    Sound
}