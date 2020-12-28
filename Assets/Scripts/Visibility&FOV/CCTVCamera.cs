using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(FieldOfView))]
public class CCTVCamera : MonoBehaviour
{
    FieldOfView fov;
    Transform aim;
    float rotationAngle;
    float rotationSpeed;
    MinMaxData rotationLimits;
    float waitTime;

    public bool IsStatic { get => fov.IsStatic; set => fov.IsStatic = value; }
    public bool ShowFOV { get => fov.canDraw; set => fov.canDraw = value; }
    public Transform Aim => aim;

    void Awake()
    {
        fov = GetComponent<FieldOfView>();
        aim = fov.aim = transform.GetChild(0);
    }

    public void SetCamViewDistance(float distance) => fov.viewRadius = distance;
    public void SetCamViewAngle(float angle) => fov.viewAngle = angle;
    public void SetCamRotAngle(float angle) => rotationAngle = angle;
    public void SetCamRotSpeed(float speed) => rotationSpeed = speed;
    public void SetCamWaitTime(float time) => waitTime = time;
    public void SetCamRotLimits(MinMaxData limits) => rotationLimits = limits;

    public void InitCam(float viewDistance, float viewAngle)
    {
        SetCamViewDistance(viewDistance);
        SetCamViewAngle(viewAngle);
    }

    public void InitCam(float viewDistance, float viewAngle, float lookDirAngle)
    {
        Vector3 rotEuler = aim.rotation.eulerAngles;
        rotEuler.z = lookDirAngle;
        aim.rotation = Quaternion.Euler(rotEuler);

        SetCamViewDistance(viewDistance);
        SetCamViewAngle(viewAngle);

        IsStatic = true;
    }

    public void InitCam(float viewDistance, float viewAngle, float lookDirAngle, MinMaxData rotLimits, float rotSpeed, float waitTime)
    {
        Vector3 rotEuler = aim.rotation.eulerAngles;
        rotEuler.z = lookDirAngle;
        aim.rotation = Quaternion.Euler(rotEuler);

        SetCamViewDistance(viewDistance);
        SetCamViewAngle(viewAngle);
        SetCamRotSpeed(rotSpeed);
        SetCamRotLimits(new MinMaxData(aim.rotation.eulerAngles.z - rotLimits.min, aim.rotation.eulerAngles.z + rotLimits.max));
        SetCamWaitTime(waitTime);

        StartCoroutine(RotateCam());
    }

    IEnumerator RotateCam()
    {
        var limit = Random.value < 0.5f ? rotationLimits.min : rotationLimits.max;
        var delay = new WaitForSeconds(waitTime);

        yield return new WaitForSeconds(Random.Range(0f, waitTime));

        while (true)
        {
            var current = aim.rotation;
            var end = Quaternion.Euler(0, 0, limit);

            var t = 0f;
            while(aim.rotation != Quaternion.Euler(0, 0, limit) && t < 1.01f)
            {
                t += rotationSpeed * Time.deltaTime;
                aim.rotation = Quaternion.Slerp(current, end, t);

                yield return null;
            }

            if (limit == rotationLimits.min)
                limit = rotationLimits.max;
            else
                limit = rotationLimits.min;

            yield return delay;
        }
    }

    public float GetCoverageSize()
    {
        var meshPoints = fov.GetFOVSnapshot();

        float temp = 0;
        int i = 0;

        for(; i < meshPoints.Count; i++)
        {
            if(i != meshPoints.Count - 1)
            {
                float mulA = meshPoints[i].x * meshPoints[i + 1].y;
                float mulB = meshPoints[i + 1].x * meshPoints[i].y;
                temp = temp + (mulA - mulB);
            }
            else
            {
                float mulA = meshPoints[i].x * meshPoints[0].y;
                float mulB = meshPoints[0].x * meshPoints[i].y;
                temp = temp + (mulA - mulB);
            }
        }

        temp *= 0.5f;

        return Mathf.Abs(temp);
    }

    public Vector3 GetCoverageCenter()
    {
        var meshPoints = fov.GetFOVSnapshot();
        Vector3 total = Vector3.zero;

        for(int i = 0; i < meshPoints.Count; i++)
        {
            total = new Vector3(total.x + meshPoints[i].x, total.y + meshPoints[i].y, total.z + meshPoints[i].z);
        }

        return total / meshPoints.Count;
    }

    public List<Vector3> GetTopDirections()
    {
        var temp = new List<Vector3>();

        for(int i = 0; i < Mathf.RoundToInt(rotationLimits.max - rotationLimits.min); i++)
        {
            var hit = Physics2D.Raycast(transform.position, Quaternion.Euler(0, 0, rotationLimits.min + i) * Vector3.up, fov.viewRadius, fov.obstacleMask);

            Vector2 freeDir = Quaternion.Euler(0, 0, rotationLimits.min + i) * Vector2.up * fov.viewRadius;
            var point = hit.collider == null ? (Vector2)transform.position + freeDir : hit.point;
            var cell = Physics2D.OverlapCircle(point, 1f, 1 << 10);

            if (cell != null && cell.gameObject.GetComponentInParent<MazeCell>().state > 1)
                continue;

            if (hit.collider == null)
            {
                temp.Add(freeDir);
            }
            else
            {
                temp.Add(hit.point - (Vector2)transform.position);
            }
        }

        return temp.OrderByDescending(x => x.sqrMagnitude).ToList();
    }

    private void OnDrawGizmos()
    {
        var list = GetTopDirections();
        Color tester = new Color(0, 0, 0, 1);
        var temp = tester;

        if (list.Count == 0)
            Debug.Log($"empty candidate list for {gameObject.name}");

        int i = 0;
        for(; i < 50 && i < list.Count; )
        {
            tester = Color.Lerp(temp, Color.red, i / 50f);
            Debug.DrawRay(transform.position, list[i], tester);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + list[i], GameManager.CellDiagonal / 4f);
            i++;
        }

        var center = GetCoverageCenter();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 5f);
        Gizmos.color = Color.white;
    }
}
