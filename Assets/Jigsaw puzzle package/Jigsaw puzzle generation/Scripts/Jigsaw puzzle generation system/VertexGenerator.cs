using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static SVGParser;
using static VectorUtils;

public class VertexGenerator
{ 
    public List<Vector3> GeneratePuzzleVertices(string svg, float sizePerPiece, Vector2Int puzzleSize, 
        Vector2 scale, out List<List<Vector2>> puzzlePieceUvs, int newVerticesPerSide = 40)
    {
        List<Vector3> verticesList = new List<Vector3>();

        using (var reader = new StringReader(svg))
        {
            var sceneInfo = ImportSVG(reader);

            var options = new TessellationOptions()
            {
                StepDistance = 0.25f,
                MaxCordDeviation = 0.1f,
                MaxTanAngleDeviation = 1f,
                SamplingStepSize = 1f
            };

            var geometryList = TessellateScene(sceneInfo.Scene, options);

            int horizontalPaths = puzzleSize.y - 1;
            int verticalPaths = puzzleSize.y - 1;

            var verticesByPathAndSide = new List<List<List<Vector2>>>();

            for (int i = 0; i < geometryList.Count; i++)
            {
                var geometry = geometryList[i];
                var vertices = AverageAdjacentVertices(geometry.Vertices);

                var evenlySpacedVertices = ConvertToEvenlySpacedVertices(vertices, sizePerPiece,
                    newVerticesPerSide, out List<List<Vector2>> verticesBySide);

                verticesList.AddRange(ConvertToVector3ForSeams(evenlySpacedVertices, scale));

                verticesByPathAndSide.Add(verticesBySide);
            }

            puzzlePieceUvs = ConvertToVerticesPerPiece(verticesByPathAndSide, puzzleSize, sizePerPiece);
            ScaleVerticesPerPieceToUvs(puzzlePieceUvs, scale);
        }

        return verticesList;
    }


    private List<Vector2> AverageAdjacentVertices(Vector2[] input)
    {
        var output = new List<Vector2>();
        Vector2 previousAverageVertex = Vector2.zero;

        for (int i = 0; i < input.Length - 1; i += 2)
        {
            var vertex1 = input[i];
            var vertex2 = input[i + 1];

            var averageVertex = 0.5f * (vertex1 + vertex2);

            // Prevent two extremely close vertices being added
            if (i > 0 && Vector2.Distance(averageVertex, previousAverageVertex) < 1e-4)
            {
                continue;
            }

            output.Add(averageVertex);
            previousAverageVertex = averageVertex;
        }

        return output;
    }


