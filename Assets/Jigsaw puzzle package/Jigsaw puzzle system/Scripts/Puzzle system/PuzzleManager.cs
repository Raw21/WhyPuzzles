using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class PuzzleManager : MonoBehaviour
{
    // Replace events these with whatever event system you prefer.
    // Look for where these are invoked below by seraching for .Invoke()
    public static UnityEvent RestartPuzzleEvent = new UnityEvent();
    public static UnityEvent PuzzleScatterStartEvent = new UnityEvent();
    public static UnityEvent PuzzleScatterCompleteEvent = new UnityEvent();
    public static UnityEvent FinalPieceInPositionEvent = new UnityEvent();
    public static UnityEvent FinalEdgePieceInPositionEvent = new UnityEvent();
    public static UnityEvent PuzzleCompletionEffectFinishedEvent = new UnityEvent();
    public static UnityEvent PuzzlePieceInPositionEvent = new UnityEvent();

    [Header("Settings")]
    public BehaviourSettings m_behaviourSettings;
    public SoundEffects m_soundEffects;

    [Header("Materials")]
    public Material m_puzzleMaterial;
    public Material m_ghostMaterial;

    [Header("Particle system")]
    public ParticleSystem m_particleSystem;

    [Header("Scattering settings")]
    [Tooltip("Set to true if you want the puzzle pieces to automatically scatter when starting a new puzzle, " +
        "set to false if you want to arrange the pieces in some other way when starting a new puzzle " +
        "(part completed puzzles will be reloaded in the state they were left in regardless of this setting)")]
    public bool m_scatter = true;
    [Tooltip("The number of child objects to ignore when finding the puzzle pieces to scatter (e.g. the frame and ghost image objects). " +
        "If you don't have a frame then set this to 1 so only the ghost image is ignored.")]
    [SerializeField] int m_childrenToIgnore = 2;
    [Tooltip("The average size of the puzzle pieces")]
    public float m_pieceDiameter = 0.5f;
    [Tooltip("The proportion of the puzzle piece size to add on to the distance between pieces when scattering")]
    [SerializeField] float m_jitterRatio = 0.05f;
    [Tooltip("The number of attemts when generating scatter locations (should never need more than 1 attempt for the square sampling)")]
    [SerializeField] int m_scatterGenerationAttempts = 20;
    [Tooltip("How high the pieces should hover off of the background when scattered as a proprtion of the piece size")]
    [SerializeField] float m_hoverHeightRatio = 0.05f;
    [Tooltip("The extra height added to the pieces when being picked up as a proportion of the piece size")]
    [SerializeField] float m_pickUpHeightRatio = 0.05f;
    [Tooltip("A cap on the height the pieces hover at in case the hover height ratio causes the hover height to be too large")]
    [SerializeField] float m_minHoverHeight = 0.025f;
    [Tooltip("A cap on the extra hight added to the pieces when being picked up in case the pick up height ratio causes it to be too large")]
    [SerializeField] float m_minPickUpHeight = 0.025f;

    [Header("Scatter exclusion zones")]
    [Tooltip("Use these to prevent puzzle pieces being scattered into the corners of the screen where UI elements might be placed")]
    [SerializeField] Vector2 m_topLeftExclusion = new Vector2(0.05f, 0.05f);
    [Tooltip("Use these to prevent puzzle pieces being scattered into the corners of the screen where UI elements might be placed")]
    [SerializeField] Vector2 m_topRightExclusion = new Vector2(0.05f, 0.05f);
    [Tooltip("Use these to prevent puzzle pieces being scattered into the corners of the screen where UI elements might be placed")]
    [SerializeField] Vector2 m_bottomLeftExclusion = new Vector2(0.05f, 0.05f);
    [Tooltip("Use these to prevent puzzle pieces being scattered into the corners of the screen where UI elements might be placed")]
    [SerializeField] Vector2 m_bottomRightExclusion = new Vector2(0.05f, 0.05f);

    [Tooltip("Use this to prevent puzzle pieces being scattered into the puzzle frame itself")]
    public Bounds[] m_centreExclusions = new Bounds[] 
    {
        new Bounds(Vector3.zero, new Vector3(3f, 0, 3f))
    };

    [HideInInspector]
    public List<Transform> m_puzzlePieceAnchors;

    public int m_numberOfScatterPoints;
    private Camera m_camera;
    private float m_cameraSize;
    private Vector3 m_cameraPosition;
    private AudioSource m_audio;
    private List<Vector3> m_possibleScatterPoints;
    private int m_numberOfPiecesInPuzzle;
    private int m_numberOfPiecesInFinalPosition;
    private int m_numberOfEdgePiecesInPuzzle;
    private int m_numberOfEdgePiecesInFinalPosition;
    private PuzzlePieceController[] m_puzzlePieces;
    private bool m_initialised;
    private GameObject m_renderTextureQuad;
    private float m_hoverHeight;
    private float m_pickUpHeight;
    private SaveDataManager m_saveDataManager;
    private bool m_showOnlyEdgePieces;
    private Vector2 m_puzzleSize;
    private float m_maxEffectRadius;

    private float m_startEffectRadius = 0;

    private float m_normalStrength;
    private float m_shadowStrength;
    private float m_autoSaveInterval = 30f;

    private int m_effectCentreId;
    private int m_effectRadiusId;

    private bool m_canRaisePuzzlePieceInPositionEvent;

    public bool Scattering { get; private set; }


    public void AssignImageTexture(Texture imageTexture)
    {
        m_puzzleMaterial.SetTexture("_Albedo", imageTexture);
        m_ghostMaterial.SetTexture("_MainTex", imageTexture);
    }


    public void Init(SaveDataManager saveDataManager = null, PuzzlePiecesData puzzlePiecesData = null)
    {
        m_effectCentreId = Shader.PropertyToID("_Effect_centre");
        m_effectRadiusId = Shader.PropertyToID("_Effect_radius");

        m_puzzleMaterial.SetFloat(m_effectRadiusId, m_startEffectRadius);

        m_normalStrength = m_puzzleMaterial.GetFloat("_Normal_strength");
        m_shadowStrength = m_puzzleMaterial.GetFloat("_Shadow_strength");

        m_camera = Camera.main;
        m_cameraSize = m_camera.orthographicSize;
        m_cameraPosition = m_camera.transform.position;
        m_puzzlePieces = GetComponentsInChildren<PuzzlePieceController>();
        m_audio = GetComponent<AudioSource>();

        m_hoverHeight = Mathf.Max(m_minHoverHeight, m_pieceDiameter * m_hoverHeightRatio);
        m_pickUpHeight = Mathf.Max(m_minPickUpHeight, m_pieceDiameter * m_pickUpHeightRatio);

        m_numberOfEdgePiecesInPuzzle = 0;

        foreach (var puzzlePiece in m_puzzlePieces)
        {
            puzzlePiece.Init();
            puzzlePiece.m_behaviourSettings = m_behaviourSettings;
            puzzlePiece.m_audio = m_audio;
            puzzlePiece.m_soundEffects = m_soundEffects;
            puzzlePiece.m_puzzleManager = this;
            puzzlePiece.m_hoverHeight = m_hoverHeight;
            puzzlePiece.m_pickUpHeight = m_pickUpHeight + m_pickUpHeight;
            puzzlePiece.SetMaterial(m_puzzleMaterial);
        }

        m_saveDataManager = saveDataManager;
        FindPuzzlePieceAnchors(puzzlePiecesData);
        GetPossibleScatterPoints();

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);

            if (child.name.ToLower().Contains("ghost"))
            {
                var renderer = child.GetComponent<MeshRenderer>();
                var bounds = renderer.bounds;
                m_puzzleSize = new Vector2(bounds.extents.x * 2f, bounds.extents.z * 2f);
                m_maxEffectRadius = m_puzzleSize.magnitude * 1.1f;
                break;
            }
        }

        m_initialised = true;
    }


    private void Start()
    {
        if (!m_initialised)
            Init();

        if (m_scatter)
            StartCoroutine(DelayScatter());
        else
            SetPuzzlePieceInputs(true);

        StartCoroutine(AutoSaver());
    }


    private void SetPuzzlePieceInputs(bool acceptInputs)
    {
        foreach (var piece in m_puzzlePieces)
            piece.SetInputs(acceptInputs);
    }


    private IEnumerator AutoSaver()
    {
        while (true)
        {
            yield return new WaitForSeconds(m_autoSaveInterval);

            if (m_saveDataManager != null)
                m_saveDataManager.SavePuzzlePieceData(true);
        }
    }


    private Bounds CalculateScatterArea()
    {
        var camera = m_camera != null ? m_camera : Camera.main;

        float sizeZ = m_cameraSize > 0 ? m_cameraSize * 2 : camera.orthographicSize * 2;
        float sizeX = sizeZ * camera.aspect;
        var centre = m_cameraSize > 0 ? m_cameraPosition : camera.transform.position;
        centre.y = 0;
        float pieceSize = m_pieceDiameter * (1f + 2f * m_jitterRatio);
        var scatterArea = new Bounds(centre, new Vector3(sizeX - pieceSize, 0f, sizeZ - pieceSize));

        return scatterArea;
    }


    public void ToggleNonEdgePieces()
    {
        if (!gameObject.activeSelf)
            return;

        foreach (var piece in m_puzzlePieces)
        {
            if (piece.IsEdgePiece || piece.m_partOfCluster || piece.m_inFinalPosition)
                continue;

            piece.gameObject.SetActive(!piece.gameObject.activeSelf);
        }

        m_showOnlyEdgePieces = !m_showOnlyEdgePieces;
    }


    public void TurnOnAllPieces()
    {
        foreach (var piece in m_puzzlePieces)
            piece.gameObject.SetActive(true);

        m_showOnlyEdgePieces = false;
    }


    public void FindAllAdjacentPieces()
    {
        var puzzlePieces = GetComponentsInChildren<PuzzlePieceController>();

        m_numberOfEdgePiecesInPuzzle = 0;

        foreach (var piece in puzzlePieces)
        {
            piece.FindAdjacentPieces();
            if (piece.IsEdgePiece)
                m_numberOfEdgePiecesInPuzzle++;
        }
    }


    private void FindPuzzlePieceAnchors(PuzzlePiecesData puzzlePiecesData = null)
    {
        m_puzzlePieceAnchors = new List<Transform>();
        for (int i = m_childrenToIgnore; i < transform.childCount; i++)
            m_puzzlePieceAnchors.Add(transform.GetChild(i));

        FindAllAdjacentPieces();

        m_numberOfPiecesInPuzzle = m_puzzlePieceAnchors.Count;

        if (puzzlePiecesData != null && m_numberOfPiecesInPuzzle == puzzlePiecesData.transforms.Length)
        {
            m_audio.enabled = false;

            for (int i = 0; i < m_puzzlePieceAnchors.Count; i++)
            {
                var position = puzzlePiecesData.transforms[i].position;
                var rotation = puzzlePiecesData.transforms[i].rotation;

                m_puzzlePieceAnchors[i].position = new Vector3(position[0], position[1], position[2]);
                m_puzzlePieceAnchors[i].rotation = Quaternion.Euler(rotation[0], rotation[1], rotation[2]);
            }

            foreach (var puzzlePiece in m_puzzlePieces)
            {
                puzzlePiece.EnableCollider(false);
                puzzlePiece.EnableCollider(true);
            }
     
            StartCoroutine(CheckPuzzlePieceLocationsDelayed());
            m_scatter = false;
        }
    }


    private IEnumerator CheckPuzzlePieceLocationsDelayed()
    {
        // Wait one frame for all the events to get registered properly
        yield return null;

        foreach (var puzzlePiece in m_puzzlePieces)
            puzzlePiece.CheckPieceLocation(m_behaviourSettings.snapDistanceOnLoad);

        m_audio.enabled = true;
        m_canRaisePuzzlePieceInPositionEvent = true;
    }


    public void GetPossibleScatterPoints()
    {
        var scatterArea = CalculateScatterArea();

        var sampleSize = new Vector2(scatterArea.size.x, scatterArea.size.z);
        var offset = scatterArea.min;

        if (m_puzzlePieceAnchors == null)
            FindPuzzlePieceAnchors();

        m_possibleScatterPoints = new List<Vector3>();

        var cameraBounds = GetCameraBounds();
        
        var topLeftExclusion = GetTopLeftExclusionBounds(cameraBounds);
        var topRightExclusion = GetTopRightExclusionBounds(cameraBounds);
        var bottomLeftExclusion = GetBottomLeftExclusionBounds(cameraBounds);
        var bottomRightExclusion = GetBottomRightExclusionBounds(cameraBounds);

        int attempt = 1;
        while (m_possibleScatterPoints.Count < m_numberOfPiecesInPuzzle && attempt <= m_scatterGenerationAttempts)
        {
            // Alternatively use the PoissonDiscSampling method for a more random looking distribution
            // (although it is more prone to overlaps and not being able to find space for all the pieces)
            var points = SquareScatterSampling.GeneratePoints(m_pieceDiameter, m_jitterRatio, sampleSize);
            
            m_possibleScatterPoints = new List<Vector3>();

            foreach (var point in points)
            {
                var testPosition = new Vector3(point.x, m_hoverHeight, point.y);
                testPosition += offset;

                bool inExclusionArea = false;

                foreach (var scatterExclusion in m_centreExclusions)
                    inExclusionArea = inExclusionArea || InExclusionArea(testPosition, scatterExclusion);

                inExclusionArea = inExclusionArea || InExclusionArea(testPosition, topLeftExclusion);
                inExclusionArea = inExclusionArea || InExclusionArea(testPosition, topRightExclusion);
                inExclusionArea = inExclusionArea || InExclusionArea(testPosition, bottomLeftExclusion);
                inExclusionArea = inExclusionArea || InExclusionArea(testPosition, bottomRightExclusion);

                if (!inExclusionArea)
                    m_possibleScatterPoints.Add(testPosition);
            }

            attempt++;
        }

        attempt--;

        if (attempt >= m_scatterGenerationAttempts / 2)
            print("Scatter attempts " + attempt);

        m_numberOfScatterPoints = m_possibleScatterPoints.Count;
    }


    public Bounds GetCameraBounds()
    {
        var camera = m_camera != null ? m_camera : Camera.main;
        var position = m_cameraSize > 0 ? m_cameraPosition : camera.transform.position;
        position.y = 0;
        float size = m_cameraSize > 0 ? m_cameraSize : camera.orthographicSize;
        var cameraBounds = new Bounds(position, new Vector3(camera.aspect * size * 2, 0f, size * 2));

        return cameraBounds;
    }


    public Bounds GetTopLeftExclusionBounds(Bounds cameraBounds)
    {
        var topLeftExclsuionPosition = new Vector3(cameraBounds.min.x + m_topLeftExclusion.x * 0.5f, 0f, cameraBounds.max.z - m_topLeftExclusion.y * 0.5f);
        var topLeftExclusion = new Bounds(topLeftExclsuionPosition, new Vector3(m_topLeftExclusion.x, 0f, m_topLeftExclusion.y));

        return topLeftExclusion;
    }


    public Bounds GetTopRightExclusionBounds(Bounds cameraBounds)
    {
        var topRightExclsuionPosition = new Vector3(cameraBounds.max.x - m_topRightExclusion.x * 0.5f, 0f, cameraBounds.max.z - m_topRightExclusion.y * 0.5f);
        var topRightExclusion = new Bounds(topRightExclsuionPosition, new Vector3(m_topRightExclusion.x, 0f, m_topRightExclusion.y));

        return topRightExclusion;
    }


    public Bounds GetBottomLeftExclusionBounds(Bounds cameraBounds)
    {
        var bottomLeftExclsuionPosition = new Vector3(cameraBounds.min.x + m_bottomLeftExclusion.x * 0.5f, 0f, cameraBounds.min.z + m_bottomLeftExclusion.y * 0.5f);
        var bottomLeftExclusion = new Bounds(bottomLeftExclsuionPosition, new Vector3(m_bottomLeftExclusion.x, 0f, m_bottomLeftExclusion.y));

        return bottomLeftExclusion;
    }


    public Bounds GetBottomRightExclusionBounds(Bounds cameraBounds)
    {
        var bottomRightExclsuionPosition = new Vector3(cameraBounds.max.x - m_bottomRightExclusion.x * 0.5f, 0f, cameraBounds.min.z + m_bottomRightExclusion.y * 0.5f);
        var bottomRightExclusion = new Bounds(bottomRightExclsuionPosition, new Vector3(m_bottomRightExclusion.x, 0f, m_bottomRightExclusion.y));

        return bottomRightExclusion;
    }


    private bool InExclusionArea(Vector3 testPosition, Bounds scatterExclusion)
    {
        float buffer = m_pieceDiameter * 0.5f;

        return testPosition.x >= scatterExclusion.min.x - buffer && testPosition.x <= scatterExclusion.max.x + buffer
            && testPosition.z >= scatterExclusion.min.z - buffer && testPosition.z <= scatterExclusion.max.z + buffer;
    }


    public void ResetPuzzle()
    {
        if (Scattering || !gameObject.activeSelf)
            return;

        Scattering = true;
        bool puzzleComplete = m_numberOfPiecesInFinalPosition == m_numberOfPiecesInPuzzle;

        foreach (var puzzlePiece in m_puzzlePieces)
        {
            if (puzzleComplete || (!puzzlePiece.m_inFinalPosition && !puzzlePiece.m_partOfCluster))
                puzzlePiece.ResetPiece();
        }

        if (puzzleComplete)
        {
            var clusters = GetComponentsInChildren<ClusterManager>();

            foreach (var cluster in clusters)
                Destroy(cluster.gameObject);

            m_numberOfPiecesInFinalPosition = 0;
            m_numberOfEdgePiecesInFinalPosition = 0;
            m_puzzleMaterial.SetFloat(m_effectRadiusId, m_startEffectRadius);

            if (m_initialised)
            {
                m_puzzleMaterial.SetFloat("_Normal_strength", m_normalStrength);
                m_puzzleMaterial.SetFloat("_Shadow_strength", m_shadowStrength);
            }  
        }

        EnableAllPieceColliders(false);
        GetPossibleScatterPoints();

        StartCoroutine(ScatterPieces(puzzleComplete));
    }


    /// <summary>
    /// Hook up to a cheat code for easier testing
    /// </summary>
    public void AutoCompletePuzzle()
    {
        if (Scattering || !gameObject.activeSelf)
            return;

        foreach (var puzzlePiece in m_puzzlePieces)
            puzzlePiece.SetToFinalPosition();
    }


    /// <summary>
    /// Hook up to a cheat code for easier testing
    /// </summary>
    public void AutoFitOnePieceAtMousePosition()
    {
        if(Scattering || !gameObject.activeSelf)
            return;

        Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        Vector3 position = m_camera.ScreenToWorldPoint(mousePosition);
        position.y = 0;

        float closestDistance = float.MaxValue;
        PuzzlePieceController closestPiece = null;

        foreach (var puzzlePiece in m_puzzlePieces)
        {
            if (!puzzlePiece.m_inFinalPosition && (!m_showOnlyEdgePieces || puzzlePiece.IsEdgePiece))
            {
                float distanceFromMousePositionToFinalPosition = Vector3.Distance(puzzlePiece.InitialPosition, position);

                if (distanceFromMousePositionToFinalPosition < closestDistance)
                {
                    closestDistance = distanceFromMousePositionToFinalPosition;
                    closestPiece = puzzlePiece;
                }
            }
        }

        if (closestPiece != null)
            closestPiece.SetToFinalPosition();
    }


    /// <summary>
    /// Hook up to a cheat code for easier testing
    /// </summary>
    public void AutoFitOneRandomPiece()
    {
        if (Scattering || !gameObject.activeSelf)
            return;

        var unfitPieces = new List<PuzzlePieceController>();

        foreach (var puzzlePiece in m_puzzlePieces)
        {
            if (!puzzlePiece.m_inFinalPosition && (!m_showOnlyEdgePieces || puzzlePiece.IsEdgePiece))
                unfitPieces.Add(puzzlePiece);
        }

        int count = unfitPieces.Count;

        if (count == 0)
            return;

        int index = Random.Range(0, count);

        unfitPieces[index].SetToFinalPosition();
    }


    private void EnableAllPieceColliders(bool enable)
    {
        foreach (var piece in m_puzzlePieces)
            piece.EnableCollider(enable);
    }


    private IEnumerator DelayScatter()
    {
        EnableAllPieceColliders(false);
        Scattering = true;

        yield return new WaitForSeconds(m_behaviourSettings.scatterDelay);
        
        yield return ScatterPieces(true);
    }


    public IEnumerator ScatterPieces(bool scatterAllPieces)
    {
        float timeAvailableForScattering = m_behaviourSettings.inputDelay - m_behaviourSettings.scatterDelay - m_behaviourSettings.scatterDurationPerPiece;
        float timeBetweenAnimations = timeAvailableForScattering / m_numberOfPiecesInPuzzle;

        PuzzleScatterStartEvent.Invoke();

        if (scatterAllPieces)
        {
            RestartPuzzleEvent.Invoke();

            foreach (var child in m_puzzlePieceAnchors)
            {
                if (m_possibleScatterPoints.Count == 0)
                    break;

                int index = Random.Range(0, m_possibleScatterPoints.Count);

                StartCoroutine(AnimatePiece(child, m_possibleScatterPoints[index]));

                m_possibleScatterPoints.RemoveAt(index);

                if (timeBetweenAnimations < Time.deltaTime)
                    yield return null;
                else
                    yield return new WaitForSeconds(timeBetweenAnimations);
            }

            yield return new WaitForSeconds(m_behaviourSettings.scatterDurationPerPiece);
        }
        else
        {
            foreach (var puzzlePiece in m_puzzlePieces)
            {
                if (m_possibleScatterPoints.Count == 0)
                    break;

                if (puzzlePiece.m_inFinalPosition)
                    continue;

                if (puzzlePiece.m_partOfCluster)
                {
                    puzzlePiece.SetInputs(true);
                    puzzlePiece.EnableCollider(true);
                    continue;
                }

                int index = Random.Range(0, m_possibleScatterPoints.Count);

                StartCoroutine(AnimatePiece(puzzlePiece.transform.parent, m_possibleScatterPoints[index]));

                m_possibleScatterPoints.RemoveAt(index);

                if (timeBetweenAnimations < Time.deltaTime)
                    yield return null;
                else
                    yield return new WaitForSeconds(timeBetweenAnimations);

                puzzlePiece.SetInputs(true);
                puzzlePiece.EnableCollider(true);
            }

            yield return new WaitForSeconds(m_behaviourSettings.scatterDurationPerPiece);
        }

        Scattering = false;
        m_canRaisePuzzlePieceInPositionEvent = true;

        if (scatterAllPieces)
        {
            SetPuzzlePieceInputs(true);
            EnableAllPieceColliders(true);
        }

        PuzzleScatterCompleteEvent.Invoke();
    }


    private IEnumerator AnimatePiece(Transform piece, Vector3 scatterPosition)
    {
        var initialPosition = piece.position;
        float scatterDuration = m_behaviourSettings.scatterDurationPerPiece;
        var curve = m_behaviourSettings.scatterCurve;

        float time = 0;
        while (time < scatterDuration)
        {
            time += Time.deltaTime;
            float t = time / scatterDuration;
            float tAdjusted = curve.Evaluate(t);

            var position = Vector3.Lerp(initialPosition, scatterPosition, tAdjusted);
            piece.position = position;

            yield return null;
        }

        piece.position = scatterPosition;
    }


    public void PuzzlePiecePutInFinalPosition()
    {
        m_numberOfPiecesInFinalPosition++;

        if (m_canRaisePuzzlePieceInPositionEvent)
        {
            PuzzlePieceInPositionEvent.Invoke();
        }   

        if (m_numberOfPiecesInFinalPosition == m_numberOfPiecesInPuzzle)
            PuzzleComplete();
    }


    public void PuzzleEdgePiecePutInFinalPosition()
    {
        m_numberOfEdgePiecesInFinalPosition++;

        if (m_numberOfEdgePiecesInFinalPosition == m_numberOfEdgePiecesInPuzzle)
        {
            TurnOnAllPieces();

            FinalEdgePieceInPositionEvent.Invoke();
        }
    }


    public void AttractPieces()
    {
        if (Scattering)
            return;

        float butffer = m_pieceDiameter * 0.7f;
        float minX = -m_puzzleSize.x * 0.5f - butffer;
        float maxX = -minX;
        float minZ = -m_puzzleSize.y * 0.5f - butffer;
        float maxZ = -minZ;

        foreach (var puzzlePiece in m_puzzlePieces)
        {
            if (!puzzlePiece.m_inFinalPosition && puzzlePiece.gameObject.activeSelf && !puzzlePiece.m_partOfCluster)
            {
                var originalPosition = puzzlePiece.transform.parent.position;

                if (originalPosition.x > minX && originalPosition.x < maxX
                    && originalPosition.z > minZ && originalPosition.z < maxZ)
                    continue;

                float x = originalPosition.x;
                float z = originalPosition.z;

                if (Mathf.Abs(x) < 0.01f)
                    x += 0.01f;

                if (Mathf.Abs(z) < 0.01f)
                    z += 0.01f;

                float xLine = x > 0 ? maxX : minX;
                float tx = Mathf.InverseLerp(0f, x, xLine);

                float zLine = z > 0 ? maxZ : minZ;
                float tz = Mathf.InverseLerp(0f, z, zLine);

                float t = tx < tz ? tx : tz;
                t = 0.5f * (1f + t);

                var newPosition = Vector3.Lerp(Vector3.zero, originalPosition, t);

                newPosition.y = originalPosition.y;

                puzzlePiece.transform.parent.position = newPosition;
            }
        }
    }


    private void PuzzleComplete()
    {
        FinalPieceInPositionEvent.Invoke();

        Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        Vector4 position = m_camera.ScreenToWorldPoint(mousePosition);
        position.y = 0;
        position.w = 0;

        if (position.magnitude > 0.5f * m_maxEffectRadius)
            position = Vector4.zero;

        m_puzzleMaterial.SetVector(m_effectCentreId, position);

        position.y = m_particleSystem.transform.position.y;
        m_particleSystem.transform.position = position;

        StartCoroutine(ExpandEffectRadius());
    }


    private IEnumerator ExpandEffectRadius()
    {
        float time = 0;
        float radiusIncreasePerSecond = m_maxEffectRadius / m_behaviourSettings.effectDuration;

        var particleSystem = Instantiate(m_particleSystem);

        var particleSystemShape = particleSystem.shape;
        var particleSystemRate = particleSystem.emission;
        
        particleSystem.Play();

        while (time < m_behaviourSettings.effectDuration)
        {
            time += Time.deltaTime;
            float frac = time / m_behaviourSettings.effectDuration;
            float effectRadius = radiusIncreasePerSecond * time;
            m_puzzleMaterial.SetFloat(m_effectRadiusId, effectRadius);

            float emissionRate = Mathf.Lerp(m_behaviourSettings.particleRateMinMax.x, 
                m_behaviourSettings.particleRateMinMax.y, time / m_behaviourSettings.effectDuration);

            particleSystemShape.radius = effectRadius + m_behaviourSettings.particleRadiusBuffer;
            particleSystemRate.rateOverTime = emissionRate;
            yield return null;
        }

        particleSystem.Stop();

        PuzzleCompletionEffectFinishedEvent.Invoke();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        var cameraBounds = GetCameraBounds();
        Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);

        Gizmos.color = Color.yellow;
        var scatterArea = CalculateScatterArea();
        var size = scatterArea.size;
        size.x += m_pieceDiameter * (1f + 2f * m_jitterRatio);
        size.z += m_pieceDiameter * (1f + 2f * m_jitterRatio);
        Gizmos.DrawWireCube(scatterArea.center, size);

        Gizmos.color = Color.red;
        foreach (var scatterExclusion in m_centreExclusions)
            Gizmos.DrawWireCube(scatterExclusion.center, scatterExclusion.size);

        var topLeftExclusion = GetTopLeftExclusionBounds(cameraBounds);
        var topRightExclusion = GetTopRightExclusionBounds(cameraBounds);
        var bottomLeftExclusion = GetBottomLeftExclusionBounds(cameraBounds);
        var bottomRightExclusion = GetBottomRightExclusionBounds(cameraBounds);

        Gizmos.DrawWireCube(topLeftExclusion.center, topLeftExclusion.size);
        Gizmos.DrawWireCube(topRightExclusion.center, topRightExclusion.size);
        Gizmos.DrawWireCube(bottomLeftExclusion.center, bottomLeftExclusion.size);
        Gizmos.DrawWireCube(bottomRightExclusion.center, bottomRightExclusion.size);

        Gizmos.color = Color.green;
        if (m_possibleScatterPoints != null)
        {
            foreach (Vector3 point in m_possibleScatterPoints)
            {
                Gizmos.DrawWireCube(point, m_pieceDiameter * Vector3.one);
            }
        }
    }


    private void OnDestroy()
    {
        m_puzzleMaterial.SetFloat(m_effectRadiusId, m_startEffectRadius);
        m_puzzleMaterial.SetVector(m_effectCentreId, Vector4.zero);

        if (m_initialised)
        {
            m_puzzleMaterial.SetFloat("_Normal_strength", m_normalStrength);
            m_puzzleMaterial.SetFloat("_Shadow_strength", m_shadowStrength);
        }

        if (m_saveDataManager != null)
        {
            m_saveDataManager.SavePuzzlePieceData();
        }
    }
}
