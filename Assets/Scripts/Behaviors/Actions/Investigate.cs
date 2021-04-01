using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;
using System.Threading;
using Cysharp.Threading.Tasks;

public class Investigate : ActionNode
{
    public Investigate(AI owner)
    {
        this.owner = owner;
        Name = "Investigate";
    }

    protected async override UniTask Action(CancellationToken token)
    {
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Investigate, FOVType.Alert, false);

        if(Random.value > 0.5f)
            await owner.LookAround(token);

        var possiblePositions = Propagation.instance.Propagate(owner.CurrentCell, 100000, 10);

        while (owner.IsAlert && !token.IsCancellationRequested)
        {
            var dest = possiblePositions[Random.Range(0, possiblePositions.Count)];
            owner.ShouldRun = Random.value > 0.5f ? true : false;

            await owner.GoTo(token, dest.cell);

            if (Random.value > 0.2f)
                await owner.LookAround(token, Random.Range(0, 4f));
        }

        await UniTask.NextFrame(token);

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
