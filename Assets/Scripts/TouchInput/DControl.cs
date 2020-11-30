﻿using System.Collections;
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

        public Player player;
        public TouchUI touchUI;
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
                DTouch.OnFingerTap += FingerTap;
                DTouch.OnFingerDown += FingerDown;
                DTouch.OnFingerUpdate += FingerUpdate;
                DTouch.OnFingerUp += FingerUp;
            }
        }

        private void OnDisable()
        {
            if(instances[0] == this)
            {
                DTouch.OnFingerTap -= FingerTap;
                DTouch.OnFingerDown -= FingerDown;
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
                    else if(touchUI.CurrentSelectedButton == null || touchUI.CurrentSelectedButton.ButtonType != ButtonActionType.Menu)
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
                                    touchUI.ShowMainMenu();
                                }
                            }
                        }
                    }
                    else if (touchUI.CurrentSelectedButton.ButtonType == ButtonActionType.Menu)
                    {
                        touchUI.ResumeGame();
                    }
                }
                else
                {
                    player.LaunchProjectile();
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

                if((finger.screenPos - finger.lastScreenPos).sqrMagnitude > 0.001f || player.lineReset)
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
                touchUI.ShowAimUI = false;
                aiming = false;
            }
        }

        #endregion Finger Handlers

        #region UI Methods

        public void DrawAimUI(Vector3 aimPos, bool isCenter = true)
        {
            if (isCenter)
            {
                touchUI.AimCenter = touchUI.AimPos = aimPos;
                touchUI.ShowAimUI = true;
            }
            else
            {
                touchUI.AimPos = aimPos;
            }
        }

        #endregion UI Methods

        #region Utilities

        private void DispatchAction(DFinger finger)
        {
            if (!touchUI.CurrentSelectedButton)
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
                    }
                }
            }
            else
            {
                switch (touchUI.CurrentSelectedButton.ButtonType)
                {
                    case ButtonActionType.PlaceObject:
                        player.PutOrRemoveItem(touchUI.CurrentSelectedButton.ItemType, currentCellHit);
                        touchUI.CurrentSelectedButton.Deselect();
                        touchUI.CurrentSelectedButton = null;
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

                        touchUI.CurrentSelectedButton.Deselect();
                        touchUI.CurrentSelectedButton = null;
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
                    cellIsPlayer = currentCellHit == player.currentPlayerCell;
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