using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonClickManager : MonoBehaviour
{
    [SerializeField] Button m_resetButton;
    [SerializeField] Button m_attractPiecesButton;
    [SerializeField] Button m_toggleGhostImageButton;
    [SerializeField] Button m_toggleNonEdgePiecesButton;
    [SerializeField] CheatCode m_completePuzzleCheatCode;

    private PuzzleManager m_puzzleManager;


    private void FindPuzzleManager()
    {
        if (m_puzzleManager == null)
            m_puzzleManager = (PuzzleManager) FindObjectOfType(typeof(PuzzleManager));
    }


    public void ResetPuzzle()
    {
        FindPuzzleManager();
        m_puzzleManager.ResetPuzzle();
    }


    public void ToggleNonEdgePieces()
    {
        FindPuzzleManager();
        m_puzzleManager.ToggleNonEdgePieces();
    }


    public void AttractPieces()
    {
        FindPuzzleManager();
        m_puzzleManager.AttractPieces();
    }


    public void ToggleGhostImage()
    {
        FindPuzzleManager();

        for (int i = 0; i < m_puzzleManager.transform.childCount; i++)
        {
            var child = m_puzzleManager.transform.GetChild(i);

            if (child.name.ToLower().Contains("ghost"))
            {
                child.gameObject.SetActive(!child.gameObject.activeSelf);
                return;
            }
        }
    }


    public void AutoCompletePuzzle()
    {
        FindPuzzleManager();
        m_puzzleManager.AutoCompletePuzzle();
    }


    public void AutoFitOnePieceAtMousePosition()
    {
        FindPuzzleManager();
        m_puzzleManager.AutoFitOnePieceAtMousePosition();
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
            Quit();
    }


    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    private void TurnOffResetButton()
    {
        m_resetButton.gameObject.SetActive(false);
    }


    private void TurnOnResetButton()
    {
        m_resetButton.gameObject.SetActive(true);
    }


    private void TurnOffToggleNonEdgePiecesButton()
    {
        m_toggleNonEdgePiecesButton.gameObject.SetActive(false);
    }


    private void TurnOnToggleNonEdgePiecesButton()
    {
        m_toggleNonEdgePiecesButton.gameObject.SetActive(true);
    }


    private void ResetCompleteCheatCode()
    {
        m_completePuzzleCheatCode.Reset();
    }


    private void OnEnable()
    {
        PuzzleManager.PuzzleScatterStartEvent.AddListener(TurnOffResetButton);
        PuzzleManager.PuzzleScatterCompleteEvent.AddListener(TurnOnResetButton);
        PuzzleManager.FinalPieceInPositionEvent.AddListener(TurnOffResetButton);
        PuzzleManager.PuzzleCompletionEffectFinishedEvent.AddListener(TurnOnResetButton);
        PuzzleManager.FinalEdgePieceInPositionEvent.AddListener(TurnOffToggleNonEdgePiecesButton);
        PuzzleManager.RestartPuzzleEvent.AddListener(TurnOnToggleNonEdgePiecesButton);
        PuzzleManager.RestartPuzzleEvent.AddListener(ResetCompleteCheatCode);
    }


    private void OnDisable()
    {
        PuzzleManager.PuzzleScatterStartEvent.RemoveListener(TurnOffResetButton);
        PuzzleManager.PuzzleScatterCompleteEvent.RemoveListener(TurnOnResetButton);
        PuzzleManager.FinalPieceInPositionEvent.RemoveListener(TurnOffResetButton);
        PuzzleManager.PuzzleCompletionEffectFinishedEvent.RemoveListener(TurnOnResetButton);
        PuzzleManager.FinalEdgePieceInPositionEvent.RemoveListener(TurnOffToggleNonEdgePiecesButton);
        PuzzleManager.RestartPuzzleEvent.RemoveListener(TurnOnToggleNonEdgePiecesButton);
        PuzzleManager.RestartPuzzleEvent.RemoveListener(ResetCompleteCheatCode);
    }
}
