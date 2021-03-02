using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class FollowChartedPath : ActionNode
{
    FOVType fovType;
    ChartedPathType pathType;
    bool shouldRun;

    public FollowChartedPath(AI owner, ChartedPathType pathType, FOVType fovType, bool shouldRun = false)
    {
        this.owner = owner;
        this.pathType = pathType;
        this.fovType = fovType;
        this.shouldRun = shouldRun;
        Name = "Follow Path";
    }

    protected override IEnumerator Action()
    {
        owner.ActiveActionNode = this;
        owner.IsActive = true;

        if (shouldRun)
            owner.ShouldRun = true;
        else
            owner.ShouldRun = false;

        owner.SetFOV(fovType);

        if((pathType == ChartedPathType.Loop && owner.GetLoop()) || pathType != ChartedPathType.Loop) { }
        {

            var path = owner.GetPath(pathType);

            (MazeCell cell, int index) next;

            if(pathType == ChartedPathType.Loop)
                next = path.GetNext(owner.CurrentCell, true);
            else
                next = path.GetNext(owner.CurrentCell);

            while (next.cell != null)
            {
                if (next.index == -1)
                    yield return owner.GoTo(next.cell);
                else
                    yield return owner.GoTo(next.cell, next.index);

                next = path.GetNext(owner.CurrentCell);
            }

            path.Clear();

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
