using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Trajectory : MonoBehaviour
{
    public List<Vector3> points = new List<Vector3>();
    public List<Vector3> dirs = new List<Vector3>();
    public float width;

    // mesh gen
    MeshFilter meshFilter;
    public Mesh sharedMesh;
    public Mesh lineMesh;
    public Mesh projMesh;
    [SerializeField]
    int circleVertCount = 51;

    private void Start()
    {
        lineMesh = new Mesh();
        lineMesh.name = "Lines";
        projMesh = new Mesh();
        projMesh.name = "Projectiles";

        meshFilter = GetComponent<MeshFilter>();
        sharedMesh = meshFilter.sharedMesh = new Mesh();
        sharedMesh.name = "Trajectory";
    }

    public void DrawTrajectory()
    {
        // generate lines
        lineMesh.Clear();

        int lineCount = points.Count - 1;

        Vector3[] vertices = new Vector3[lineCount * 4];
        Vector3[] normals = new Vector3[lineCount * 4];
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0, v = 0; i < lineCount; i++, v += 4)
        {
            var startA = points[i] + (Vector3)Vector2.Perpendicular(dirs[i]) * width / 2f;
            var startB = points[i] - (Vector3)Vector2.Perpendicular(dirs[i]) * width / 2f;
            var endA = points[i + 1] + (Vector3)Vector2.Perpendicular(dirs[i]) * width / 2f;
            var endB = points[i + 1] - (Vector3)Vector2.Perpendicular(dirs[i]) * width / 2f;

            vertices[v] = startA;
            normals[v] = Vector3.back;
            uv[v] = new Vector2(0, 0);
            vertices[v + 1] = startB;
            normals[v + 1] = Vector3.back;
            uv[v + 1] = new Vector2(1, 0);
            vertices[v + 2] = endA;
            normals[v + 2] = Vector3.back;
            uv[v + 2] = new Vector2(0, 1);
            vertices[v + 3] = endB;
            normals[v + 3] = Vector3.back;
            uv[v + 3] = new Vector2(1, 1);
        }

        int[] triangles = new int[lineCount * 6];

        for (int line = 0, v = 0, t = 0; line < lineCount; line++, v += 4, t += 6)
        {
            triangles[t] = triangles[t + 4] = v;
            triangles[t + 1] = triangles[t + 3] = v + 3;
            triangles[t + 2] = v + 1;
            triangles[t + 5] = v + 2;
        }

        lineMesh.vertices = vertices;
        lineMesh.normals = normals;
        lineMesh.SetUVs(0, uv);
        lineMesh.SetTriangles(triangles, 0);
        lineMesh.RecalculateNormals();

        // generate circles
        projMesh.Clear();
        int projCount = lineCount; // this currently draws a circle at the end point as well, do -1 to not draw final hit point

        Vector3[] circleVerts = new Vector3[projCount * circleVertCount];
        Vector3[] circleNormals = new Vector3[projCount * circleVertCount];
        Vector2[] circleUV = new Vector2[projCount * circleVertCount];
        int[] circleTriangles = new int[projCount * circleVertCount * 3];

        float angle = 360.0f / (circleVertCount - 1);

        for (int i = 0, c = 0; i < projCount; i++, c += circleVertCount)
        {
            circleVerts[c] = points[i + 1];
            circleUV[c] = new Vector2(0.5f, 0.5f);
            circleNormals[c] = Vector3.back;

            for (int j = 1; j < circleVertCount; j++)
            {
                circleVerts[c + j] = circleVerts[c] + Quaternion.AngleAxis(angle * (j - 1), Vector3.back) * Vector3.up * (width / 2f);
                circleNormals[c + j] = Vector3.back;
                float normedHorizontal = (circleVerts[c + j].x + 1.0f) * 0.5f;
                float normedVertical = (circleVerts[c + j].y + 1.0f) * 0.5f;
                circleUV[c + j] = new Vector2(normedHorizontal, normedVertical);
            }

            for (int j = 0; j < circleVertCount - 2; j++)
            {
                int index = (c + j) * 3;
                circleTriangles[index + 0] = c + 0;
                circleTriangles[index + 1] = c + j + 1;
                circleTriangles[index + 2] = c + j + 2;
            }

            var lastTriangleIndex = ((i + 1) * circleVertCount * 3) - 3;
            circleTriangles[lastTriangleIndex + 0] = c + 0;
            circleTriangles[lastTriangleIndex + 1] = c + circleVertCount - 1;
            circleTriangles[lastTriangleIndex + 2] = c + 1;
        }

        projMesh.vertices = circleVerts;
        projMesh.normals = circleNormals;
        projMesh.SetUVs(0, circleUV);
        projMesh.SetTriangles(circleTriangles, 0);
        projMesh.RecalculateNormals();

        CombineInstance[] combine = new CombineInstance[2];
        combine[0].mesh = lineMesh;
        combine[0].transform = transform.localToWorldMatrix;
        combine[1].mesh = projMesh;
        combine[1].transform = transform.localToWorldMatrix;

        meshFilter.sharedMesh.CombineMeshes(combine);
    }

    void OnDrawGizmos()
    {
        if (points.Count == 0 || points == null || dirs.Count == 0 || dirs == null) return;

        float i = 0;
        foreach (var p in points)
        {
            Gizmos.color = new Color(i / 10f, i / 10f, i / 10f, 1);
            Gizmos.DrawSphere(p, 1.5f);
            i++;
        }

        int j = 0;
        foreach (var d in dirs)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(points[j], d * 10f);
            j++;
        }
    }
}
