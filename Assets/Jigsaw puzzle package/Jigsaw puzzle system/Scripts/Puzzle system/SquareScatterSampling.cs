using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SquareScatterSampling
{
    public static List<Vector2> GeneratePoints(float squareSize, float jitterRatio, Vector2 sampleRegionSize)
    {
        var points = new List<Vector2>();

        float distanceBetween = squareSize * (1f + jitterRatio * 2f);

        int maxXSamples = Mathf.FloorToInt(sampleRegionSize.x / distanceBetween) + 1;
        int maxYSamples = Mathf.FloorToInt(sampleRegionSize.y / distanceBetween) + 1;

        var offset = 0.5f * (sampleRegionSize - distanceBetween * new Vector2(maxXSamples - 1, maxYSamples - 1));

        for (int x = 0; x < maxXSamples; x++)
        {
            for (int y = 0; y < maxYSamples; y++)
            {
                points.Add(new Vector2(x * distanceBetween, y * distanceBetween) + offset);
            }
        }

        return points;
    }
}
