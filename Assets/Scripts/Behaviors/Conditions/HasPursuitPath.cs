using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class HasPursuitPath : Condition
{
    private AI owner;
    private MazeCell searchStart;

    public HasPursuitPath(AI owner) : base($"Has Pursuit?")
    {
        this.owner = owner;
    }

    protected override void OnReset() { }

    protected override NodeStatus OnRun()
    {
        //string test = $"{owner.gameObject.name} - ready for pursuit: {owner.ReadyForPursuit} pursuit cells length: {owner.pursuit.cells.Length}";
        //Debug.Log(test);
        string test = "";

        if (owner.ReadyForPursuit)
        {
            owner.ReadyForPursuit = false;

            searchStart = owner.ObservationPoint;
            owner.pursuit = PathDesigner.Instance.GetPursuitPath(owner, owner.CurrentCell, searchStart);

            test = "New Pursuit Path: ";
            for (int i = 0; i < owner.pursuit.cells.Length; i++)
            {
                test += owner.pursuit.cells[i].gameObject.name + " - ";

                if (i < owner.pursuit.indices.Length)
                    test += owner.pursuit.indices[i] + " - ";
            }
            Debug.Log(test);

            return NodeStatus.Success;
        }
        else if (owner.pursuit.cells != null && owner.pursuit.cells.Length > 0)
        {
            test = "Pursuit Path: ";
            for (int i = 0; i < owner.pursuit.cells.Length; i++)
            {
                test += owner.pursuit.cells[i].gameObject.name + " - ";

                if (i < owner.pursuit.indices.Length)
                    test += owner.pursuit.indices[i] + " - ";
            }
            Debug.Log(test);

            return NodeStatus.Success;
        }


        return NodeStatus.Failure;
    }


}