using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ImageSquarifier
{ 
    public static Texture2D SquarifyImage(Texture2D input)
    {
        int width = input.width;
        int height = input.height;

        int resolution = Mathf.Max(width, height);

        var inputPixels = input.GetPixels();
        var output = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
        output.wrapMode = TextureWrapMode.Clamp;
        
        output.SetPixels(0, 0, width, height, inputPixels);
        output.Apply();

        return output;
    }
}
