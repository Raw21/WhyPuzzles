using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SeamsTextureCreator
{
    public static Texture GenerateSeamsTexture(int resolution, float seamsThickness, List<Vector3> vertices, ComputeShader seamsComputeShader)
    {
        var rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true;
        rt.Create();
        rt.filterMode = FilterMode.Point;

        int setTextureHandle = seamsComputeShader.FindKernel("CSSetTexture");
        int createSeamsHandle = seamsComputeShader.FindKernel("CSCreateSeams");

        var verticesBuffer = new ComputeBuffer(vertices.Count, 3 * sizeof(float));
        verticesBuffer.SetData(vertices);

        float halfThickness = seamsThickness * 0.5f / resolution;
        float halfThicknessSq = halfThickness * halfThickness;

        int groups = resolution / 8;

        seamsComputeShader.SetInt("Resolution", resolution);
        seamsComputeShader.SetInt("NumVertices", vertices.Count);
        seamsComputeShader.SetFloat("HalfThickness", halfThickness);
        seamsComputeShader.SetFloat("HalfThicknessSq", halfThicknessSq);

        seamsComputeShader.SetTexture(setTextureHandle, "Result", rt);
        seamsComputeShader.Dispatch(setTextureHandle, groups, groups, 1);

        groups = Mathf.CeilToInt(vertices.Count / 64f);

        seamsComputeShader.SetBuffer(createSeamsHandle, "Vertices", verticesBuffer);
        seamsComputeShader.SetTexture(createSeamsHandle, "Result", rt);
        seamsComputeShader.Dispatch(createSeamsHandle, groups, 1, 1);

        verticesBuffer.Release();

        return rt;
    }


    public static Texture GenerateSeamsTextureOld(int resolution)
    {
        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        var colours = new Color[resolution * resolution];
        int quart = resolution / 4;

        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                int index = i + (j * resolution);

                float value = i == resolution / 2 || j == resolution / 2 || i == j
                        || (i == quart && j == 3 * quart) || ((j == quart && i == 3 * quart))
                    ? 0f
                    : 1f;

                float red = value;
                float green = value;
                float blue = value;

                var colour = new Color(red, green, blue);
                colours[index] = colour;
            }
        }

        texture.SetPixels(colours);
        texture.Apply();

        return texture;
    }
}
