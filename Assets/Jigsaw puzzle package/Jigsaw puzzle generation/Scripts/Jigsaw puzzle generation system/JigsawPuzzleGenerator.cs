using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Audio;

public enum OutputResolution
{
    x16 = 16,
    x32 = 32,
    x64 = 64,
    x128 = 128,
    x512 = 512,
    x1024 = 1024,
    x2048 = 2048,
    x4096 = 4096
}


public enum PuzzleSizeFilter
{
    AnySize = 1,
    MultiplesOf5 = 5,
    MultiplesOf10 = 10,
    MultiplesOf20 = 20,
    MultiplesOf50 = 50,
    MultiplesOf100 = 100,
}


public class JigsawPuzzleGenerator : MonoBehaviour
{
    private enum SeamsOrDots
    {
        Seams,
        Dots
    }


    public enum PuzzleOrientation
    {
        Landscape,
        Portrait
    }


    [Tooltip("Import the input image as a sprite to retain correct dimensions")]
    public Texture2D m_inputImage;
    [SerializeField] OutputResolution m_outputResolution = OutputResolution.x4096;

    [Header("Puzzle dimensions settings")]
    [Range(4, 1000)]
    public int m_minPuzzlePieces = 20;
    [Range(4, 1000)]
    public int m_maxPuzzlePieces = 600;
    [Range(0f, 0.2f)]
    public float m_maxPieceNonSquareness = 0.05f;
    public PuzzleSizeFilter m_puzzleSizeFilter = PuzzleSizeFilter.MultiplesOf10;

    [Header("Puzzle seams settings")]
    [Range(0.005f, 0.025f)]
    public float m_seamsThicknessFrac = 0.01f;
    [Range(0, 10)]
    public float m_heightMapBlurMultiplier = 5;
    [Range(1, 3)]
    [SerializeField] int m_seamsSmoothHalfWindow = 1; 
    
    [Header("Puzzle piece tabs settings")]
    [Range(15f, 25f)]
    public float m_tabSizePercent = 20f;
    [Range(0f, 4f)]
    public float m_tabJitterPercent = 4f;
    [Range(1, 1000)]
    public int m_seed = 1;

    [Header("Prefab assets folders")]
    [Tooltip("The root folder for all the prefab assets sub-folders (exclude Assets/ and the / at the end)")]
    public string m_puzzlePrefabAssetsRootFolder = "Jigsaw puzzle package/Jigsaw puzzle system/Prefab assets";
    [Tooltip("The sub-folder for all the seams textures used by the puzzle model materials")]
    public string m_puzzleModelSeamsTexturesFolder = "Seams textures";
    [Tooltip("The sub-folder for all the normal map textures used by the puzzle model materials")]
    public string m_puzzleModelNormalMapsFolder = "Normal maps";
    [Tooltip("The sub-folder for all the materials applied to the jigsaw puzzle models")]
    public string m_puzzleModelMaterialsFolder = "Materials";
    [Tooltip("The sub-folder for all the jigsaw puzzle model prefabs themselves")]
    public string m_puzzleModelPrefabsFolder = "Prefabs";
    [Tooltip("The sub-folder for all the squarified images")]
    public string m_squarifiedImagesFolder = "Squarified images";

    [Header("Materials")]
    [SerializeField] Material m_testSquarifiedImageMaterial;
    [SerializeField] Material m_testPuzzleImageMaterial;
    [SerializeField] Material m_ghostMaterial;

    [Header("Compute shaders")]
    [Tooltip("Only use Dots for checking the placement of the generated vertices")]
    [SerializeField] SeamsOrDots m_seamsOrDots = SeamsOrDots.Seams;
    [SerializeField] ComputeShader m_seamsShader;
    [SerializeField] ComputeShader m_dotsShader;
    [SerializeField] ComputeShader m_blurShader;
    [SerializeField] ComputeShader m_normalFromHeightShader;

    [Header("Frame")]
    [SerializeField] GameObject m_framePrefab;
    [SerializeField] Vector2 m_frameBufferSize = new Vector2(0.11f, 0.11f);

    [Header("Completion particle effect")]
    [SerializeField] ParticleSystem m_completionParticleSystem;

    [Header("Jigsaw manager behaviour settings")]
    [SerializeField] BehaviourSettings m_behaviourSettings;

    [Header("Audio settings")]
    [SerializeField] SoundEffects m_soundEffects;
    [SerializeField] AudioMixerGroup m_sfxMixer;

