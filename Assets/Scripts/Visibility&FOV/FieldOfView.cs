using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Taken and adapted to 2D from Sebastian Lague's FOV tutorial

[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class FieldOfView : MonoBehaviour
{
	[Header("Mesh Settings")]
	public float viewRadius;
	[Range(0, 360)]
	public float viewAngle;

	public LayerMask targetMask;
	public LayerMask obstacleMask;
	Collider2D[] hits;
	ContactFilter2D filter;

	[HideInInspector]
	public List<Transform> visibleTargets = new List<Transform>();

	public float meshResolution;
	public int edgeResolveIterations;
	public float edgeDstThreshold;

	public float maskCutawayDst = .1f;

	MeshFilter viewMeshFilter;
	Mesh viewMesh;

	public Transform aim;
	public float zOffset;

	public bool canDraw = true;
	bool meshCleared = false;
	public bool IsStatic { get; set; } = false;

	List<Vector3> viewPoints = new List<Vector3>();
	Vector3[] vertices;
	List<int> triangles;

	PerObjectMaterialProperties props;

	// detection settings
	public bool AccumulateExposure { get; set; } = false;
	public float ContinuousExposureTime { get; private set; }
	public float TotalExposureTime { get; private set; }
	public float ExposureLimit { get; set; }
	public MazeCell lastKnownPlayerPos;

	public void SetColorBlendFactor(float factor) => props.SetBlendFactor(factor);
	public MazeCell GetPlayerPos()
    {
		var cell = lastKnownPlayerPos;
		lastKnownPlayerPos = null;
		return cell;
    }

	void Start()
	{
		props = GetComponent<PerObjectMaterialProperties>();

		// manually sized vertex array and tri list
		vertices = new Vector3[800];
		triangles = new List<int>(2400);

		viewMeshFilter = GetComponent<MeshFilter>();
		viewMesh = new Mesh();
		viewMesh.name = "View Mesh";
		viewMeshFilter.mesh = viewMesh;

		hits = new Collider2D[100];
		filter.layerMask = obstacleMask;
		zOffset = transform.parent.transform.position.z;

		StartCoroutine("FindTargetsWithDelay", .35f);

		//if (IsStatic)
		//	DrawFieldOfView();
	}

    private void Update()
    {
		if (CanSeePlayer())
		{
			ContinuousExposureTime += Time.deltaTime;
			TotalExposureTime += Time.deltaTime;

			lastKnownPlayerPos = GameManager.player.CurrentCell;
		}
        else
        {
			if (ContinuousExposureTime > ExposureLimit)
				ContinuousExposureTime = ExposureLimit;

			if (!AccumulateExposure)
				ContinuousExposureTime = 0;
			else if (ContinuousExposureTime > 0)
				ContinuousExposureTime -= Time.deltaTime;
        }
    }

    private void LateUpdate()
	{
        if (!IsStatic)
        {
			if (canDraw)
			{
				DrawFieldOfView();
				meshCleared = false;
			}
			else if (!meshCleared)
			{
				viewMesh.Clear();
				meshCleared = true;
			}
        }
    }

    public void Disable()
    {
		ClearMesh();
        enabled = false;
        ContinuousExposureTime = 0f;
		visibleTargets.Clear();
    }

    IEnumerator FindTargetsWithDelay(float delay)
	{
		var cachedDelay = new WaitForSeconds(delay);

		while (true)
		{
			yield return cachedDelay;
			FindVisibleTargets();
		}
	}

	public void ClearMesh()
	{
		viewMesh.Clear();
		meshCleared = true;
	}

	private void FindVisibleTargets()
	{
		visibleTargets.Clear();
		var count = Physics2D.OverlapCircleNonAlloc(transform.position, viewRadius, hits, targetMask);

		for (int i = 0; i < count; i++)
		{
			Transform target = hits[i].transform;
			Vector2 dirToTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
			if (Vector2.Angle(aim.up, dirToTarget) < viewAngle / 2)
			{
				float dstToTarget = Vector2.Distance(transform.position, target.position);
				if (!Physics2D.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
				{
					visibleTargets.Add(target);
				}
			}
		}
	}

	public bool CanSeePlayer()
    {
		if (visibleTargets.Contains(GameManager.player.transform))
			return true;

		return false;
    }

	public void DrawFieldOfView()
    {
        ConstructViewMesh();

        int vertexCount = viewPoints.Count + 1;

        triangles.Clear();

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]) + Vector3.up * maskCutawayDst;

            if (i < vertexCount - 2)
            {
                triangles.Add(0);
                triangles.Add(i + 1);
                triangles.Add(i + 2);
            }
        }

        viewMesh.Clear();

        viewMesh.vertices = vertices;
        viewMesh.SetTriangles(triangles, 0);
    }

    private void ConstructViewMesh()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        viewPoints.Clear();
        ViewCastInfo oldViewCast = new ViewCastInfo();
        for (int i = 0; i <= stepCount; i++)
        {
            float angle;

            // using aim holder rotation for facing direction
            angle = -aim.eulerAngles.z - viewAngle / 2 + stepAngleSize * i;

            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0)
            {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector2.zero)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector2.zero)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }
    }

	public List<Vector3> GetFOVSnapshot()
    {
		var points = new List<Vector3>();

		int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
		float stepAngleSize = viewAngle / stepCount;
		ViewCastInfo oldViewCast = new ViewCastInfo();
		for (int i = 0; i <= stepCount; i++)
		{
			float angle;

			// using aim holder rotation for facing direction
			angle = -aim.eulerAngles.z - viewAngle / 2 + stepAngleSize * i;

			ViewCastInfo newViewCast = ViewCast(angle);

			if (i > 0)
			{
				bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
				if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
				{
					EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
					if (edge.pointA != Vector2.zero)
					{
						points.Add(edge.pointA);
					}
					if (edge.pointB != Vector2.zero)
					{
						points.Add(edge.pointB);
					}
				}
			}

			points.Add(newViewCast.point);
			oldViewCast = newViewCast;
		}

		return points;
    }

	EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
	{
		float minAngle = minViewCast.angle;
		float maxAngle = maxViewCast.angle;
		Vector2 minPoint = Vector2.zero;
		Vector2 maxPoint = Vector2.zero;

		for (int i = 0; i < edgeResolveIterations; i++)
		{
			float angle = (minAngle + maxAngle) / 2;
			ViewCastInfo newViewCast = ViewCast(angle);

			bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
			if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
			{
				minAngle = angle;
				minPoint = newViewCast.point;
			}
			else
			{
				maxAngle = angle;
				maxPoint = newViewCast.point;
			}
		}

		return new EdgeInfo(minPoint, maxPoint);
	}


	ViewCastInfo ViewCast(float globalAngle)
	{
		Vector2 dir = DirFromAngle(globalAngle, true);
		RaycastHit2D hit;

		hit = Physics2D.Raycast(transform.position, dir, viewRadius, obstacleMask);

		if (hit.collider != null)
			return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
		else
			return new ViewCastInfo(false, (Vector2)transform.position + dir * viewRadius, viewRadius, globalAngle);
	}

	public Vector2 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
	{
		if (!angleIsGlobal)
		{
			angleInDegrees += transform.eulerAngles.z;
		}
		return new Vector2(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
	}

	public struct ViewCastInfo
	{
		public bool hit;
		public Vector2 point;
		public float dst;
		public float angle;

		public ViewCastInfo(bool _hit, Vector2 _point, float _dst, float _angle)
		{
			hit = _hit;
			point = _point;
			dst = _dst;
			angle = _angle;
		}
	}

	public struct EdgeInfo
	{
		public Vector2 pointA;
		public Vector2 pointB;

		public EdgeInfo(Vector2 _pointA, Vector2 _pointB)
		{
			pointA = _pointA;
			pointB = _pointB;
		}
	}
}