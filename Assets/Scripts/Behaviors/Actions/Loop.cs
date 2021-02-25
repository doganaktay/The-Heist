﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Loop : ActionNode
{
    public Loop(AI owner)
    {
        this.owner = owner;
        Name = "Loop";
    }

    protected override IEnumerator Action()
    {
        owner.ActiveActionNode = this;
        owner.IsActive = true;

        if (owner.GetLoop())
        {
            var next = owner.loopPath.GetNext(owner.CurrentCell, true);

            while (next.cell != null)
            {
                if (next.index == -1)
                    yield return owner.GoTo(next.cell);
                else
                    yield return owner.GoTo(next.cell, next.index);

                next = owner.loopPath.GetNext(owner.CurrentCell);
            }
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
