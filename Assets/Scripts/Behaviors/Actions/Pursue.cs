using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Pursue : ActionNode
{
    bool shouldRun;

    public Pursue(AI owner, bool shouldRun = false)
    {
        this.owner = owner;
        this.shouldRun = shouldRun;
        Name = "Pursue";
    }

    protected override IEnumerator Action()
    {
        owner.ActiveActionNode = this;
        owner.IsActive = true;

        owner.SetBehaviorParams(BehaviorType.Pursue, FOVType.Chase, shouldRun);
        
        var path = owner.GetPath(ChartedPathType.Pursuit);

        (MazeCell cell, int index) next;
        next = path.GetNext(owner.CurrentCell);
        var last = path.Last;

        while (next.cell != null)
        {
            if(next.cell != last)
            {
                if (next.index == -1)
                    yield return owner.GoTo(next.cell);
                else
                    yield return owner.GoTo(next.cell, next.index);
            }
            else
            {
                if (next.cell.IsDeadEnd)
                {
                    if (next.index == -1)
                        yield return owner.GoTo(next.cell, last);
                    else
                        yield return owner.GoTo(next.cell, next.index, last);
                }
                else
                {
                    if (next.index == -1)
                        yield return owner.GoTo(next.cell);
                    else
                        yield return owner.GoTo(next.cell, next.index);
                }



                break;
            }

            next = path.GetNext(owner.CurrentCell);
        }

        path.Clear();
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
