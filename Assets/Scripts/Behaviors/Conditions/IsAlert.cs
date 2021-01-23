using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class IsAlert : Condition
{
    private AI owner;

    public IsAlert(AI owner) : base($"Is alert?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        return owner.IsAlert ? NodeStatus.Success : NodeStatus.Failure;
    }
}