    [Header("Debug properties - for inspection purposes only")]
    public GameObject m_squarifiedImageQuad;
    public PuzzleOrientation m_puzzleOrientation;
    public float m_imageAspectRatio;
    public Vector2 m_imageDimensions;
    public PuzzleSize m_puzzleSize;
    public List<PuzzleSize> m_availablePuzzleSizes = new List<PuzzleSize>();

    private GameObject m_puzzleMeshesGameObject;
    private float m_puzzleScale = 6f;
    private Texture m_smoothedSeamsTexture;
    private Texture m_normalMapTexture;
    private Texture2D m_squarifiedImage;


    private void Awake()
    {
        GenerateSquarifiedImage();
        GenerateAvailablePuzzleSizes();
    }


    public void GenerateSquarifiedImage()
    {
        m_squarifiedImage = ImageSquarifier.SquarifyImage(m_inputImage);

        ActivateSquarifiedImageQuad(true);

        if (m_puzzleMeshesGameObject != null)
            Destroy(m_puzzleMeshesGameObject);
    }


    public void RotateImageLeft()
    {
        m_inputImage = RotateTexture(m_inputImage, false);
        GenerateSquarifiedImage();
    }


    public void RotateImageRight()
    {
        m_inputImage = RotateTexture(m_inputImage, true);
        GenerateSquarifiedImage();
    }


    private Texture2D RotateTexture(Texture2D originalTexture, bool clockwise)
    {
        Color32[] original = originalTexture.GetPixels32();
        Color32[] rotated = new Color32[original.Length];
        int w = originalTexture.width;
        int h = originalTexture.height;

        int iRotated, iOriginal;

        for (int j = 0; j < h; ++j)
        {
            for (int i = 0; i < w; ++i)
            {
                iRotated = (i + 1) * h - j - 1;
                iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
                rotated[iRotated] = original[iOriginal];
            }
        }

        Texture2D rotatedTexture = new Texture2D(h, w);
        rotatedTexture.name = originalTexture.name;
        rotatedTexture.SetPixels32(rotated);
        rotatedTexture.Apply();
        return rotatedTexture;
    }


    public void GenerateAvailablePuzzleSizes()
    {
        int imageWidth = m_inputImage.width;
        int imageHeight = m_inputImage.height;

        m_imageDimensions = new Vector2(imageWidth, imageHeight);

        m_imageAspectRatio = (float) Mathf.Max(imageWidth, imageHeight) / Mathf.Min(imageWidth, imageHeight);
        bool increment = true;

        // Square images are counted as Landscape, for simplicity
        m_puzzleOrientation = imageWidth >= imageHeight
            ? PuzzleOrientation.Landscape
            : PuzzleOrientation.Portrait;

        m_availablePuzzleSizes = new List<PuzzleSize>();

        int horizontalPieces;
        int verticalPieces;

        switch (m_puzzleOrientation)
        {
            case PuzzleOrientation.Landscape:
                verticalPieces = 2;

                while (increment)
                {
                    float horizontalPiecesFloat = verticalPieces * m_imageAspectRatio;
                    int minHorizontalPieces = Mathf.FloorToInt(horizontalPiecesFloat / (1f + m_maxPieceNonSquareness));
                    int maxHorizontalPieces = Mathf.CeilToInt(horizontalPiecesFloat * (1f + m_maxPieceNonSquareness));

                    bool incrementAny = false;
                    int sizesChecked = 0;
                    for (horizontalPieces = minHorizontalPieces; horizontalPieces <= maxHorizontalPieces; horizontalPieces++)
                    {
                        float verticalPieceSize = (float) imageHeight / verticalPieces;
                        float horizontalPieceSize = (float) imageWidth / horizontalPieces;

                        float nonSquareness = Mathf.Max(horizontalPieceSize, verticalPieceSize) / Mathf.Min(horizontalPieceSize, verticalPieceSize) - 1f;

                        if (nonSquareness <= m_maxPieceNonSquareness)
                        {
                            bool incrementThis = AddPuzzleSizeAndIncrement(horizontalPieces, verticalPieces, nonSquareness);
                            incrementAny = incrementAny || incrementThis;
                            sizesChecked++;
                        }
                    }

                    if (incrementAny || sizesChecked == 0)
                        verticalPieces++;
                    else
                        increment = false;
                }
                break;

            case PuzzleOrientation.Portrait:
                horizontalPieces = 2;

                while (increment)
                {
                    float verticalPiecesFloat = horizontalPieces * m_imageAspectRatio;
                    int minVerticalPieces = Mathf.FloorToInt(verticalPiecesFloat / (1f + m_maxPieceNonSquareness));
                    int maxVerticalPieces = Mathf.CeilToInt(verticalPiecesFloat * (1f + m_maxPieceNonSquareness));

                    bool incrementAny = false;
                    int sizesChecked = 0;
                    for (verticalPieces = minVerticalPieces; verticalPieces <= maxVerticalPieces; verticalPieces++)
                    {
                        float verticalPieceSize = (float) imageHeight / verticalPieces;
                        float horizontalPieceSize = (float) imageWidth / horizontalPieces;

                        float nonSquareness = Mathf.Max(horizontalPieceSize, verticalPieceSize) / Mathf.Min(horizontalPieceSize, verticalPieceSize) - 1f;

                        if (nonSquareness <= m_maxPieceNonSquareness)
                        {
                            bool incrementThis = AddPuzzleSizeAndIncrement(horizontalPieces, verticalPieces, nonSquareness);
                            incrementAny = incrementAny || incrementThis;
                            sizesChecked++;
                        }
                    }

                    if (incrementAny || sizesChecked == 0)
                        horizontalPieces++;
                    else
                        increment = false;
                }
                break;
        }

        m_availablePuzzleSizes.Sort((t1, t2) => t1.Size.CompareTo(t2.Size));

        if (m_availablePuzzleSizes.Count > 0)
            m_puzzleSize = m_availablePuzzleSizes[0];
    }


