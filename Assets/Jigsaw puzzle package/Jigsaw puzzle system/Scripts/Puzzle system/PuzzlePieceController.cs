using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshCollider))]
public class PuzzlePieceController : MonoBehaviour
{
    [HideInInspector]
    public BehaviourSettings m_behaviourSettings;

    [HideInInspector]
    public AudioSource m_audio;

    [HideInInspector]
    public SoundEffects m_soundEffects;

    [HideInInspector]
    public PuzzleManager m_puzzleManager;
    
    [HideInInspector]
    public bool m_partOfCluster;

    [HideInInspector]
    public bool m_inFinalPosition;

    [HideInInspector]
    public ClusterManager m_cluster;

    [HideInInspector]
    public float m_hoverHeight = 0.05f;
    private float m_hoverIncrement;

    [HideInInspector]
    public float m_pickUpHeight = 0.05f;

    private MeshCollider m_collider;
    private bool m_acceptInputs;
    private Camera m_camera;
    private Vector3 m_offset;
    private Vector3 m_initialPosition; 
    private MeshRenderer m_renderer;
    private Bounds m_boundsAtStartOfMove;
    private Vector3 m_positionAtStartOfMove;
    private Vector3 m_cameraCentre;
    private float m_cameraHalfHeight;
    private Transform m_originalRoot;

    private Bounds m_cameraBounds;
    private Bounds m_topLeftExclusionBounds;
    private Bounds m_topRightExclusionBounds;
    private Bounds m_bottomLeftExclusionBounds;
    private Bounds m_bottomRightExclusionBounds;

    private List<PuzzlePieceController> m_adjacentPieces;
    private List<PuzzlePieceController> m_joinedAdjacentPieces;

    private bool m_behindUI;

    private bool m_initialised;
    

    private float SnapDistance { get { return m_behaviourSettings.snapDistanceRatio * m_puzzleManager.m_pieceDiameter; } }


    public bool IsEdgePiece
    {
        get { return m_adjacentPieces.Count < 4; }
    }


    public Vector3 InitialPosition
    {
        get { return m_initialPosition; }
    }


    private void Awake()
    {
        if (!m_initialised)
            Init();
    }


    public void Init()
    {
        if (m_initialised)
            return;

        if (transform.parent != null && transform.parent.parent != null)
            m_originalRoot = transform.parent.parent.transform;

        m_initialPosition = transform.parent.position;
        m_camera = Camera.main;
        m_renderer = GetComponent<MeshRenderer>();
        m_collider = GetComponent<MeshCollider>();
        m_hoverIncrement = Random.Range(-0.002f, 0.002f);
        m_initialised = true;
    }


    private void Start()
    {
        m_cameraHalfHeight = m_camera.orthographicSize;
        m_cameraCentre = m_camera.transform.position;

        FindAdjacentPieces();
    }


    public void SetMaterial(Material material)
    {
        if (m_renderer == null)
            m_renderer = GetComponent<MeshRenderer>();

        m_renderer.material = material;
    }


    public Bounds Bounds
    {
        get
        {
            if (m_renderer == null)
                m_renderer = GetComponent<MeshRenderer>();

            var bounds = m_renderer.bounds;
            var extents = bounds.extents;
            extents.y = 0.1f;
            bounds.extents = extents;

            return bounds;
        }
    }


    public void FindAdjacentPieces()
    {
        if (m_adjacentPieces != null)
            return;

        m_adjacentPieces = new List<PuzzlePieceController>();
        m_joinedAdjacentPieces = new List<PuzzlePieceController>();

        var colliders = Physics.OverlapSphere(Bounds.center, Bounds.extents.magnitude);

        //print("This piece: " + name);
        for (int i = 0; i < colliders.Length; i++)
        {
            var piece = colliders[i].GetComponent<PuzzlePieceController>();
            if (piece != null && piece != this)
            {
                //print("Touching piece: " + piece.name);
                var offset = transform.parent.position - piece.transform.parent.position;

                float xDiff = Math.Abs(offset.x);
                float zDiff = Math.Abs(offset.z);

                // Ignore pieces on the diagonal
                if (xDiff < Bounds.extents.x || zDiff < Bounds.extents.z)
                {
                    //print("Adjacent piece:" + piece.name);
                    m_adjacentPieces.Add(piece);
                }
            }
        }
    }


    public void SetInputs(bool acceptInputs)
    {
        m_acceptInputs = acceptInputs;
    }


