using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;

public class JigsawPuzzleGeneratorSettingsManager : MonoBehaviour
{
#if UNITY_EDITOR

    [SerializeField] JigsawPuzzleGenerator m_jigsawPuzzleGenerator;
    [SerializeField] Slider m_minPiecesSlider;
    [SerializeField] Text m_minPiecesText;
    [SerializeField] Slider m_maxPiecesSlider;
    [SerializeField] Text m_maxPiecesText;
    [SerializeField] Slider m_maxNonSquarenessSlider;
    [SerializeField] Text m_maxNonSquarenessText;
    [SerializeField] Dropdown m_puzzleSizeFilterDropdown;
    [SerializeField] Dropdown m_puzzleSizeDropdown;

    [SerializeField] Slider m_seamsThicknessSlider;
    [SerializeField] Text m_seamsThicknessText;
    [SerializeField] Slider m_edgeSmoothnessSlider;
    [SerializeField] Text m_edgeSmoothnessText;

    [SerializeField] Slider m_tabSizeSlider;
    [SerializeField] Text m_tabSizeText;
    [SerializeField] Slider m_tabJitterSlider;
    [SerializeField] Text m_tabJitterText;
    [SerializeField] Slider m_seedSlider;
    [SerializeField] Text m_seedText;

    [SerializeField] Button m_rotateLeftButton;
    [SerializeField] Button m_rotateRightButton;

    private int m_previousSizeFilterIndex;

    private int m_defaultPuzzleSizeFilterIndex;
    private int m_defaultMinPieces;
    private int m_defaultMaxPieces;
    private float m_defaultMaxNonSquareness;
    private float m_defaultSeamsThickness;
    private float m_defaultEdgeSmoothness;
    private float m_defaultTabSize;
    private float m_defaultTabJitter;
    private int m_defaultSeed;


    private void Start()
    {
        var filters = Enum.GetValues(typeof(PuzzleSizeFilter)).Cast<PuzzleSizeFilter>().ToList();

        m_puzzleSizeFilterDropdown.ClearOptions();
        m_puzzleSizeFilterDropdown.AddOptions(filters.Select(t => ConvertFilterToString(t)).ToList());

        var filter = m_jigsawPuzzleGenerator.m_puzzleSizeFilter;
        m_defaultPuzzleSizeFilterIndex = filters.IndexOf(filter);

        m_defaultMinPieces = m_jigsawPuzzleGenerator.m_minPuzzlePieces;
        m_defaultMaxPieces = m_jigsawPuzzleGenerator.m_maxPuzzlePieces;
        m_defaultMaxNonSquareness = m_jigsawPuzzleGenerator.m_maxPieceNonSquareness;
        m_defaultSeamsThickness = m_jigsawPuzzleGenerator.m_seamsThicknessFrac;
        m_defaultEdgeSmoothness = m_jigsawPuzzleGenerator.m_heightMapBlurMultiplier;
        m_defaultTabSize = m_jigsawPuzzleGenerator.m_tabSizePercent;
        m_defaultTabJitter = m_jigsawPuzzleGenerator.m_tabJitterPercent;
        m_defaultSeed = m_jigsawPuzzleGenerator.m_seed;
        
        ResetToDefaults();
    }


    private string ConvertFilterToString(PuzzleSizeFilter filter)
    {
        int value = (int) filter;
        if (value == 1)
            return "Any number of pieces";

        return $"Multiples of {value} pieces";
    }


    public void ResetToDefaults()
    {
        m_puzzleSizeFilterDropdown.value = m_defaultPuzzleSizeFilterIndex;

        m_minPiecesText.text = $"Min pieces: {m_defaultMinPieces}";
        m_minPiecesSlider.value = m_defaultMinPieces;

        m_maxPiecesText.text = $"Max pieces: {m_defaultMaxPieces}";
        m_maxPiecesSlider.value = m_defaultMaxPieces;

        m_maxNonSquarenessText.text = $"Max piece non-squareness: {m_defaultMaxNonSquareness:0.##}";
        m_maxNonSquarenessSlider.value = m_defaultMaxNonSquareness;

        m_seamsThicknessText.text = $"Seams thickness: {m_defaultSeamsThickness:0.###}";
        m_seamsThicknessSlider.value = m_defaultSeamsThickness;

        m_edgeSmoothnessText.text = $"Edge smoothness: {m_defaultEdgeSmoothness:0.#}";
        m_edgeSmoothnessSlider.value = m_defaultEdgeSmoothness;

        m_tabSizeText.text = $"Tab size: {m_defaultTabSize:0}";
        m_tabSizeSlider.value = m_defaultTabSize;

        m_tabJitterText.text = $"Tab jitter: {m_defaultTabJitter:0.#}";
        m_tabJitterSlider.value = m_defaultTabJitter;

        m_seedText.text = $"Random number seed: {m_defaultSeed}";
        m_seedSlider.value = m_defaultSeed;
    }


    public void SetMinPieces(float minPieces)
    {
        m_minPiecesText.text = $"Min pieces: {minPieces}";
        m_jigsawPuzzleGenerator.m_minPuzzlePieces = (int) minPieces;
        VerifyMaxPiecesSlider();
        PopuatePuzzleSizeDropdown();
    }


