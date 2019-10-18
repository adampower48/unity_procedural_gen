using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Helpers
{
    public static float Wrap(float val, float min, float max)
    {
        // min inclusive, max exclusive
        return (val - min) % (max - min) + min;
    }

    public static int Wrap(int val, int min, int max)
    {
        // min inclusive, max exclusive
        return (val - min) % (max - min) + min;
    }

    public static double Wrap(double val, double min, double max)
    {
        // min inclusive, max exclusive
        return (val - min) % (max - min) + min;
    }
}