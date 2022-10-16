using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class JigsawSvgGenerator
{
    private int seed;
    private float a;
    private float b;
    private float c;
    private float d;
    private float e;
    private float t;
    private float j;
    private bool flip;
    private float xi;
    private float yi;
    private float xn;
    private float yn;
    private bool vertical;
    private float offset;
    private int width;
    private int height;


    public string GenerateJigsawSvg(int seed, float tabSize, float jitter, int xPieces, int yPieces, int width, int height)
    {
        this.seed = seed;
        this.width = width;
        this.height = height;
        t = tabSize / 200f;
        j = jitter / 100f;
        xn = xPieces;
        yn = yPieces;

        string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.0\" ";
        svg += "width=\"" + width + "mm\" height=\"" + height + "mm\" viewBox=\"0 0 " + width + " " + height + "\">";
        svg += "<path fill=\"none\" stroke=\"black\" stroke-width=\"0.1\" d=\"";

        svg += Generate();

        svg += "\"></path></svg>";

        return svg;
    }


    private string Generate()
    {
        string str = "";

        vertical = false;

        for (yi = 1; yi < yn; ++yi)
        {
            xi = 0;
            First();
            str += "M" + P0l() + "," + P0w() + " ";
            for (xi = 0; xi < xn; ++xi)
            {
                str += "C" + P1l() + "," + P1w() + " " + P2l() + "," + P2w() + " " + P3l() + "," + P3w() + " ";
                str += "C" + P4l() + "," + P4w() + " " + P5l() + "," + P5w() + " " + P6l() + "," + P6w() + " ";
                str += "C" + P7l() + "," + P7w() + " " + P8l() + "," + P8w() + " " + P9l() + "," + P9w() + " ";
                Next();
            }
        }

        vertical = true;
        for (xi = 1; xi < xn; ++xi)
        {
            yi = 0;
            First();
            str += "M" + P0w() + "," + P0l() + " ";
            for (yi = 0; yi < yn; ++yi)
            {
                str += "C" + P1w() + "," + P1l() + " " + P2w() + "," + P2l() + " " + P3w() + "," + P3l() + " ";
                str += "C" + P4w() + "," + P4l() + " " + P5w() + "," + P5l() + " " + P6w() + "," + P6l() + " ";
                str += "C" + P7w() + "," + P7l() + " " + P8w() + "," + P8l() + " " + P9w() + "," + P9l() + " ";
                Next();
            }
        }

        return str;
    }


    private float Random()
    {
        float x = Mathf.Sin(seed) * 10000;
        seed += 1;
        return x - Mathf.Floor(x);
    }


    private float Uniform(float min, float max)
    {
        float r = Random();

        return min + r * (max - min);
    }


    private bool RBool()
    {
        return Random() > 0.5f;
    }


    private void First()
    {
        e = Uniform(-j, j);
        Next();
    }


    private void Next()
    {
        bool flipOld = flip;
        flip = RBool();
        a = flip == flipOld
            ? -e : e;

        b = Uniform(-j, j);
        c = Uniform(-j, j);
        d = Uniform(-j, j);
        e = Uniform(-j, j);
    }


    private float Sl()
    {
        return vertical
            ? height / yn
            : width / xn;
    }


    private float Sw()
    {
        return vertical
            ? width / xn
            : height / yn;
    }


    private float Ol()
    {
        return offset + Sl() * (vertical ? yi : xi);
    }


    private float Ow()
    {
        return offset + Sw() * (vertical ? xi : yi);
    }


    private float L(float v)
    {
        float ret = Ol() + Sl() * v;

        return Mathf.Round(ret * 100f) / 100f;
    }


    private float W(float v)
    {
        float ret = Ow() + Sw() * v * (flip ? -1f : 1f);

        return Mathf.Round(ret * 100f) / 100f;
    }


    private string P0l()
    {
        return L(0f).ToString(CultureInfo.InvariantCulture);
    }


    private string P0w()
    {
        return W(0f).ToString(CultureInfo.InvariantCulture);
    }


    private string P1l()
    {
        return L(0.2f).ToString(CultureInfo.InvariantCulture);
    }


    private string P1w()
    {
        return W(a).ToString(CultureInfo.InvariantCulture);
    }


    private string P2l()
    {
        return L(0.5f + b + d).ToString(CultureInfo.InvariantCulture);
    }


    private string P2w()
    {
        return W(-t + c).ToString(CultureInfo.InvariantCulture);
    }


    private string P3l()
    {
        return L(0.5f - t + b).ToString(CultureInfo.InvariantCulture);
    }


    private string P3w()
    {
        return W(t + c).ToString(CultureInfo.InvariantCulture);
    }


    private string P4l()
    {
        return L(0.5f - 2f * t + b - d).ToString(CultureInfo.InvariantCulture);
    }


    private string P4w()
    {
        return W(3f * t + c).ToString(CultureInfo.InvariantCulture);
    }


    private string P5l()
    {
        return L(0.5f + 2f * t + b - d).ToString(CultureInfo.InvariantCulture);
    }


    private string P5w()
    {
        return W(3f * t + c).ToString(CultureInfo.InvariantCulture);
    }


    private string P6l()
    {
        return L(0.5f + t + b).ToString(CultureInfo.InvariantCulture);
    }


    private string P6w()
    {
        return W(t + c).ToString(CultureInfo.InvariantCulture);
    }


    private string P7l()
    {
        return L(0.5f + b + d).ToString(CultureInfo.InvariantCulture);
    }


    private string P7w()
    {
        return W(-t + c).ToString(CultureInfo.InvariantCulture);
    }


    private string P8l()
    {
        return L(0.8f).ToString(CultureInfo.InvariantCulture);
    }


    private string P8w()
    {
        return W(e).ToString(CultureInfo.InvariantCulture);
    }


    private string P9l()
    {
        return L(1f).ToString(CultureInfo.InvariantCulture);
    }


    private string P9w()
    {
        return W(0f).ToString(CultureInfo.InvariantCulture);
    }
}
