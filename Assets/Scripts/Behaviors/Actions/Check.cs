using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Check : Node
{
    private AI owner;

    public Check(AI owner)
    {
        this.owner = owner;
        Name = "Check Cell";
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if (EvaluationCount == 0)
        {
            //owner.SetBehaviorData(new BehaviorData(BehaviorType.Check, FOVType.Alert));
            return NodeStatus.Running;
        }

        return NodeStatus.Success;
    }
}
