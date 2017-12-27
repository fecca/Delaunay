using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Generate))]
public class GeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var generator = (Generate)target;

		generator.Size = EditorGUILayout.IntField("Size", generator.Size);
		generator.NumberOfCandidates = EditorGUILayout.IntField("Number of candidates", generator.NumberOfCandidates);

		if (GUILayout.Button("Rebuild"))
		{
			generator.Create();
		}
	}
}




















