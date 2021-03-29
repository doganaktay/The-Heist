using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class HasCellNotification : Condition
{
    private AI owner;

    public HasCellNotification(AI owner) : base($"Has Cell Notification?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if ((int)owner.CurrentBehaviorType < (int)BehaviorType.Pursue && owner.PointOfInterest != null)
        {
            owner.SetAlertStatus();
            return NodeStatus.Success;
        }

        return NodeStatus.Failure;
    }


}
