using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class CanSeePlayer : Condition
{
    private AI owner;

    public CanSeePlayer(AI owner) : base($"Can see player?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        return owner.CanSeePlayer ? NodeStatus.Success : NodeStatus.Failure;
    }
}
