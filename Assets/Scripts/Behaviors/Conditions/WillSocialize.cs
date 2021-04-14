using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class WillSocialize : Condition
{
    private AI owner;

    public WillSocialize(AI owner) : base($"Will Socialize?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        if (owner.WillSocialize)
            return NodeStatus.Success;

        // the reset timer is called with true
        // to implement a shorter wait (currently hard-coded 10f)
        // than the full reset time

        owner.CanSocialize = false;
        owner.WaitUntilCanSocialize(true).Forget();
        return NodeStatus.Failure;
    }
}