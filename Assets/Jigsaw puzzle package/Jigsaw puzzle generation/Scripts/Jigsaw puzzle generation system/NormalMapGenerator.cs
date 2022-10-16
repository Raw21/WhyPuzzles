using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NormalMapGenerator
{ 
    public static Texture GenerateNormalMap(Texture heightMap, ComputeShader normalFromHeightComputeShader)
    {
        int resolution = heightMap.width;
      
        var rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true;
        rt.Create();

        int normalFromHeightHandle = normalFromHeightComputeShader.FindKernel("CSNormalFromHeight");

        int groups = Mathf.CeilToInt(resolution / 8f);

        normalFromHeightComputeShader.SetTexture(normalFromHeightHandle, "Height", heightMap);
        normalFromHeightComputeShader.SetTexture(normalFromHeightHandle, "Normal", rt);
        normalFromHeightComputeShader.Dispatch(normalFromHeightHandle, groups, groups, 1);

        return rt;
    }
}
