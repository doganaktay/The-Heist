using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Patrol : AI
{
    protected override void GenerateBehaviorTree()
    {
        BehaviorTree = new Selector($"{gameObject.name} AI Tree",
                                new Sequence("Chase Player",
                                    new CanSeePlayer(this),
                                    new Chase(this)),
                                new RandomSelector( "Random Select",
                                    //new Wander(this),
                                    new Loop(this)
                                ));

        // if can see
            // if coordinated
                // broadcast observation point
            // chase
            // set point of interest
        // if point of interest
            // consume point to build search queue
            // while path !empty search
        // if isAlert
            // investigate surroundings
        // casual behaviors
            // if chance and proximity
                // socialize
            // wander
            // loop

        //BehaviorTree = new Selector($"{gameObject.name} AI Tree",
        //                    new Sequence("Chase Player",
        //                        new CanSeePlayer(this),
        //                        new Chase(this)),
        //                    new Sequence($"{gameObject.name} Alert Behavior",
        //                        new IsAlert(this),
        //                        new Selector("Decide Alert Behavior",
        //                            new Sequence("Check Target Cell",
        //                                new HasSearchTarget(this),
        //                                new Check(this)),
        //                            new Investigate(this)
        //                            )),
        //                    new RandomSelector($"{gameObject.name} Relaxed Behavior",
        //                        new Wander(this),
        //                        new Loop(this)
        //                        ));
    }

    protected override void HandleNotification(MazeCell cell, CellNotificationData data)
    {
        if (cell != currentCell)
            return;


        Debug.Log($"{gameObject.name} at {currentCell.pos.x},{currentCell.pos.y} is handling notification with {data.priority} priority, {data.signalStrength} signal strength, centered at {data.signalCenter.gameObject.name}");
    }
}
