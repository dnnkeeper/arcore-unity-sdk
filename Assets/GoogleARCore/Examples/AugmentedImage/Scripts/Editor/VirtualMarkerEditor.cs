using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VirtualMarker))]
[CanEditMultipleObjects]
public class VirtualMarkerEditor : Editor
{
    SerializedProperty databaseIndex;

    SerializedProperty imageDatabase;

    void OnEnable()
    {
        databaseIndex = serializedObject.FindProperty("databaseIndex");
        imageDatabase = serializedObject.FindProperty("imageDatabase");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(databaseIndex);
        EditorGUILayout.PropertyField(imageDatabase);

        bool needUpdate = false;

        if (serializedObject.hasModifiedProperties)
        {
            needUpdate = true;
        }

        serializedObject.ApplyModifiedProperties();

        if (needUpdate)
        {
            ((VirtualMarker)serializedObject.targetObject).UpdateImage();
        }
    }

    /*public void OnSceneGUI()
    {
        var t = (target as LookAtPoint);

        EditorGUI.BeginChangeCheck();
        Vector3 pos = Handles.PositionHandle(t.lookAtPoint, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Move point");
            t.lookAtPoint = pos;
            t.Update();
        }
    }*/
}
