using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Archi.BT;

public class Guard : AI
{
    [Header("Guard Parameters")]
    public GuardRole role = GuardRole.Free;

    protected override void GenerateBehaviorTree()
    {
        BehaviorTree = new Selector($"{gameObject.name} AI Tree",
                                new Sequence("Chase",
                                    new RegisterPlayer(this),
                                    new Chase(this)),
                                new Sequence("Pursuit",
                                    new HasPursuitPath(this),
                                    new Pursue(this, true)),
                                new Sequence("Check",
                                    new HasCellNotification(this),
                                    new Check(this)),
                                //new Sequence("See Alert Patrol",
                                //    new SeeOtherAI(this, BehaviorType.Casual, false, typeof(AI)),
                                //    new FollowOther(this)),
                                new Sequence("Alert",
                                    // the inverted register check is less than ideal and is a hotfix
                                    // because on the edge case that the Chase node has been evaluated
                                    // and alert is set after, AI will trigger investigate
                                    // before chase
                                    new Inverter("No registered target",
                                        new RegisterPlayer(this)),
                                    new IsAlert(this),
                                    new Investigate(this)),

                                new Selector("Select Casual",

                                    //new Sequence("Socialize",
                                    //    new SelectorWithFail("Social Check",
                                    //        new IsSocializing(this),
                                    //        new Sequence("New Social Check",
                                    //            new WillSocialize(this),
                                    //            new SeeOtherAI(this, BehaviorType.Casual, true),
                                    //            new HasSocialTarget(this))),
                                    //    new Socialize(this)),

                                    new Sequence("Post",
                                        new SelectorWithFail("Post Check",
                                            new IsPosting(this),
                                            new ShouldPost(this)),
                                        new Post(this)),

                                    new PerformGuardRole(this))
                                );
    }

    protected override void HandleNotification(MazeCell cell, CellNotificationData data)
    {
        if (cell != currentCell)
            return;

        if (CurrentBehaviorType < BehaviorType.Pursue)
        {
            PointOfInterest = data.signalCenter;
            Debug.Log($"{gameObject.name} at {currentCell.pos.x},{currentCell.pos.y} is handling notification: type {data.type}, signal ratio {data.attenuatedSignalRatio}, center {data.signalCenter.gameObject.name}");
        }
        else
            Debug.Log($"{gameObject.name} at {currentCell.pos.x},{currentCell.pos.y} received notification, but has higher priority behavior");
    }
}