    public void EnableCollider(bool enable)
    {
        if (m_collider == null)
            m_collider = GetComponent<MeshCollider>();

        m_collider.enabled = enable;
    }


    public void ResetPiece()
    {
        m_acceptInputs = false;
        m_inFinalPosition = false;
        m_collider.enabled = true;
        m_cluster = null;
        m_partOfCluster = false;
        m_joinedAdjacentPieces.Clear();

        if (transform.parent != null)
            transform.parent.parent = m_originalRoot;
    }


    private void OnMouseDown()
    {
        m_behindUI = this.IsPointerOverGameObject();

        if (m_inFinalPosition || !m_acceptInputs || m_behindUI || Input.touchCount > 1)
            return;

        m_boundsAtStartOfMove = m_partOfCluster
            ? m_cluster.GetBounds()
            : m_renderer.bounds;

        m_positionAtStartOfMove = m_partOfCluster
            ? m_cluster.transform.position
            : transform.parent.position;

        Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        Vector3 position = m_camera.ScreenToWorldPoint(mousePosition);
        position.y = 0;

        m_offset = m_partOfCluster
            ? m_cluster.transform.position - position 
            : transform.parent.position - position;

        m_offset.y = 0;

        m_cameraBounds = m_puzzleManager.GetCameraBounds();
        m_topLeftExclusionBounds = m_puzzleManager.GetTopLeftExclusionBounds(m_cameraBounds);
        m_topRightExclusionBounds = m_puzzleManager.GetTopRightExclusionBounds(m_cameraBounds);
        m_bottomLeftExclusionBounds = m_puzzleManager.GetBottomLeftExclusionBounds(m_cameraBounds);
        m_bottomRightExclusionBounds = m_puzzleManager.GetBottomRightExclusionBounds(m_cameraBounds);
    }


    private void OnMouseDrag()
    {
        if (m_inFinalPosition || !m_acceptInputs || m_behindUI || Input.touchCount > 1)
            return;

        Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        Vector3 position = m_camera.ScreenToWorldPoint(mousePosition);
        position.y = m_pickUpHeight;

        if (m_partOfCluster)
            m_cluster.transform.position = position + m_offset;
        else
            transform.parent.position = position + m_offset;

        CheckBoundsAreWithinCameraView();
    }


    private void OnMouseUp()
    {
        if (m_inFinalPosition || !m_acceptInputs || m_behindUI || Input.touchCount > 1)
            return;

        CheckPieceLocation(SnapDistance);
        CheckBoundsAreWithinCameraView();
    }


    public void CheckPieceLocation(float snapDistance)
    {
        bool getsConnected = false;
        if (m_partOfCluster)
        {
            getsConnected = m_cluster.CheckAdjacentPieces(snapDistance);
        }
        else if (getsConnected = CheckAdjacentPieces(snapDistance))
        {
            // If this piece gets joined into a cluster we need to check the cluster again to see if any other nearby pieces can be connected
            m_cluster.CheckAdjacentPieces(snapDistance);
        }

        if (getsConnected)
        {
            m_soundEffects.PlayClip(m_audio, m_soundEffects.puzzlePieceConnect);
        }

        Vector3 position = m_partOfCluster ? m_cluster.transform.position : transform.parent.position;

        Vector3 initialPosition = m_partOfCluster ? m_cluster.m_initialPosition : m_initialPosition;
        position.y = initialPosition.y;

        float distance = Vector3.Distance(initialPosition, position);

        if (distance < SnapDistance)
        {
            if (m_partOfCluster)
                m_cluster.SetToFinalPosition();
            else
                SetToFinalPosition(!getsConnected);
        }
        else
        {
            position.y = initialPosition.y + m_hoverHeight;

            if (m_partOfCluster)
                m_cluster.transform.position = position;
            else
            {
                position.y += m_hoverIncrement;
                transform.parent.position = position;
            }

            if (!getsConnected)
            {
                m_soundEffects.PlayClip(m_audio, m_soundEffects.puzzlePiecePutDown);
            }
        }
    }


    public void SetToFinalPosition(bool playClip = true)
    {
        if (m_inFinalPosition)
            return;

        transform.parent.position = m_initialPosition;
        m_inFinalPosition = true;
        m_collider.enabled = false;

        if (playClip)
            m_soundEffects.PlayClip(m_audio, m_soundEffects.puzzlePiecePutInFinalPosition);

        if (IsEdgePiece)
            m_puzzleManager.PuzzleEdgePiecePutInFinalPosition();

        m_puzzleManager.PuzzlePiecePutInFinalPosition();
    }