    private List<Vector2> ConvertToEvenlySpacedVertices(List<Vector2> input, float sizePerPiece, 
        int newVerticesPerSide, out List<List<Vector2>> verticesBySide)
    {
        float epsilon = (float) 1e-4;
        newVerticesPerSide /= 2;

        var evenlySpacedVertices = new List<Vector2>();
        var originalVerticesBySide = new List<List<Vector2>>();

        for (int i = 0; i < input.Count - 1; i++)
        {
            var vertex = input[i];
            float modX = vertex.x % sizePerPiece;
            float modY = vertex.y % sizePerPiece;
          
            bool newPath = (modX < epsilon || (sizePerPiece - modX < epsilon)) && (modY < epsilon || (sizePerPiece - modY < epsilon));

            if (newPath)
                originalVerticesBySide.Add(new List<Vector2>());

            originalVerticesBySide[originalVerticesBySide.Count - 1].Add(vertex);

            if (newPath && originalVerticesBySide.Count > 1)
                originalVerticesBySide[originalVerticesBySide.Count - 2].Add(vertex);
        }

        originalVerticesBySide[originalVerticesBySide.Count - 1].Add(input[input.Count - 1]);

        verticesBySide = new List<List<Vector2>>();

        foreach (var originalVertices in originalVerticesBySide)
        {
            var distances = new List<float>();
            float totalDistance = 0f;

            for (int i = 1; i < originalVertices.Count; i++)
            {
                var previousVertex = originalVertices[i - 1];
                var thisVertex = originalVertices[i];
                float distance = Vector2.Distance(previousVertex, thisVertex);
                distances.Add(distance);
                totalDistance += distance;
            }

            float distancePerVertexLarge = totalDistance / (newVerticesPerSide - 1);
            float distancePerVertexNormal = distancePerVertexLarge * 0.5f;
            float distancePerVertexSmall = distancePerVertexLarge / 3f;
            float segment1Distance = totalDistance * 0.15f;
            float segment2Distance = totalDistance * 0.35f;
            float segment3Distance = totalDistance * 0.65f;
            float segment4Distance = totalDistance * 0.85f;

            var newVertices = new List<Vector2>();
            newVertices.Add(originalVertices[0]);
            evenlySpacedVertices.Add(originalVertices[0]);

            float cumulativeDistance = 0;
            float distanceAtNextVertex = distancePerVertexLarge;

            // First and last original vertices become first and last new vertices so 
            // only need to go between 2nd and penultimate original vertices here
            bool lastVertexIncluded = false;
            for (int i = 1; i < originalVertices.Count; i++)
            {
                var originalVertex = originalVertices[i];
                float distance = distances[i - 1];
                cumulativeDistance += distance;

                if (cumulativeDistance > distanceAtNextVertex)
                {
                    var previousOriginalVertex = originalVertices[i - 1];
                    float previousCumulativeDistance = cumulativeDistance - distance;

                    float t = Mathf.InverseLerp(previousCumulativeDistance, cumulativeDistance, distanceAtNextVertex);
                    var newVertex = Vector2.Lerp(previousOriginalVertex, originalVertex, t);

                    newVertices.Add(newVertex);

                    if (i == originalVertices.Count - 1 && (1f - t) < epsilon)
                        lastVertexIncluded = true;
                    else
                        evenlySpacedVertices.Add(newVertex);

                    if (cumulativeDistance < segment1Distance || cumulativeDistance > segment4Distance)
                        distanceAtNextVertex += distancePerVertexLarge;
                    else if (cumulativeDistance < segment2Distance || cumulativeDistance > segment3Distance)
                        distanceAtNextVertex += distancePerVertexSmall;
                    else
                        distanceAtNextVertex += distancePerVertexNormal;
                }
            }

            // In case the last vertex was not accounted for
            if (!lastVertexIncluded)
            {
                var lastOriginalVertex = originalVertices[originalVertices.Count - 1];
                var lastNewVertex = newVertices[newVertices.Count - 1];

                if (Vector2.Distance(lastOriginalVertex, lastNewVertex) > 1e-4)
                    newVertices.Add(lastOriginalVertex);
            }

            verticesBySide.Add(newVertices);
        }

        var lastVertex = input[input.Count - 1];
        evenlySpacedVertices.Add(lastVertex);

        return evenlySpacedVertices;
    }


    private List<List<Vector2>> ConvertToVerticesPerPiece(List<List<List<Vector2>>> verticesByPathAndSide, Vector2Int puzzleSize, float sizePerPiece)
    {
        var verticesPerPiece = new List<List<Vector2>>();

        int horizontalPieces = puzzleSize.x;
        int verticalPieces = puzzleSize.y;

        for (int x = 1; x <= horizontalPieces; x++)
        {
            for (int y = verticalPieces; y > 0; y--)
            {
                if (x == 1 && y == 1)   // Bottom left corner
                {
                    var bottomLeftPiece = BuildBottomLeftPiece(verticesByPathAndSide, verticalPieces);
                    verticesPerPiece.Add(bottomLeftPiece);
                }
                else if (x == 1 && y == verticalPieces) // Top left corner
                {
                    var topLeftPiece = BuildTopLeftPiece(verticesByPathAndSide, verticalPieces, sizePerPiece);
                    verticesPerPiece.Add(topLeftPiece);
                }
                else if (x == horizontalPieces && y == 1)   // Bottom right corner
                {
                    var bottomRightPiece = BuildBottomRightPiece(verticesByPathAndSide, horizontalPieces, verticalPieces, sizePerPiece);
                    verticesPerPiece.Add(bottomRightPiece);
                }
                else if (x == horizontalPieces && y == verticalPieces)  // Top right corner
                {
                    var topRightPiece = BuildTopRightPiece(verticesByPathAndSide, horizontalPieces, verticalPieces, sizePerPiece);
                    verticesPerPiece.Add(topRightPiece);
                }
                else if (x == 1)    // Left edge
                {
                    var leftEdgePiece = BuildLeftEdgePiece(verticesByPathAndSide, verticalPieces, y);
                    verticesPerPiece.Add(leftEdgePiece);
                }
                else if (x == horizontalPieces) // Right edge
                {
                    var rightEdgePiece = BuildRightEdgePiece(verticesByPathAndSide, horizontalPieces, verticalPieces, y);
                    verticesPerPiece.Add(rightEdgePiece);
                }
                else if (y == 1)    // Bottom edge
                {
                    var bottomEdgePiece = BuildBottomEdgePiece(verticesByPathAndSide, verticalPieces, x);
                    verticesPerPiece.Add(bottomEdgePiece);
                }
                else if (y == verticalPieces)   // Top edge
                {
                    var topEdgePiece = BuildTopEdgePiece(verticesByPathAndSide, verticalPieces, x);
                    verticesPerPiece.Add(topEdgePiece);
                }
                else    // Inner pieces
                {
                    var innerPiece = BuildInnerPiece(verticesByPathAndSide, verticalPieces, x, y);
                    verticesPerPiece.Add(innerPiece);
                }
            }
        }

        return verticesPerPiece;
    }


