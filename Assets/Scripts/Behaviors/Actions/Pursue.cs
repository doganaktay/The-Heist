using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;
using System.Threading;
using Cysharp.Threading.Tasks;

public class Pursue : ActionNode
{
    bool shouldRun;

    public Pursue(AI owner, bool shouldRun = false)
    {
        this.owner = owner;
        this.shouldRun = shouldRun;
        Name = "Pursue";
    }

    protected async override UniTask Action(CancellationToken token)
    {
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Pursue, FOVType.Chase, shouldRun);
        
        var path = owner.GetPath(ChartedPathType.Pursuit);

        (MazeCell cell, int index) next;
        next = path.GetNext(owner.CurrentCell);
        var last = path.End;

        while (next.cell != null && !token.IsCancellationRequested)
        {
            if(next.cell != last)
            {
                if (next.index == -1)
                    await owner.GoTo(token, next.cell);
                else
                    await owner.GoTo(token, next.cell, next.index);
            }
            else
            {
                if (next.cell.IsDeadEnd)
                {
                    if (next.index == -1)
                        await owner.GoTo(token, next.cell, last);
                    else
                        await owner.GoTo(token, next.cell, next.index, last);
                }
                else
                {
                    if (next.index == -1)
                        await owner.GoTo(token, next.cell);
                    else
                        await owner.GoTo(token, next.cell, next.index);
                }

                break;
            }

            next = path.GetNext(owner.CurrentCell);
        }

        owner.ClearPath(ChartedPathType.Pursuit);
        
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