    public bool CheckAdjacentPieces(float snapDistance)
    {
        List<PuzzlePieceController> others;

        if (CheckAdjacentPiecesDistance(snapDistance, out others))
        {
            // Only check the first connected piece because the others will get checked when the resulting cluster is tidied up
            var other = others[0];
            Vector3 otherPosition = other.transform.parent.position;
            Vector3 initialOffsetToOther = m_initialPosition - other.m_initialPosition;

            Vector3 testPosition = otherPosition + initialOffsetToOther;
      
            if (other.m_partOfCluster)
            {
                // Add this to other cluster
                transform.parent.position = testPosition;
                other.m_cluster.AddToCluster(this);
            }
            else
            {
                // Create a new cluster and add both pieces to it
                transform.parent.position = testPosition;
                var clusterGameObject = new GameObject(transform.name + " - cluster");
                var position = transform.parent.position;

                position.y -= Random.Range(0.001f, 0.002f); // I'm puzzled as to why this should be -ve.  
                                                            // I think it's because the position of the cluster gets set back to 0.05 later on
                                                            // and all the pieces are then above it by this random amount.
                                                            // Whatever, it seems to work fine.

                clusterGameObject.transform.SetPositionAndRotation(position, Quaternion.identity);
                var cluster = clusterGameObject.AddComponent<ClusterManager>();
                cluster.transform.parent = m_originalRoot;
                cluster.m_initialPosition = m_initialPosition;
                cluster.AddToCluster(this);
                cluster.AddToCluster(other);
                m_cluster = cluster;
            }

            if (!m_joinedAdjacentPieces.Contains(other))
                m_joinedAdjacentPieces.Add(other);

            if (!other.m_joinedAdjacentPieces.Contains(this))
                other.m_joinedAdjacentPieces.Add(this);

            return true;
        }

        return false;
    }


    public bool CheckAdjacentPiecesDistance(float snapDistance, out List<PuzzlePieceController> connectedPieces)
    {     
        connectedPieces = new List<PuzzlePieceController>();
        int alreadyConnectedPieces = 0;
        foreach (var other in m_adjacentPieces)
        {
            if (!other.gameObject.activeSelf)
                continue;

            if (m_joinedAdjacentPieces.Contains(other))
            {
                alreadyConnectedPieces++;
                continue;
            }

            Vector3 otherPosition = other.transform.parent.position;
            Vector3 otherInitialPosition = other.m_initialPosition;
            Vector3 initialOffsetToOther = m_initialPosition - otherInitialPosition;

            Vector3 testPosition = otherPosition + initialOffsetToOther;
            testPosition.y = transform.parent.position.y;

            float distance = Vector3.Distance(transform.parent.position, testPosition);

            if (distance < snapDistance)
                connectedPieces.Add(other);
        }
        
        bool makingConnection = connectedPieces.Count > 0;

        return makingConnection;
    }


    public void ConnectOtherPieceToThisCluster(PuzzlePieceController other)
    {
        Vector3 otherPosition = other.transform.parent.position;
        Vector3 initialOffsetToOther = m_initialPosition - other.m_initialPosition;

        Vector3 testPosition = otherPosition + initialOffsetToOther;

        m_cluster.MoveClusterToPosition(this, testPosition);

        if (other.m_partOfCluster)
            m_cluster.MergeOtherClusterToThis(other.m_cluster);
        else
        {
            m_cluster.AddToCluster(other);
            other.m_cluster = m_cluster;
        }

        if (!m_joinedAdjacentPieces.Contains(other))
            m_joinedAdjacentPieces.Add(other);

        if (!other.m_joinedAdjacentPieces.Contains(this))
            other.m_joinedAdjacentPieces.Add(this);
    }


    public void AddPieceToJoinedAdjacentPieces(PuzzlePieceController other)
    {
        if (m_adjacentPieces.Contains(other))
        {
            if (!m_joinedAdjacentPieces.Contains(other))
                m_joinedAdjacentPieces.Add(other);
        }
    }