    public void SetPuzzleSize(int index)
    {
        m_puzzleSize = m_availablePuzzleSizes[index];
    }


    public void SetPuzzleSizeFilter(int index)
    {
        var filters = Enum.GetValues(typeof(PuzzleSizeFilter)).Cast<PuzzleSizeFilter>().ToList();

        int previousIndex = filters.IndexOf(m_puzzleSizeFilter);

        if (previousIndex == index)
            return;

        m_puzzleSizeFilter = filters[index];
    }


    private bool AddPuzzleSizeAndIncrement(int horizontalPieces, int verticalPieces, float nonSquareness)
    {
        bool increment = true;
        int numberOfPieces = verticalPieces * horizontalPieces;

        if (numberOfPieces >= m_minPuzzlePieces && numberOfPieces <= m_maxPuzzlePieces)
        {
            var puzzleSize = new PuzzleSize(horizontalPieces, verticalPieces, nonSquareness);
            bool allowedMultiple = puzzleSize.Size % (int) m_puzzleSizeFilter == 0;

            if (allowedMultiple && !m_availablePuzzleSizes.Contains(puzzleSize))
            {
                m_availablePuzzleSizes.Add(puzzleSize);
            }
        }
        else if (numberOfPieces > m_maxPuzzlePieces)
            increment = false;

        return increment;
    }


    private void ActivateSquarifiedImageQuad(bool active)
    {
        if (m_squarifiedImageQuad != null)
            m_squarifiedImageQuad.SetActive(active);

        if (active)
            m_testSquarifiedImageMaterial.SetTexture("_MainTex", m_squarifiedImage);
    }


