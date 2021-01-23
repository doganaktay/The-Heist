using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class IsNearFriend : Condition
{
    private AI owner;

    public IsNearFriend(AI owner) : base($"Is near friend?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        return owner.manager.ProximityCheck(owner) ? NodeStatus.Success : NodeStatus.Failure;
    }
}