    private void CheckBoundsAreWithinCameraView()
    {
        var bounds = m_boundsAtStartOfMove;

        Vector3 position = m_partOfCluster
            ? m_cluster.transform.position
            : transform.parent.position;

        bounds.center = bounds.center + position - m_positionAtStartOfMove;

        var offset = Vector3.zero;
        
        float cameraHalfWidth = m_cameraHalfHeight * m_camera.aspect;
        
        var cameraMin = new Vector3(m_cameraCentre.x - cameraHalfWidth, 0, m_cameraCentre.z - m_cameraHalfHeight);
        var cameraMax = new Vector3(m_cameraCentre.x + cameraHalfWidth, 0, m_cameraCentre.z + m_cameraHalfHeight);

        if (bounds.min.x < cameraMin.x)
            offset.x = cameraMin.x - bounds.min.x;
        else if (bounds.max.x > cameraMax.x)
            offset.x = cameraMax.x - bounds.max.x;

        if (bounds.min.z < cameraMin.z)
            offset.z = cameraMin.z - bounds.min.z;
        else if (bounds.max.z > cameraMax.z)
            offset.z = cameraMax.z - bounds.max.z;

        if (m_partOfCluster)
            m_cluster.transform.position = m_cluster.transform.position + offset;
        else
            transform.parent.position = transform.parent.position + offset;

        bounds.center = bounds.center + offset;

        CheckBoundsAreNotOverlappingExclusionAreas(bounds);
    }


    private void CheckBoundsAreNotOverlappingExclusionAreas(Bounds bounds)
    {
        var offset = Vector3.zero;

        // Top left exclusion area
        float xDiff = m_partOfCluster 
            ? m_topLeftExclusionBounds.max.x - bounds.center.x 
            : m_topLeftExclusionBounds.max.x - bounds.min.x;

        float zDiff = m_partOfCluster 
            ? bounds.center.z - m_topLeftExclusionBounds.min.z 
            : bounds.max.z - m_topLeftExclusionBounds.min.z;

        if (xDiff > 0 && zDiff > 0)
        {
            if (xDiff > zDiff)
                offset.z = -zDiff;
            else
                offset.x = xDiff;
        }

        // Top right exclusion area
        xDiff = m_partOfCluster
            ? bounds.center.x - m_topRightExclusionBounds.min.x
            : bounds.max.x - m_topRightExclusionBounds.min.x;

        zDiff = m_partOfCluster
            ? bounds.center.z - m_topRightExclusionBounds.min.z
            : bounds.max.z - m_topRightExclusionBounds.min.z;

        if (xDiff > 0 && zDiff > 0)
        {
            if (xDiff > zDiff)
                offset.z = -zDiff;
            else
                offset.x = -xDiff;
        }

        // Bottom left exclusion area
        xDiff = m_partOfCluster
            ? m_bottomLeftExclusionBounds.max.x - bounds.center.x
            : m_bottomLeftExclusionBounds.max.x - bounds.min.x;

        zDiff = m_partOfCluster
            ? m_bottomLeftExclusionBounds.max.z - bounds.center.z
            : m_bottomLeftExclusionBounds.max.z - bounds.min.z;

        if (xDiff > 0 && zDiff > 0)
        {
            if (xDiff > zDiff)
                offset.z = zDiff;
            else
                offset.x = xDiff;
        }

        // Bottom right exclusion area
        xDiff = m_partOfCluster
            ? bounds.center.x - m_bottomRightExclusionBounds.min.x
            : bounds.max.x - m_bottomRightExclusionBounds.min.x;

        zDiff = m_partOfCluster
            ? m_bottomRightExclusionBounds.max.z - bounds.center.z
            : m_bottomRightExclusionBounds.max.z - bounds.min.z;

        if (xDiff > 0 && zDiff > 0)
        {
            if (xDiff > zDiff)
                offset.z = zDiff;
            else
                offset.x = -xDiff;
        }

        if (m_partOfCluster)
            m_cluster.transform.position = m_cluster.transform.position + offset;
        else
            transform.parent.position = transform.parent.position + offset;
    }


    private void OnDrawGizmosSelected()
    {
        if (m_adjacentPieces == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.parent.position, 0.1f);
        Gizmos.DrawWireSphere(m_initialPosition, 0.1f);

        Gizmos.color = Color.yellow;
        foreach (var piece in m_adjacentPieces)
        {
            Gizmos.DrawWireCube(piece.Bounds.center, piece.Bounds.size);
            Gizmos.DrawSphere(piece.transform.parent.position, 0.1f);
            Gizmos.DrawWireSphere(piece.m_initialPosition, 0.1f);
        }

        Gizmos.color = Color.blue;
        foreach (var piece in m_joinedAdjacentPieces)
        {
            Gizmos.DrawWireCube(piece.Bounds.center, piece.Bounds.size);
            Gizmos.DrawSphere(piece.transform.parent.position, 0.1f);
            Gizmos.DrawSphere(piece.m_initialPosition, 0.1f);
        }
    }
}