    public void GeneratePuzzle()
    {
        ActivateSquarifiedImageQuad(false);

        var vertices = GetSeamsVertices(out List<List<Vector2>> puzzlePieceUvs);
        //var vertices = GetTestVertices();

        float seamsThickness = GetSeamsThicknessInPixels();
        var computeShader = m_seamsOrDots == SeamsOrDots.Seams ? m_seamsShader : m_dotsShader;
        var seamsTexture = SeamsTextureCreator.GenerateSeamsTexture((int) m_outputResolution, seamsThickness, vertices, computeShader);
 
        var kernel = GetFlatKernel(m_seamsSmoothHalfWindow);
        m_smoothedSeamsTexture = TextureBlurrer.GetBlurredTexture(seamsTexture, m_seamsSmoothHalfWindow, kernel, m_blurShader);

        int heightMapBlurHalfWindow = Mathf.CeilToInt(seamsThickness * m_heightMapBlurMultiplier);

        kernel = GetExponentialKernel(heightMapBlurHalfWindow);
        var heightMapTexture = TextureBlurrer.GetBlurredTexture(seamsTexture, heightMapBlurHalfWindow, kernel, m_blurShader);

        m_normalMapTexture = NormalMapGenerator.GenerateNormalMap(heightMapTexture, m_normalFromHeightShader);

        if (m_testPuzzleImageMaterial != null)
        {
            m_testPuzzleImageMaterial.SetTexture("_Albedo", m_squarifiedImage);
            m_testPuzzleImageMaterial.SetTexture("_Shadows", m_smoothedSeamsTexture);
            m_testPuzzleImageMaterial.SetTexture("_Normal", m_normalMapTexture);
        }

        if (m_ghostMaterial != null)
            m_ghostMaterial.SetTexture("_MainTex", m_squarifiedImage);

        var maxUvs = m_puzzleOrientation == PuzzleOrientation.Landscape
            ? new Vector2(1f, 1f / m_imageAspectRatio)
            : new Vector2(1f / m_imageAspectRatio, 1f);

        if (m_puzzleMeshesGameObject != null)
            Destroy(m_puzzleMeshesGameObject);

        string orientation = Mathf.Abs(m_imageAspectRatio - 1f) < 1e-3 ? "Square" : m_puzzleOrientation.ToString();
        string puzzleName = $"{orientation} puzzle - {m_puzzleSize.ToString()} - seed {m_seed} - {m_imageAspectRatio:0.0##} aspect ratio";

        // An image with 1.5 aspect ratio should have a scale of 6
        // A square image will have a scale of ~4.9
        // All resulting puzzles have the same area of 24
        m_puzzleScale = Mathf.Sqrt(m_imageAspectRatio * 24);    

        m_puzzleMeshesGameObject = MeshGenerator.GeneratePuzzlePieceMeshes(puzzlePieceUvs,
            maxUvs, m_testPuzzleImageMaterial, m_ghostMaterial, puzzleName, m_puzzleScale);

        print($"Number of vertices: {vertices.Count}");
    }


