using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;
using System.Threading;
using Cysharp.Threading.Tasks;

public class Check : ActionNode
{
    public Check(AI owner)
    {
        this.owner = owner;
        Name = "Check";
    }

    protected async override UniTask Action(CancellationToken token)
    {
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Check, FOVType.Alert, Random.value < 0.25f ? false : true);

        await owner.GoTo(token, owner.PointOfInterest);

        owner.PointOfInterest = null;

        owner.IsActive = false;
        owner.ActiveActionNode = null;
    }

    protected override bool ShouldAssignAction()
    {
        if (IsCurrentAction())
        {


            return false;
        }

        return true;
    }
}
