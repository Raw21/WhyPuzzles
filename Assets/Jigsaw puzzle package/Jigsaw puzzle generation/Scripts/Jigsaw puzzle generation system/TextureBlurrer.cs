using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureBlurrer
{
    public static Texture GetBlurredTexture(Texture input, int halfWindow, float[] kernel, ComputeShader blurComputeShader)
    {
        int resolution = input.width;

        var rts = new RenderTexture[2];
        for (int i = 0; i < 2; i++)
        {
            var rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            rt.enableRandomWrite = true;
            rt.Create();
            //rt.filterMode = FilterMode.Point;
            rts[i] = rt;
        }


        int horizonalHandle = blurComputeShader.FindKernel("CSHorizontalBlur");
        int verticalHandle = blurComputeShader.FindKernel("CSVerticalBlur");

        blurComputeShader.SetInt("Resolution", resolution);
        blurComputeShader.SetInt("KernelSize", kernel.Length);
        blurComputeShader.SetInt("HalfKernelSize", halfWindow);

        var kernelBuffer = new ComputeBuffer(kernel.Length, sizeof(float));
        kernelBuffer.SetData(kernel);

        int groups = Mathf.CeilToInt(resolution / 8f);

        blurComputeShader.SetBuffer(horizonalHandle, "Kernel", kernelBuffer);
        blurComputeShader.SetTexture(horizonalHandle, "Prev", input);
        blurComputeShader.SetTexture(horizonalHandle, "Result", rts[0]);
        blurComputeShader.Dispatch(horizonalHandle, groups, groups, 1);

        blurComputeShader.SetBuffer(verticalHandle, "Kernel", kernelBuffer);
        blurComputeShader.SetTexture(verticalHandle, "Prev", rts[0]);
        blurComputeShader.SetTexture(verticalHandle, "Result", rts[1]);
        blurComputeShader.Dispatch(verticalHandle, groups, groups, 1);

        kernelBuffer.Release();
        rts[0].Release();

        return rts[1];
    }
}