    public void SetMaxPieces(float maxPieces)
    {
        m_maxPiecesText.text = $"Max pieces: {maxPieces}";
        m_jigsawPuzzleGenerator.m_maxPuzzlePieces = (int) maxPieces;
        VerifyMinPiecesSlider();
        PopuatePuzzleSizeDropdown();
    }


    private void VerifyMaxPiecesSlider()
    {
        if (m_minPiecesSlider.value > m_maxPiecesSlider.value)
            m_maxPiecesSlider.value = m_minPiecesSlider.value;
    }


    private void VerifyMinPiecesSlider()
    { 
        if (m_maxPiecesSlider.value < m_minPiecesSlider.value)
            m_minPiecesSlider.value = m_maxPiecesSlider.value;
    }


    public void SetNonSquareness(float maxNonSquareness)
    {
        maxNonSquareness = 0.01f * Mathf.Round(maxNonSquareness * 100f);

        m_maxNonSquarenessText.text = $"Max piece non-squareness: {maxNonSquareness:0.##}";
        m_jigsawPuzzleGenerator.m_maxPieceNonSquareness = maxNonSquareness;
        PopuatePuzzleSizeDropdown();
    }


    public void SetPuzzleSizeFilter(int index)
    {
        if (index == m_previousSizeFilterIndex)
            return;

        m_previousSizeFilterIndex = index;
        m_jigsawPuzzleGenerator.SetPuzzleSizeFilter(index);
        PopuatePuzzleSizeDropdown();
    }


    private void PopuatePuzzleSizeDropdown()
    {
        m_jigsawPuzzleGenerator.GenerateAvailablePuzzleSizes();

        var puzzleSizes = m_jigsawPuzzleGenerator.m_availablePuzzleSizes.Select(t => t.ToString()).ToList();

        m_puzzleSizeDropdown.ClearOptions();
        m_puzzleSizeDropdown.AddOptions(puzzleSizes);
    }


    public void SetPuzzleSize(int index)
    {
        m_jigsawPuzzleGenerator.SetPuzzleSize(index);
    }


    public void SetSeamsThickness(float seamsThickness)
    {
        seamsThickness = 0.001f * Mathf.Round(seamsThickness * 1000f);

        m_seamsThicknessText.text = $"Seams thickness: {seamsThickness:0.###}";
        m_jigsawPuzzleGenerator.m_seamsThicknessFrac = seamsThickness;
    }


    public void SetEdgeSmoothness(float edgeSmoothness)
    {
        edgeSmoothness = 0.1f * Mathf.Round(edgeSmoothness * 10f);

        m_edgeSmoothnessText.text = $"Edge smoothness: {edgeSmoothness:0.#}";
        m_jigsawPuzzleGenerator.m_heightMapBlurMultiplier = edgeSmoothness;
    }


    public void SetTabSize(float tabSize)
    {
        tabSize = Mathf.Round(tabSize);

        m_tabSizeText.text = $"Tab size: {tabSize:0}";
        m_jigsawPuzzleGenerator.m_tabSizePercent = tabSize;
    }


    public void SetTabJitter(float tabJitter)
    {
        tabJitter = 0.1f * Mathf.Round(tabJitter * 10f);

        m_tabJitterText.text = $"Tab jitter: {tabJitter:0.#}";
        m_jigsawPuzzleGenerator.m_tabJitterPercent = tabJitter;
    }


    public void SetSeed(float seed)
    {
        seed = Mathf.Round(seed);

        m_seedText.text = $"Random number seed: {seed:0}";
        m_jigsawPuzzleGenerator.m_seed = (int) seed;
    }


    public void LoadInputImage()
    {
        string path = EditorUtility.OpenFilePanel("Load image", "", "png");

        if (path.Length == 0)
            return;

        Texture2D texture = new Texture2D(512, 512);
        string[] imageNameParts = path.Split('/', '.');
        string imageName = imageNameParts[imageNameParts.Length - 2];

        var fileContent = File.ReadAllBytes(path);
        texture.LoadImage(fileContent);
        texture.name = imageName;

        m_jigsawPuzzleGenerator.m_inputImage = texture;
        m_jigsawPuzzleGenerator.GenerateSquarifiedImage();
        PopuatePuzzleSizeDropdown();

        m_rotateLeftButton.gameObject.SetActive(true);
        m_rotateRightButton.gameObject.SetActive(true);
    }


    public void GeneratePuzzle()
    {
        m_rotateLeftButton.gameObject.SetActive(false);
        m_rotateRightButton.gameObject.SetActive(false);

        m_jigsawPuzzleGenerator.GeneratePuzzle();
    }


    public void RotateImageLeft()
    {
        m_jigsawPuzzleGenerator.RotateImageLeft();
        PopuatePuzzleSizeDropdown();
    }


    public void RotateImageRight()
    {
        m_jigsawPuzzleGenerator.RotateImageRight();
        PopuatePuzzleSizeDropdown();
    }

#endif
}
