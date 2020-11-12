using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.Touch
{
    [RequireComponent(typeof(Player))]
    public class DControl : MonoBehaviour
    {
        // this is used so that only the first instance of the object
        // (in this case the player) registers for events
        // this is needed to avoid registering w physics sim duplicates
        static List<DControl> instances = new List<DControl>();

        static int cellLayerMask = 1 << 10;

        Player player;
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
            player = GetComponent<Player>();
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
                if (DTouch.instances[0].FindFinger(1) == null && cellIsWalkable)
                {
                    if (cellIsPlayer)
                    {
                        if (player.IsMoving)
                            player.StopGoToDestination();
                    }
                    else
                    {
                        if (finger.tapCount == 1)
                            player.Move(currentCellHit);
                        else if (finger.tapCount == 2)
                            player.Move(currentCellHit, true);
                    }
                }
                else if (DTouch.instances[0].FindFinger(1) != null)
                {
                    player.LaunchProjectile();
                }
            }
        }

        private void FingerDown(DFinger finger)
        {
            if(finger.index == 0)
            {
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
            if(finger.index == 0 && !aiming && cellIsPlayer && finger.age > 0.5f)
            {
                DrawRadialUI(player.CurrentPlayerCell.transform.position);
            }

            else if(finger.index == 0 && aiming)
            {
                player.touchUI.ShowInputUI = false;
            }

            else if(finger.index == 1 && aiming)
            {
                aimTouchTarget = cam.ScreenToWorldPoint(finger.screenPos);
                Vector2 diff = aimTouchTarget - aimTouchPivot;
                diff = diff.normalized;
                float rot = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                player.aim.transform.localRotation = Quaternion.Euler(0f, 0f, rot - 90f);

                if((finger.screenPos - finger.lastScreenPos).magnitude > 0.1f || player.lineReset)
                {
                    player.SetTrajectory();
                }

                DrawAimUI(aimTouchTarget, false);
            }
        }

        private void FingerUp(DFinger finger)
        {
            if (finger.index == 0 && player.touchUI.ShowInputUI)
            {
                var guiList = DTouch.RaycastGUI(finger);

                foreach(var item in guiList)
                {
                    if(item.gameObject.TryGetComponent(out UIButton button))
                    {
                        button.PlaceObject();
                        break;
                    }
                }

                player.touchUI.ShowInputUI = false;
            }

            else if (finger.index == 1)
            {
                player.ResetTrajectory();
                player.touchUI.ShowAimUI = false;
                aiming = false;
            }
        }

        #endregion Finger Handlers

        #region UI Methods

        public void DrawAimUI(Vector3 aimPos, bool isCenter = true)
        {
            if (isCenter)
            {
                player.touchUI.AimCenter = player.touchUI.AimPos = aimPos;
                player.touchUI.ShowAimUI = true;
            }
            else
            {
                player.touchUI.AimPos = aimPos;
            }
        }

        public void DrawRadialUI(Vector2 placementPos)
        {
            player.touchUI.InputUIPos = placementPos;
            player.touchUI.ShowInputUI = true;
        }

        #endregion UI Methods

        #region Utilities

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
                    cellIsPlayer = currentCellHit == player.CurrentPlayerCell;
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