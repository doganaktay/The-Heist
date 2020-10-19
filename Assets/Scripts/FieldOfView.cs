using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Taken and adapted to 2D from Sebastian Lague's FOV tutorial

public class FieldOfView : MonoBehaviour
{

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

	void Start()
	{
		viewMeshFilter = GetComponent<MeshFilter>();
		viewMesh = new Mesh();
		viewMesh.name = "View Mesh";
		viewMeshFilter.mesh = viewMesh;

		hits = new Collider2D[100];
		filter.layerMask = obstacleMask;
		zOffset = transform.parent.transform.position.z;

		StartCoroutine("FindTargetsWithDelay", .2f);
	}


	IEnumerator FindTargetsWithDelay(float delay)
	{
		while (true)
		{
			yield return new WaitForSeconds(delay);
			FindVisibleTargets();
		}
	}

	void LateUpdate()
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

	void FindVisibleTargets()
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

	void DrawFieldOfView()
	{
		int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
		float stepAngleSize = viewAngle / stepCount;
		List<Vector3> viewPoints = new List<Vector3>();
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

		int vertexCount = viewPoints.Count + 1;
		Vector3[] vertices = new Vector3[vertexCount];
		int[] triangles = new int[(vertexCount - 2) * 3];

		vertices[0] = Vector3.zero;
		for (int i = 0; i < vertexCount - 1; i++)
		{
			vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]) + Vector3.up * maskCutawayDst;

			if (i < vertexCount - 2)
			{
				triangles[i * 3] = 0;
				triangles[i * 3 + 1] = i + 1;
				triangles[i * 3 + 2] = i + 2;
			}
		}

		viewMesh.Clear();

		viewMesh.vertices = vertices;
		viewMesh.triangles = triangles;
		viewMesh.RecalculateNormals();
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
		{
			// zOffset is added here to make the directions consistent with 2D
			return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
		}
		else
		{
			return new ViewCastInfo(false, (Vector2)transform.position + dir * viewRadius, viewRadius, globalAngle);
		}
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

//public class FieldOfView : MonoBehaviour
//{

//	public float viewRadius;
//	[Range(0, 360)]
//	public float viewAngle;

//	public LayerMask targetMask;
//	public LayerMask obstacleMask;
//	Collider2D[] hits;
//	ContactFilter2D filter;

//	[HideInInspector]
//	public List<Transform> visibleTargets = new List<Transform>();

//	public float meshResolution;
//	public int edgeResolveIterations;
//	public float edgeDstThreshold;

//	public float maskCutawayDst = .1f;

//	MeshFilter viewMeshFilter;
//	Mesh viewMesh;

//	public Transform aim;
//	public float playerZOffset;

//	public bool canDraw = true;
//	bool meshCleared = false;

//	void Start()
//	{
//		viewMeshFilter = GetComponent<MeshFilter>();
//		viewMesh = new Mesh();
//		viewMesh.name = "View Mesh";
//		viewMeshFilter.mesh = viewMesh;

//		hits = new Collider2D[100];
//		filter.layerMask = obstacleMask;
//		playerZOffset = transform.parent.transform.position.z;

//		StartCoroutine("FindTargetsWithDelay", .2f);
//	}


//	IEnumerator FindTargetsWithDelay(float delay)
//	{
//		while (true)
//		{
//			yield return new WaitForSeconds(delay);
//			FindVisibleTargets();
//		}
//	}

//	void LateUpdate()
//	{
//		if (canDraw)
//		{
//			DrawFieldOfView();
//			meshCleared = false;
//		}
//		else if (!meshCleared)
//		{
//			viewMesh.Clear();
//			meshCleared = true;
//		}
//	}

//	void FindVisibleTargets()
//	{
//		visibleTargets.Clear();
//		var count = Physics2D.OverlapCircleNonAlloc(transform.position, viewRadius, hits, targetMask);

