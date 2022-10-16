using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class ClusterManager : MonoBehaviour
{
    public Vector3 m_initialPosition;
    public List<PuzzlePieceController> m_piecesInCluster;


    private void Awake()
    {
        m_piecesInCluster = new List<PuzzlePieceController>();
    }


    public void AddToCluster(PuzzlePieceController piece)
    {
        if (!m_piecesInCluster.Contains(piece))
        {
            m_piecesInCluster.Add(piece);
            piece.transform.parent.parent = transform;
            piece.m_cluster = this;
            piece.m_partOfCluster = true;
        }
    }


    public void SetToFinalPosition()
    {
        foreach (var piece in m_piecesInCluster)
            piece.SetToFinalPosition();
    }


    public void MoveClusterToPosition(PuzzlePieceController puzzlePiece, Vector3 position)
    {
        Vector3 offsetToPiece = position - puzzlePiece.transform.parent.position;
        transform.position += offsetToPiece;
    }


    public bool CheckAdjacentPieces(float snapDistance)
    {
        //Profiler.BeginSample("Check cluster adjacent pieces");

        bool connectedToOtherPiece = false;
        int count = m_piecesInCluster.Count;

        for (int i = 0; i < count; i++)
        {
            var testPiece = m_piecesInCluster[i];
            List<PuzzlePieceController> others;
            
            if (testPiece.CheckAdjacentPiecesDistance(snapDistance, out others))
            {
                foreach (var other in others)
                {
                    testPiece.ConnectOtherPieceToThisCluster(other);
                    TidyUpAdjacentPieces();
                    connectedToOtherPiece = true;
                }
            }

            count = m_piecesInCluster.Count;
        }

        Profiler.EndSample();

        return connectedToOtherPiece;
    }


    private void TidyUpAdjacentPieces()
    {
        for (int i = 0; i < m_piecesInCluster.Count; i++)
        {
            for (int j = i + 1; j < m_piecesInCluster.Count; j++)
            {
                var first = m_piecesInCluster[i];
                var second = m_piecesInCluster[j];

                first.AddPieceToJoinedAdjacentPieces(second);
                second.AddPieceToJoinedAdjacentPieces(first);
            }
        }
    }


    public void MergeOtherClusterToThis(ClusterManager otherCluster)
    {
        if (string.Equals(otherCluster.name, transform.name))
            return;

        foreach (var piece in otherCluster.m_piecesInCluster)
            AddToCluster(piece);

        otherCluster.m_piecesInCluster.Clear();
        
        Destroy(otherCluster.gameObject);
    }


    public Bounds GetBounds()
    {
        var bounds = m_piecesInCluster[0].Bounds;

        for (int i = 1; i < m_piecesInCluster.Count; i++)
            bounds.Encapsulate(m_piecesInCluster[i].Bounds);

        return bounds;
    }
}
