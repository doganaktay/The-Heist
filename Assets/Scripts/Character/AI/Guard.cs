using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Guard : AI
{
    public GuardRole role = GuardRole.Free;

    protected override void GenerateBehaviorTree()
    {
        BehaviorTree = new Selector($"{gameObject.name} AI Tree",
                                new Sequence("Chase",
                                    new RegisterPlayer(this),
                                    new Chase(this)),
                                new Sequence("Pursuit",
                                    new HasPursuitPath(this),
                                    new FollowChartedPath(this, ChartedPathType.Pursuit, BehaviorType.Pursue, FOVType.Chase, true)),
                                new Sequence("Check",
                                    new HasCellNotification(this),
                                    new Check(this)),
                                new Sequence("Alert",
                                    new IsAlert(this),
                                    new Investigate(this)),
                                new Selector("Select Casual",
                                    //new Wander(this),
                                    //new Loop(this),
                                    new PerformGuardRole(this)
                                ));
    }

    protected override void HandleNotification(MazeCell cell, CellNotificationData data)
    {
        if (cell != currentCell)
            return;

        if ((int)CurrentBehavior < (int)BehaviorType.Pursue)
            PointOfInterest = data.signalCenter;

        Debug.Log($"{gameObject.name} at {currentCell.pos.x},{currentCell.pos.y} is handling notification: type {data.type}, signal ratio {data.attenuatedSignalRatio}, center {data.signalCenter.gameObject.name}");
    }
}
