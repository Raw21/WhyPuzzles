using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshGenerator
{
    public static GameObject GeneratePuzzlePieceMeshes(List<List<Vector2>> puzzlePieceUvs, Vector2 maxUvs, 
        Material puzzlePieceMaterial, Material ghostMaterial, string puzzleName, float puzzleScale = 3f)
    {
        var parent = new GameObject(puzzleName);

        var centre = maxUvs * 0.5f;

        var outlineUvs = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(0f, maxUvs.y),
            new Vector2(maxUvs.x, maxUvs.y),
            new Vector2(maxUvs.x, 0f)
        }.ToArray();

        float y = 0f;
        var outlineVertices = new List<Vector3>
        {
            puzzleScale * new Vector3(-centre.x, y, -centre.y),
            puzzleScale * new Vector3(-centre.x, y, maxUvs.y - centre.y),
            puzzleScale * new Vector3(maxUvs.x - centre.x, y, maxUvs.y - centre.y),
            puzzleScale * new Vector3(maxUvs.x - centre.x, y, -centre.y)
        }.ToArray();

        var outlineTrianglator = new Triangulator(outlineUvs);

        int[] outlineIndices = outlineTrianglator.Triangulate();

        var outlineMesh = new Mesh();
        outlineMesh.name = $"Ghost";
        outlineMesh.vertices = outlineVertices;
        outlineMesh.uv = outlineUvs;
        outlineMesh.triangles = outlineIndices;
        outlineMesh.RecalculateNormals();
        outlineMesh.RecalculateTangents();
        outlineMesh.RecalculateBounds();
        outlineMesh.Optimize();

        var outlineObject = new GameObject(outlineMesh.name);

        var outlineRenderer = outlineObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        var outlineFilter = outlineObject.AddComponent(typeof(MeshFilter)) as MeshFilter;       

        outlineRenderer.material = ghostMaterial;
        outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        outlineFilter.mesh = outlineMesh;

        outlineObject.transform.parent = parent.transform;
        outlineObject.SetActive(false);

        for (int piece = 0; piece < puzzlePieceUvs.Count; piece++)
        {
            var puzzlePiece = puzzlePieceUvs[piece];
            var uvs = puzzlePiece.ToArray();

            var trianglator = new Triangulator(uvs);

            int[] indices = trianglator.Triangulate();

            Vector3[] vertices = new Vector3[uvs.Length];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = puzzleScale * new Vector3(uvs[i].x - centre.x, 0f, uvs[i].y - centre.y);

            var mesh = new Mesh();
            mesh.name = $"Piece {piece + 1}";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            mesh.Optimize();

            var puzzlePieceObject = new GameObject(mesh.name);
      
            var renderer = puzzlePieceObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            var filter = puzzlePieceObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            puzzlePieceObject.gameObject.AddComponent<MeshCollider>();

            renderer.material = puzzlePieceMaterial;
            filter.mesh = mesh;

            puzzlePieceObject.transform.parent = parent.transform;
        }

        return parent;
    }
}
