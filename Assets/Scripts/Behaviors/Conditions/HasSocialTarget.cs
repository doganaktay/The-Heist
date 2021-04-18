using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class HasSocialTarget : Condition
{
    private AI owner;

    public HasSocialTarget(AI owner) : base($"Has Social Target?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        List<AI> finalGroup = new List<AI>();
        List<float> timeToSpend = new List<float>();

        foreach(var candidate in owner.socialTargets)
        {
            if (!candidate.IsSocializing && candidate.WillSocialize)
            {
                finalGroup.Add(candidate);
                timeToSpend.Add(candidate.GetSocialTimer());
            }
        }

        timeToSpend.Add(owner.GetSocialTimer());
        timeToSpend.Sort();

        if (finalGroup.Count > 0)
        {
            finalGroup.Add(owner);

            foreach(var final in finalGroup)
            {
                final.SetSocialTargets(new List<AI>(finalGroup));
                final.CurrentSocialTime = timeToSpend[0];
                final.CanSocialize = false;
                final.IsSocializing = true;
            }

            return NodeStatus.Success;
        }
        else
            return NodeStatus.Failure;
    }
}