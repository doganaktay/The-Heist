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

        // there can be a smarter way to handle a WillSocialize fail
        // (such as a shorter, separate timer just for running the social check)
        // currently we're resetting the ability by triggering the full wait timer
        // if the social check fails (there's a random roll against initiative)
        // without this, valid AIs in view would repeatedly ask each other to socialize
        // until the check passes, invalidating the value of the initiative score of the AI

        owner.CanSocialize = false;
        owner.CanSocializeResetTimer(true).Forget();
        return NodeStatus.Failure;
    }
}