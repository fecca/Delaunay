using UnityEditor;
using UnityEngine;

public static class VectorExtensions
{
	public static Vertex3 ToVertex3(this Vector3 vector)
	{
		return new Vertex3(vector.x, vector.y, vector.z);
	}
}

[CustomEditor(typeof(Generate))]
public class GeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		DrawDefaultInspector();

		var generator = (Generate)target;
		if (GUILayout.Button("Rebuild"))
		{
			generator.Create();
		}

		serializedObject.ApplyModifiedProperties();
	}
}