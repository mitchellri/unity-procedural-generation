using UnityEngine;

abstract class Generator
{
    public int Width;
    public int Length;
    public Generator(int width, int length)
    {
        Width = width;
        Length = length;
    }

    public virtual void Reset()
    {
        
    }
}