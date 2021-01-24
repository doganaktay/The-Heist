﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Chase : Node
{
    private AI owner;

    public Chase(AI owner)
    {
        this.owner = owner;
        Name = "Chase";
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if (EvaluationCount == 0)
        {
            owner.SetBehaviorData(new BehaviorData(BehaviorType.Chase, FOVType.Chase));
            return NodeStatus.Running;
        }

        return NodeStatus.Success;
    }
}
