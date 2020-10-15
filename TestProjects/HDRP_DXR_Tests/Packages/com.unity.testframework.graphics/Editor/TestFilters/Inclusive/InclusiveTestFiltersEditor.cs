using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(InclusiveTestFilters))]
class InclusiveTestFiltersEditor : Editor
{
    SerializedProperty filter;

    public void OnEnable()
    {
        filter = serializedObject.FindProperty("filter");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var fieldLayoutOptions = new GUILayoutOption[]
        {
            GUILayout.Width(120f)
        };
        var scenes = filter.FindPropertyRelative("FilteredScenes");
        var buildPlatform = filter.FindPropertyRelative("BuildPlatform");

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(40);
        if (scenes.arraySize > 0)
        {
            // This little space is needed so the foldout carrot doesn't overlap the previous field.
            GUILayout.Space(10);
        }
        EditorGUILayout.LabelField(new GUIContent("Scenes", "The scene to apply this filter to"), GUILayout.Width(150));
        if (scenes.arraySize > 0)
        {
            EditorGUILayout.LabelField(new GUIContent("Platform", "The build platform to filter, No Target will filter all platforms."), fieldLayoutOptions);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(scenes.FindPropertyRelative("Array.size"), GUIContent.none, GUILayout.Width(40));
        if (scenes.arraySize > 1)
        {
            // This little space is needed so the foldout carrot doesn't overlap the previous field.
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(GUILayout.Width(150));

            EditorGUILayout.BeginFoldoutHeaderGroup(true, "Filtered Scenes");

            for (int j = 0; j < scenes.arraySize; j++)
            {
                var singleScene = scenes.FindPropertyRelative(string.Format("Array.data[{0}]", j));
                EditorGUILayout.PropertyField(singleScene, GUIContent.none, GUILayout.Width(150));
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        else if (scenes.arraySize == 1)
        {
            var singleScene = scenes.FindPropertyRelative("Array.data[0]");
            EditorGUILayout.PropertyField(singleScene, GUIContent.none, fieldLayoutOptions);
        }

        EditorGUILayout.PropertyField(buildPlatform, GUIContent.none, fieldLayoutOptions);
        EditorGUILayout.EndHorizontal();
        serializedObject.ApplyModifiedProperties();
    }
}
