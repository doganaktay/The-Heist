using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;
using System.Threading;
using Cysharp.Threading.Tasks;

public class Wander : ActionNode
{
    public Wander(AI owner)
    {
        this.owner = owner;
        Name = "Wander";
    }

    protected async override UniTask Action(CancellationToken token)
    {
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Casual, FOVType.Regular, false);

        owner.Move();

        await UniTask.NextFrame(token);

        while (owner.IsMoving)
            await UniTask.NextFrame(token);

        await owner.LookAround(token);

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
