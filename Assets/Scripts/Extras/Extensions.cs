using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    // material extensions are in the ShaderControl script

    public static void Shuffle<T>(this IList<T> list)
    {
        System.Random rng = new System.Random();
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

    public static void LookAt2D(this Transform transform, Transform target, float turnSpeed)
    {
        transform.up = Vector2.Lerp(transform.up, target.position - transform.position, turnSpeed * Time.deltaTime);
    }

    public static void LookAt2D(this Transform transform, Transform target)
    {
        transform.up = (Vector2)(target.position - transform.position);
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
        {
            Debug.LogError("Index can't be less than zero.");
            return;
        }

        for (; index < holder.transform.childCount; index++)
        {
            var t = holder.transform.GetChild(index);
            UnityEngine.Object.Destroy(t.gameObject);
        }
    }
}