    private List<Vector2> BuildBottomLeftPiece(List<List<List<Vector2>>> verticesByPathAndSide, int verticalPieces)
    {
        var vertices = new List<Vector2>();
        vertices.Add(Vector2.zero);

        int firstVerticalPath = verticalPieces - 1;

        var topSide = new List<Vector2>(verticesByPathAndSide[0][0]);
        var rightSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath][0]);

        rightSide.Reverse();
        rightSide.RemoveAt(0);

        vertices.AddRange(topSide);
        vertices.AddRange(rightSide);

        return vertices;
    }


    private List<Vector2> BuildTopLeftPiece(List<List<List<Vector2>>> verticesByPathAndSide, int verticalPieces, float sizePerPiece)
    {
        var vertices = new List<Vector2>();
        vertices.Add(new Vector2(0f, verticalPieces * sizePerPiece));

        int firstVerticalPath = verticalPieces - 1;

        var rightSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath][verticalPieces - 1]);
        var bottomSide = new List<Vector2>(verticesByPathAndSide[verticalPieces - 2][0]);

        rightSide.Reverse();
        bottomSide.Reverse();
        bottomSide.RemoveAt(0);

        vertices.AddRange(rightSide);
        vertices.AddRange(bottomSide);

        return vertices;
    }


    private List<Vector2> BuildBottomRightPiece(List<List<List<Vector2>>> verticesByPathAndSide, 
        int horizontalPieces, int verticalPieces, float sizePerPiece)
    {
        var vertices = new List<Vector2>();
        vertices.Add(new Vector2(horizontalPieces * sizePerPiece, 0f));

        int firstVerticalPath = verticalPieces - 1;

        var leftSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath + horizontalPieces - 2][0]);
        var topSide = new List<Vector2>(verticesByPathAndSide[0][horizontalPieces - 1]);

        topSide.RemoveAt(0);

        vertices.AddRange(leftSide);
        vertices.AddRange(topSide);

        return vertices;
    }


    private List<Vector2> BuildTopRightPiece(List<List<List<Vector2>>> verticesByPathAndSide, 
        int horizontalPieces, int verticalPieces, float sizePerPiece)
    {
        var vertices = new List<Vector2>();
        vertices.Add(new Vector2(horizontalPieces * sizePerPiece, verticalPieces * sizePerPiece));

        int firstVerticalPath = verticalPieces - 1;


        var bottomSide = new List<Vector2>(verticesByPathAndSide[verticalPieces - 2][horizontalPieces - 1]);
        var leftSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath + horizontalPieces - 2][verticalPieces - 1]);

        bottomSide.Reverse();
        leftSide.RemoveAt(0);

        vertices.AddRange(bottomSide);
        vertices.AddRange(leftSide);

        return vertices;
    }


    private List<Vector2> BuildLeftEdgePiece(List<List<List<Vector2>>> verticesByPathAndSide, int verticalPieces, int y)
    {
        var vertices = new List<Vector2>();

        int firstVerticalPath = verticalPieces - 1;

        var topSide = new List<Vector2>(verticesByPathAndSide[y - 1][0]);
        var rightSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath][y - 1]);
        var bottomSide = new List<Vector2>(verticesByPathAndSide[y - 2][0]);

        rightSide.Reverse();
        bottomSide.Reverse();

        rightSide.RemoveAt(0);
        bottomSide.RemoveAt(0);

        vertices.AddRange(topSide);
        vertices.AddRange(rightSide);
        vertices.AddRange(bottomSide);

        return vertices;
    }


    private List<Vector2> BuildRightEdgePiece(List<List<List<Vector2>>> verticesByPathAndSide, int horizonalPieces, int verticalPieces, int y)
    {
        var vertices = new List<Vector2>();

        int firstVerticalPath = verticalPieces - 1;

        var bottomSide = new List<Vector2>(verticesByPathAndSide[y - 2][horizonalPieces - 1]);
        var leftSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath + horizonalPieces - 2][y - 1]);
        var topSide = new List<Vector2>(verticesByPathAndSide[y - 1][horizonalPieces - 1]);

        bottomSide.Reverse();

        leftSide.RemoveAt(0);
        topSide.RemoveAt(0);

        vertices.AddRange(bottomSide);
        vertices.AddRange(leftSide);
        vertices.AddRange(topSide);

        return vertices;
    }


    private List<Vector2> BuildBottomEdgePiece(List<List<List<Vector2>>> verticesByPathAndSide, int verticalPieces, int x)
    {
        var vertices = new List<Vector2>();

        int firstVerticalPath = verticalPieces - 1;

        var leftSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath + x - 2][0]);
        var topSide = new List<Vector2>(verticesByPathAndSide[0][x - 1]);
        var rightSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath + x - 1][0]);

        rightSide.Reverse();

        topSide.RemoveAt(0);
        rightSide.RemoveAt(0);

        vertices.AddRange(leftSide);
        vertices.AddRange(topSide);
        vertices.AddRange(rightSide);

        return vertices;
    }


    private List<Vector2> BuildTopEdgePiece(List<List<List<Vector2>>> verticesByPathAndSide, int verticalPieces, int x)
    {
        var vertices = new List<Vector2>();

        int firstVerticalPath = verticalPieces - 1;

        var rightSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath + x - 1][verticalPieces - 1]);
        var bottomSide = new List<Vector2>(verticesByPathAndSide[verticalPieces - 2][x - 1]);
        var leftSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath + x - 2][verticalPieces - 1]);

        bottomSide.Reverse();
        rightSide.Reverse();

        bottomSide.RemoveAt(0);
        leftSide.RemoveAt(0);

        vertices.AddRange(rightSide);
        vertices.AddRange(bottomSide);
        vertices.AddRange(leftSide);

        return vertices;
    }


    private List<Vector2> BuildInnerPiece(List<List<List<Vector2>>> verticesByPathAndSide, int verticalPieces, int x, int y)
    {
        var vertices = new List<Vector2>();

        int firstVerticalPath = verticalPieces - 1;

        var leftSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath + x - 2][y - 1]);
        var topSide = new List<Vector2>(verticesByPathAndSide[y - 1][x - 1]);
        var rightSide = new List<Vector2>(verticesByPathAndSide[firstVerticalPath + x - 1][y - 1]);
        var bottomSide = new List<Vector2>(verticesByPathAndSide[y - 2][x - 1]);

        rightSide.Reverse();
        bottomSide.Reverse();

        leftSide.RemoveAt(0);
        topSide.RemoveAt(0);
        rightSide.RemoveAt(0);
        bottomSide.RemoveAt(0);

        vertices.AddRange(leftSide);
        vertices.AddRange(topSide);
        vertices.AddRange(rightSide);
        vertices.AddRange(bottomSide);

        return vertices;
    }


    private List<Vector3> ConvertToVector3ForSeams(List<Vector2> input, Vector2 scale)
    {
        var output = new List<Vector3>();

        for (int i = 0; i < input.Count; i++)
        {
            float z = i == input.Count - 1
                ? 0f
                : 1f;

            var inputVector = scale * input[i];

            output.Add(new Vector3(inputVector.x, inputVector.y, z));
        }

        return output;
    }


    private void ScaleVerticesPerPieceToUvs(List<List<Vector2>> verticesPerPiece, Vector2 scale)
    {
        foreach (var piece in verticesPerPiece)
        {
            for (int i = 0; i < piece.Count; i++)
            {
                piece[i] *= scale; 
            }
        }
    }
}
