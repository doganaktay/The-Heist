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

    public bool ShowFOV { get => fov.canDraw; set => fov.canDraw = value; }

    void Awake()
    {
        fov = GetComponent<FieldOfView>();
        aim = fov.aim = transform.GetChild(0);
    }

    public void SetCamViewDistance(float distance) => fov.viewRadius = distance;
    public void SetCamViewAngle(float angle) => fov.viewAngle = angle;
    public void SetCamRotAngle(float angle) => rotationAngle = angle;
    public void SetCamRotSpeed(float speed) => rotationSpeed = speed;

    public void InitCam(float viewDistance, float viewAngle, float lookDirAngle = 0, float rotAngle = 0, float rotSpeed = 0)
    {
        Vector3 rotEuler = aim.rotation.eulerAngles;
        rotEuler.z = lookDirAngle;
        aim.rotation = Quaternion.Euler(rotEuler);

        SetCamViewDistance(viewDistance);
        SetCamViewAngle(viewAngle);

        SetCamRotAngle(rotAngle);
        SetCamRotSpeed(rotSpeed);
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
