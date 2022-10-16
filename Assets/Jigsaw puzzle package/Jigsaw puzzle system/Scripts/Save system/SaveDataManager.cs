using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class SaveDataManager : MonoBehaviour
{
    private PuzzleManager m_puzzleManager;
    private ImageAssigner m_imageAssigner;
    private string m_puzzleName;
    private bool m_puzzlePieceDataSaved;


    private void Awake()
    {
        // If instantiating a puzzle prefab do that here rather than finding the prefab as I'm doing
        m_puzzleManager = (PuzzleManager) FindObjectOfType(typeof(PuzzleManager));
        m_imageAssigner = (ImageAssigner) FindObjectOfType(typeof(ImageAssigner));

        if (m_puzzleManager == null || m_imageAssigner == null)
            return;

        m_puzzleName = GetPuzzleName(m_puzzleManager, m_imageAssigner);

        //print($"Loading data for {m_puzzleName}");

        var puzzlePiecesData = SaveSystem.LoadPuzzlePiecesData(m_puzzleName);
        m_puzzleManager.Init(this, puzzlePiecesData);
    }


    private string GetPuzzleName(PuzzleManager puzzleManager, ImageAssigner imageAssigner)
    {
        return $"{puzzleManager.name} - {imageAssigner.ImageName}";
    }


    public void SavePuzzlePieceData(bool forceSave = false)
    {
        if (m_puzzleManager.Scattering || (m_puzzlePieceDataSaved && !forceSave))
        {
            return;
        }

        //print($"Saving data for {m_puzzleName}");

        var puzzlePiecesData = new PuzzlePiecesData(m_puzzleName, m_puzzleManager);
        SaveSystem.SavePuzzlePieceData(puzzlePiecesData);

        if (!forceSave)
            m_puzzlePieceDataSaved = true;
    }


    private void OnDestroy()
    {
        if (m_puzzleManager != null)
        {
            SavePuzzlePieceData();
        }
    }


    private void OnApplicationPause(bool pause)
    {
        if (pause && m_puzzleManager != null)
        {
            SavePuzzlePieceData();
        }
    }


    public void DeleteSaveData()
    {
        var imageAssigner = (ImageAssigner) FindObjectOfType(typeof(ImageAssigner));
        var puzzleManagers = FindObjectsOfType<PuzzleManager>();

        foreach (var puzzleManager in puzzleManagers)
        {
            string puzzleName = GetPuzzleName(puzzleManager, imageAssigner);

            print($"Deleting save data for {puzzleName}");
            SaveSystem.DeleteSaveData(puzzleName);
        }
    }
}
