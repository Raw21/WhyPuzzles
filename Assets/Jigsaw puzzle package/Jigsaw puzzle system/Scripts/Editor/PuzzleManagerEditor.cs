using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PuzzleManager))]
[CanEditMultipleObjects]
public class PuzzleManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PuzzleManager myScript = (PuzzleManager) target;
        if (GUILayout.Button("Get possible scatter points"))
        {
            myScript.GetPossibleScatterPoints();
        }

        if (GUILayout.Button("Find all adjacent pieces"))
        {
            myScript.FindAllAdjacentPieces();
        }
    }
}
