using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PuzzlePiecesData
{
    public TransformData[] transforms;
    public string name;


    public PuzzlePiecesData(string dataName, PuzzleManager puzzleManager)
    {
        name = dataName;

        if (puzzleManager == null)
        {
            transforms = new TransformData[0];
            return;
        }
        
        var puzzlePieces = puzzleManager.m_puzzlePieceAnchors;

        transforms = new TransformData[puzzlePieces.Count];

        for (int i = 0; i < transforms.Length; i++)
            transforms[i] = new TransformData(puzzlePieces[i]);
    }	
}
