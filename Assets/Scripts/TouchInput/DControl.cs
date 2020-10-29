using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.Touch
{
    [RequireComponent(typeof(Player))]
    public class DControl : MonoBehaviour
    {
        static int cellLayerMask = 1 << 10;

        Player player;
        Collider2D[] touchHits1, touchHits2;
        uint colliderBufferCounter = 0;
        [SerializeField]
        float overlapCircleRadius = 1f;
        bool isWalkable, isPlayer;
        Camera cam;
        MazeCell currentCellHit;
        List<MazeCell> currentPath;

        private void Start()
        {
            player = GetComponent<Player>();
            cam = Camera.main;

            touchHits1 = new Collider2D[10];
            touchHits2 = new Collider2D[10];
        }

        private void OnEnable()
        {
            DTouch.OnFingerTap += FingerTap;
        }

        private void OnDisable()
        {
            DTouch.OnFingerTap -= FingerTap;
        }

        private void FingerTap(DFinger finger)
        {
            if(finger.index == 0  && CheckFingerPosition(finger))
            {
                if (isPlayer)
                    Debug.Log("On player");
                else
                {
                    if (finger.tapCount == 1)
                        player.Move(currentCellHit);
                    else if (finger.tapCount == 2)
                        player.Move(currentCellHit, true);
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
                    isWalkable = true;
                    isPlayer = currentCellHit == player.CurrentPlayerCell;
                    return true;
                }
                else
                {
                    isWalkable = isPlayer = false;
                    return false;
                }
            }

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
    }
}