using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ShaderControl
{
	// static property references
	public static readonly int colorIndex = Shader.PropertyToID("_ColorIndex");
	public static readonly int pathIndex = Shader.PropertyToID("_PathIndex");
	public static readonly int pathCount = Shader.PropertyToID("_PathCount");
	public static readonly int baseColor = Shader.PropertyToID("_BaseColor");
	// following is not exposed as a property on the shader
	// a shader property needs to not be exposed in order to be set globally in script
	public static readonly int restartTime = Shader.PropertyToID("_RestartTime");

	// material extension methods for setting properties
	public static void SetColorIndex(this Material mat, int index) => mat.SetInt(colorIndex, index);
	public static int GetColorIndex(this Material mat) { return mat.GetInt(colorIndex); }
	public static void SetPathIndex(this Material mat, float index) => mat.SetFloat(pathIndex, index);
	public static float GetPathIndex(this Material mat) { return mat.GetFloat(pathIndex); }
	public static void SetPathCount(this Material mat, float count) => mat.SetFloat(pathCount, count);
	public static float GetPathCount(this Material mat) { return mat.GetFloat(pathCount); }
	public static void SetRestartTime(this Material mat, float time) => mat.SetFloat(restartTime, time);
	public static float GetRestartTime(this Material mat) { return mat.GetFloat(restartTime); }
	public static void SetBaseColor(this Material mat, Color color) => mat.SetColor(baseColor, color);

}
