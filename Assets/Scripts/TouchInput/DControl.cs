using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.Touch
{
    public class DControl : MonoBehaviour
    {
        // this is used so that only the first instance of the object
        // (in this case the player) registers for events
        // this is needed to avoid registering w physics sim duplicates
        static List<DControl> instances = new List<DControl>();

        static int cellLayerMask = 1 << 10;
        static int wallLayerMask = 1 << 9;

        public Player player;
        Collider2D[] touchHits1, touchHits2;
        uint colliderBufferCounter = 0;
        [SerializeField]
        float overlapCircleRadius = 1f;
        bool cellIsWalkable, cellIsPlayer;
        Camera cam;
        MazeCell currentCellHit;
        private Vector3 aimTouchPivot, aimTouchTarget;
        private bool aiming = false;

        private void Start()
        {
            cam = Camera.main;

            touchHits1 = new Collider2D[10];
            touchHits2 = new Collider2D[10];
        }

        private void OnEnable()
        {
            instances.Add(this);

            if(instances[0] == this)
            {
                DTouch.OnFingerDown += FingerDown;
                DTouch.OnFingerTap += FingerTap;
                DTouch.OnFingerSwipe += FingerSwipe;
                DTouch.OnFingerUpdate += FingerUpdate;
                DTouch.OnFingerUp += FingerUp;
            }
        }

        private void OnDisable()
        {
            if(instances[0] == this)
            {
                DTouch.OnFingerDown -= FingerDown;
                DTouch.OnFingerTap -= FingerTap;
                DTouch.OnFingerSwipe -= FingerSwipe;
                DTouch.OnFingerUpdate -= FingerUpdate;
                DTouch.OnFingerUp -= FingerUp;
            }

            instances.Remove(this);
        }

        #region Finger Handlers

        private void FingerTap(DFinger finger)
        {
            if(finger.index == 0)
            {
                if (DTouch.instances[0].FindFinger(1) == null)
                {
                    if (!finger.startedOverGUI && !finger.IsOverGUI)
                    {
                        DispatchAction(finger);
                    }
                    else if(TouchUI.instance.CurrentSelectedButton == null || TouchUI.instance.CurrentSelectedButton.ButtonType != ButtonActionType.Menu)
                    {
                        var hitResults = DTouch.RaycastGUI(finger);

                        if(hitResults.Count > 0)
                        {
                            var item = hitResults[0].gameObject.GetComponentInParent<UIMenuItem>();

                            if(item != null)
                            {
                                hitResults[0].gameObject.GetComponentInParent<UIMenu>().SelectMenuItem(item);

                                if(item.ButtonType == ButtonActionType.Menu)
                                {
                                    TouchUI.instance.ShowMainMenu();
                                }
                            }
                        }
                    }
                    else if (TouchUI.instance.CurrentSelectedButton.ButtonType == ButtonActionType.Menu)
                    {
                        TouchUI.instance.ResumeGame();
                    }
                }
                else if (DTouch.instances[0].FindFinger(1).age > 0.5f)
                {
                    player.LaunchProjectile();
                }
            }
        }

        private void FingerSwipe(DFinger finger)
        {
            if (finger.index == 0 && DTouch.instances[0].FindFinger(1) == null && cellIsPlayer)
            {
                var pos = cam.ScreenToWorldPoint(finger.startScreenPos);
                var dir = finger.SwipeScaledDelta;

                RaycastHit2D hit = Physics2D.Raycast(pos, dir, Mathf.Infinity, wallLayerMask);

                if (hit.collider != null)
                {
                    var wall = hit.collider.GetComponentInParent<MazeCellWall>();
                    var target = wall.CheckCell(currentCellHit);

                    if(target != null)
                    {
                        player.Move(target, PathLayer.Special);
                    }
                }
            }
        }
        
        private void FingerDown(DFinger finger)
        {
            if(finger.index == 0)
            {
                if (!finger.IsOverGUI)
                    CheckFingerPosition(finger);
            }

            else if(finger.index == 1 && cellIsPlayer)
            {
                aimTouchPivot = cam.ScreenToWorldPoint(finger.screenPos);
                aiming = true;

                if (player.IsMoving)
                    player.StopGoToDestination();

                DrawAimUI(aimTouchPivot);
            }
        }

        private void FingerUpdate(DFinger finger)
        {
            if(finger.index == 1 && aiming)
            {
                aimTouchTarget = cam.ScreenToWorldPoint(finger.screenPos);
                Vector2 diff = aimTouchTarget - aimTouchPivot;
                diff = diff.normalized;
                float rot = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                player.aim.transform.localRotation = Quaternion.Euler(0f, 0f, rot - 90f);

                if((finger.screenPos - finger.lastScreenPos).sqrMagnitude > 0.1f || player.lineReset)
                {
                    player.SetTrajectory();
                }

                DrawAimUI(aimTouchTarget, false);
            }
        }

        private void FingerUp(DFinger finger)
        {
            if (finger.index == 1)
            {
                player.ResetTrajectory();
                TouchUI.instance.ShowAimUI = false;
                aiming = false;
            }
        }

        #endregion Finger Handlers

        #region UI Methods

        public void DrawAimUI(Vector3 aimPos, bool isCenter = true)
        {
            if (isCenter)
            {
                TouchUI.instance.AimCenter = TouchUI.instance.AimPos = aimPos;
                TouchUI.instance.ShowAimUI = true;
            }
            else
            {
                TouchUI.instance.AimPos = aimPos;
            }
        }

        #endregion UI Methods

        #region Utilities

        private void DispatchAction(DFinger finger)
        {
            if (!TouchUI.instance.CurrentSelectedButton)
            {
                if (cellIsWalkable)
                {
                    player.StopTask();

                    if (cellIsPlayer)
                    {
                        if (player.IsMoving)
                            player.StopGoToDestination();
                    }
                    else
                    {
                        if (finger.tapCount % 2 == 1)
                        {
                            player.ShouldRun = false;
                            player.Move(currentCellHit);
                        }
                        else
                        {
                            player.ShouldRun = true;
                            player.Move(currentCellHit);
                        }

                        TouchUI.instance.TouchPoint(currentCellHit.transform.position);
                    }
                }
            }
            else
            {
                switch (TouchUI.instance.CurrentSelectedButton.ButtonType)
                {
                    case ButtonActionType.PlaceObject:
                        player.PutOrRemoveItem(TouchUI.instance.CurrentSelectedButton.ItemType, currentCellHit);
                        TouchUI.instance.CurrentSelectedButton.Deselect();
                        TouchUI.instance.CurrentSelectedButton = null;
                        break;
                    case ButtonActionType.UseObject:
                        if(!currentCellHit.HasPlacedItem())
                        {
                            Debug.Log("No placed item");
                        }
                        else
                        {
                            foreach(var item in currentCellHit.placedItems.Values)
                            {
                                if (item)
                                {
                                    item.UseItem();
                                }
                            }
                        }

                        TouchUI.instance.CurrentSelectedButton.Deselect();
                        TouchUI.instance.CurrentSelectedButton = null;
                        break;
                }
            }
        }

        private bool CheckFingerPosition(DFinger finger)
        {
            return CheckFingerPosition(finger.screenPos, overlapCircleRadius, GetColliderBuffer(), cellLayerMask);
        }

        private bool CheckFingerPosition(Vector2 screenPos, float radius, Collider2D[] colliders, int layerMask)
        {
            if (Physics2D.OverlapCircleNonAlloc(cam.ScreenToWorldPoint(screenPos), radius, results: colliders, layerMask) > 0)
            {
                currentCellHit = colliders[0].GetComponent<MazeCell>();

                if (currentCellHit.IsWalkable())
                {
                    cellIsWalkable = true;
                    cellIsPlayer = currentCellHit == player.CurrentCell;
                    return true;
                }
            }

            cellIsWalkable = cellIsPlayer = false;

            return false;
        }

        private Collider2D[] GetColliderBuffer()
        {
            Collider2D[] buffer;

            if (colliderBufferCounter % 2 == 0)
                buffer = touchHits1;
            else
                buffer = touchHits2;

            colliderBufferCounter++;

            return buffer;
        }

        #endregion Utilities
    }
}