using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class SeeOtherAI : Condition
{
    private AI owner;
    private BehaviorType type;
    private bool wantExactBehaviorMatch;

    public SeeOtherAI(AI owner, BehaviorType type, bool wantExactBehaviorMatch) : base($"See {type.ToString()} AI?")
    {
        this.owner = owner;
        this.type = type;
        this.wantExactBehaviorMatch = wantExactBehaviorMatch;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        owner.socialTargets = owner.GetVisible<AI>(type, wantExactBehaviorMatch);

        if (owner.socialTargets != null)
            return NodeStatus.Success;
        else
            return NodeStatus.Failure;
    }
}