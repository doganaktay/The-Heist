using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    // material extensions are in the ShaderControl script

    public static void Shuffle<T>(this IList<T> list)
    {
        System.Random rng = GameManager.Random;
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static IntVector2 GetDirection(this MazeCell from, MazeCell to)
    {
        return new IntVector2(to.pos.x - from.pos.x, to.pos.y - from.pos.y);
    }

    public static IntVector2 GetNextVector(this IntVector2 current, int stepCount = 1)
    {
        for (int i = 0; i < MazeDirections.allVectors.Length; i++)
        {
            if (current == MazeDirections.allVectors[i])
                return MazeDirections.allVectors[(i + stepCount) % MazeDirections.allVectors.Length];
        }

        return new IntVector2(0, 0);
    }

    public static void Reset(this LineRenderer line) => line.positionCount = 0;

    public static Vector2 GetMidPoint(this Vector2 a, Vector2 b)
    {
        return new Vector2(a.x + (b.x - a.x) / 2f, a.y + (b.y - a.y) / 2f);
    }

    public static RaycastHit2D GetClosestPoint(Vector2 s1, RaycastHit2D e1, Vector2 s2, RaycastHit2D e2, Vector2 s3, RaycastHit2D e3)
    {
        var d1 = Vector2.Distance(s1, e1.point);
        var d2 = Vector2.Distance(s2, e2.point);
        var d3 = Vector2.Distance(s3, e3.point);

        var d = Mathf.Min(d1, d2, d3);

        if (d == d1)
            return e1;
        else if (d == d2)
            return e2;
        else
            return e3;
    }

    public static Vector2 Get2DPerp(Vector2 vec)
    {
        return new Vector2(-vec.y, vec.x);
    }

    public static Vector2 GetIntersection(Vector2 startPosA, Vector2 endPosA, Vector2 startPosB, Vector2 endPosB)
    {
        Vector2 c = startPosB - startPosA;
        Vector2 u = endPosB - startPosB;
        Vector2 v = endPosA - startPosA;
        Vector2 uPerp = Get2DPerp(u);

        float t = Vector2.Dot(uPerp, c) / Vector2.Dot(uPerp, v);

        Vector2 intersectionPoint = Vector2.Lerp(startPosA, endPosA, t);

        return intersectionPoint;
    }

    public static void Face(this Transform t, Transform other, ref Quaternion deriv, float maxDelta)
    {
        Vector3 dir = (other.position - t.position).normalized;
        float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg) - 90f;
        angle = angle < 0 ? angle + 360f : angle;

        var newRot = Quaternion.AngleAxis(angle, Vector3.forward);
        t.rotation = QuaternionUtil.SmoothDamp(t.rotation, newRot, ref deriv, maxDelta * Time.deltaTime);
    }

    public static void Face(this Transform t, Vector3 pos, ref Quaternion deriv, float maxDelta)
    {
        Vector3 dir = (pos - t.position).normalized;
        float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg) - 90f;
        angle = angle < 0 ? angle + 360f : angle;

        var newRot = Quaternion.AngleAxis(angle, Vector3.forward);
        t.rotation = QuaternionUtil.SmoothDamp(t.rotation, newRot, ref deriv, maxDelta * Time.deltaTime);
    }

    public static void Face(this Transform t, Quaternion target, ref Quaternion deriv, float maxDelta)
    {
        t.rotation = QuaternionUtil.SmoothDamp(t.rotation, target, ref deriv, maxDelta * Time.deltaTime);
    }

    public static Vector3 SmoothLerp(Vector3 pastPosition, Vector3 pastTargetPosition, Vector3 targetPosition, float time, float speed)
    {
        Vector3 f = pastPosition - pastTargetPosition + (targetPosition - pastTargetPosition) / (speed * time);
        return targetPosition - (targetPosition - pastTargetPosition) / (speed * time) + f * Mathf.Exp(-speed * time);
    }

    public static float SmoothLerp(float currentAngle, float pastTargetAngle, float targetAngle, float time, float speed)
    {
        float f = currentAngle - pastTargetAngle + (targetAngle - pastTargetAngle) / (speed * time);
        return targetAngle - (targetAngle - pastTargetAngle) / (speed * time) + f * Mathf.Exp(-speed * time);
    }

    public static bool IsWalkable(this MazeCell cell)
    {
        return cell.state < 2;
    }

    public static float GetLookAngle(this Vector3 direction)
    {
        var dirNormalized = direction.normalized;
        return Mathf.Atan2(dirNormalized.x, dirNormalized.y) * Mathf.Rad2Deg;
    }

    public static float GetLookAngle(this Vector2 direction)
    {
        var dirNormalized = direction.normalized;
        return Mathf.Atan2(dirNormalized.x, dirNormalized.y) * Mathf.Rad2Deg;
    }

    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    public static Color FromRGB(this Color color, int r, int g, int b, int a = 255)
    {
        color.r = Mathf.Clamp((float)r, 0, 255) / 255;
        color.g = Mathf.Clamp((float)g, 0, 255) / 255;
        color.b = Mathf.Clamp((float)b, 0, 255) / 255;
        color.a = Mathf.Clamp((float)a, 0, 255) / 255;

        return color;
    }

    public static void ClearChildren(this GameObject holder, int index = 0)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException();

        for (; index < holder.transform.childCount; index++)
        {
            var t = holder.transform.GetChild(index);
            UnityEngine.Object.Destroy(t.gameObject);
        }
    }

    public static bool IsWithinRange(this Vector3 from, Vector3 to, float distance)
    {
        return (to - from).sqrMagnitude < distance * distance;
    }

    public static bool IsConnectedTo(this MazeCell current, MazeCell next, bool isCardinal)
    {
        if (isCardinal)
        {
            if (current.connectedCells.Contains(next) || current.placedConnectedCells.Contains(next))
                return true;
        }
        else
        {
            int connected = 0;

            foreach (var cell in current.connectedCells)
            {
                if (cell.connectedCells.Contains(next) || cell.placedConnectedCells.Contains(next))
                    connected++;
            }
            foreach (var cell in current.placedConnectedCells)
            {
                if (cell.connectedCells.Contains(next) || cell.placedConnectedCells.Contains(next))
                    connected++;
            }

            if (connected == 2)
                return true;
        }

        return false;
    }

    public static bool IsCloseAndInView(this Transform from, Transform to, float distance, LayerMask mask) =>
        (to.position - from.position).sqrMagnitude < distance && !Physics2D.Raycast(from.position, to.position - from.position, distance, mask);

#if UNITY_EDITOR

    public static string Debug<T>(this ICollection<T> collection)
    {
        string result = "";

        foreach (var item in collection)
            result += item + "-";

        return result;
    }

#endif
}
