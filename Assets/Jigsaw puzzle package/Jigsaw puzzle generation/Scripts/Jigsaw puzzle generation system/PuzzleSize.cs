using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PuzzleSize
{
    private Vector2Int m_dimensions;


    public PuzzleSize(int x, int y, float nonSquareness)
    {
        m_dimensions = new Vector2Int(x, y);
        NonSquareness = nonSquareness;
    }


    public int X
    {
        get { return m_dimensions.x; }
        set { m_dimensions.y = value; }
    }


    public int Y
    {
        get { return m_dimensions.y; }
        set { m_dimensions.y = value; }
    }


    public float NonSquareness { get; set; }


    public int Size { get { return X * Y; } } 


    public Vector2Int Dimensions { get { return m_dimensions; } }


    public override string ToString()
    {
        return $"{Size} pieces ({X} x {Y})";
    }


    public override bool Equals(object obj)
    {
        if (obj is PuzzleSize)
        {
            var other = (PuzzleSize) obj;

            return (other.Dimensions.Equals(Dimensions));
        }

        return false;
    }


    public override int GetHashCode()
    {
        return Dimensions.GetHashCode();
    }
}