    public void SavePrefab()
    {
#if UNITY_EDITOR
        if (m_puzzleMeshesGameObject == null)
            return;

        string prefabFolder = "Assets/" + m_puzzlePrefabAssetsRootFolder + "/" + m_puzzleModelPrefabsFolder + "/";
        string prefabPath = prefabFolder + m_puzzleMeshesGameObject.name + ".prefab";

        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        string seamsTexturesFolder = m_puzzlePrefabAssetsRootFolder + "/" + m_puzzleModelSeamsTexturesFolder + "/";
        string normalMapsFolder = m_puzzlePrefabAssetsRootFolder + "/" + m_puzzleModelNormalMapsFolder + "/";

        var seamsTexture2D = new Texture2D(m_smoothedSeamsTexture.width, m_smoothedSeamsTexture.height, TextureFormat.ARGB32, false);
        RenderTexture.active = (RenderTexture) m_smoothedSeamsTexture;
        seamsTexture2D.ReadPixels(new Rect(0, 0, m_smoothedSeamsTexture.width, m_smoothedSeamsTexture.height), 0, 0);
        seamsTexture2D.Apply();
        var seamsTextureBytes = ImageConversion.EncodeToPNG(seamsTexture2D);

        string seamsTextureName = m_puzzleMeshesGameObject.name + " - seams texture.png";

        File.WriteAllBytes(Application.dataPath + "/" + seamsTexturesFolder + seamsTextureName, seamsTextureBytes);

        var normalMapTexture2D = new Texture2D(m_normalMapTexture.width, m_normalMapTexture.height, TextureFormat.ARGB32, false);
        RenderTexture.active = (RenderTexture) m_normalMapTexture;
        normalMapTexture2D.ReadPixels(new Rect(0, 0, m_normalMapTexture.width, m_normalMapTexture.height), 0, 0);
        normalMapTexture2D.Apply();
        var normalMapTextureBytes = ImageConversion.EncodeToPNG(normalMapTexture2D);

        string normalMapTextureName = m_puzzleMeshesGameObject.name + " - normal map.png";

        File.WriteAllBytes(Application.dataPath + "/" + normalMapsFolder + normalMapTextureName, normalMapTextureBytes);

        var puzzleMaterial = new Material(m_testPuzzleImageMaterial);

        string seamsAssetPath = "Assets/" + seamsTexturesFolder + seamsTextureName;
        AssetDatabase.ImportAsset(seamsAssetPath);
        var seamsTexture = (Texture2D) AssetDatabase.LoadMainAssetAtPath(seamsAssetPath);

        string normalMapAssetPath = "Assets/" + normalMapsFolder + normalMapTextureName;
        AssetDatabase.ImportAsset(normalMapAssetPath);
        var normalMapTexture = (Texture2D) AssetDatabase.LoadMainAssetAtPath(normalMapAssetPath);

        puzzleMaterial.SetTexture("_Shadows", seamsTexture);
        puzzleMaterial.SetTexture("_Normal", normalMapTexture);
        puzzleMaterial.SetFloat("_Shadow_strength", 1f);
        puzzleMaterial.SetFloat("_Normal_strength", 1f);

        string materialName = m_puzzleMeshesGameObject.name + " - material.mat";
        string materialsFolder = "Assets/" + m_puzzlePrefabAssetsRootFolder + "/" + m_puzzleModelMaterialsFolder + "/";

        AssetDatabase.CreateAsset(puzzleMaterial, materialsFolder + materialName);
        AssetDatabase.SaveAssets();

        BuildCompletePuzzleGameObject(m_puzzleMeshesGameObject, puzzleMaterial);
     
        var puzzlePrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(m_puzzleMeshesGameObject, prefabPath, InteractionMode.UserAction);   

        for (int i = 0; i < m_puzzleMeshesGameObject.transform.childCount; i++)
        {
            var childTransform = m_puzzleMeshesGameObject.transform.GetChild(i);
            var meshFilter = childTransform.GetComponentInChildren<MeshFilter>();
            
            if (meshFilter != null)
            {
                var mesh = meshFilter.mesh;
                AssetDatabase.AddObjectToAsset(mesh, prefabPath);

                var child = puzzlePrefab.transform.GetChild(i);
                var childMeshFilter = child.GetComponentInChildren<MeshFilter>();
                var childMeshCollider = child.GetComponentInChildren<MeshCollider>();

                if (childMeshFilter != null)
                    childMeshFilter.sharedMesh = mesh;

                if (childMeshCollider != null)
                    childMeshCollider.sharedMesh = mesh;
            }
        }

        AssetDatabase.SaveAssets();

        print($"The following assets were saved for [{puzzlePrefab.name}]:");
        print($"Puzzle prefab was saved to [{prefabFolder}]");
        print($"Puzzle textures were saved to [{"Assets/" + seamsTexturesFolder}]");
        print($"Puzzle material was saved to [{materialsFolder}]");

        Destroy(m_puzzleMeshesGameObject);

        ActivateSquarifiedImageQuad(true);
#endif
    }


    private void BuildCompletePuzzleGameObject(GameObject puzzleMeshesGameObject, Material puzzleMaterial)
    {
#if UNITY_EDITOR
        var rootTemp = new GameObject(puzzleMeshesGameObject.name + " - root temp");

        var ghostTransform = puzzleMeshesGameObject.transform.GetChild(0);
        
        ghostTransform.position = new Vector3(0f, -0.01f, 0f);
        ghostTransform.parent = rootTemp.transform;

        while (puzzleMeshesGameObject.transform.childCount > 0)
        {
            var child = puzzleMeshesGameObject.transform.GetChild(0);

            var renderer = child.GetComponent<MeshRenderer>();
            renderer.material = puzzleMaterial;
            Bounds bounds = renderer.bounds;

            var position = bounds.center;
            var anchor = new GameObject(child.name + " - anchor");

            anchor.transform.SetPositionAndRotation(position, Quaternion.identity);

            anchor.transform.parent = rootTemp.transform;
            child.parent = anchor.transform;

            child.gameObject.AddComponent<PuzzlePieceController>();
        }

        while (rootTemp.transform.childCount > 0)
        {
            var child = rootTemp.transform.GetChild(0);
            child.parent = puzzleMeshesGameObject.transform;
        }

        Destroy(rootTemp.gameObject);

        var audio = puzzleMeshesGameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.outputAudioMixerGroup = m_sfxMixer;

        var frameInstance = Instantiate(m_framePrefab);
        frameInstance.transform.parent = puzzleMeshesGameObject.transform;
        frameInstance.transform.SetAsFirstSibling();

        var spriteRenderer = frameInstance.GetComponent<SpriteRenderer>();

        float xScale = m_puzzleOrientation == PuzzleOrientation.Landscape
            ? m_puzzleScale
            : m_puzzleScale / m_imageAspectRatio;

        float yScale = m_puzzleOrientation == PuzzleOrientation.Portrait
            ? m_puzzleScale
            : m_puzzleScale / m_imageAspectRatio;

        spriteRenderer.size = new Vector2(xScale, yScale) + m_frameBufferSize;

        var puzzleManager = puzzleMeshesGameObject.AddComponent<PuzzleManager>();
        puzzleManager.m_behaviourSettings = m_behaviourSettings;
        puzzleManager.m_soundEffects = m_soundEffects;
        puzzleManager.m_puzzleMaterial = puzzleMaterial;
        puzzleManager.m_ghostMaterial = m_ghostMaterial;
        puzzleManager.m_particleSystem = m_completionParticleSystem;
        puzzleManager.m_pieceDiameter = m_puzzleOrientation == PuzzleOrientation.Landscape
            ? m_puzzleScale / m_puzzleSize.X
            : m_puzzleScale / m_puzzleSize.Y;

        puzzleManager.m_centreExclusions = new Bounds[]
        {
            new Bounds(Vector3.zero, new Vector3(xScale, 0, yScale))
        };

        puzzleManager.m_scatter = true;
#endif
    }


