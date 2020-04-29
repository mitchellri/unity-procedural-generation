using UnityEngine;

abstract class Generator
{
    public int Width;
    public int Length;
    public int Height;
    public Generator(int width, int length, int height)
    {
        Width = width;
        Length = length;
        Height = height;
    }

    public virtual void Reset()
    {
        
    }
}