//		for (int i = 0; i < count; i++)
//		{
//			Transform target = hits[i].transform;
//			Vector3 dirToTarget = (target.position - transform.position).normalized;
//			if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
//			{
//				float dstToTarget = Vector3.Distance(transform.position, target.position);
//				if (!Physics2D.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
//				{
//					visibleTargets.Add(target);
//				}
//			}
//		}
//	}

//	void DrawFieldOfView()
//	{
//		int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
//		float stepAngleSize = viewAngle / stepCount;
//		List<Vector3> viewPoints = new List<Vector3>();
//		ViewCastInfo oldViewCast = new ViewCastInfo();
//		for (int i = 0; i <= stepCount; i++)
//		{
//			// using aim holder rotation for facing direction
//			float angle = -aim.eulerAngles.z - viewAngle / 2 + stepAngleSize * i;
//			ViewCastInfo newViewCast = ViewCast(angle);

//			if (i > 0)
//			{
//				bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
//				if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
//				{
//					EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
//					if (edge.pointA != Vector3.zero)
//					{
//						viewPoints.Add(edge.pointA);
//					}
//					if (edge.pointB != Vector3.zero)
//					{
//						viewPoints.Add(edge.pointB);
//					}
//				}

//			}

//			viewPoints.Add(newViewCast.point);
//			oldViewCast = newViewCast;
//		}

//		int vertexCount = viewPoints.Count + 1;
//		Vector3[] vertices = new Vector3[vertexCount];
//		int[] triangles = new int[(vertexCount - 2) * 3];

//		vertices[0] = Vector3.zero;
//		for (int i = 0; i < vertexCount - 1; i++)
//		{
//			vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]) + Vector3.up * maskCutawayDst;

//			if (i < vertexCount - 2)
//			{
//				triangles[i * 3] = 0;
//				triangles[i * 3 + 1] = i + 1;
//				triangles[i * 3 + 2] = i + 2;
//			}
//		}

//		viewMesh.Clear();

//		viewMesh.vertices = vertices;
//		viewMesh.triangles = triangles;
//		viewMesh.RecalculateNormals();
//	}


//	EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
//	{
//		float minAngle = minViewCast.angle;
//		float maxAngle = maxViewCast.angle;
//		Vector3 minPoint = Vector3.zero;
//		Vector3 maxPoint = Vector3.zero;

//		for (int i = 0; i < edgeResolveIterations; i++)
//		{
//			float angle = (minAngle + maxAngle) / 2;
//			ViewCastInfo newViewCast = ViewCast(angle);

//			bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
//			if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
//			{
//				minAngle = angle;
//				minPoint = newViewCast.point;
//			}
//			else
//			{
//				maxAngle = angle;
//				maxPoint = newViewCast.point;
//			}
//		}

//		return new EdgeInfo(minPoint, maxPoint);
//	}


//	ViewCastInfo ViewCast(float globalAngle)
//	{
//		Vector2 dir = DirFromAngle(globalAngle, true);
//		RaycastHit2D hit;

//		hit = Physics2D.Raycast(transform.position, dir, viewRadius, obstacleMask);

//		if (hit.collider != null)
//		{
//			// playerZOffset is added here to make the directions consistent with 2D
//			return new ViewCastInfo(true, new Vector3(hit.point.x, hit.point.y, playerZOffset), hit.distance, globalAngle);
//		}
//		else
//		{
//			return new ViewCastInfo(false, transform.position + (Vector3)dir * viewRadius, viewRadius, globalAngle);
//		}
//	}

//	public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
//	{
//		if (!angleIsGlobal)
//		{
//			angleInDegrees += transform.eulerAngles.z;
//		}
//		return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), 0f);
//	}

//	public struct ViewCastInfo
//	{
//		public bool hit;
//		public Vector3 point;
//		public float dst;
//		public float angle;

//		public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
//		{
//			hit = _hit;
//			point = _point;
//			dst = _dst;
//			angle = _angle;
//		}
//	}

//	public struct EdgeInfo
//	{
//		public Vector3 pointA;
//		public Vector3 pointB;

//		public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
//		{
//			pointA = _pointA;
//			pointB = _pointB;
//		}
//	}

//}