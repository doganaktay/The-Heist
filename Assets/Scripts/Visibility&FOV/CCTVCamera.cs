using System.Collections;
using System.Collections.Generic;
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

        while (true)
        {
            var current = aim.rotation;
            var end = Quaternion.Euler(0, 0, limit);

            //var current = aim.rotation.eulerAngles.z;

            Debug.DrawRay(transform.position + Vector3.back * 3f, Quaternion.Euler(0, 0, limit) * Vector3.up * 20f, Color.red, waitTime);

            var t = 0f;
            while(aim.rotation != Quaternion.Euler(0, 0, limit) && t < 1.01f)
            {
                t += rotationSpeed * Time.deltaTime;
                aim.rotation = Quaternion.Slerp(current, end, t);

                //t += rotationSpeed * Time.deltaTime;
                //float z = Mathf.Lerp(current, limit, t);

                //aim.rotation = Quaternion.Euler(0, 0, z);

                yield return null;
            }

            if (limit == rotationLimits.min)
                limit = rotationLimits.max;
            else
                limit = rotationLimits.min;

            Debug.Log($"{gameObject.name} completed rotation. Now going to rotate from {current} to {limit}");

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

}
