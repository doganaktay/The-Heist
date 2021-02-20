using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Loop : Node
{
    private AI owner;

    public Loop(AI owner)
    {
        this.owner = owner;
        Name = "Loop";
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if (EvaluationCount == 0)
        {
            owner.SetBehaviorData(new BehaviorData(BehaviorType.Loop, FOVType.Regular, true));
            return NodeStatus.Running;
        }

        return NodeStatus.Success;
    }
}
