using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class SeeAlert : Condition
{
    private AI owner;

    public SeeAlert(AI owner) : base($"See alert guard?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        var visible = owner.GetVisibleAI(BehaviorType.Casual);

        if (owner.followTarget)
        {
            owner.SetAlertStatus();
            return NodeStatus.Success;
        }
        else if (visible != null && owner.followTarget == null)
        {
            return NodeStatus.Success;
        }
        else
            return NodeStatus.Failure;
    }
}