    public void SaveSquarifiedImage()
    {
#if UNITY_EDITOR
        if (m_squarifiedImage == null)
            return;

        string squarifiedImageName = "Squarified " + m_inputImage.name + ".png";

        string imagesFolder = m_puzzlePrefabAssetsRootFolder + "/" + m_squarifiedImagesFolder + "/";

        var squarifiedImageBytes = ImageConversion.EncodeToPNG(m_squarifiedImage);

        File.WriteAllBytes(Application.dataPath + "/" + imagesFolder + squarifiedImageName, squarifiedImageBytes);

        string imageAssetPath = "Assets/" + imagesFolder + squarifiedImageName;
        AssetDatabase.ImportAsset(imageAssetPath);

        print($"Squarified image [{squarifiedImageName}] was saved to [{"Assets/" + imagesFolder}]");
#endif
    }


    private float GetSeamsThicknessInPixels()
    {
        float maxPieces = Mathf.Max(m_puzzleSize.X, m_puzzleSize.Y);
        float pixelsPerPiece = (int) m_outputResolution / maxPieces;
        float seamsThickness = pixelsPerPiece * m_seamsThicknessFrac;

        return seamsThickness;
    }


    private List<Vector3> GetSeamsVertices(out List<List<Vector2>> puzzlePieceUvs)
    {
        int sizePerPiece = 50;
        int xPieces = m_puzzleSize.X;
        int yPieces = m_puzzleSize.Y;

        int width = xPieces * sizePerPiece;
        int height = yPieces * sizePerPiece;

        var jigsawSvgGenerator = new JigsawSvgGenerator();
        string svgPath = jigsawSvgGenerator.GenerateJigsawSvg(m_seed, m_tabSizePercent, m_tabJitterPercent, xPieces, yPieces, width, height);

        Vector2 scale = Vector2.one;

        switch (m_puzzleOrientation)
        {
            case PuzzleOrientation.Landscape:
                scale = new Vector2(1f / width, 1f / (height * m_imageAspectRatio));
                break;

            case PuzzleOrientation.Portrait:
                scale = new Vector2(1f / (m_imageAspectRatio * width), 1f / height);
                break;
        }
        
        var vertexGenerator = new VertexGenerator();
        var vertices = vertexGenerator.GeneratePuzzleVertices(svgPath, sizePerPiece, m_puzzleSize.Dimensions, scale, out puzzlePieceUvs);

        return vertices;
    }


    private float[] GetExponentialKernel(int halfWindow)
    {
        int window = halfWindow * 2 + 1;
        var kernel = new float[window];

        float sum = 0f;
        for (int i = 0; i <= halfWindow; i++)
        {
            float value = Mathf.Exp(-i * 5f / halfWindow);
            kernel[halfWindow + i] = value;
            kernel[halfWindow - i] = value;
        }

        for (int i = 0; i < window; i++)
            sum += kernel[i];

        for (int i = 0; i < window; i++)
            kernel[i] /= sum;

        return kernel;
    }


    private float[] GetFlatKernel(int halfWindow)
    {
        int window = halfWindow * 2 + 1;
        var kernel = new float[window];

        float value = 1f / window;

        for (int i = 0; i < window; i++)
            kernel[i] = value;

        return kernel;
    }
}
