﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
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

    public static int Clip(int val, int min, int max)
    {
        if (val < min) return min;
        if (val > max) return max;
        return val;
    }

    public static float Clip(float val, float min, float max)
    {
        if (val < min) return min;
        if (val > max) return max;
        return val;
    }

    public static double Clip(double val, double min, double max)
    {
        if (val < min) return min;
        if (val > max) return max;
        return val;
    }

    public struct MeshInfo
    {
        public Vector3[] vertices;
        public int[] triangles;
    }

    public static MeshInfo FixMesh(Vector3[] verts, int[] tris)
    {
        // Split faces using the same vertices so they are independent.

        var newVerts = new List<Vector3>(verts);
        var newTris = (int[]) tris.Clone();

        var triSet = new HashSet<int>();
        for (var i = 0; i < newTris.Length; i++)
        {
            var tri = newTris[i];
            if (triSet.Contains(tri))
            {
                // Create new vert at same location
                newVerts.Add(verts[tri]);
                // Change triangle to use new vert
                newTris[i] = newVerts.Count - 1;
            }
            else
            {
                triSet.Add(tri);
            }
        }

        return new MeshInfo
        {
            vertices = newVerts.ToArray(),
            triangles = newTris
        };
    }

    public static Material MaterialAverage(Material[] mats)
    {
        // Blends materials together

        var baseMat = new Material(mats[0]);
        for (var i = 1; i < mats.Length; i++)
        {
            baseMat.Lerp(baseMat, mats[i], (float) i / (i + 1));
        }

        return baseMat;
    }

    public static float AnimationInverseLerp(float a, float b, float t, AnimationCurve curve)
    {
        return curve.Evaluate(Mathf.InverseLerp(a, b, t));
    }

    public static float[] SampleAnimationCurve(AnimationCurve ac, int numSamples)
    {
        var sampled = new float[numSamples];
        for (var i = 0; i < numSamples; i++)
        {
            sampled[i] = ac.Evaluate(i / (float) (numSamples - 1));
        }

        return sampled;
    }

    public static float EvalSampledAnimCurve(float[] sampledAC, float val)
    {
        return sampledAC[(int) Clip(val * (sampledAC.Length - 1), 0, (sampledAC.Length - 1))];
    }

    public static Color[] SampleColorBar(ColorBar colorBar, AnimationCurve ac, int numSamples)
    {
        var sampled = new Color[numSamples];
        for (var i = 0; i < numSamples; i++)
        {
            sampled[i] = colorBar.GetColorAt(i / (float) (numSamples - 1), ac);
        }

        return sampled;
    }

    public static Color EvalSampledColorBar(Color[] sampledCB, float val)
    {
        return sampledCB[(int) Clip(val * (sampledCB.Length - 1), 0, (sampledCB.Length - 1))];
    }
}

public class HelperRandom
{
    private System.Random _rand;

    public HelperRandom()
    {
        _rand = new System.Random();
    }

    public HelperRandom(int seed)
    {
        _rand = new System.Random(seed);
    }

    public void SetSeed(int seed)
    {
        _rand = new System.Random(seed);
    }

    public float FloatRange(float min, float max)
    {
        return (float) (_rand.NextDouble() * (max - min) + min);
    }

    public int IntRange(int min, int max)
    {
        return (int) (_rand.NextDouble() * (max - min) + min);
    }

    public double NormalValue(double mean, double std)
    {
        // https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform

        var u1 = _rand.NextDouble();
        var u2 = _rand.NextDouble();

        // Basic form
        var z1 = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
//        var z2 = Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2); // Second value can be generated

        return z1 * std + mean;
    }

    public double NormalValue(NormalRange range)
    {
        return Helpers.Clip(NormalValue(range.mean, range.std), range.min, range.max);
    }

    public double Double()
    {
        return _rand.NextDouble();
    }

    public float Float()
    {
        return (float) _rand.NextDouble();
    }
}

[Serializable]
public struct NormalRange
{
    public double min;
    public double max;
    public double mean;
    [Min(0)] public double std;
}


[Serializable]
public struct ColorPoint
{
    public float point;
    public Color color;
}

[Serializable]
public class ColorBar
{
    // Represents a smooth linear color scale

    public ColorPoint[] colors;

    public ColorBar(ColorPoint[] colorPoints)
    {
        colors = colorPoints.OrderBy(c => c.point).ToArray();
    }

    public Color GetColorAt(float val)
    {
        // Linearly Lerps between 2 nearest colours. todo: check if val is valid.

        var i = 0;
        while ((i < colors.Length) & (val > colors[i].point)) i++;

        if (i == 0) return colors[0].color;

        return Color.Lerp(colors[i - 1].color, colors[i].color,
            Mathf.InverseLerp(colors[i - 1].point, colors[i].point, val)
        );
    }

    public Color GetColorAt(float val, AnimationCurve curve)
    {
        // Lerps between 2 nearest colours with provided curve

        var i = 0;
        while ((i < colors.Length) & (val > colors[i].point)) i++;

        if (i == 0) return colors[0].color;

        return Color.Lerp(colors[i - 1].color, colors[i].color,
            Helpers.AnimationInverseLerp(colors[i - 1].point, colors[i].point, val, curve)
        );
    }
}