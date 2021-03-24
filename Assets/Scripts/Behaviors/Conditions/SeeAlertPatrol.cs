using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class SeeAlertPatrol : Condition
{
    private AI owner;

    public SeeAlertPatrol(AI owner) : base($"Is near friend?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun() => owner.CanSeeAlertPatrol ? NodeStatus.Success : NodeStatus.Failure;
}