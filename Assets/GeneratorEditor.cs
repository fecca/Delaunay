using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Generate))]
public class GeneratorEditor : Editor
{
	SerializedProperty Size;
	SerializedProperty NumberOfCandidates;

	private void OnEnable()
	{
		Size = serializedObject.FindProperty("Size");
		NumberOfCandidates = serializedObject.FindProperty("NumberOfCandidates");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(Size);
		EditorGUILayout.PropertyField(NumberOfCandidates);

		var generator = (Generate)target;
		if (GUILayout.Button("Rebuild"))
		{
			generator.Create();
		}

		serializedObject.ApplyModifiedProperties();
	}
}




















