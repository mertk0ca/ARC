using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DayNightController))]
public class DayNightControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty hour = serializedObject.FindProperty("hourOfDay");
        float value = hour.floatValue;
        int h = Mathf.FloorToInt(value);
        int m = Mathf.FloorToInt((value - h) * 60f);
        bool night = value >= serializedObject.FindProperty("nightStartHour").floatValue
                     || value < serializedObject.FindProperty("nightEndHour").floatValue;

        EditorGUILayout.Space(4f);
        Rect box = EditorGUILayout.BeginVertical("box");
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(night ? "☾  Gece" : "☀  Gündüz", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{h:00}:{m:00}", EditorStyles.largeLabel);
        EditorGUI.BeginChangeCheck();
        float next = EditorGUILayout.Slider("Güneş konumu", value, 0f, 24f);
        if (EditorGUI.EndChangeCheck())
            hour.floatValue = next;
        EditorGUILayout.LabelField("0 = gece yarısı, 12 = öğle, 24 = gece");
        EditorGUILayout.Space(4f);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(8f);

        DrawPropertiesExcluding(serializedObject, "m_Script", "hourOfDay");
        serializedObject.ApplyModifiedProperties();

        if (GUI.changed && target is DayNightController controller)
            controller.ApplyHour(hour.floatValue);
    }
}
