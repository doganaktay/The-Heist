using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class SeeOtherAI : Condition
{
    private AI owner;
    private BehaviorType behaviorType;
    private bool wantExactBehaviorMatch;

    public SeeOtherAI(AI owner, BehaviorType behaviorType, bool wantExactBehaviorMatch) : base($"See {behaviorType.ToString()} AI?")
    {
        this.owner = owner;
        this.behaviorType = behaviorType;
        this.wantExactBehaviorMatch = wantExactBehaviorMatch;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        owner.socialTargets = owner.GetVisible<AI>(behaviorType, wantExactBehaviorMatch);

        if (owner.socialTargets.Count > 0)
            return NodeStatus.Success;
        else
            return NodeStatus.Failure;
    }
}