using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Wander : Node
{
    private AI owner;

    public Wander(AI owner)
    {
        this.owner = owner;
        Name = "Wander";
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if(EvaluationCount == 0)
        {
            owner.SetBehaviorData(new BehaviorData(BehaviorType.Wander, FOVType.Regular, true));
            return NodeStatus.Running;
        }

        return NodeStatus.Success;
    }
}
