using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SaveDataManager))]

public class SaveDataManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SaveDataManager myScript = (SaveDataManager) target;
        if (GUILayout.Button("Delete save data for active puzzles"))
        {
            myScript.DeleteSaveData();
        }
    }
}
