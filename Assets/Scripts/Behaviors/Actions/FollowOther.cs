using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;
using System.Threading;
using Cysharp.Threading.Tasks;

public class FollowOther : ActionNode
{
    AI target;

    public FollowOther(AI owner)
    {
        this.owner = owner;
        Name = "Follow Other";
    }

    protected async override UniTask Action(CancellationToken token)
    {
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Follow, FOVType.Alert, true);
        owner.SetMaxExposureTime();

        var currentTargetCell = target.CurrentCell;
        owner.Move(currentTargetCell);

        await UniTask.NextFrame(token);

        while (owner.IsMoving && !token.IsCancellationRequested)
        {
            if (currentTargetCell != target.CurrentCell)
            {
                currentTargetCell = target.CurrentCell;
                owner.Move(currentTargetCell);
            }

            await UniTask.NextFrame(token);
        }

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
